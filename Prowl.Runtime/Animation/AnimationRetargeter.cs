// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Prowl.Vector;

namespace Prowl.Runtime.Animation;

/// <summary>
/// Runtime retargeter that translates animation poses and clips from a source humanoid rig
/// to a target humanoid rig using precomputed <see cref="HumanoidCalibration"/> data.
/// <para>
/// Zero allocations in the hot path (<see cref="TryRetargetPose(ReadOnlySpan{BonePose}, out BonePose[])"/>).
/// </para>
/// </summary>
public sealed class AnimationRetargeter
{
    private struct RetargetBinding
    {
        public HumanoidBone HumanoidBone;
        public int SourceSkeletonIndex;
        public int TargetSkeletonIndex;
        public Quaternion SourceRefLocalRot;
        public Quaternion TargetRefLocalRot;
        public Quaternion InvSourceRefLocalRot;
        public Float3 SourceRefLocalPos;
        public Float3 TargetRefLocalPos;
        public Float3 SourceRefLocalScale;
        public Float3 TargetRefLocalScale;
        public bool IsRoot;
    }

    private SkeletonAsset _sourceSkeleton = null!;
    private SkeletonAsset _targetSkeleton = null!;
    private HumanoidMapping _sourceMapping = null!;
    private HumanoidMapping _targetMapping = null!;
    private HumanoidCalibration _sourceCalibration = null!;
    private HumanoidCalibration _targetCalibration = null!;

    private RetargetBinding[] _bindings = Array.Empty<RetargetBinding>();
    private BonePose[] _defaultTargetPoses = Array.Empty<BonePose>();
    private BonePose[] _targetPoseBuffer = Array.Empty<BonePose>();
    private float _positionScale = 1.0f;

    private readonly AnimationSampler _sampler = new();

    /// <summary>Source skeleton asset.</summary>
    public SkeletonAsset SourceSkeleton => _sourceSkeleton;

    /// <summary>Target skeleton asset.</summary>
    public SkeletonAsset TargetSkeleton => _targetSkeleton;

    /// <summary>Source humanoid calibration.</summary>
    public HumanoidCalibration SourceCalibration => _sourceCalibration;

    /// <summary>Target humanoid calibration.</summary>
    public HumanoidCalibration TargetCalibration => _targetCalibration;

    /// <summary>Hips position translation scale factor.</summary>
    public float PositionScale => _positionScale;

    /// <summary>
    /// Builds an <see cref="AnimationRetargeter"/> from source and target skeletons, mappings, and calibrations.
    /// </summary>
    public static bool Build(
        SkeletonAsset? sourceSkeleton,
        HumanoidMapping? sourceMapping,
        HumanoidCalibration? sourceCalibration,
        SkeletonAsset? targetSkeleton,
        HumanoidMapping? targetMapping,
        HumanoidCalibration? targetCalibration,
        out AnimationRetargeter? retargeter) =>
        Build(sourceSkeleton, sourceMapping, sourceCalibration, targetSkeleton, targetMapping, targetCalibration, out retargeter, out _);

