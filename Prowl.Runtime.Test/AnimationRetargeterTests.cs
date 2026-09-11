// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Prowl.Echo;
using Prowl.Runtime.Animation;
using Prowl.Vector;
using Xunit;

namespace Prowl.Runtime.Test;

public class AnimationRetargeterTests : RuntimeTestBase
{
    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    private static (SkeletonAsset, HumanoidMapping, HumanoidCalibration) CreateRig(string name, float heightScale = 1.0f)
    {
        var skel = new SkeletonAsset { Name = name };

        // 0: Hips
        skel.AddBone(new SkeletonBone("Hips", -1, new Float3(0, 1.0f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));

        // Spine chain (1-5)
        skel.AddBone(new SkeletonBone("Spine", 0, new Float3(0, 0.2f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));       // 1
        skel.AddBone(new SkeletonBone("Chest", 1, new Float3(0, 0.2f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));       // 2
        skel.AddBone(new SkeletonBone("UpperChest", 2, new Float3(0, 0.2f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));  // 3
        skel.AddBone(new SkeletonBone("Neck", 3, new Float3(0, 0.1f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));        // 4
        skel.AddBone(new SkeletonBone("Head", 4, new Float3(0, 0.2f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));        // 5

        // Left Arm chain (6-9)
        skel.AddBone(new SkeletonBone("LeftShoulder", 2, new Float3(-0.1f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 6
        skel.AddBone(new SkeletonBone("LeftUpperArm", 6, new Float3(-0.2f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 7
        skel.AddBone(new SkeletonBone("LeftLowerArm", 7, new Float3(-0.25f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 8
        skel.AddBone(new SkeletonBone("LeftHand", 8, new Float3(-0.15f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 9

        // Right Arm chain (10-13)
        skel.AddBone(new SkeletonBone("RightShoulder", 2, new Float3(0.1f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 10
        skel.AddBone(new SkeletonBone("RightUpperArm", 10, new Float3(0.2f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 11
        skel.AddBone(new SkeletonBone("RightLowerArm", 11, new Float3(0.25f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 12
        skel.AddBone(new SkeletonBone("RightHand", 12, new Float3(0.15f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 13

        // Left Leg chain (14-17)
        skel.AddBone(new SkeletonBone("LeftUpperLeg", 0, new Float3(-0.1f * heightScale, -0.4f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 14
        skel.AddBone(new SkeletonBone("LeftLowerLeg", 14, new Float3(0, -0.4f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 15
        skel.AddBone(new SkeletonBone("LeftFoot", 15, new Float3(0, -0.1f * heightScale, 0.1f * heightScale), Quaternion.Identity, Float3.One, Float4x4.Identity));     // 16
        skel.AddBone(new SkeletonBone("LeftToes", 16, new Float3(0, 0, 0.1f * heightScale), Quaternion.Identity, Float3.One, Float4x4.Identity));         // 17

        // Right Leg chain (18-21)
        skel.AddBone(new SkeletonBone("RightUpperLeg", 0, new Float3(0.1f * heightScale, -0.4f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 18
        skel.AddBone(new SkeletonBone("RightLowerLeg", 18, new Float3(0, -0.4f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 19
        skel.AddBone(new SkeletonBone("RightFoot", 19, new Float3(0, -0.1f * heightScale, 0.1f * heightScale), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 20
        skel.AddBone(new SkeletonBone("RightToes", 20, new Float3(0, 0, 0.1f * heightScale), Quaternion.Identity, Float3.One, Float4x4.Identity));        // 21

        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skel) };
        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
            mapping.Set((HumanoidBone)i, i);

        HumanoidCalibration.Build(skel, mapping, out var calibration, out _);

        return (skel, mapping, calibration!);
    }

    private static BonePose[] CreateDefaultPoses(SkeletonAsset skeleton)
    {
        var poses = new BonePose[skeleton.BoneCount];
        for (int i = 0; i < skeleton.BoneCount; i++)
        {
            poses[i] = new BonePose
            {
                Position = skeleton[i].LocalPosition,
                Rotation = skeleton[i].LocalRotation,
                Scale = skeleton[i].LocalScale
            };
        }
        return poses;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pose Retargeting Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RetargetPose_IdenticalRigs_YieldsIdenticalPose()
    {
        var (srcSkel, srcMap, srcCal) = CreateRig("Source");
        var (tgtSkel, tgtMap, tgtCal) = CreateRig("Target");

        Assert.True(AnimationRetargeter.Build(srcSkel, srcMap, srcCal, tgtSkel, tgtMap, tgtCal, out var retargeter, out var errors), string.Join("; ", errors));

        var sourcePoses = CreateDefaultPoses(srcSkel);

        // Animate source arm rotation
        var armRot = Quaternion.AngleAxis(45f, Float3.UnitZ);
        sourcePoses[7].Rotation = armRot; // LeftUpperArm

        Assert.True(retargeter!.TryRetargetPose(sourcePoses, out var targetPoses));

        // Target LeftUpperArm should receive the exact rotation
        Assert.Equal(armRot, targetPoses[7].Rotation);

        // Target Hips position should match
        Assert.Equal(sourcePoses[0].Position, targetPoses[0].Position);
    }

    [Fact]
    public void RetargetPose_DifferentProportions_ScalesHipsPosition()
    {
        var (srcSkel, srcMap, srcCal) = CreateRig("Source", heightScale: 1.0f);
        var (tgtSkel, tgtMap, tgtCal) = CreateRig("Target", heightScale: 2.0f); // Target is 2x taller

        Assert.True(AnimationRetargeter.Build(srcSkel, srcMap, srcCal, tgtSkel, tgtMap, tgtCal, out var retargeter, out _));

        Assert.Equal(2.0f, retargeter!.PositionScale, 3);

        var sourcePoses = CreateDefaultPoses(srcSkel);
        // Move source hips up by +0.5
        sourcePoses[0].Position = new Float3(0, 1.5f, 0); // delta = +0.5

        Assert.True(retargeter.TryRetargetPose(sourcePoses, out var targetPoses));

        // Target rest hips = (0, 2.0, 0), delta scaled = +0.5 * 2.0 = +1.0 -> target = (0, 3.0, 0)
        Assert.Equal(new Float3(0, 3.0f, 0), targetPoses[0].Position);
    }

    [Fact]
    public void RetargetPose_RelativeRotationTransferred()
    {
        var (srcSkel, srcMap, srcCal) = CreateRig("Source");
        var (tgtSkel, tgtMap, tgtCal) = CreateRig("Target");

        Assert.True(AnimationRetargeter.Build(srcSkel, srcMap, srcCal, tgtSkel, tgtMap, tgtCal, out var retargeter, out _));

        var sourcePoses = CreateDefaultPoses(srcSkel);
        var deltaRot = Quaternion.AngleAxis(30f, Float3.UnitX);
        sourcePoses[1].Rotation = deltaRot; // Spine

        Assert.True(retargeter!.TryRetargetPose(sourcePoses, out var targetPoses));

        Assert.Equal(deltaRot, targetPoses[1].Rotation);
    }

    [Fact]
    public void RetargetPose_DifferentReferencePoses_MaintainsRelativeMotion()
    {
        var (srcSkel, srcMap, srcCal) = CreateRig("Source");

        // Target skeleton has Head tilted 15 degrees in rest pose
        var targetTilt = Quaternion.AngleAxis(15f, Float3.UnitX);
        var (tgtSkel, tgtMap, _) = CreateRig("Target");
        tgtSkel[5] = new SkeletonBone("Head", 4, tgtSkel[5].LocalPosition, targetTilt, Float3.One, Float4x4.Identity);
        HumanoidCalibration.Build(tgtSkel, tgtMap, out var tgtCal, out _);

        Assert.True(AnimationRetargeter.Build(srcSkel, srcMap, srcCal, tgtSkel, tgtMap, tgtCal!, out var retargeter, out _));

        var sourcePoses = CreateDefaultPoses(srcSkel);

        // Case 1: Source in rest pose (Identity) -> Target must be in its rest pose (targetTilt)
        Assert.True(retargeter!.TryRetargetPose(sourcePoses, out var targetPoses1));
        Assert.Equal(targetTilt, targetPoses1[5].Rotation);

        // Case 2: Source adds 20 degrees rotation -> Target gets targetTilt * 20deg
        var animRot = Quaternion.AngleAxis(20f, Float3.UnitY);
        sourcePoses[5].Rotation = animRot;

        Assert.True(retargeter.TryRetargetPose(sourcePoses, out var targetPoses2));
        var expectedRot = targetTilt * animRot;
        Assert.Equal(expectedRot, targetPoses2[5].Rotation);
    }

    [Fact]
    public void RetargetPose_MissingOptionalBones_HandledGracefully()
    {
        var (srcSkel, srcMap, srcCal) = CreateRig("FullSource");

        // Target has no Shoulders, UpperChest, or Toes
        var tgtSkel = new SkeletonAsset { Name = "CompactTarget" };
        tgtSkel.AddBone(new SkeletonBone("Hips", -1, new Float3(0, 1.0f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 0
        tgtSkel.AddBone(new SkeletonBone("Spine", 0, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));  // 1
        tgtSkel.AddBone(new SkeletonBone("Chest", 1, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));  // 2
        tgtSkel.AddBone(new SkeletonBone("Neck", 2, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 3
        tgtSkel.AddBone(new SkeletonBone("Head", 3, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 4
        tgtSkel.AddBone(new SkeletonBone("LeftUpperArm", 2, new Float3(-0.3f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 5
        tgtSkel.AddBone(new SkeletonBone("LeftLowerArm", 5, new Float3(-0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 6
        tgtSkel.AddBone(new SkeletonBone("LeftHand", 6, new Float3(-0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 7
        tgtSkel.AddBone(new SkeletonBone("RightUpperArm", 2, new Float3(0.3f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 8
        tgtSkel.AddBone(new SkeletonBone("RightLowerArm", 8, new Float3(0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 9
        tgtSkel.AddBone(new SkeletonBone("RightHand", 9, new Float3(0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 10
        tgtSkel.AddBone(new SkeletonBone("LeftUpperLeg", 0, new Float3(-0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 11
        tgtSkel.AddBone(new SkeletonBone("LeftLowerLeg", 11, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 12
        tgtSkel.AddBone(new SkeletonBone("LeftFoot", 12, new Float3(0, -0.1f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));        // 13
        tgtSkel.AddBone(new SkeletonBone("RightUpperLeg", 0, new Float3(0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 14
        tgtSkel.AddBone(new SkeletonBone("RightLowerLeg", 14, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 15
        tgtSkel.AddBone(new SkeletonBone("RightFoot", 15, new Float3(0, -0.1f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));       // 16

        var tgtMap = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(tgtSkel) };
        tgtMap[HumanoidBone.Hips] = 0;
        tgtMap[HumanoidBone.Spine] = 1;
        tgtMap[HumanoidBone.Chest] = 2;
        tgtMap[HumanoidBone.Neck] = 3;
        tgtMap[HumanoidBone.Head] = 4;
        tgtMap[HumanoidBone.LeftUpperArm] = 5;
        tgtMap[HumanoidBone.LeftLowerArm] = 6;
        tgtMap[HumanoidBone.LeftHand] = 7;
        tgtMap[HumanoidBone.RightUpperArm] = 8;
        tgtMap[HumanoidBone.RightLowerArm] = 9;
        tgtMap[HumanoidBone.RightHand] = 10;
        tgtMap[HumanoidBone.LeftUpperLeg] = 11;
        tgtMap[HumanoidBone.LeftLowerLeg] = 12;
        tgtMap[HumanoidBone.LeftFoot] = 13;
        tgtMap[HumanoidBone.RightUpperLeg] = 14;
        tgtMap[HumanoidBone.RightLowerLeg] = 15;
        tgtMap[HumanoidBone.RightFoot] = 16;

        HumanoidCalibration.Build(tgtSkel, tgtMap, out var tgtCal, out _);

        Assert.True(AnimationRetargeter.Build(srcSkel, srcMap, srcCal, tgtSkel, tgtMap, tgtCal!, out var retargeter, out _));

        var sourcePoses = CreateDefaultPoses(srcSkel);
        var armRot = Quaternion.AngleAxis(45f, Float3.UnitZ);
        sourcePoses[7].Rotation = armRot; // LeftUpperArm on source (index 7)

        Assert.True(retargeter!.TryRetargetPose(sourcePoses, out var targetPoses));

        // Target LeftUpperArm is at index 5 on tgtSkel
        Assert.Equal(armRot, targetPoses[5].Rotation);
    }

    [Fact]
    public void RetargetPose_InvalidInputs_ReturnsFalse()
    {
        var (srcSkel, srcMap, srcCal) = CreateRig("Source");
        var (tgtSkel, tgtMap, tgtCal) = CreateRig("Target");

        Assert.False(AnimationRetargeter.Build(null, srcMap, srcCal, tgtSkel, tgtMap, tgtCal, out _, out var errors));
        Assert.NotEmpty(errors);

        Assert.False(AnimationRetargeter.Build(srcSkel, null, srcCal, tgtSkel, tgtMap, tgtCal, out _, out errors));
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void RetargetPose_ZeroAllocationsInHotPath()
    {
        var (srcSkel, srcMap, srcCal) = CreateRig("Source");
        var (tgtSkel, tgtMap, tgtCal) = CreateRig("Target");

        Assert.True(AnimationRetargeter.Build(srcSkel, srcMap, srcCal, tgtSkel, tgtMap, tgtCal, out var retargeter, out _));

        var sourcePoses = CreateDefaultPoses(srcSkel);

        // Warm up
        retargeter!.TryRetargetPose(sourcePoses, out var poseBuffer1);

        // Repeated call should return same buffer reference
        retargeter.TryRetargetPose(sourcePoses, out var poseBuffer2);
        Assert.Same(poseBuffer1, poseBuffer2);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Animation Clip Retargeting Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RetargetAnimation_PreservesTimingAndKeyframes()
    {
        var (srcSkel, srcMap, srcCal) = CreateRig("Source", heightScale: 1.0f);
        var (tgtSkel, tgtMap, tgtCal) = CreateRig("Target", heightScale: 1.5f);

        Assert.True(AnimationRetargeter.Build(srcSkel, srcMap, srcCal, tgtSkel, tgtMap, tgtCal, out var retargeter, out _));

        // Create a simple source clip
        var clip = new AnimationClip
        {
            Name = "Walk",
            Duration = 2.0f,
            TicksPerSecond = 30f,
            DurationInTicks = 60f,
            Wrap = AnimationWrapMode.Loop
        };

        for (int i = 0; i < srcSkel.BoneCount; i++)
        {
            string bonePath = AnimationRetargeter.GetSkeletonBonePath(srcSkel, i);
            clip.AddBone(new AnimationClip.AnimBone
            {
                BoneName = bonePath,
                Position = new Vector.AnimationCurve(3,
                    new Keyframe(0f, srcSkel[i].LocalPosition),
                    new Keyframe(2.0f, srcSkel[i].LocalPosition)),
                Rotation = new Vector.AnimationCurve(4,
                    new Keyframe(0f, new Float4(0, 0, 0, 1)),
                    new Keyframe(2.0f, new Float4(0, 0, 0, 1))),
                Scale = new Vector.AnimationCurve(3,
                    new Keyframe(0f, Float3.One),
                    new Keyframe(2.0f, Float3.One))
            });
        }

        Assert.True(retargeter!.TryRetargetAnimation(clip, out var targetClip));
        Assert.NotNull(targetClip);

        Assert.Equal(clip.Duration, targetClip!.Duration);
        Assert.Equal(clip.Wrap, targetClip.Wrap);
        Assert.Equal(clip.TicksPerSecond, targetClip.TicksPerSecond);
        Assert.Equal(tgtSkel.BoneCount, targetClip.Bones.Count);

        // Hips position at t=0 should be scaled for target
        var targetHips = targetClip.GetBone("Hips");
        Assert.NotNull(targetHips);
        Assert.Equal(1.5f, targetHips!.EvaluatePositionAt(0f).Y, 3);
    }
}
