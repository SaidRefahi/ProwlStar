// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using Prowl.Echo;
using Prowl.Vector;
using Xunit;

namespace Prowl.Runtime.Test;

public class HumanoidMappingTests : RuntimeTestBase
{
    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds a full 22-bone humanoid skeleton with standard parent-child relationships.
    /// </summary>
    private static SkeletonAsset CreateFullHumanoidSkeleton()
    {
        var skel = new SkeletonAsset { Name = "HumanoidRig" };

        // 0: Hips (Root)
        skel.AddBone(new SkeletonBone("Hips", -1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));

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

    /// <summary>
    /// Populates a valid 1:1 mapping between all 22 HumanoidBones and the 22 skeleton bones.
    /// </summary>
    private static HumanoidMapping CreateFullValidMapping(SkeletonAsset skeleton)
    {
        var mapping = new HumanoidMapping
        {
            Skeleton = new AssetRef<SkeletonAsset>(skeleton)
        };

        mapping[HumanoidBone.Hips] = 0;
        mapping[HumanoidBone.Spine] = 1;
        mapping[HumanoidBone.Chest] = 2;
        mapping[HumanoidBone.UpperChest] = 3;
        mapping[HumanoidBone.Neck] = 4;
        mapping[HumanoidBone.Head] = 5;

        mapping[HumanoidBone.LeftShoulder] = 6;
        mapping[HumanoidBone.LeftUpperArm] = 7;
        mapping[HumanoidBone.LeftLowerArm] = 8;
        mapping[HumanoidBone.LeftHand] = 9;

        mapping[HumanoidBone.RightShoulder] = 10;
        mapping[HumanoidBone.RightUpperArm] = 11;
        mapping[HumanoidBone.RightLowerArm] = 12;
        mapping[HumanoidBone.RightHand] = 13;

        mapping[HumanoidBone.LeftUpperLeg] = 14;
        mapping[HumanoidBone.LeftLowerLeg] = 15;
        mapping[HumanoidBone.LeftFoot] = 16;
        mapping[HumanoidBone.LeftToes] = 17;

        mapping[HumanoidBone.RightUpperLeg] = 18;
        mapping[HumanoidBone.RightLowerLeg] = 19;
        mapping[HumanoidBone.RightFoot] = 20;
        mapping[HumanoidBone.RightToes] = 21;

        return mapping;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Basic Mapping Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CreateMapping_InitialState_AllUnassigned()
    {
        var mapping = new HumanoidMapping();

        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            var bone = (HumanoidBone)i;
            Assert.Equal(-1, mapping.Get(bone));
            Assert.False(mapping.TryGet(bone, out int idx));
            Assert.Equal(-1, idx);
        }
    }

    [Fact]
    public void SetAndGet_WorksForEveryHumanoidBone()
    {
        var mapping = new HumanoidMapping();

        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            var bone = (HumanoidBone)i;
            mapping.Set(bone, i * 2);
            Assert.Equal(i * 2, mapping.Get(bone));
            Assert.True(mapping.TryGet(bone, out int idx));
            Assert.Equal(i * 2, idx);
        }
    }

    [Fact]
    public void Indexer_GetAndSet()
    {
        var mapping = new HumanoidMapping();
        mapping[HumanoidBone.Head] = 42;
        Assert.Equal(42, mapping[HumanoidBone.Head]);

        mapping[HumanoidBone.Head] = -1;
        Assert.Equal(-1, mapping[HumanoidBone.Head]);
        Assert.False(mapping.TryGet(HumanoidBone.Head, out _));
    }

    [Fact]
    public void ClearAndClearAll()
    {
        var mapping = new HumanoidMapping();
        mapping[HumanoidBone.Hips] = 0;
        mapping[HumanoidBone.Spine] = 1;

        mapping.Clear(HumanoidBone.Hips);
        Assert.Equal(-1, mapping[HumanoidBone.Hips]);
        Assert.Equal(1, mapping[HumanoidBone.Spine]);

        mapping.ClearAll();
        Assert.Equal(-1, mapping[HumanoidBone.Spine]);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Validation Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Validate_MissingSkeleton_ReturnsFalse()
    {
        var mapping = new HumanoidMapping();
        Assert.False(mapping.Validate(out string? error));
        Assert.Contains("missing or null", error);
    }

    [Fact]
    public void Validate_IndexOutOfRange_ReturnsFalse()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        // Assign Head to an index >= skeleton.BoneCount
        mapping[HumanoidBone.Head] = skeleton.BoneCount + 5;

        Assert.False(mapping.Validate(out List<string> errors));
        Assert.Contains(errors, e => e.Contains("out-of-range"));
    }

    [Fact]
    public void Validate_RequiredBoneUnassigned_ReturnsFalse()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        // Unassign required bone (Head)
        mapping[HumanoidBone.Head] = -1;

