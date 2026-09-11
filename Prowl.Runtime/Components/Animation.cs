// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Prowl.Vector;
using Prowl.Runtime.Animation;

namespace Prowl.Runtime;

/// <summary>
/// Plays AnimationClips by driving bone Transforms in the hierarchy.
/// Supports direct playback (index-based or legacy string) and real-time humanoid retargeting.
///
/// When retargeting is configured (<see cref="SourceSkeleton"/>, <see cref="SourceHumanoidMapping"/>,
/// <see cref="Skeleton"/> and <see cref="HumanoidMapping"/>), poses are retargeted from the source
/// rig to the target rig in real-time with zero runtime GC allocations.
///
/// When a <see cref="Skeleton"/> is assigned without retargeting, bone resolution is index-based:
/// a one-time binding maps clip bones → skeleton bone indices → cached Transforms.
///
/// When no skeleton is assigned, the legacy string-path Dictionary resolution is used
/// for full backward compatibility.
/// </summary>
[AddComponentMenu("Animation/Animation")]
[ComponentIcon("\uf008")] // Film
public class AnimationComponent : MonoBehaviour
{
    /// <summary>Default animation clip to play on start.</summary>
    public AssetRef<AnimationClip> DefaultClip;

    /// <summary>All available animation clips for this model.</summary>
    public List<AssetRef<AnimationClip>> Clips = new();

    /// <summary>Target skeleton asset for index-based bone resolution and retargeting.</summary>
    public AssetRef<SkeletonAsset> Skeleton;

    /// <summary>Optional target humanoid mapping for humanoid retargeting.</summary>
    public AssetRef<HumanoidMapping> HumanoidMapping;

    /// <summary>Optional source skeleton asset (authoring rig of the clips) for retargeting.</summary>
    public AssetRef<SkeletonAsset> SourceSkeleton;

    /// <summary>Optional source humanoid mapping (authoring rig humanoid mapping) for retargeting.</summary>
    public AssetRef<HumanoidMapping> SourceHumanoidMapping;

    /// <summary>Auto-play the default clip on enable.</summary>
    public bool PlayAutomatically = true;

    /// <summary>Playback speed multiplier.</summary>
    public float Speed = 1f;

    /// <summary>Whether the current animation is playing.</summary>
    [NonSerialized] public bool IsPlaying;

    /// <summary>Current playback time in seconds.</summary>
    [NonSerialized] public float Time;

    /// <summary>The currently playing clip.</summary>
    [NonSerialized] public AnimationClip? CurrentClip;

    // ── Common sampler ──────────────────────────────────────────────────────
    private readonly AnimationSampler _sampler = new();
    private BonePose[] _cachedPoses = Array.Empty<BonePose>();

    // ── Retargeting state ────────────────────────────────────────────────────
    [NonSerialized] private AnimationRetargeter? _retargeter;
    [NonSerialized] private SkeletonAsset? _boundRetargetSourceSkeleton;
    [NonSerialized] private HumanoidMapping? _boundRetargetSourceMapping;
    [NonSerialized] private SkeletonAsset? _boundRetargetTargetSkeleton;
    [NonSerialized] private HumanoidMapping? _boundRetargetTargetMapping;
    [NonSerialized] private AnimationClip? _boundRetargetClip;
    [NonSerialized] private Transform?[] _retargetTargetTransforms = Array.Empty<Transform?>();
    private ClipBoneBinding[] _sourceClipBindings = Array.Empty<ClipBoneBinding>();
    private int _sourceClipBindingCount;
    private BonePose[] _sourceSkeletonPoses = Array.Empty<BonePose>();

    // ── Skeleton-based direct index path (used when Skeleton is assigned) ───
    private ClipBoneBinding[] _bindings = Array.Empty<ClipBoneBinding>();
    private int _bindingCount;
    [NonSerialized] private AnimationClip? _boundClip;
    [NonSerialized] private SkeletonAsset? _boundSkeleton;

    // ── Legacy string-path fallback (used when no Skeleton is assigned) ─────
    [NonSerialized] private Dictionary<string, Transform>? _boneCache;
    [NonSerialized] private Dictionary<string, SkinnedMeshRenderer?>? _blendShapeTargets;
    [NonSerialized] private Transform? _boneCacheRoot;

    // Deferred auto-play: clip was still streaming in at OnEnable.
    [NonSerialized] private bool _pendingAutoPlay;

    // Reusable buffer for path segment splitting (avoids allocation in BuildBindings).
    private static readonly char[] s_pathSeparator = { '/' };