    /// <summary>
    /// Builds an <see cref="AnimationRetargeter"/> from source and target assets, providing error details on failure.
    /// </summary>
    public static bool Build(
        SkeletonAsset? sourceSkeleton,
        HumanoidMapping? sourceMapping,
        HumanoidCalibration? sourceCalibration,
        SkeletonAsset? targetSkeleton,
        HumanoidMapping? targetMapping,
        HumanoidCalibration? targetCalibration,
        out AnimationRetargeter? retargeter,
        out List<string> errors)
    {
        retargeter = null;
        errors = new List<string>();

        if (sourceSkeleton == null) { errors.Add("Source SkeletonAsset is null."); return false; }
        if (targetSkeleton == null) { errors.Add("Target SkeletonAsset is null."); return false; }
        if (sourceMapping == null) { errors.Add("Source HumanoidMapping is null."); return false; }
        if (targetMapping == null) { errors.Add("Target HumanoidMapping is null."); return false; }
        if (sourceCalibration == null) { errors.Add("Source HumanoidCalibration is null."); return false; }
        if (targetCalibration == null) { errors.Add("Target HumanoidCalibration is null."); return false; }

        if (!sourceMapping.Validate(sourceSkeleton, out var srcErrors))
        {
            errors.AddRange(srcErrors);
            return false;
        }

        if (!targetMapping.Validate(targetSkeleton, out var tgtErrors))
        {
            errors.AddRange(tgtErrors);
            return false;
        }

        var instance = new AnimationRetargeter
        {
            _sourceSkeleton = sourceSkeleton,
            _targetSkeleton = targetSkeleton,
            _sourceMapping = sourceMapping,
            _targetMapping = targetMapping,
            _sourceCalibration = sourceCalibration,
            _targetCalibration = targetCalibration
        };

        // 1. Calculate height / position scale factor between source and target rigs
        float srcHeight = 0f;
        float tgtHeight = 0f;
        if (sourceCalibration.TryGetReferencePose(HumanoidBone.Hips, out var srcHips, out _) &&
            targetCalibration.TryGetReferencePose(HumanoidBone.Hips, out var tgtHips, out _))
        {
            srcHeight = Math.Abs(srcHips.Y);
            tgtHeight = Math.Abs(tgtHips.Y);
        }

        instance._positionScale = (srcHeight > 1e-4f && tgtHeight > 1e-4f) ? (tgtHeight / srcHeight) : 1.0f;

        // 2. Pre-populate default target rest poses for all target skeleton bones
        int tgtBoneCount = targetSkeleton.BoneCount;
        instance._defaultTargetPoses = new BonePose[tgtBoneCount];
        instance._targetPoseBuffer = new BonePose[tgtBoneCount];

        for (int i = 0; i < tgtBoneCount; i++)
        {
            var skelBone = targetSkeleton[i];
            instance._defaultTargetPoses[i] = new BonePose
            {
                Position = skelBone.LocalPosition,
                Rotation = skelBone.LocalRotation,
                Scale = skelBone.LocalScale
            };
        }

        // 3. Build active bindings between mapped source and target humanoid bones
        var bindingList = new List<RetargetBinding>();
        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            var hb = (HumanoidBone)i;
            if (sourceMapping.TryGet(hb, out int srcIdx) && targetMapping.TryGet(hb, out int tgtIdx))
            {
                var srcBone = sourceSkeleton[srcIdx];
                var tgtBone = targetSkeleton[tgtIdx];

                bindingList.Add(new RetargetBinding
                {
                    HumanoidBone = hb,
                    SourceSkeletonIndex = srcIdx,
                    TargetSkeletonIndex = tgtIdx,
                    SourceRefLocalRot = srcBone.LocalRotation,
                    TargetRefLocalRot = tgtBone.LocalRotation,
                    InvSourceRefLocalRot = Quaternion.Inverse(srcBone.LocalRotation),
                    SourceRefLocalPos = srcBone.LocalPosition,
                    TargetRefLocalPos = tgtBone.LocalPosition,
                    SourceRefLocalScale = srcBone.LocalScale,
                    TargetRefLocalScale = tgtBone.LocalScale,
                    IsRoot = (hb == HumanoidBone.Hips)
                });
            }
        }

        instance._bindings = bindingList.ToArray();
        retargeter = instance;
        return true;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  POSE RETARGETING (Hot Path - Zero Allocations)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retargets a pose buffer evaluated from the source skeleton to the target skeleton.
    /// Returns a reference to the internal pre-allocated target pose buffer.
    /// </summary>
    public bool TryRetargetPose(ReadOnlySpan<BonePose> sourcePoses, out BonePose[] targetPoses)
    {
        // 1. Initialize target poses with rest poses
        Array.Copy(_defaultTargetPoses, _targetPoseBuffer, _defaultTargetPoses.Length);

        // 2. Retarget each mapped humanoid bone
        for (int i = 0; i < _bindings.Length; i++)
        {
            ref readonly var b = ref _bindings[i];
            if (b.SourceSkeletonIndex >= sourcePoses.Length) continue;

            ref readonly var srcPose = ref sourcePoses[b.SourceSkeletonIndex];

            // Rotation retargeting: delta from source rest pose applied to target rest pose
            // delta = Inv(SourceRefRot) * SourceAnimRot
            // TargetAnimRot = TargetRefRot * delta
            Quaternion localDelta = b.InvSourceRefLocalRot * srcPose.Rotation;
            Quaternion targetRot = b.TargetRefLocalRot * localDelta;

            // Position retargeting (Hips/Root scaled by character proportion)
            Float3 targetPos;
            if (b.IsRoot)
            {
                Float3 posDelta = srcPose.Position - b.SourceRefLocalPos;
                targetPos = b.TargetRefLocalPos + (posDelta * _positionScale);
            }
            else
            {
                targetPos = b.TargetRefLocalPos;
            }

            // Scale retargeting
            Float3 targetScale = b.TargetRefLocalScale;
            if (Math.Abs(b.SourceRefLocalScale.X) > 1e-5f && Math.Abs(b.SourceRefLocalScale.Y) > 1e-5f && Math.Abs(b.SourceRefLocalScale.Z) > 1e-5f)
            {
                targetScale = new Float3(
                    b.TargetRefLocalScale.X * (srcPose.Scale.X / b.SourceRefLocalScale.X),
                    b.TargetRefLocalScale.Y * (srcPose.Scale.Y / b.SourceRefLocalScale.Y),
                    b.TargetRefLocalScale.Z * (srcPose.Scale.Z / b.SourceRefLocalScale.Z)
                );
            }

            _targetPoseBuffer[b.TargetSkeletonIndex] = new BonePose
            {
                Position = targetPos,
                Rotation = targetRot,
                Scale = targetScale
            };
        }

        targetPoses = _targetPoseBuffer;
        return true;
    }

