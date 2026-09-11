// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Prowl.Vector;
using Prowl.Runtime.Animation;

namespace Prowl.Runtime;

/// <summary>
/// Humanoid limbs that can be controlled via Inverse Kinematics.
/// </summary>
public enum AvatarIKGoal
{
    LeftHand = 0,
    RightHand = 1,
    LeftFoot = 2,
    RightFoot = 3
}

/// <summary>
/// Configuration and target parameters for an IK goal (hand or foot).
/// </summary>
public sealed class AvatarIKTarget
{
    public Transform? Target;
    public Transform? Hint;
    public Float3? Position;
    public Quaternion? Rotation;
    public Float3? HintPosition;
    public Float3 PositionOffset = Float3.Zero;
    public Quaternion RotationOffset = Quaternion.Identity;
    public float PositionWeight = 0f;
    public float RotationWeight = 0f;
    public float HintWeight = 0f;

    /// <summary>Convenience accessor for <see cref="PositionWeight"/>.</summary>
    public float Weight
    {
        get => PositionWeight;
        set => PositionWeight = value;
    }
}

/// <summary>
/// Multi-layer state-based animation component supporting layers, blend trees (1D/2D), avatar masks,
/// parameters, condition-driven transitions, cross-fade blending, direct indexed skeleton playback,
/// and real-time humanoid retargeting.
/// </summary>
[AddComponentMenu("Animation/Animator")]
[ComponentIcon("\uf008")]
public class Animator : MonoBehaviour
{
    /// <summary>All animation layers configured on this animator. Layer 0 is the Base Layer.</summary>
    public List<AnimatorLayer> Layers = new();

    /// <summary>All parameter declarations defined on this animator.</summary>
    public List<AnimatorParameter> Parameters = new();

    /// <summary>Global playback speed multiplier.</summary>
    public float Speed = 1f;

    /// <summary>Auto-play the default state on enable.</summary>
    public bool PlayAutomatically = true;

    /// <summary>Target skeleton asset for index-based bone resolution and retargeting.</summary>
    public AssetRef<SkeletonAsset> Skeleton;

    /// <summary>Optional target humanoid mapping for humanoid retargeting.</summary>
    public AssetRef<HumanoidMapping> HumanoidMapping;

    /// <summary>Optional source skeleton asset (authoring rig of the clips) for retargeting.</summary>
    public AssetRef<SkeletonAsset> SourceSkeleton;

    /// <summary>Optional source humanoid mapping for retargeting.</summary>
    public AssetRef<HumanoidMapping> SourceHumanoidMapping;

    // ── Convenience Base-Layer Redirections (Backwards Compatibility) ──────
    public List<AnimatorState> States => EnsureBaseLayer().States;
    public List<AnimatorTransition> Transitions => EnsureBaseLayer().Transitions;

    public string DefaultState
    {
        get => EnsureBaseLayer().DefaultState;
        set => EnsureBaseLayer().DefaultState = value;
    }

    public AnimatorState? CurrentState => EnsureBaseLayer().CurrentState;
    public AnimatorState? TargetState => EnsureBaseLayer().TargetState;
    public bool IsInTransition => EnsureBaseLayer().IsInTransition;
    public bool IsPlaying => EnsureBaseLayer().IsPlaying;
    public float CurrentTime => EnsureBaseLayer().CurrentTime;
    public float TargetTime => EnsureBaseLayer().TargetTime;
    public float TransitionTime => EnsureBaseLayer().TransitionTime;
    public float TransitionDuration => EnsureBaseLayer().TransitionDuration;

    // ── Root Motion ────────────────────────────────────────────────────────
    public bool ApplyRootMotion { get; set; } = false;
    public Float3 RootMotionPositionDelta { get; private set; } = Float3.Zero;
    public Quaternion RootMotionRotationDelta { get; private set; } = Quaternion.Identity;
    public Float3 RootMotionPosition => RootMotionPositionDelta;
    public Quaternion RootMotionRotation => RootMotionRotationDelta;

    private Float3 _prevRootPosition = Float3.Zero;
    private Quaternion _prevRootRotation = Quaternion.Identity;
    private Float3 _rootRestPosition = Float3.Zero;
    private Quaternion _rootRestRotation = Quaternion.Identity;
    private bool _hasPrevRootPose = false;
    private float _prevBaseLayerTime = 0f;

    // ── Inverse Kinematics (IK) ────────────────────────────────────────────
    public bool EnableIK { get; set; } = true;
    public AvatarIKTarget LeftHandIK { get; } = new();
    public AvatarIKTarget RightHandIK { get; } = new();
    public AvatarIKTarget LeftFootIK { get; } = new();
    public AvatarIKTarget RightFootIK { get; } = new();

    public AvatarIKTarget GetIKTarget(AvatarIKGoal goal) => goal switch
    {
        AvatarIKGoal.LeftHand => LeftHandIK,
        AvatarIKGoal.RightHand => RightHandIK,
        AvatarIKGoal.LeftFoot => LeftFootIK,
        AvatarIKGoal.RightFoot => RightFootIK,
        _ => LeftHandIK
    };

    public void SetIKPosition(AvatarIKGoal goal, Float3 position) => GetIKTarget(goal).Position = position;
    public void SetIKRotation(AvatarIKGoal goal, Quaternion rotation) => GetIKTarget(goal).Rotation = rotation;
    public void SetIKPositionWeight(AvatarIKGoal goal, float weight) => GetIKTarget(goal).PositionWeight = weight;
    public void SetIKRotationWeight(AvatarIKGoal goal, float weight) => GetIKTarget(goal).RotationWeight = weight;
    public void SetIKHintPosition(AvatarIKGoal goal, Float3 hintPosition) => GetIKTarget(goal).HintPosition = hintPosition;
    public void SetIKHintWeight(AvatarIKGoal goal, float weight) => GetIKTarget(goal).HintWeight = weight;

    public float NormalizedTime
    {
        get
        {
            float duration = CurrentState?.GetDuration() ?? 0f;
            if (duration <= 0f) return 0f;
            return CurrentTime / duration;
        }
    }

    public float TransitionProgress
    {
        get
        {
            var baseLayer = EnsureBaseLayer();
            if (!baseLayer.IsInTransition || baseLayer.TransitionDuration <= 0f) return 1f;
            return Math.Clamp(baseLayer.TransitionTime / baseLayer.TransitionDuration, 0f, 1f);
        }
    }

    // ── Parameter Storage (Zero GC in Hot Path) ────────────────────────────
    private struct AnimatorParamValue
    {
        public AnimatorParameterType Type;
        public float FloatVal;
        public int IntVal;
        public bool BoolVal;
        public bool TriggerVal;
    }

    private readonly Dictionary<string, AnimatorParamValue> _paramValues = new(StringComparer.Ordinal);

    // ── Multi-Layer Samplers & Pre-allocated Pose Buffers ──────────────────
    private const int MaxLayers = 8;
    private const int MaxBlendTreeSamplersPerLayer = 16;

    private readonly AnimationSampler _samplerCurrent = new();
    private readonly AnimationSampler _samplerTarget = new();