    public override void OnEnable()
    {
        // Invalidate all caches on enable
        _retargeter = null;
        _boundRetargetSourceSkeleton = null;
        _boundRetargetSourceMapping = null;
        _boundRetargetTargetSkeleton = null;
        _boundRetargetTargetMapping = null;
        _boundRetargetClip = null;
        _retargetTargetTransforms = Array.Empty<Transform?>();
        _sourceClipBindingCount = 0;

        _boneCache = null;
        _blendShapeTargets = null;
        _boundClip = null;
        _boundSkeleton = null;
        _bindingCount = 0;

        if (PlayAutomatically)
        {
            var clip = ResolveAutoPlayClip();
            if (clip != null)
                Play(clip);
            else
                _pendingAutoPlay = true;
        }
    }

    /// <summary>The clip PlayAutomatically should start: the default clip, else the first clip.</summary>
    private AnimationClip? ResolveAutoPlayClip()
    {
        var clip = DefaultClip.Res;
        // CollectionsMarshal.AsSpan: List<T>'s indexer would copy the value-type element, so
        // AssetRef.Res's internal caching would mutate a throwaway copy and never actually stick.
        if (clip == null && Clips.Count > 0)
            clip = CollectionsMarshal.AsSpan(Clips)[0].Res;
        return clip;
    }

    public override void Update()
    {
        // Deferred auto-play: the clip was still streaming in at OnEnable; start it now.
        if (_pendingAutoPlay)
        {
            var clip = ResolveAutoPlayClip();
            if (clip != null)
            {
                _pendingAutoPlay = false;
                Play(clip);
            }
        }

        if (!IsPlaying || CurrentClip == null) return;

        Time += Prowl.Runtime.Time.DeltaTime * Speed;
        float duration = CurrentClip.Duration;

        if (duration <= 0f) return;

        // Handle wrap mode
        switch (CurrentClip.Wrap)
        {
            case AnimationWrapMode.Once:
                if (Time >= duration)
                {
                    Time = duration;
                    IsPlaying = false;
                }
                break;
            case AnimationWrapMode.Loop:
                Time %= duration;
                break;
            case AnimationWrapMode.PingPong:
                float cycle = Time / duration;
                int wholeCycles = (int)cycle;
                float frac = cycle - wholeCycles;
                Time = (wholeCycles % 2 == 0) ? frac * duration : (1f - frac) * duration;
                break;
            case AnimationWrapMode.ClampForever:
                if (Time >= duration)
                    Time = duration;
                break;
        }

        // Time is a playhead from zero, but a clip's keys sit at whatever times the source authored,
        // which is not necessarily zero. Offsetting here is what lets a clip cut from a shared
        // timeline play from its first key instead of holding that pose until the playhead catches up.
        ApplyPose(CurrentClip, CurrentClip.StartTime + Time);
    }

    /// <summary>Play a specific animation clip from the beginning.</summary>
    public void Play(AnimationClip clip)
    {
        CurrentClip = clip;
        Time = 0f;
        IsPlaying = true;
    }

    /// <summary>Play a clip by name (searches the Clips list).</summary>
    public void Play(string clipName)
    {
        var clips = CollectionsMarshal.AsSpan(Clips);
        for (int i = 0; i < clips.Length; i++)
        {
            var clip = clips[i].Res;
            if (clip != null && clip.Name == clipName)
            {
                Play(clip);
                return;
            }
        }
        Debug.LogWarning($"[Animation] Clip '{clipName}' not found.");
    }

    /// <summary>Stop playback and reset to the beginning.</summary>
    public void Stop()
    {
        IsPlaying = false;
        Time = 0f;
    }

    /// <summary>Pause playback at the current time.</summary>
    public void Pause() => IsPlaying = false;

    /// <summary>Resume playback from the current time.</summary>
    public void Resume() => IsPlaying = true;

    // ════════════════════════════════════════════════════════════════════════
    //  POSE APPLICATION
    // ════════════════════════════════════════════════════════════════════════

