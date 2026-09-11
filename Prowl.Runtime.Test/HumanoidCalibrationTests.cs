// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using Prowl.Echo;
using Prowl.Vector;
using Xunit;

namespace Prowl.Runtime.Test;

public class HumanoidCalibrationTests : RuntimeTestBase
{
    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    private static SkeletonAsset CreateFullHumanoidSkeleton()
    {
        var skel = new SkeletonAsset { Name = "CalibrationRig" };

        // 0: Hips (Root)
        skel.AddBone(new SkeletonBone("Hips", -1, new Float3(0, 1.0f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));

        // Spine chain (1-5)
        skel.AddBone(new SkeletonBone("Spine", 0, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));       // 1
        skel.AddBone(new SkeletonBone("Chest", 1, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));       // 2
        skel.AddBone(new SkeletonBone("UpperChest", 2, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));  // 3
        skel.AddBone(new SkeletonBone("Neck", 3, new Float3(0, 0.1f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));        // 4
        skel.AddBone(new SkeletonBone("Head", 4, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));        // 5

        // Left Arm chain (6-9)
        skel.AddBone(new SkeletonBone("LeftShoulder", 2, new Float3(-0.1f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 6
        skel.AddBone(new SkeletonBone("LeftUpperArm", 6, new Float3(-0.2f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 7
        skel.AddBone(new SkeletonBone("LeftLowerArm", 7, new Float3(-0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 8
        skel.AddBone(new SkeletonBone("LeftHand", 8, new Float3(-0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 9

        // Right Arm chain (10-13)
        skel.AddBone(new SkeletonBone("RightShoulder", 2, new Float3(0.1f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 10
        skel.AddBone(new SkeletonBone("RightUpperArm", 10, new Float3(0.2f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 11
        skel.AddBone(new SkeletonBone("RightLowerArm", 11, new Float3(0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 12
        skel.AddBone(new SkeletonBone("RightHand", 12, new Float3(0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 13

        // Left Leg chain (14-17)
        skel.AddBone(new SkeletonBone("LeftUpperLeg", 0, new Float3(-0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 14
        skel.AddBone(new SkeletonBone("LeftLowerLeg", 14, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 15
        skel.AddBone(new SkeletonBone("LeftFoot", 15, new Float3(0, -0.1f, 0.1f), Quaternion.Identity, Float3.One, Float4x4.Identity));     // 16
        skel.AddBone(new SkeletonBone("LeftToes", 16, new Float3(0, 0, 0.1f), Quaternion.Identity, Float3.One, Float4x4.Identity));         // 17

        // Right Leg chain (18-21)
        skel.AddBone(new SkeletonBone("RightUpperLeg", 0, new Float3(0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 18
        skel.AddBone(new SkeletonBone("RightLowerLeg", 18, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 19
        skel.AddBone(new SkeletonBone("RightFoot", 19, new Float3(0, -0.1f, 0.1f), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 20
        skel.AddBone(new SkeletonBone("RightToes", 20, new Float3(0, 0, 0.1f), Quaternion.Identity, Float3.One, Float4x4.Identity));        // 21

        return skel;
    }

    private static HumanoidMapping CreateFullValidMapping(SkeletonAsset skeleton)
    {
        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skeleton) };
        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            mapping.Set((HumanoidBone)i, i);
        }
        return mapping;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Build and Pose Extraction Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Build_ValidHumanoid_Succeeds()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        bool success = HumanoidCalibration.Build(skeleton, mapping, out var calibration, out var errors);

        Assert.True(success, string.Join("; ", errors));
        Assert.NotNull(calibration);
    }

    [Fact]
    public void Build_ExtractsAccurateModelSpacePositions()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        Assert.True(HumanoidCalibration.Build(skeleton, mapping, out var cal, out _));

        // Hips at (0, 1.0, 0)
        Assert.True(cal!.TryGetReferencePose(HumanoidBone.Hips, out var hipsPos, out _));
        AssertClose(new Float3(0, 1.0f, 0), hipsPos);

        // Spine at (0, 1.0 + 0.2 = 1.2, 0)
        Assert.True(cal.TryGetReferencePose(HumanoidBone.Spine, out var spinePos, out _));
        AssertClose(new Float3(0, 1.2f, 0), spinePos);

        // Chest at (0, 1.4, 0)
        Assert.True(cal.TryGetReferencePose(HumanoidBone.Chest, out var chestPos, out _));
        AssertClose(new Float3(0, 1.4f, 0), chestPos);

        // UpperChest at (0, 1.6, 0)
        Assert.True(cal.TryGetReferencePose(HumanoidBone.UpperChest, out var upperChestPos, out _));
        AssertClose(new Float3(0, 1.6f, 0), upperChestPos);

        // Neck at (0, 1.7, 0)
        Assert.True(cal.TryGetReferencePose(HumanoidBone.Neck, out var neckPos, out _));
        AssertClose(new Float3(0, 1.7f, 0), neckPos);

        // Head at (0, 1.9, 0)
        Assert.True(cal.TryGetReferencePose(HumanoidBone.Head, out var headPos, out _));
        AssertClose(new Float3(0, 1.9f, 0), headPos);
    }

    private static void AssertClose(Float3 expected, Float3 actual, int precision = 4)
    {
        Assert.Equal(expected.X, actual.X, precision);
        Assert.Equal(expected.Y, actual.Y, precision);
        Assert.Equal(expected.Z, actual.Z, precision);
    }

    [Fact]
    public void Build_ComputesAccurateSegmentLengths()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        Assert.True(HumanoidCalibration.Build(skeleton, mapping, out var cal, out _));

        // Hips -> Spine length = 0.2
        Assert.True(cal!.TryGetBoneLength(HumanoidBone.Hips, out float hipsLen));
        Assert.Equal(0.2f, hipsLen, 4);

        // LeftUpperArm -> LeftLowerArm length = 0.25
        Assert.True(cal.TryGetBoneLength(HumanoidBone.LeftUpperArm, out float upperArmLen));
        Assert.Equal(0.25f, upperArmLen, 4);

        // LeftLowerArm -> LeftHand length = 0.15
        Assert.True(cal.TryGetBoneLength(HumanoidBone.LeftLowerArm, out float lowerArmLen));
        Assert.Equal(0.15f, lowerArmLen, 4);

        // Head (leaf) length = 0
        Assert.True(cal.TryGetBoneLength(HumanoidBone.Head, out float headLen));
        Assert.Equal(0f, headLen);
    }

    [Fact]
    public void Build_ComputesNormalizedDirections()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        Assert.True(HumanoidCalibration.Build(skeleton, mapping, out var cal, out _));

        // Spine chain points straight UP: (0, 1, 0)
        Assert.True(cal!.TryGetBoneDirection(HumanoidBone.Hips, out var hipsDir));
        Assert.Equal(new Float3(0, 1, 0), hipsDir);
        Assert.Equal(1f, Float3.Length(hipsDir), 4);

        // Left arm points along -X: (-1, 0, 0)
        Assert.True(cal.TryGetBoneDirection(HumanoidBone.LeftUpperArm, out var leftArmDir));
        Assert.Equal(new Float3(-1, 0, 0), leftArmDir);
        Assert.Equal(1f, Float3.Length(leftArmDir), 4);

        // Right arm points along +X: (1, 0, 0)
        Assert.True(cal.TryGetBoneDirection(HumanoidBone.RightUpperArm, out var rightArmDir));
        Assert.Equal(new Float3(1, 0, 0), rightArmDir);
        Assert.Equal(1f, Float3.Length(rightArmDir), 4);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Optional Bones Missing
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Build_WithoutOptionalBones_ConnectsDirectly()
    {
        var skel = new SkeletonAsset { Name = "CompactRig" };
        skel.AddBone(new SkeletonBone("Hips", -1, new Float3(0, 1.0f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 0
        skel.AddBone(new SkeletonBone("Spine", 0, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));  // 1
        skel.AddBone(new SkeletonBone("Chest", 1, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));  // 2
        skel.AddBone(new SkeletonBone("Neck", 2, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 3 (no UpperChest)
        skel.AddBone(new SkeletonBone("Head", 3, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 4

        skel.AddBone(new SkeletonBone("LeftUpperArm", 2, new Float3(-0.3f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 5 (no Shoulder)
        skel.AddBone(new SkeletonBone("LeftLowerArm", 5, new Float3(-0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 6
        skel.AddBone(new SkeletonBone("LeftHand", 6, new Float3(-0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 7

        skel.AddBone(new SkeletonBone("RightUpperArm", 2, new Float3(0.3f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 8 (no Shoulder)
        skel.AddBone(new SkeletonBone("RightLowerArm", 8, new Float3(0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 9
        skel.AddBone(new SkeletonBone("RightHand", 9, new Float3(0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 10

        skel.AddBone(new SkeletonBone("LeftUpperLeg", 0, new Float3(-0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 11
        skel.AddBone(new SkeletonBone("LeftLowerLeg", 11, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 12
        skel.AddBone(new SkeletonBone("LeftFoot", 12, new Float3(0, -0.1f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));        // 13 (no Toes)

        skel.AddBone(new SkeletonBone("RightUpperLeg", 0, new Float3(0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 14
        skel.AddBone(new SkeletonBone("RightLowerLeg", 14, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 15
        skel.AddBone(new SkeletonBone("RightFoot", 15, new Float3(0, -0.1f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));       // 16 (no Toes)

        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skel) };
        mapping[HumanoidBone.Hips] = 0;
        mapping[HumanoidBone.Spine] = 1;
        mapping[HumanoidBone.Chest] = 2;
        mapping[HumanoidBone.Neck] = 3;
        mapping[HumanoidBone.Head] = 4;
        mapping[HumanoidBone.LeftUpperArm] = 5;
        mapping[HumanoidBone.LeftLowerArm] = 6;
        mapping[HumanoidBone.LeftHand] = 7;
        mapping[HumanoidBone.RightUpperArm] = 8;
        mapping[HumanoidBone.RightLowerArm] = 9;
        mapping[HumanoidBone.RightHand] = 10;
        mapping[HumanoidBone.LeftUpperLeg] = 11;
        mapping[HumanoidBone.LeftLowerLeg] = 12;
        mapping[HumanoidBone.LeftFoot] = 13;
        mapping[HumanoidBone.RightUpperLeg] = 14;
        mapping[HumanoidBone.RightLowerLeg] = 15;
        mapping[HumanoidBone.RightFoot] = 16;

        bool success = HumanoidCalibration.Build(skel, mapping, out var cal, out var errors);
        Assert.True(success, string.Join("; ", errors));

        // Chest -> Neck directly (length = 0.2, dir = (0, 1, 0))
        Assert.True(cal!.TryGetBoneLength(HumanoidBone.Chest, out float chestLen));
        Assert.Equal(0.2f, chestLen, 4);

        // Foot has length 0 (since no toes)
        Assert.True(cal.TryGetBoneLength(HumanoidBone.LeftFoot, out float footLen));
        Assert.Equal(0f, footLen);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Degenerate and Error Case Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Build_DegenerateZeroLengthSegment_FailsValidation()
    {
        var skel = new SkeletonAsset { Name = "DegenerateRig" };
        // Hips and Spine at identical positions (0, 0, 0)
        skel.AddBone(new SkeletonBone("Hips", -1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("Spine", 0, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity)); // Length = 0!
        skel.AddBone(new SkeletonBone("Chest", 1, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("Neck", 2, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("Head", 3, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftUpperArm", 2, new Float3(-0.3f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftLowerArm", 5, new Float3(-0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftHand", 6, new Float3(-0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightUpperArm", 2, new Float3(0.3f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightLowerArm", 8, new Float3(0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightHand", 9, new Float3(0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftUpperLeg", 0, new Float3(-0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftLowerLeg", 11, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftFoot", 12, new Float3(0, -0.1f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightUpperLeg", 0, new Float3(0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightLowerLeg", 14, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightFoot", 15, new Float3(0, -0.1f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));

        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skel) };
        for (int i = 0; i < 17; i++)
        {
            var bone = (HumanoidBone)(i < 3 ? i : (i < 5 ? i + 1 : (i < 8 ? i + 2 : (i < 11 ? i + 3 : (i < 14 ? i + 3 : i + 4)))));
            mapping[bone] = i;
        }

        bool success = HumanoidCalibration.Build(skel, mapping, out var cal, out var errors);
        Assert.False(success);
        Assert.Contains(errors, e => e.Contains("Degenerate zero-length segment"));
    }

    [Fact]
    public void Build_InvalidMapping_Fails()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skeleton) };
        // Empty mapping

        bool success = HumanoidCalibration.Build(skeleton, mapping, out _, out var errors);
        Assert.False(success);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Build_NonFiniteData_Fails()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        // Inject NaN position
        skeleton[0] = new SkeletonBone("Hips", -1, new Float3(float.NaN, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity);
        var mapping = CreateFullValidMapping(skeleton);

        bool success = HumanoidCalibration.Build(skeleton, mapping, out _, out var errors);
        Assert.False(success);
        Assert.Contains(errors, e => e.Contains("Non-finite"));
    }

    [Fact]
    public void Build_DoesNotModifySkeletonAsset()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        // Capture skeleton rest poses before
        var positionsBefore = new Float3[skeleton.BoneCount];
        for (int i = 0; i < skeleton.BoneCount; i++)
            positionsBefore[i] = skeleton[i].LocalPosition;

        HumanoidCalibration.Build(skeleton, mapping, out _, out _);

        // Verify skeleton rest poses unchanged
        for (int i = 0; i < skeleton.BoneCount; i++)
            Assert.Equal(positionsBefore[i], skeleton[i].LocalPosition);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Serialization Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Serialization_RoundTrip_PreservesCalibrationData()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        Assert.True(HumanoidCalibration.Build(skeleton, mapping, out var cal, out _));
        cal!.Name = "CalibratedHumanoid";

        var serialized = Serializer.Serialize(cal);
        var deserialized = Serializer.Deserialize<HumanoidCalibration>(serialized);

        Assert.NotNull(deserialized);
        Assert.Equal("CalibratedHumanoid", deserialized.Name);

        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            var bone = (HumanoidBone)i;
            var orig = cal[bone];
            var clone = deserialized[bone];

            Assert.Equal(orig.IsAssigned, clone.IsAssigned);
            if (orig.IsAssigned)
            {
                Assert.Equal(orig.SkeletonBoneIndex, clone.SkeletonBoneIndex);
                Assert.Equal(orig.Position, clone.Position);
                Assert.Equal(orig.Rotation, clone.Rotation);
                Assert.Equal(orig.Direction, clone.Direction);
                Assert.Equal(orig.Length, clone.Length, 4);
            }
        }
    }
}