        Assert.False(mapping.Validate(out List<string> errors));
        Assert.Contains(errors, e => e.Contains("Required humanoid bone 'Head' is not assigned"));
    }

    [Fact]
    public void Validate_DuplicateBoneAssignment_ReturnsFalse()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        // Map both Head and Neck to the same skeleton bone index (4)
        mapping[HumanoidBone.Head] = 4;
        mapping[HumanoidBone.Neck] = 4;

        Assert.False(mapping.Validate(out List<string> errors));
        Assert.Contains(errors, e => e.Contains("multiple humanoid roles"));
    }

    [Fact]
    public void Validate_FullValidHierarchy_ReturnsTrue()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        bool isValid = mapping.Validate(out List<string> errors);
        Assert.True(isValid, string.Join("\n", errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingOptionalBones_ReturnsTrue()
    {
        var skeleton = new SkeletonAsset { Name = "CompactHumanoid" };

        // Skeleton without optional bones (UpperChest, Shoulders, Toes)
        // 0: Hips
        skelAdd(skeleton, "Hips", -1);
        // 1: Spine (parent: Hips)
        skelAdd(skeleton, "Spine", 0);
        // 2: Chest (parent: Spine)
        skelAdd(skeleton, "Chest", 1);
        // 3: Neck (parent: Chest) - direct, no UpperChest
        skelAdd(skeleton, "Neck", 2);
        // 4: Head (parent: Neck)
        skelAdd(skeleton, "Head", 3);

        // Left Arm (parent: Chest) - direct, no Shoulder
        skelAdd(skeleton, "LeftUpperArm", 2);   // 5
        skelAdd(skeleton, "LeftLowerArm", 5);   // 6
        skelAdd(skeleton, "LeftHand", 6);       // 7

        // Right Arm (parent: Chest) - direct, no Shoulder
        skelAdd(skeleton, "RightUpperArm", 2);  // 8
        skelAdd(skeleton, "RightLowerArm", 8);  // 9
        skelAdd(skeleton, "RightHand", 9);      // 10

        // Left Leg (parent: Hips) - no Toes
        skelAdd(skeleton, "LeftUpperLeg", 0);   // 11
        skelAdd(skeleton, "LeftLowerLeg", 11);  // 12
        skelAdd(skeleton, "LeftFoot", 12);      // 13

        // Right Leg (parent: Hips) - no Toes
        skelAdd(skeleton, "RightUpperLeg", 0);  // 14
        skelAdd(skeleton, "RightLowerLeg", 14); // 15
        skelAdd(skeleton, "RightFoot", 15);     // 16

        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skeleton) };
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

        // All 5 optional bones are left as -1 (unassigned)
        Assert.True(HumanoidMapping.IsOptional(HumanoidBone.UpperChest));
        Assert.True(HumanoidMapping.IsOptional(HumanoidBone.LeftShoulder));
        Assert.True(HumanoidMapping.IsOptional(HumanoidBone.RightShoulder));
        Assert.True(HumanoidMapping.IsOptional(HumanoidBone.LeftToes));
        Assert.True(HumanoidMapping.IsOptional(HumanoidBone.RightToes));

        bool isValid = mapping.Validate(out List<string> errors);
        Assert.True(isValid, string.Join("\n", errors));
    }

    [Fact]
    public void Validate_InvalidHierarchy_ReturnsFalse()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        // Swap LeftFoot (16) with LeftHand (9) -> hierarchy mismatch!
        mapping[HumanoidBone.LeftFoot] = 9;
        mapping[HumanoidBone.LeftHand] = 16;

        Assert.False(mapping.Validate(out List<string> errors));
        Assert.Contains(errors, e => e.Contains("Hierarchy mismatch"));
    }

    [Fact]
    public void Validate_InvertedSpine_ReturnsFalse()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);

        // Swap Spine (1) and Chest (2)
        mapping[HumanoidBone.Spine] = 2;
        mapping[HumanoidBone.Chest] = 1;

        Assert.False(mapping.Validate(out List<string> errors));
        Assert.Contains(errors, e => e.Contains("Hierarchy mismatch"));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Serialization Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Serialization_RoundTrip_PreservesAllMappings()
    {
        var skeleton = CreateFullHumanoidSkeleton();
        var mapping = CreateFullValidMapping(skeleton);
        mapping.Name = "TestHumanoid";

        var serialized = Serializer.Serialize(mapping);
        var deserialized = Serializer.Deserialize<HumanoidMapping>(serialized);

        Assert.NotNull(deserialized);
        Assert.Equal("TestHumanoid", deserialized.Name);

        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            var bone = (HumanoidBone)i;
            Assert.Equal(mapping[bone], deserialized[bone]);
        }

        // Validate deserialized mapping against the original skeleton
        bool isValid = deserialized.Validate(skeleton, out List<string> errors);
        Assert.True(isValid, string.Join("\n", errors));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Utility helper
    // ════════════════════════════════════════════════════════════════════════

    private static void skelAdd(SkeletonAsset skel, string name, int parent)
    {
        skel.AddBone(new SkeletonBone(name, parent, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
    }
}