    private void ApplyPose(AnimationClip clip, float time)
    {
        // Evaluate all bone curves into the clip-indexed pose buffer
        _sampler.Evaluate(clip, time, out _cachedPoses);

        if (TryEnsureRetargeting(clip))
        {
            // ── RETARGETED PATH: Retarget from Source Rig to Target Rig ──
            ApplyRetargetedPose();
        }
        else if (TryEnsureSkeletonBindings(clip))
        {
            // ── DIRECT INDEX PATH: arrays only, zero string lookups ──
            for (int i = 0; i < _bindingCount; i++)
            {
                ref var b = ref _bindings[i];
                if (b.Target == null) continue;

                var pose = _cachedPoses[b.ClipBoneIndex];
                b.Target.LocalPosition = pose.Position;
                b.Target.LocalRotation = pose.Rotation;
                b.Target.LocalScale = pose.Scale;
            }
        }
        else
        {
            // ── LEGACY PATH: string Dictionary fallback ──
            EnsureBoneCache();
            if (_boneCache == null || _boneCache.Count == 0) return;

            for (int i = 0; i < clip.Bones.Count; i++)
            {
                var animBone = clip.Bones[i];
                if (!_boneCache.TryGetValue(animBone.BoneName, out Transform? bone)) continue;
                if (bone == null) continue;

                var pose = _cachedPoses[i];
                bone.LocalPosition = pose.Position;
                bone.LocalRotation = pose.Rotation;
                bone.LocalScale = pose.Scale;
            }
        }

        ApplyBlendShapes(clip, time);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  RETARGETING PIPELINE
    // ════════════════════════════════════════════════════════════════════════

    private bool TryEnsureRetargeting(AnimationClip clip)
    {
        var srcSkel = SourceSkeleton.Res;
        var srcMap = SourceHumanoidMapping.Res;
        var tgtSkel = Skeleton.Res;
        var tgtMap = HumanoidMapping.Res;

        if (srcSkel == null || srcMap == null || tgtSkel == null || tgtMap == null)
            return false;

        // Rebuild retargeter if any rig configuration changed
        if (_retargeter == null ||
            _boundRetargetSourceSkeleton != srcSkel ||
            _boundRetargetSourceMapping != srcMap ||
            _boundRetargetTargetSkeleton != tgtSkel ||
            _boundRetargetTargetMapping != tgtMap)
        {
            if (!HumanoidCalibration.Build(srcSkel, srcMap, out var srcCal, out _) ||
                !HumanoidCalibration.Build(tgtSkel, tgtMap, out var tgtCal, out _) ||
                !AnimationRetargeter.Build(srcSkel, srcMap, srcCal, tgtSkel, tgtMap, tgtCal, out _retargeter, out _))
            {
                _retargeter = null;
                return false;
            }

            _boundRetargetSourceSkeleton = srcSkel;
            _boundRetargetSourceMapping = srcMap;
            _boundRetargetTargetSkeleton = tgtSkel;
            _boundRetargetTargetMapping = tgtMap;

            // Resolve target skeleton scene transforms
            if (_retargetTargetTransforms.Length != tgtSkel.BoneCount)
                _retargetTargetTransforms = new Transform?[tgtSkel.BoneCount];
            ResolveSkeletonTransforms(tgtSkel, _retargetTargetTransforms);

            // Preallocate source skeleton pose buffer
            if (_sourceSkeletonPoses.Length != srcSkel.BoneCount)
                _sourceSkeletonPoses = new BonePose[srcSkel.BoneCount];
        }

        // Rebuild clip bindings to source skeleton if clip changed
        if (_boundRetargetClip != clip)
        {
            _boundRetargetClip = clip;
            BuildSourceClipBindings(clip, srcSkel);
        }

        return _retargeter != null;
    }

    private void BuildSourceClipBindings(AnimationClip clip, SkeletonAsset srcSkel)
    {
        int clipBoneCount = clip.Bones.Count;
        if (_sourceClipBindings.Length < clipBoneCount)
            _sourceClipBindings = new ClipBoneBinding[clipBoneCount];

        _sourceClipBindingCount = 0;
        for (int i = 0; i < clipBoneCount; i++)
        {
            string bonePath = clip.Bones[i].BoneName;
            int skelIdx = ResolveClipBoneToSkeleton(bonePath, srcSkel);
            if (skelIdx >= 0)
            {
                _sourceClipBindings[_sourceClipBindingCount++] = new ClipBoneBinding
                {
                    ClipBoneIndex = i,
                    SkeletonBoneIndex = skelIdx,
                    Target = null
                };
            }
        }
    }

    private void ApplyRetargetedPose()
    {
        if (_retargeter == null || _boundRetargetSourceSkeleton == null) return;

        // 1. Initialize source skeleton pose buffer with rest poses
        for (int i = 0; i < _sourceSkeletonPoses.Length; i++)
        {
            var bone = _boundRetargetSourceSkeleton[i];
            _sourceSkeletonPoses[i] = new BonePose
            {
                Position = bone.LocalPosition,
                Rotation = bone.LocalRotation,
                Scale = bone.LocalScale
            };
        }

        // 2. Overwrite with evaluated clip curve poses
        for (int i = 0; i < _sourceClipBindingCount; i++)
        {
            ref var b = ref _sourceClipBindings[i];
            _sourceSkeletonPoses[b.SkeletonBoneIndex] = _cachedPoses[b.ClipBoneIndex];
        }

        // 3. Retarget from source skeleton to target skeleton poses
        _retargeter.TryRetargetPose(_sourceSkeletonPoses, out var targetPoses);

        // 4. Apply target poses to cached scene transforms
        for (int i = 0; i < targetPoses.Length; i++)
        {
            var targetTransform = _retargetTargetTransforms[i];
            if (targetTransform == null) continue;

            var pose = targetPoses[i];
            targetTransform.LocalPosition = pose.Position;
            targetTransform.LocalRotation = pose.Rotation;
            targetTransform.LocalScale = pose.Scale;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  SKELETON BINDING (one-time, on clip/skeleton change)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if skeleton bindings are available and current.
    /// Rebuilds them if the clip or skeleton changed since last build.
    /// </summary>
    private bool TryEnsureSkeletonBindings(AnimationClip clip)
    {
        var skel = Skeleton.Res;
        if (skel == null) return false;

        if (_boundClip != clip || _boundSkeleton != skel)
            BuildBindings(clip, skel);

        return _bindingCount > 0;
    }

    /// <summary>
    /// Builds the one-time binding from clip bones → skeleton bone indices → Transforms.
    /// After this, ApplyPose uses only arrays and indices.
    /// </summary>
    private void BuildBindings(AnimationClip clip, SkeletonAsset skeleton)
    {
        _boundClip = clip;
        _boundSkeleton = skeleton;

        int clipBoneCount = clip.Bones.Count;
        int skelBoneCount = skeleton.BoneCount;

        // 1. Resolve skeleton bone index → Transform (hierarchy-aware)
        var skelTransforms = new Transform?[skelBoneCount];
        ResolveSkeletonTransforms(skeleton, skelTransforms);

        // 2. Build clip bone → skeleton bone bindings
        if (_bindings.Length < clipBoneCount)
            _bindings = new ClipBoneBinding[clipBoneCount];
        _bindingCount = 0;

        for (int i = 0; i < clipBoneCount; i++)
        {
            string bonePath = clip.Bones[i].BoneName;
            int skelIdx = ResolveClipBoneToSkeleton(bonePath, skeleton);

            _bindings[_bindingCount] = new ClipBoneBinding
            {
                ClipBoneIndex = i,
                SkeletonBoneIndex = skelIdx,
                Target = skelIdx >= 0 ? skelTransforms[skelIdx] : null
            };
            _bindingCount++;
        }
    }

    /// <summary>
    /// Resolves each skeleton bone to its corresponding Transform in the GO hierarchy.
    /// Processes top-down (parents before children) so children can verify their parent mapping.
    /// </summary>
    private void ResolveSkeletonTransforms(SkeletonAsset skeleton, Transform?[] output)
    {
        Transform root = FindBoneCacheRoot();

        for (int i = 0; i < skeleton.BoneCount; i++)
        {
            var skelBone = skeleton[i];
            Transform? searchRoot;

            if (skelBone.ParentIndex >= 0 && skelBone.ParentIndex < i)
            {
                searchRoot = output[skelBone.ParentIndex];
                if (searchRoot == null)
                {
                    output[i] = null;
                    continue;
                }
            }
            else
            {
                searchRoot = root;
            }

            Transform? match = null;
            int matchCount = 0;
            FindNamedTransform(searchRoot, skelBone.Name, skelBone.ParentIndex < 0, ref match, ref matchCount);

            if (matchCount == 1)
            {
                output[i] = match;
            }
            else if (matchCount > 1)
            {
                Debug.LogWarning($"[Animation] Ambiguous skeleton Transform for bone '{skelBone.Name}': {matchCount} candidates found. Leaving unresolved.");
                output[i] = null;
            }
            else
            {
                output[i] = null;
            }
        }
    }

    private static void FindNamedTransform(Transform searchRoot, string boneName, bool isRootBone, ref Transform? match, ref int matchCount)
    {
        if (isRootBone)
        {
            FindNamedTransformRecursive(searchRoot, boneName, ref match, ref matchCount);
        }
        else
        {
            foreach (var child in searchRoot.GameObject.Children)
            {
                if (child.Name == boneName)
                {
                    match = child.Transform;
                    matchCount++;
                }
            }
        }
    }

    private static void FindNamedTransformRecursive(Transform t, string name, ref Transform? match, ref int matchCount)
    {
        foreach (var child in t.GameObject.Children)
        {
            if (child.Name == name)
            {
                match = child.Transform;
                matchCount++;
            }
            FindNamedTransformRecursive(child.Transform, name, ref match, ref matchCount);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PATH RESOLUTION: AnimBone.BoneName → SkeletonAsset bone index
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves a clip bone path (e.g. "Armature/Hips/Spine") to a skeleton bone index.
    /// </summary>
    internal static int ResolveClipBoneToSkeleton(string bonePath, SkeletonAsset skeleton)
    {
        if (string.IsNullOrEmpty(bonePath)) return -1;

        string[] segments = bonePath.Split(s_pathSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return -1;

        string leafName = segments[segments.Length - 1];

        int candidateCount = 0;
        int lastCandidate = -1;
        int exactMatchCount = 0;
        int exactMatch = -1;

        for (int i = 0; i < skeleton.BoneCount; i++)
        {
            if (skeleton[i].Name != leafName) continue;
            candidateCount++;
            lastCandidate = i;

            if (VerifyPathMatch(i, segments, skeleton))
            {
                exactMatchCount++;
                exactMatch = i;
            }
        }

        if (exactMatchCount == 1) return exactMatch;
        if (exactMatchCount > 1)
        {
            Debug.LogWarning($"[Animation] Multiple exact path matches for '{bonePath}' in skeleton. Leaving unbound.");
            return -1;
        }

        if (candidateCount == 1) return lastCandidate;
        if (candidateCount > 1)
        {
            Debug.LogWarning($"[Animation] Ambiguous leaf name '{leafName}' for path '{bonePath}' in skeleton ({candidateCount} candidates). Leaving unbound.");
            return -1;
        }

        return -1;
    }

    private static bool VerifyPathMatch(int boneIndex, string[] segments, SkeletonAsset skeleton)
    {
        int segIdx = segments.Length - 1;
        int current = boneIndex;

        while (current >= 0 && segIdx >= 0)
        {
            if (skeleton[current].Name != segments[segIdx])
                return false;

            segIdx--;
            current = skeleton.GetParentIndex(current);
        }

        return true;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  BLEND SHAPES
    // ════════════════════════════════════════════════════════════════════════

    private void ApplyBlendShapes(AnimationClip clip, float time)
    {
        if (clip.BlendShapes.Count == 0) return;
        _blendShapeTargets ??= new Dictionary<string, SkinnedMeshRenderer?>();

        EnsureBoneCache();

        foreach (var track in clip.BlendShapes)
        {
            if (!_blendShapeTargets.TryGetValue(track.Path, out SkinnedMeshRenderer? smr))
            {
                Transform? target = string.IsNullOrEmpty(track.Path)
                    ? _boneCacheRoot
                    : (_boneCache != null && _boneCache.TryGetValue(track.Path, out Transform? t) ? t : null);
                smr = target?.GameObject.GetComponent<SkinnedMeshRenderer>();
                _blendShapeTargets[track.Path] = smr;
            }
            if (smr.IsValid()) smr.SetBlendShapeWeight(track.ShapeName, track.EvaluateAt(time));
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  LEGACY BONE CACHE
    // ════════════════════════════════════════════════════════════════════════

    private void EnsureBoneCache()
    {
        if (_boneCache != null) return;
        _boneCache = new Dictionary<string, Transform>();

        Transform root = FindBoneCacheRoot();
        _boneCacheRoot = root;

        foreach (var child in root.GameObject.Children)
            CacheBonesRecursive(child.Transform, "");
    }

    private Transform FindBoneCacheRoot()
    {
        Transform root = Transform;
        while (root.Parent != null) root = root.Parent;

        var clip = CurrentClip.IsValid() ? CurrentClip : DefaultClip.Res;
        if (clip != null && clip.Bones.Count > 0)
        {
            string testPath = clip.Bones[0].BoneName;
            var ancestors = new List<Transform>();
            Transform current = Transform;
            while (current != null) { ancestors.Add(current); current = current.Parent; }

            for (int i = ancestors.Count - 1; i >= 0; i--)
            {
                foreach (var child in ancestors[i].GameObject.Children)
                {
                    if (child.Name == testPath.Split('/')[0])
                        return ancestors[i];
                }
            }
        }

        return root;
    }

    private void CacheBonesRecursive(Transform t, string parentPath)
    {
        string path = string.IsNullOrEmpty(parentPath)
            ? t.GameObject.Name
            : parentPath + "/" + t.GameObject.Name;

        _boneCache.TryAdd(path, t);

        foreach (var child in t.GameObject.Children)
            CacheBonesRecursive(child.Transform, path);
    }
}