    private AnimationSampler[]? _blendTreeSamplers;
    private readonly float[] _blendWeights = new float[MaxBlendTreeSamplersPerLayer];
    private BonePose[] _stateBlendAccumulator = Array.Empty<BonePose>();

    private BonePose[][] _layerPoses = new BonePose[MaxLayers][];
    private BonePose[] _finalCombinedPoses = Array.Empty<BonePose>();

    private AnimationSampler[] GetBlendTreeSamplers()
    {
        if (_blendTreeSamplers == null)
        {
            _blendTreeSamplers = new AnimationSampler[MaxBlendTreeSamplersPerLayer * MaxLayers];
            for (int i = 0; i < _blendTreeSamplers.Length; i++)
                _blendTreeSamplers[i] = new AnimationSampler();
        }
        return _blendTreeSamplers;
    }

    private BonePose[] _currentClipPoses = Array.Empty<BonePose>();
    private BonePose[] _targetClipPoses = Array.Empty<BonePose>();

    // ── Retargeting state ──────────────────────────────────────────────────
    [NonSerialized] private AnimationRetargeter? _retargeter;
    [NonSerialized] private SkeletonAsset? _boundRetargetSourceSkeleton;
    [NonSerialized] private HumanoidMapping? _boundRetargetSourceMapping;
    [NonSerialized] private SkeletonAsset? _boundRetargetTargetSkeleton;
    [NonSerialized] private HumanoidMapping? _boundRetargetTargetMapping;
    [NonSerialized] private Transform?[] _retargetTargetTransforms = Array.Empty<Transform?>();

    private ClipBoneBinding[] _sourceClipBindings = Array.Empty<ClipBoneBinding>();
    private int _sourceClipBindingCount;
    [NonSerialized] private AnimationClip? _boundRetargetClip;

    private BonePose[] _sourceSkeletonPoses = Array.Empty<BonePose>();

    // ── Skeleton-based direct index path ───────────────────────────────────
    private ClipBoneBinding[] _bindings = Array.Empty<ClipBoneBinding>();
    private int _bindingCount;
    [NonSerialized] private AnimationClip? _boundClip;
    [NonSerialized] private SkeletonAsset? _boundSkeleton;

    private BonePose[] _skeletonPoses = Array.Empty<BonePose>();
    private Transform?[] _skeletonTransforms = Array.Empty<Transform?>();

    // ── Legacy string-path fallback ────────────────────────────────────────
    [NonSerialized] private Dictionary<string, Transform>? _boneCache;
    [NonSerialized] private Transform? _boneCacheRoot;

    // ── Humanoid Bone Lookup Cache ─────────────────────────────────────────
    private int[] _skeletonBoneToHumanoidBone = Array.Empty<int>();

    // ── Inverse Kinematics (IK) State ──────────────────────────────────────
    private struct IKChain
    {
        public bool IsValid;
        public int RootBoneIndex;
        public int MidBoneIndex;
        public int EndBoneIndex;
        public int ParentBoneIndex;
    }

    private readonly IKChain[] _ikChains = new IKChain[4];
    [NonSerialized] private SkeletonAsset? _boundIKSkeleton;
    [NonSerialized] private HumanoidMapping? _boundIKMapping;

    private Float3[] _fkWorldPositions = Array.Empty<Float3>();
    private Quaternion[] _fkWorldRotations = Array.Empty<Quaternion>();
    private Float3[] _fkWorldScales = Array.Empty<Float3>();

    private static readonly char[] s_pathSeparator = { '/' };

    public AnimatorLayer EnsureBaseLayer()
    {
        if (Layers.Count == 0)
            Layers.Add(new AnimatorLayer("Base Layer", 1f));
        return Layers[0];
    }

    public override void OnEnable()
    {
        EnsureBaseLayer();
        InvalidateCaches();
        InitializeParameters();

        for (int i = 0; i < Layers.Count; i++)
        {
            var layer = Layers[i];
            if (PlayAutomatically)
            {
                var defaultSt = layer.ResolveDefaultState();
                if (defaultSt != null)
                    layer.PlayStateImmediate(defaultSt);
            }
        }
    }

    private void InvalidateCaches()
    {
        _retargeter = null;
        _boundRetargetSourceSkeleton = null;
        _boundRetargetSourceMapping = null;
        _boundRetargetTargetSkeleton = null;
        _boundRetargetTargetMapping = null;
        _boundRetargetClip = null;
        _retargetTargetTransforms = Array.Empty<Transform?>();

        _boundClip = null;
        _boundSkeleton = null;
        _skeletonTransforms = Array.Empty<Transform?>();

        _boneCache = null;
        _boneCacheRoot = null;
        _skeletonBoneToHumanoidBone = Array.Empty<int>();

        _hasPrevRootPose = false;
        RootMotionPositionDelta = Float3.Zero;
        RootMotionRotationDelta = Quaternion.Identity;
    }

    private void InitializeParameters()
    {
        var parameters = CollectionsMarshal.AsSpan(Parameters);
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (string.IsNullOrEmpty(p.Name)) continue;

            if (!_paramValues.ContainsKey(p.Name))
            {
                _paramValues[p.Name] = new AnimatorParamValue
                {
                    Type = p.Type,
                    FloatVal = p.DefaultFloat,
                    IntVal = p.DefaultInt,
                    BoolVal = p.DefaultBool,
                    TriggerVal = false
                };
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PARAMETER API
    // ════════════════════════════════════════════════════════════════════════

    public void SetBool(string name, bool value)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (_paramValues.TryGetValue(name, out var val))
        {
            val.BoolVal = value;
            val.Type = AnimatorParameterType.Bool;
            _paramValues[name] = val;
        }
        else
        {
            _paramValues[name] = new AnimatorParamValue
            {
                Type = AnimatorParameterType.Bool,
                BoolVal = value
            };
        }
    }

    public bool GetBool(string name)
    {
        if (!string.IsNullOrEmpty(name) && _paramValues.TryGetValue(name, out var val))
            return val.BoolVal;
        return false;
    }

    public void SetInt(string name, int value)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (_paramValues.TryGetValue(name, out var val))
        {
            val.IntVal = value;
            val.Type = AnimatorParameterType.Int;
            _paramValues[name] = val;
        }
        else
        {
            _paramValues[name] = new AnimatorParamValue
            {
                Type = AnimatorParameterType.Int,
                IntVal = value
            };
        }
    }

    public int GetInt(string name)
    {
        if (!string.IsNullOrEmpty(name) && _paramValues.TryGetValue(name, out var val))
            return val.IntVal;
        return 0;
    }

