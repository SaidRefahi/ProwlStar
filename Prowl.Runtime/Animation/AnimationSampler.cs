// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

namespace Prowl.Runtime.Animation;

/// <summary>
/// Runtime-reusable sampler that evaluates an <see cref="AnimationClip"/> at a given time
/// and produces a buffer of <see cref="BonePose"/> structs without touching Transforms.
/// <para>
/// This is a plain class (not a MonoBehaviour) designed to be owned by any consumer that
/// needs to sample animation data: <see cref="AnimationComponent"/>, future retargeters,
/// editor preview tools, etc.
/// </para>
/// <para>
/// Each instance maintains its own reusable buffer so multiple consumers can sample
/// concurrently without sharing state. The buffer only grows, never shrinks, avoiding
/// per-frame allocations.
/// </para>
/// </summary>
public sealed class AnimationSampler
{
    private BonePose[] _poseBuffer = Array.Empty<BonePose>();

    /// <summary>
    /// Evaluates every bone curve in <paramref name="clip"/> at <paramref name="time"/> and
    /// writes the results into the internal buffer. The caller receives a reference to that
    /// buffer via <paramref name="poses"/>; treat it as read-only until the next Evaluate call.
    /// </summary>
    public void Evaluate(AnimationClip clip, float time, out BonePose[] poses)
    {
        int boneCount = clip.Bones.Count;

        if (_poseBuffer.Length < boneCount)
            _poseBuffer = new BonePose[boneCount];

        for (int i = 0; i < boneCount; i++)
        {
            var animBone = clip.Bones[i];
            _poseBuffer[i] = new BonePose
            {
                Position = animBone.EvaluatePositionAt(time),
                Rotation = animBone.EvaluateRotationAt(time),
                Scale    = animBone.EvaluateScaleAt(time)
            };
        }

        poses = _poseBuffer;
    }
}
