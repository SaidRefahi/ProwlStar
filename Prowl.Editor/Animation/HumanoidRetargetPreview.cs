// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Prowl.Runtime;
using Prowl.Runtime.Animation;
using Prowl.Vector;

namespace Prowl.Editor.Animation;

/// <summary>
/// Editor utility for previewing animation retargeting between two humanoid rigs.
/// </summary>
public sealed class HumanoidRetargetPreview
{
    private AnimationRetargeter? _retargeter;
    private readonly AnimationSampler _sampler = new();
    private BonePose[] _sourceClipPoses = Array.Empty<BonePose>();
    private BonePose[] _sourceSkeletonPoses = Array.Empty<BonePose>();
    private ClipBoneBinding[] _sourceClipBindings = Array.Empty<ClipBoneBinding>();
    private int _sourceClipBindingCount;

    /// <summary>Active retargeter instance.</summary>
    public AnimationRetargeter? Retargeter => _retargeter;

    /// <summary>Whether the preview is successfully configured and ready to sample.</summary>
    public bool IsValid => _retargeter != null;

    /// <summary>
    /// Configures the retarget preview from source and target rigs.
    /// </summary>
    public bool Setup(
        SkeletonAsset sourceSkeleton,
        HumanoidMapping sourceMapping,
        SkeletonAsset targetSkeleton,
        HumanoidMapping targetMapping,
        out List<string> errors)
    {
        errors = new List<string>();
        _retargeter = null;

        if (!HumanoidCalibration.Build(sourceSkeleton, sourceMapping, out var srcCal, out var srcErrors))
        {
            errors.AddRange(srcErrors);
            return false;
        }

        if (!HumanoidCalibration.Build(targetSkeleton, targetMapping, out var tgtCal, out var tgtErrors))
        {
            errors.AddRange(tgtErrors);
            return false;
        }

        if (!AnimationRetargeter.Build(sourceSkeleton, sourceMapping, srcCal, targetSkeleton, targetMapping, tgtCal, out _retargeter, out var retargetErrors))
        {
            errors.AddRange(retargetErrors);
            return false;
        }

        _sourceSkeletonPoses = new BonePose[sourceSkeleton.BoneCount];
        return true;
    }

    /// <summary>
    /// Evaluates the source clip at <paramref name="time"/>, retargets to target skeleton, and outputs target poses.
    /// </summary>
    public bool SampleRetargetedPose(AnimationClip sourceClip, float time, out BonePose[] targetPoses)
    {
        targetPoses = Array.Empty<BonePose>();
        if (_retargeter == null || sourceClip == null) return false;

        // 1. Evaluate raw clip curves
        _sampler.Evaluate(sourceClip, time, out _sourceClipPoses);

        // 2. Map clip bones to source skeleton
        EnsureClipBindings(sourceClip, _retargeter.SourceSkeleton);

        // 3. Fill source skeleton buffer with rest pose, then overwrite with clip curves
        for (int i = 0; i < _sourceSkeletonPoses.Length; i++)
        {
            var bone = _retargeter.SourceSkeleton[i];
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
            _sourceSkeletonPoses[b.SkeletonBoneIndex] = _sourceClipPoses[b.ClipBoneIndex];
        }

        // 4. Retarget to target skeleton
        return _retargeter.TryRetargetPose(_sourceSkeletonPoses, out targetPoses);
    }

    private void EnsureClipBindings(AnimationClip clip, SkeletonAsset sourceSkeleton)
    {
        if (_sourceClipBindings.Length < clip.Bones.Count)
            _sourceClipBindings = new ClipBoneBinding[clip.Bones.Count];

        _sourceClipBindingCount = 0;
        for (int i = 0; i < clip.Bones.Count; i++)
        {
            int skelIdx = AnimationComponent.ResolveClipBoneToSkeleton(clip.Bones[i].BoneName, sourceSkeleton);
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
}