    public void SetFloat(string name, float value)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (_paramValues.TryGetValue(name, out var val))
        {
            val.FloatVal = value;
            val.Type = AnimatorParameterType.Float;
            _paramValues[name] = val;
        }
        else
        {
            _paramValues[name] = new AnimatorParamValue
            {
                Type = AnimatorParameterType.Float,
                FloatVal = value
            };
        }
    }

    public float GetFloat(string name)
    {
        if (!string.IsNullOrEmpty(name) && _paramValues.TryGetValue(name, out var val))
            return val.FloatVal;
        return 0f;
    }

    public void SetTrigger(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (_paramValues.TryGetValue(name, out var val))
        {
            val.TriggerVal = true;
            val.Type = AnimatorParameterType.Trigger;
            _paramValues[name] = val;
        }
        else
        {
            _paramValues[name] = new AnimatorParamValue
            {
                Type = AnimatorParameterType.Trigger,
                TriggerVal = true
            };
        }
    }

    public void ResetTrigger(string name)
    {
        if (!string.IsNullOrEmpty(name) && _paramValues.TryGetValue(name, out var val))
        {
            val.TriggerVal = false;
            _paramValues[name] = val;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  LAYER & STATE MACHINE CONTROL
    // ════════════════════════════════════════════════════════════════════════

    public void AddLayer(AnimatorLayer layer) => Layers.Add(layer);

    public AnimatorLayer? GetLayer(string name)
    {
        var span = CollectionsMarshal.AsSpan(Layers);
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Name == name)
                return span[i];
        }
        return null;
    }

    public AnimatorLayer? GetLayer(int index)
    {
        if (index >= 0 && index < Layers.Count)
            return Layers[index];
        return null;
    }

    public void SetLayerWeight(int layerIndex, float weight)
    {
        if (layerIndex >= 0 && layerIndex < Layers.Count)
            Layers[layerIndex].Weight = Math.Clamp(weight, 0f, 1f);
    }

    public float GetLayerWeight(int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < Layers.Count)
            return Layers[layerIndex].Weight;
        return 0f;
    }

    public AnimatorState? ResolveDefaultState() => EnsureBaseLayer().ResolveDefaultState();
    public AnimatorState? GetState(string name) => EnsureBaseLayer().GetState(name);
    public void AddState(AnimatorState state) => EnsureBaseLayer().AddState(state);
    public void AddTransition(AnimatorTransition transition) => EnsureBaseLayer().AddTransition(transition);
    public void AddParameter(AnimatorParameter parameter) => Parameters.Add(parameter);

    public void Play(string stateName, float transitionDuration = -1f) => Play(stateName, 0, transitionDuration);

    public void Play(string stateName, int layerIndex, float transitionDuration = -1f)
    {
        if (layerIndex >= 0 && layerIndex < Layers.Count)
        {
            if (layerIndex == 0 && transitionDuration <= 0f)
                _hasPrevRootPose = false;
            Layers[layerIndex].Play(stateName, transitionDuration);
        }
    }

    public void CrossFade(string stateName, float duration, int layerIndex = 0)
    {
        Play(stateName, layerIndex, duration > 0f ? duration : 0f);
    }

    public void Stop()
    {
        for (int i = 0; i < Layers.Count; i++)
            Layers[i].IsPlaying = false;
    }

    public void Pause()
    {
        for (int i = 0; i < Layers.Count; i++)
            Layers[i].IsPlaying = false;
    }

    public void Resume()
    {
        for (int i = 0; i < Layers.Count; i++)
            Layers[i].IsPlaying = true;
    }

    private static AnimationClip? GetPrimaryClip(AnimatorState? state)
    {
        if (state == null) return null;
        if (state.BlendTree != null && state.BlendTree.Children.Count > 0)
        {
            for (int i = 0; i < state.BlendTree.Children.Count; i++)
            {
                var clip = state.BlendTree.Children[i].Clip.Res;
                if (clip != null) return clip;
            }
        }
        return state.Clip.Res;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  UPDATE & MULTI-LAYER EVALUATION (Zero Allocations in Hot Path)
    // ════════════════════════════════════════════════════════════════════════

    public override void Update()
    {
        Update(Prowl.Runtime.Time.DeltaTime);
    }

    /// <summary>
    /// Deterministic update method for parameter condition evaluation, multi-layer progression, and pose evaluation.
    /// </summary>
    public void Update(float deltaTime)
    {
        EnsureBaseLayer();
        int layerCount = Math.Min(Layers.Count, MaxLayers);
        if (layerCount == 0) return;

        EnsureHumanoidBoneLookup();

        var baseLayer = Layers[0];
        if (!_hasPrevRootPose && baseLayer.CurrentState != null)
        {
            if (EvaluateStatePoses(baseLayer.CurrentState, baseLayer.CurrentTime, 0, _samplerCurrent, out var startPoses) && startPoses.Length > 0)
            {
                _prevRootPosition = startPoses[0].Position;
                _prevRootRotation = startPoses[0].Rotation;
                _rootRestPosition = startPoses[0].Position;
                _rootRestRotation = startPoses[0].Rotation;
                _prevBaseLayerTime = baseLayer.CurrentTime;
                _hasPrevRootPose = true;
            }
        }

        int maxCombinedBones = 0;

        // 1. Update and evaluate each layer independently
        for (int k = 0; k < layerCount; k++)
        {
            var layer = Layers[k];
            if (!layer.IsPlaying || layer.CurrentState == null) continue;

            // Check automatic transitions on this layer
            if (!layer.IsInTransition)
                CheckAutomaticTransitions(layer);

            float stateDuration = layer.CurrentState.GetDuration();
            float effectiveSpeed = Speed * layer.CurrentState.Speed;

            if (layer.IsInTransition && layer.TargetState != null)
            {
                float targetDuration = layer.TargetState.GetDuration();
                layer.CurrentTime = AdvanceTime(layer.CurrentTime, deltaTime * effectiveSpeed, stateDuration, layer.CurrentState.Wrap, layer.CurrentState.Loop);
                float targetEffectiveSpeed = Speed * layer.TargetState.Speed;
                layer.TargetTime = AdvanceTime(layer.TargetTime, deltaTime * targetEffectiveSpeed, targetDuration, layer.TargetState.Wrap, layer.TargetState.Loop);

                layer.TransitionTime += Math.Abs(deltaTime * Speed);
                float blendWeight = layer.TransitionDuration > 0f ? Math.Clamp(layer.TransitionTime / layer.TransitionDuration, 0f, 1f) : 1f;

                EvaluateBlendedLayerPoses(layer, k, blendWeight);

                if (layer.TransitionTime >= layer.TransitionDuration)
                {
                    layer.CurrentState = layer.TargetState;
                    layer.CurrentTime = layer.TargetTime;
                    layer.TargetState = null;
                    layer.IsInTransition = false;
                }
            }
            else
            {
                layer.CurrentTime = AdvanceTime(layer.CurrentTime, deltaTime * effectiveSpeed, stateDuration, layer.CurrentState.Wrap, layer.CurrentState.Loop);
                EvaluateSingleLayerPose(layer, k);
            }

            if (_layerPoses[k] != null && _layerPoses[k].Length > maxCombinedBones)
                maxCombinedBones = _layerPoses[k].Length;
        }

        if (maxCombinedBones == 0) return;

        if (_finalCombinedPoses.Length < maxCombinedBones)
            _finalCombinedPoses = new BonePose[maxCombinedBones];

        // 2. Combine layers in order: Base Layer -> Layer 1 -> Layer 2 ...
        bool baseInitialized = false;

        for (int k = 0; k < layerCount; k++)
        {
            var layer = Layers[k];
            var lPoses = _layerPoses[k];
            if (lPoses == null || lPoses.Length == 0) continue;

            float layerWeight = Math.Clamp(layer.Weight, 0f, 1f);

            if (!baseInitialized)
            {
                // Layer 0 or first active layer initializes the base pose
                Array.Copy(lPoses, _finalCombinedPoses, lPoses.Length);
                baseInitialized = true;
            }
            else if (layerWeight > 1e-4f)
            {
                // Blend secondary layer respecting layer weight and AvatarMask
                var mask = layer.Mask;
                int blendCount = Math.Min(_finalCombinedPoses.Length, lPoses.Length);

                for (int b = 0; b < blendCount; b++)
                {
                    float boneWeight = layerWeight;
                    if (mask != null)
                    {
                        int hb = (b < _skeletonBoneToHumanoidBone.Length) ? _skeletonBoneToHumanoidBone[b] : -1;
                        if (hb >= 0)
                            boneWeight = layerWeight * mask.GetEffectiveWeight((HumanoidBone)hb);
                        else
                            boneWeight = 0f;
                    }

                    if (boneWeight > 1e-4f)
                    {
                        _finalCombinedPoses[b] = BlendBonePose(in _finalCombinedPoses[b], in lPoses[b], boneWeight);
                    }
                }
            }
        }

        if (!baseInitialized) return;

        // 3. Root Motion Extraction & Application
        var primaryClip = GetPrimaryClip(Layers[0].CurrentState);
        if (_finalCombinedPoses.Length > 0)
        {
            var currentRootPose = _finalCombinedPoses[0];

            if (!_hasPrevRootPose)
            {
                _prevRootPosition = currentRootPose.Position;
                _prevRootRotation = currentRootPose.Rotation;
                _rootRestPosition = currentRootPose.Position;
                _rootRestRotation = currentRootPose.Rotation;
                _prevBaseLayerTime = baseLayer.CurrentTime;
                _hasPrevRootPose = true;

                RootMotionPositionDelta = Float3.Zero;
                RootMotionRotationDelta = Quaternion.Identity;
            }
            else
            {
                Float3 deltaPos;
                Quaternion deltaRot;

                bool wrapped = deltaTime > 0f && baseLayer.CurrentState != null &&
                               (baseLayer.CurrentState.Loop || baseLayer.CurrentState.Wrap == AnimationWrapMode.Loop) &&
                               baseLayer.CurrentTime < _prevBaseLayerTime;

                if (wrapped)
                {
                    ComputeRootCycleDelta(baseLayer.CurrentState, out Float3 cyclePos, out Quaternion cycleRot);
                    deltaPos = (currentRootPose.Position - _prevRootPosition) + cyclePos;
                    deltaRot = (currentRootPose.Rotation * Quaternion.Inverse(_prevRootRotation)) * cycleRot;
                }
                else
                {
                    deltaPos = currentRootPose.Position - _prevRootPosition;
                    deltaRot = currentRootPose.Rotation * Quaternion.Inverse(_prevRootRotation);
                }

                if (primaryClip != null && TryEnsureRetargeting(primaryClip) && _retargeter != null)
                {
                    deltaPos *= _retargeter.PositionScale;
                }

                RootMotionPositionDelta = deltaPos;
                RootMotionRotationDelta = deltaRot;

                _prevRootPosition = currentRootPose.Position;
                _prevRootRotation = currentRootPose.Rotation;
                _prevBaseLayerTime = baseLayer.CurrentTime;

                if (ApplyRootMotion)
                {
                    Transform.LocalPosition += Transform.LocalRotation * deltaPos;
                    Transform.LocalRotation = Transform.LocalRotation * deltaRot;

                    _finalCombinedPoses[0].Position = _rootRestPosition;
                    _finalCombinedPoses[0].Rotation = _rootRestRotation;
                }
            }
        }

        // 4. Apply final combined pose to scene/skeleton transforms
        if (primaryClip == null) return;

        if (TryEnsureRetargeting(primaryClip))
        {
            ApplyRetargetedPoseSingle(_finalCombinedPoses);
        }
        else if (TryEnsureSkeleton(primaryClip))
        {
            var skel = _boundSkeleton;
            if (skel != null)
            {
                if (_skeletonPoses.Length < skel.BoneCount)
                    _skeletonPoses = new BonePose[skel.BoneCount];

                for (int i = 0; i < skel.BoneCount; i++)
                {
                    var bone = skel[i];
                    _skeletonPoses[i] = new BonePose
                    {
                        Position = bone.LocalPosition,
                        Rotation = bone.LocalRotation,
                        Scale = bone.LocalScale
                    };
                }

                for (int i = 0; i < _bindingCount; i++)
                {
                    ref var b = ref _bindings[i];
                    if (b.SkeletonBoneIndex >= 0 && b.SkeletonBoneIndex < skel.BoneCount && b.ClipBoneIndex < _finalCombinedPoses.Length)
                    {
                        _skeletonPoses[b.SkeletonBoneIndex] = _finalCombinedPoses[b.ClipBoneIndex];
                    }
                }

                ApplyIK(_skeletonPoses, skel, HumanoidMapping.Res);

                for (int i = 0; i < _skeletonTransforms.Length; i++)
                {
                    var target = _skeletonTransforms[i];
                    if (target == null) continue;

                    if (i < _skeletonPoses.Length)
                    {
                        var pose = _skeletonPoses[i];
                        target.LocalPosition = pose.Position;
                        target.LocalRotation = pose.Rotation;
                        target.LocalScale = pose.Scale;
                    }
                }
            }
        }
        else
        {
            ApplyIK(_finalCombinedPoses, Skeleton.Res, HumanoidMapping.Res);
            ApplyLegacyPose(primaryClip, _finalCombinedPoses);
        }
    }

    private void ComputeRootCycleDelta(AnimatorState? state, out Float3 posDelta, out Quaternion rotDelta)
    {
        posDelta = Float3.Zero;
        rotDelta = Quaternion.Identity;

        if (state == null) return;

        if (state.BlendTree != null && state.BlendTree.Children.Count > 0)
        {
            float dur = state.GetDuration();
            if (dur > 1e-4f &&
                EvaluateBlendTree(state.BlendTree, 0f, 0, out var startPoses) && startPoses.Length > 0)
            {
                var startPos = startPoses[0].Position;
                var startRot = startPoses[0].Rotation;

                if (EvaluateBlendTree(state.BlendTree, dur, 0, out var endPoses) && endPoses.Length > 0)
                {
                    posDelta = endPoses[0].Position - startPos;
                    rotDelta = endPoses[0].Rotation * Quaternion.Inverse(startRot);
                }
            }
            return;
        }

        var clip = state.Clip.Res;
        if (clip != null && clip.Bones.Count > 0 && clip.Duration > 1e-4f)
        {
            _samplerCurrent.Evaluate(clip, clip.StartTime, out var p0);
            if (p0.Length > 0)
            {
                var startPos = p0[0].Position;
                var startRot = p0[0].Rotation;

                _samplerCurrent.Evaluate(clip, clip.StartTime + clip.Duration, out var p1);
                if (p1.Length > 0)
                {
                    posDelta = p1[0].Position - startPos;
                    rotDelta = p1[0].Rotation * Quaternion.Inverse(startRot);
                }
            }
        }
    }

    private void EnsureHumanoidBoneLookup()
    {
        var mapping = SourceHumanoidMapping.Res;
        if (mapping == null || !mapping.IsValid())
            mapping = HumanoidMapping.Res;

        var skel = SourceSkeleton.Res;
        if (skel == null || !skel.IsValid())
            skel = Skeleton.Res;

        if (mapping != null && mapping.IsValid() && skel != null && skel.IsValid())
        {
            if (_skeletonBoneToHumanoidBone.Length != skel.BoneCount)
            {
                _skeletonBoneToHumanoidBone = new int[skel.BoneCount];
                Array.Fill(_skeletonBoneToHumanoidBone, -1);

                for (int i = 0; i < Prowl.Runtime.HumanoidMapping.HumanoidBoneCount; i++)
                {
                    var hb = (HumanoidBone)i;
                    if (mapping.TryGet(hb, out int skelIdx) && skelIdx >= 0 && skelIdx < skel.BoneCount)
                    {
                        _skeletonBoneToHumanoidBone[skelIdx] = i;
                    }
                }
            }
        }
        else if (_skeletonBoneToHumanoidBone.Length == 0)
        {
            // Default 1:1 humanoid index mapping for testing/standalone
            _skeletonBoneToHumanoidBone = new int[Prowl.Runtime.HumanoidMapping.HumanoidBoneCount];
            for (int i = 0; i < Prowl.Runtime.HumanoidMapping.HumanoidBoneCount; i++)
                _skeletonBoneToHumanoidBone[i] = i;
        }
    }

    private void EvaluateSingleLayerPose(AnimatorLayer layer, int layerIndex)
    {
        if (layer.CurrentState == null) return;

        if (EvaluateStatePoses(layer.CurrentState, layer.CurrentTime, layerIndex * MaxBlendTreeSamplersPerLayer, _samplerCurrent, out var poses))
        {
            EnsureLayerPoseBuffer(layerIndex, poses.Length);
            Array.Copy(poses, _layerPoses[layerIndex], poses.Length);
        }
    }

    private void EvaluateBlendedLayerPoses(AnimatorLayer layer, int layerIndex, float blendWeight)
    {
        if (layer.CurrentState == null || layer.TargetState == null) return;

        int samplerOffsetA = layerIndex * MaxBlendTreeSamplersPerLayer;
        int samplerOffsetB = (layerIndex + 1) * MaxBlendTreeSamplersPerLayer;

        bool hasPosesA = EvaluateStatePoses(layer.CurrentState, layer.CurrentTime, samplerOffsetA, _samplerCurrent, out var posesA);
        bool hasPosesB = EvaluateStatePoses(layer.TargetState, layer.TargetTime, samplerOffsetB, _samplerTarget, out var posesB);

        if (!hasPosesA && !hasPosesB) return;
        if (!hasPosesA) { EnsureLayerPoseBuffer(layerIndex, posesB.Length); Array.Copy(posesB, _layerPoses[layerIndex], posesB.Length); return; }
        if (!hasPosesB) { EnsureLayerPoseBuffer(layerIndex, posesA.Length); Array.Copy(posesA, _layerPoses[layerIndex], posesA.Length); return; }

        int blendCount = Math.Min(posesA.Length, posesB.Length);
        EnsureLayerPoseBuffer(layerIndex, blendCount);

        for (int i = 0; i < blendCount; i++)
        {
            _layerPoses[layerIndex][i] = BlendBonePose(in posesA[i], in posesB[i], blendWeight);
        }
    }

    private void EnsureLayerPoseBuffer(int layerIndex, int length)
    {
        if (_layerPoses[layerIndex] == null || _layerPoses[layerIndex].Length < length)
            _layerPoses[layerIndex] = new BonePose[length];
    }

    private void CheckAutomaticTransitions(AnimatorLayer layer)
    {
        if (layer.CurrentState == null || layer.IsInTransition) return;

        var transitions = CollectionsMarshal.AsSpan(layer.Transitions);
        for (int i = 0; i < transitions.Length; i++)
        {
            var trans = transitions[i];
            if (trans.Conditions.Count == 0) continue;

            if (!string.IsNullOrEmpty(trans.SourceState) && trans.SourceState != layer.CurrentState.Name)
                continue;

            var targetState = layer.GetState(trans.TargetState);
            if (targetState == null || targetState == layer.CurrentState)
                continue;

            if (EvaluateConditions(trans.Conditions))
            {
                ConsumeTriggers(trans.Conditions);
                layer.Play(trans.TargetState, trans.Duration);
                break;
            }
        }
    }

    private bool EvaluateConditions(List<AnimatorCondition> conditions)
    {
        if (conditions.Count == 0) return false;

        var conds = CollectionsMarshal.AsSpan(conditions);
        for (int i = 0; i < conds.Length; i++)
        {
            var cond = conds[i];
            if (string.IsNullOrEmpty(cond.Parameter)) return false;

            if (!_paramValues.TryGetValue(cond.Parameter, out var param))
                return false;

            if (!EvaluateSingleCondition(in cond, in param))
                return false;
        }
        return true;
    }

    private static bool EvaluateSingleCondition(in AnimatorCondition cond, in AnimatorParamValue param)
    {
        switch (param.Type)
        {
            case AnimatorParameterType.Bool:
                return cond.Mode switch
                {
                    AnimatorConditionMode.If => param.BoolVal,
                    AnimatorConditionMode.IfNot => !param.BoolVal,
                    AnimatorConditionMode.Equal => param.BoolVal == (cond.Threshold > 0.5f),
                    AnimatorConditionMode.NotEqual => param.BoolVal != (cond.Threshold > 0.5f),
                    _ => false
                };

            case AnimatorParameterType.Int:
                return cond.Mode switch
                {
                    AnimatorConditionMode.Greater => param.IntVal > (int)cond.Threshold,
                    AnimatorConditionMode.Less => param.IntVal < (int)cond.Threshold,
                    AnimatorConditionMode.Equal => param.IntVal == (int)cond.Threshold,
                    AnimatorConditionMode.NotEqual => param.IntVal != (int)cond.Threshold,
                    _ => false
                };

            case AnimatorParameterType.Float:
                return cond.Mode switch
                {
                    AnimatorConditionMode.Greater => param.FloatVal > cond.Threshold,
                    AnimatorConditionMode.Less => param.FloatVal < cond.Threshold,
                    AnimatorConditionMode.Equal => MathF.Abs(param.FloatVal - cond.Threshold) < 1e-4f,
                    AnimatorConditionMode.NotEqual => MathF.Abs(param.FloatVal - cond.Threshold) >= 1e-4f,
                    _ => false
                };

            case AnimatorParameterType.Trigger:
                return cond.Mode switch
                {
                    AnimatorConditionMode.If => param.TriggerVal,
                    AnimatorConditionMode.IfNot => !param.TriggerVal,
                    _ => false
                };

            default:
                return false;
        }
    }

    private void ConsumeTriggers(List<AnimatorCondition> conditions)
    {
        var conds = CollectionsMarshal.AsSpan(conditions);
        for (int i = 0; i < conds.Length; i++)
        {
            var cond = conds[i];
            if (_paramValues.TryGetValue(cond.Parameter, out var param) && param.Type == AnimatorParameterType.Trigger)
            {
                param.TriggerVal = false;
                _paramValues[cond.Parameter] = param;
            }
        }
    }

    private static float AdvanceTime(float time, float dt, float duration, AnimationWrapMode wrap, bool loop)
    {
        if (duration <= 0f) return 0f;

        time += dt;

        var effectiveWrap = wrap;
        if (effectiveWrap == AnimationWrapMode.Loop && !loop)
            effectiveWrap = AnimationWrapMode.Once;

        switch (effectiveWrap)
        {
            case AnimationWrapMode.Once:
                if (time >= duration) return duration;
                if (time < 0f) return 0f;
                return time;

            case AnimationWrapMode.Loop:
                time %= duration;
                if (time < 0f) time += duration;
                return time;

            case AnimationWrapMode.PingPong:
                float cycle = time / duration;
                int wholeCycles = (int)cycle;
                float frac = cycle - wholeCycles;
                return (wholeCycles % 2 == 0) ? frac * duration : (1f - frac) * duration;

            case AnimationWrapMode.ClampForever:
                if (time >= duration) return duration;
                if (time < 0f) return 0f;
                return time;

            default:
                return time;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  STATE EVALUATION & BLEND TREES
    // ════════════════════════════════════════════════════════════════════════

    private bool EvaluateStatePoses(AnimatorState state, float time, int samplerOffset, AnimationSampler fallbackSampler, out BonePose[] poses)
    {
        if (state.BlendTree != null && state.BlendTree.Children.Count > 0)
        {
            return EvaluateBlendTree(state.BlendTree, time, samplerOffset, out poses);
        }

        var clip = state.Clip.Res;
        if (clip != null)
        {
            fallbackSampler.Evaluate(clip, clip.StartTime + time, out poses);
            return true;
        }

        poses = Array.Empty<BonePose>();
        return false;
    }

    private bool EvaluateBlendTree(BlendTree tree, float time, int samplerOffset, out BonePose[] poses)
    {
        int count = Math.Min(tree.Children.Count, MaxBlendTreeSamplersPerLayer);
        if (count == 0)
        {
            poses = Array.Empty<BonePose>();
            return false;
        }

        // 1. Calculate weights
        if (tree.BlendType == BlendTreeType.Simple1D)
        {
            float p = GetFloat(tree.BlendParameter);
            Compute1DWeights(tree, p, _blendWeights, count);
        }
        else
        {
            float px = GetFloat(tree.BlendParameter);
            float py = GetFloat(tree.BlendParameterY);
            Compute2DWeights(tree, new Float2(px, py), _blendWeights, count);
        }

        // 2. Accumulate weighted poses
        float accumWeight = 0f;
        int maxBones = 0;
        var samplers = GetBlendTreeSamplers();

        for (int i = 0; i < count; i++)
        {
            float w = _blendWeights[i];
            if (w < 1e-4f) continue;

            var child = tree.Children[i];
            var clip = child.Clip.Res;
            if (clip == null) continue;

            int samplerIdx = Math.Min(samplerOffset + i, samplers.Length - 1);
            samplers[samplerIdx].Evaluate(clip, clip.StartTime + (time * child.Speed), out var childPoses);
            int boneCount = childPoses.Length;
            if (boneCount > maxBones) maxBones = boneCount;

            if (_stateBlendAccumulator.Length < boneCount)
                _stateBlendAccumulator = new BonePose[boneCount];

            if (accumWeight <= 1e-5f)
            {
                Array.Copy(childPoses, _stateBlendAccumulator, boneCount);
                accumWeight = w;
            }
            else
            {
                float blendFactor = w / (accumWeight + w);
                int blendCount = Math.Min(_stateBlendAccumulator.Length, boneCount);
                for (int b = 0; b < blendCount; b++)
                {
                    _stateBlendAccumulator[b] = BlendBonePose(in _stateBlendAccumulator[b], in childPoses[b], blendFactor);
                }
                accumWeight += w;
            }
        }

        if (accumWeight > 1e-5f)
        {
            poses = _stateBlendAccumulator;
            return true;
        }

        poses = Array.Empty<BonePose>();
        return false;
    }

    private static void Compute1DWeights(BlendTree tree, float p, float[] weights, int count)
    {
        Array.Clear(weights, 0, count);

        if (count == 1)
        {
            weights[0] = 1f;
            return;
        }

        if (p <= tree.Children[0].Threshold)
        {
            weights[0] = 1f;
            return;
        }
        if (p >= tree.Children[count - 1].Threshold)
        {
            weights[count - 1] = 1f;
            return;
        }

        for (int i = 0; i < count - 1; i++)
        {
            float t0 = tree.Children[i].Threshold;
            float t1 = tree.Children[i + 1].Threshold;

            if (p >= t0 && p <= t1)
            {
                float range = t1 - t0;
                if (range > 1e-5f)
                {
                    float t = (p - t0) / range;
                    weights[i] = 1f - t;
                    weights[i + 1] = t;
                }
                else
                {
                    weights[i] = 1f;
                }
                return;
            }
        }
    }

    private static void Compute2DWeights(BlendTree tree, Float2 p, float[] weights, int count)
    {
        Array.Clear(weights, 0, count);

        if (count == 1)
        {
            weights[0] = 1f;
            return;
        }

        // 1. Check exact match
        for (int i = 0; i < count; i++)
        {
            Float2 cPos = tree.Children[i].Position;
            float dx = p.X - cPos.X;
            float dy = p.Y - cPos.Y;
            float sqrDist = dx * dx + dy * dy;

            if (sqrDist < 1e-6f)
            {
                weights[i] = 1f;
                return;
            }
        }

        // 2. Inverse Distance Weighting
        float totalW = 0f;
        for (int i = 0; i < count; i++)
        {
            Float2 cPos = tree.Children[i].Position;
            float dx = p.X - cPos.X;
            float dy = p.Y - cPos.Y;
            float sqrDist = dx * dx + dy * dy;

            float w = 1f / sqrDist;
            weights[i] = w;
            totalW += w;
        }

        if (totalW > 1e-6f)
        {
            float invTotal = 1f / totalW;
            for (int i = 0; i < count; i++)
                weights[i] *= invTotal;
        }
        else
        {
            weights[0] = 1f;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  RETARGETED PIPELINE
    // ════════════════════════════════════════════════════════════════════════

    private bool TryEnsureRetargeting(AnimationClip clip)
    {
        var srcSkel = SourceSkeleton.Res;
        var srcMap = SourceHumanoidMapping.Res;
        var tgtSkel = Skeleton.Res;
        var tgtMap = HumanoidMapping.Res;

        if (srcSkel == null || srcMap == null || tgtSkel == null || tgtMap == null)
            return false;

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

            if (_retargetTargetTransforms.Length != tgtSkel.BoneCount)
                _retargetTargetTransforms = new Transform?[tgtSkel.BoneCount];
            ResolveSkeletonTransforms(tgtSkel, _retargetTargetTransforms);

            if (_sourceSkeletonPoses.Length != srcSkel.BoneCount)
                _sourceSkeletonPoses = new BonePose[srcSkel.BoneCount];
        }

        if (_boundRetargetClip != clip)
        {
            _boundRetargetClip = clip;
            BuildSourceClipBindings(clip, srcSkel, ref _sourceClipBindings, ref _sourceClipBindingCount);
        }

        return _retargeter != null;
    }

    private void BuildSourceClipBindings(
        AnimationClip clip,
        SkeletonAsset srcSkel,
        ref ClipBoneBinding[] bindings,
        ref int bindingCount)
    {
        int clipBoneCount = clip.Bones.Count;
        if (bindings.Length < clipBoneCount)
            bindings = new ClipBoneBinding[clipBoneCount];

        bindingCount = 0;
        for (int i = 0; i < clipBoneCount; i++)
        {
            string bonePath = clip.Bones[i].BoneName;
            int skelIdx = ResolveClipBoneToSkeleton(bonePath, srcSkel);
            if (skelIdx >= 0)
            {
                bindings[bindingCount++] = new ClipBoneBinding
                {
                    ClipBoneIndex = i,
                    SkeletonBoneIndex = skelIdx,
                    Target = null
                };
            }
        }
    }

    private void ApplyRetargetedPoseSingle(BonePose[] clipPoses)
    {
        if (_retargeter == null || _boundRetargetSourceSkeleton == null) return;

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

        for (int i = 0; i < _sourceClipBindingCount; i++)
        {
            ref var b = ref _sourceClipBindings[i];
            if (b.ClipBoneIndex < clipPoses.Length)
                _sourceSkeletonPoses[b.SkeletonBoneIndex] = clipPoses[b.ClipBoneIndex];
        }

        _retargeter.TryRetargetPose(_sourceSkeletonPoses, out var targetPoses);

        ApplyIK(targetPoses, _boundRetargetTargetSkeleton, _boundRetargetTargetMapping);

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
    //  DIRECT SKELETON PIPELINE
    // ════════════════════════════════════════════════════════════════════════

    private bool TryEnsureSkeleton(AnimationClip clip)
    {
        var skel = Skeleton.Res;
        if (skel == null) return false;

        if (_skeletonTransforms.Length != skel.BoneCount)
        {
            _skeletonTransforms = new Transform?[skel.BoneCount];
            ResolveSkeletonTransforms(skel, _skeletonTransforms);
        }

        if (_boundClip != clip || _boundSkeleton != skel)
        {
            _boundClip = clip;
            _boundSkeleton = skel;

            int clipBoneCount = clip.Bones.Count;
            if (_bindings.Length < clipBoneCount)
                _bindings = new ClipBoneBinding[clipBoneCount];

            _bindingCount = 0;
            for (int i = 0; i < clipBoneCount; i++)
            {
                string bonePath = clip.Bones[i].BoneName;
                int skelIdx = ResolveClipBoneToSkeleton(bonePath, skel);
                _bindings[_bindingCount++] = new ClipBoneBinding
                {
                    ClipBoneIndex = i,
                    SkeletonBoneIndex = skelIdx,
                    Target = skelIdx >= 0 ? _skeletonTransforms[skelIdx] : null
                };
            }
        }

        return _bindingCount > 0;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  LEGACY HIERARCHY PIPELINE
    // ════════════════════════════════════════════════════════════════════════

    private void ApplyLegacyPose(AnimationClip clip, BonePose[] poses)
    {
        EnsureBoneCache();
        if (_boneCache == null || _boneCache.Count == 0) return;

        for (int i = 0; i < clip.Bones.Count; i++)
        {
            var animBone = clip.Bones[i];
            if (!_boneCache.TryGetValue(animBone.BoneName, out Transform? bone)) continue;
            if (bone == null) continue;

            if (i < poses.Length)
            {
                var pose = poses[i];
                bone.LocalPosition = pose.Position;
                bone.LocalRotation = pose.Rotation;
                bone.LocalScale = pose.Scale;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  HELPER MATH & HIERARCHY RESOLUTION
    // ════════════════════════════════════════════════════════════════════════

    private static Float3 LerpFloat3(in Float3 a, in Float3 b, float t)
    {
        return new Float3(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t
        );
    }

    private static BonePose BlendBonePose(in BonePose a, in BonePose b, float t)
    {
        return new BonePose
        {
            Position = LerpFloat3(a.Position, b.Position, t),
            Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t),
            Scale = LerpFloat3(a.Scale, b.Scale, t)
        };
    }

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

    private static int ResolveClipBoneToSkeleton(string bonePath, SkeletonAsset skeleton)
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
        if (exactMatchCount > 1) return -1;
        if (candidateCount == 1) return lastCandidate;

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

    // ════════════════════════════════════════════════════════════════════════
    //  INVERSE KINEMATICS (IK) PIPELINE (Zero Allocations in Hot Path)
    // ════════════════════════════════════════════════════════════════════════

    private void ApplyIK(BonePose[] poses, SkeletonAsset? skeleton, HumanoidMapping? mapping)
    {
        if (!EnableIK || skeleton == null || poses == null || poses.Length == 0) return;

        // Early-out: check if any IK goal has weight > 0
        bool hasWeight = (LeftHandIK.PositionWeight > 1e-4f || LeftHandIK.RotationWeight > 1e-4f) ||
                         (RightHandIK.PositionWeight > 1e-4f || RightHandIK.RotationWeight > 1e-4f) ||
                         (LeftFootIK.PositionWeight > 1e-4f || LeftFootIK.RotationWeight > 1e-4f) ||
                         (RightFootIK.PositionWeight > 1e-4f || RightFootIK.RotationWeight > 1e-4f);

        if (!hasWeight) return;

        EnsureIKChains(skeleton, mapping);
        EnsureFKBuffers(skeleton.BoneCount);

        // Forward Kinematics (FK) pass to compute world transforms
        Float3 animPos = Transform.Position;
        Quaternion animRot = Transform.Rotation;
        Float3 animScale = Transform.LossyScale;

        int boneCount = Math.Min(poses.Length, skeleton.BoneCount);
        for (int i = 0; i < boneCount; i++)
        {
            int parentIdx = skeleton.GetParentIndex(i);
            ref readonly var p = ref poses[i];

            if (parentIdx < 0)
            {
                _fkWorldRotations[i] = animRot * p.Rotation;
                _fkWorldPositions[i] = animPos + animRot * (p.Position * animScale);
                _fkWorldScales[i] = animScale * p.Scale;
            }
            else
            {
                _fkWorldRotations[i] = _fkWorldRotations[parentIdx] * p.Rotation;
                _fkWorldPositions[i] = _fkWorldPositions[parentIdx] + _fkWorldRotations[parentIdx] * (p.Position * _fkWorldScales[parentIdx]);
                _fkWorldScales[i] = _fkWorldScales[parentIdx] * p.Scale;
            }
        }

        // Solve for each active IK goal
        for (int g = 0; g < 4; g++)
        {
            ref readonly var chain = ref _ikChains[g];
            if (!chain.IsValid) continue;
            if (chain.RootBoneIndex >= poses.Length || chain.MidBoneIndex >= poses.Length || chain.EndBoneIndex >= poses.Length) continue;

            var goal = GetIKTarget((AvatarIKGoal)g);
            if (goal.PositionWeight <= 1e-4f && goal.RotationWeight <= 1e-4f) continue;

            Float3 targetPos;
            if (goal.Target != null)
                targetPos = goal.Target.Position + goal.Target.Rotation * goal.PositionOffset;
            else if (goal.Position.HasValue)
                targetPos = goal.Position.Value + goal.PositionOffset;
            else
                continue;

            Quaternion? targetRot = null;
            if (goal.Target != null)
                targetRot = goal.Target.Rotation * goal.RotationOffset;
            else if (goal.Rotation.HasValue)
                targetRot = goal.Rotation.Value * goal.RotationOffset;

            Float3? hintPos = null;
            if (goal.Hint != null)
                hintPos = goal.Hint.Position;
            else if (goal.HintPosition.HasValue)
                hintPos = goal.HintPosition.Value;

            Quaternion parentWorldRot = (chain.ParentBoneIndex >= 0 && chain.ParentBoneIndex < boneCount)
                ? _fkWorldRotations[chain.ParentBoneIndex]
                : animRot;

            if (IKSolver.SolveTwoBoneIK(
                in _fkWorldPositions[chain.RootBoneIndex], in _fkWorldRotations[chain.RootBoneIndex],
                in _fkWorldPositions[chain.MidBoneIndex], in _fkWorldRotations[chain.MidBoneIndex],
                in _fkWorldPositions[chain.EndBoneIndex], in _fkWorldRotations[chain.EndBoneIndex],
                in parentWorldRot,
                in targetPos,
                targetRot,
                hintPos,
                goal.PositionWeight,
                goal.RotationWeight,
                out Quaternion solvedRot0,
                out Quaternion solvedRot1,
                out Quaternion solvedRot2))
            {
                poses[chain.RootBoneIndex].Rotation = solvedRot0;
                poses[chain.MidBoneIndex].Rotation = solvedRot1;
                poses[chain.EndBoneIndex].Rotation = solvedRot2;
            }
        }
    }

    private void EnsureIKChains(SkeletonAsset skeleton, HumanoidMapping? mapping)
    {
        if (_boundIKSkeleton == skeleton && _boundIKMapping == mapping) return;

        _boundIKSkeleton = skeleton;
        _boundIKMapping = mapping;

        for (int i = 0; i < 4; i++)
            _ikChains[i] = default;

        ResolveIKChain(AvatarIKGoal.LeftHand, HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand, skeleton, mapping);
        ResolveIKChain(AvatarIKGoal.RightHand, HumanoidBone.RightUpperArm, HumanoidBone.RightLowerArm, HumanoidBone.RightHand, skeleton, mapping);
        ResolveIKChain(AvatarIKGoal.LeftFoot, HumanoidBone.LeftUpperLeg, HumanoidBone.LeftLowerLeg, HumanoidBone.LeftFoot, skeleton, mapping);
        ResolveIKChain(AvatarIKGoal.RightFoot, HumanoidBone.RightUpperLeg, HumanoidBone.RightLowerLeg, HumanoidBone.RightFoot, skeleton, mapping);
    }

    private void ResolveIKChain(AvatarIKGoal goal, HumanoidBone root, HumanoidBone mid, HumanoidBone end, SkeletonAsset skeleton, HumanoidMapping? mapping)
    {
        int rootIdx = -1, midIdx = -1, endIdx = -1;

        if (mapping != null && mapping.IsValid())
        {
            mapping.TryGet(root, out rootIdx);
            mapping.TryGet(mid, out midIdx);
            mapping.TryGet(end, out endIdx);
        }
        else
        {
            int r = (int)root;
            int m = (int)mid;
            int e = (int)end;
            if (r < skeleton.BoneCount && m < skeleton.BoneCount && e < skeleton.BoneCount)
            {
                rootIdx = r;
                midIdx = m;
                endIdx = e;
            }
        }

        if (rootIdx >= 0 && rootIdx < skeleton.BoneCount &&
            midIdx >= 0 && midIdx < skeleton.BoneCount &&
            endIdx >= 0 && endIdx < skeleton.BoneCount)
        {
            _ikChains[(int)goal] = new IKChain
            {
                IsValid = true,
                RootBoneIndex = rootIdx,
                MidBoneIndex = midIdx,
                EndBoneIndex = endIdx,
                ParentBoneIndex = skeleton.GetParentIndex(rootIdx)
            };
        }
    }

    private void EnsureFKBuffers(int boneCount)
    {
        if (_fkWorldPositions.Length < boneCount)
        {
            _fkWorldPositions = new Float3[boneCount];
            _fkWorldRotations = new Quaternion[boneCount];
            _fkWorldScales = new Float3[boneCount];
        }
    }

    /// <summary>
    /// Validates the animator configuration, including all layers, parameters, skeletons, and IK settings.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        // 1. Validate layers
        if (Layers.Count == 0)
        {
            errors.Add("Animator has no layers defined.");
        }
        else
        {
            for (int i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                if (layer == null)
                {
                    errors.Add($"Layer at index {i} is null.");
                    continue;
                }
                if (!layer.Validate(out List<string> layerErrors))
                {
                    foreach (var err in layerErrors)
                        errors.Add($"Layer {i} ('{layer.Name}'): {err}");
                }
            }
        }

        // 2. Validate parameters
        var paramNames = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < Parameters.Count; i++)
        {
            var p = Parameters[i];
            if (p == null)
            {
                errors.Add($"Parameter at index {i} is null.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(p.Name))
                errors.Add($"Parameter at index {i} has an empty name.");
            else if (!paramNames.Add(p.Name))
                errors.Add($"Duplicate parameter name '{p.Name}'.");
        }

        // 3. Validate skeleton & humanoid mappings
        if (Skeleton.Res != null)
        {
            var skel = Skeleton.Res;
            if (!skel.Validate(out List<string> skelErrors))
            {
                foreach (var err in skelErrors)
                    errors.Add($"Skeleton: {err}");
            }
        }

        if (HumanoidMapping.Res != null)
        {
            var mapping = HumanoidMapping.Res;
            if (!mapping.Validate(out List<string> mapErrors))
            {
                foreach (var err in mapErrors)
                    errors.Add($"HumanoidMapping: {err}");
            }
        }

        if (SourceSkeleton.Res != null)
        {
            var srcSkel = SourceSkeleton.Res;
            if (!srcSkel.Validate(out List<string> srcErrors))
            {
                foreach (var err in srcErrors)
                    errors.Add($"SourceSkeleton: {err}");
            }
        }

        if (SourceHumanoidMapping.Res != null)
        {
            var srcMap = SourceHumanoidMapping.Res;
            if (!srcMap.Validate(out List<string> srcErrors))
            {
                foreach (var err in srcErrors)
                    errors.Add($"SourceHumanoidMapping: {err}");
            }
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Validates the animator configuration.
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        bool valid = Validate(out List<string> errors);
        errorMessage = valid ? null : string.Join("; ", errors);
        return valid;
    }
}