    /// <summary>
    /// Retargets an array of source poses.
    /// </summary>
    public bool TryRetargetPose(BonePose[] sourcePoses, out BonePose[] targetPoses) =>
        TryRetargetPose(new ReadOnlySpan<BonePose>(sourcePoses), out targetPoses);

    // ════════════════════════════════════════════════════════════════════════
    //  ANIMATION CLIP RETARGETING
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retargets an entire <see cref="AnimationClip"/> from the source skeleton to a new clip
    /// suitable for playback on the target skeleton.
    /// </summary>
    public bool TryRetargetAnimation(AnimationClip sourceClip, out AnimationClip? targetClip)
    {
        targetClip = null;
        if (sourceClip == null || sourceClip.Duration <= 0f)
            return false;

        // 1. Collect all distinct keyframe timestamps from the source clip
        var timestampSet = new SortedSet<float> { 0f, sourceClip.Duration };
        foreach (var srcBone in sourceClip.Bones)
        {
            AddCurveTimestamps(srcBone.Position, timestampSet);
            AddCurveTimestamps(srcBone.Rotation, timestampSet);
            AddCurveTimestamps(srcBone.Scale, timestampSet);
        }

        var timestamps = new List<float>(timestampSet);
        if (timestamps.Count == 0)
            timestamps.Add(0f);

        int timeCount = timestamps.Count;
        int tgtBoneCount = _targetSkeleton.BoneCount;

        // 2. Prepare per-bone keyframe arrays for the target clip
        var posKeys = new List<Keyframe>[tgtBoneCount];
        var rotKeys = new List<Keyframe>[tgtBoneCount];
        var scaleKeys = new List<Keyframe>[tgtBoneCount];

        for (int i = 0; i < tgtBoneCount; i++)
        {
            posKeys[i] = new List<Keyframe>(timeCount);
            rotKeys[i] = new List<Keyframe>(timeCount);
            scaleKeys[i] = new List<Keyframe>(timeCount);
        }

        // 3. Sample and retarget at each timestamp
        for (int t = 0; t < timeCount; t++)
        {
            float time = timestamps[t];
            _sampler.Evaluate(sourceClip, time, out var sampledSourcePoses);
            TryRetargetPose(sampledSourcePoses, out var retargetedPoses);

            for (int b = 0; b < tgtBoneCount; b++)
            {
                var p = retargetedPoses[b];
                posKeys[b].Add(new Keyframe(time, p.Position));
                rotKeys[b].Add(new Keyframe(time, new Float4(p.Rotation.X, p.Rotation.Y, p.Rotation.Z, p.Rotation.W)));
                scaleKeys[b].Add(new Keyframe(time, p.Scale));
            }
        }

        // 4. Construct target AnimationClip
        var resultClip = new AnimationClip
        {
            Name = $"{sourceClip.Name}_Retargeted",
            StartTime = sourceClip.StartTime,
            Duration = sourceClip.Duration,
            TicksPerSecond = sourceClip.TicksPerSecond,
            DurationInTicks = sourceClip.DurationInTicks,
            Wrap = sourceClip.Wrap
        };

        for (int b = 0; b < tgtBoneCount; b++)
        {
            string bonePath = GetSkeletonBonePath(_targetSkeleton, b);
            var animBone = new AnimationClip.AnimBone
            {
                BoneName = bonePath,
                Position = new Vector.AnimationCurve(3, posKeys[b].ToArray()),
                Rotation = new Vector.AnimationCurve(4, rotKeys[b].ToArray()),
                Scale = new Vector.AnimationCurve(3, scaleKeys[b].ToArray())
            };
            resultClip.AddBone(animBone);
        }

        resultClip.EnsureQuaternionContinuity();
        targetClip = resultClip;
        return true;
    }

    private static void AddCurveTimestamps(Vector.AnimationCurve? curve, SortedSet<float> timestamps)
    {
        if (curve == null || curve.Count == 0) return;
        for (int i = 0; i < curve.Count; i++)
            timestamps.Add(curve[i].Time);
    }

    /// <summary>
    /// Builds the hierarchical path (e.g. "Armature/Hips/Spine") for a bone in the skeleton.
    /// </summary>
    public static string GetSkeletonBonePath(SkeletonAsset skeleton, int boneIndex)
    {
        if (boneIndex < 0 || boneIndex >= skeleton.BoneCount) return string.Empty;
        var segments = new List<string>();
        int curr = boneIndex;
        while (curr >= 0)
        {
            segments.Add(skeleton[curr].Name);
            curr = skeleton.GetParentIndex(curr);
        }
        segments.Reverse();
        return string.Join("/", segments);
    }
}
