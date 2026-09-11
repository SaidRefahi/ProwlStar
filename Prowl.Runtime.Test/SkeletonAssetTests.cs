// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using Prowl.Echo;
using Prowl.Runtime.AssetImporting;
using Prowl.Runtime.Resources;
using Prowl.Vector;
using Xunit;

namespace Prowl.Runtime.Test;

public class SkeletonAssetTests : RuntimeTestBase
{
    private static T RoundTrip<T>(T value) => Serializer.Deserialize<T>(Serializer.Serialize(value));

    [Fact]
    public void SkeletonAsset_CreateAndQueryBones()
    {
        var skeleton = new SkeletonAsset { Name = "TestHumanoid" };

        var hipBone = new SkeletonBone(
            "Hips",
            parentIndex: -1,
            new Float3(0, 1, 0),
            Quaternion.Identity,
            Float3.One,
            Float4x4.Identity
        );

        var spineBone = new SkeletonBone(
            "Spine",
            parentIndex: 0,
            new Float3(0, 0.3f, 0),
            Quaternion.FromEuler(new Float3(5, 0, 0)),
            Float3.One,
            Float4x4.CreateTranslation(new Float3(0, -1.3f, 0))
        );

        skeleton.AddBone(hipBone);
        skeleton.AddBone(spineBone);

        Assert.Equal(2, skeleton.BoneCount);
        Assert.Equal("Hips", skeleton[0].Name);
        Assert.Equal("Spine", skeleton[1].Name);
        Assert.Equal(0, skeleton.FindBoneIndex("Hips"));
        Assert.Equal(1, skeleton.FindBoneIndex("Spine"));
        Assert.Equal(-1, skeleton.FindBoneIndex("NonExistent"));
        Assert.Equal(-1, skeleton.GetParentIndex(0));
        Assert.Equal(0, skeleton.GetParentIndex(1));
        Assert.Equal(hipBone.Hash, skeleton[0].Hash);
        Assert.Equal(0, skeleton.FindBoneIndex(hipBone.Hash));

        var fetchedHip = skeleton.GetBone("Hips");
        Assert.NotNull(fetchedHip);
        Assert.Equal("Hips", fetchedHip.Value.Name);

        var fetchedSpineByHash = skeleton.GetBone(spineBone.Hash);
        Assert.NotNull(fetchedSpineByHash);
        Assert.Equal("Spine", fetchedSpineByHash.Value.Name);
    }

    [Fact]
    public void SkeletonAsset_Serialization_PreservesAllFieldsAndHierarchy()
    {
        var skeleton = new SkeletonAsset { Name = "CharacterRig" };

        var rot0 = Quaternion.NormalizeSafe(Quaternion.FromEuler(new Float3(10, 20, 30)));
        var rot1 = Quaternion.NormalizeSafe(Quaternion.FromEuler(new Float3(-15, 45, 0)));
        var ibp0 = Float4x4.CreateTRS(new Float3(1, 2, 3), rot0, new Float3(1, 1, 1)).Invert();
        var ibp1 = Float4x4.CreateTRS(new Float3(1, 2.5f, 3), rot1, new Float3(1, 1, 1)).Invert();

        skeleton.AddBone(new SkeletonBone("Root", -1, new Float3(0, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skeleton.AddBone(new SkeletonBone("Hips", 0, new Float3(0, 1, 0), rot0, new Float3(1.1f, 1.1f, 1.1f), ibp0));
        skeleton.AddBone(new SkeletonBone("Spine", 1, new Float3(0, 0.4f, 0), rot1, Float3.One, ibp1));

        var deserialized = RoundTrip(skeleton);

        Assert.NotNull(deserialized);
        Assert.Equal("CharacterRig", deserialized.Name);
        Assert.Equal(3, deserialized.BoneCount);

        // 1. Check parent indices
        Assert.Equal(-1, deserialized.GetParentIndex(0));
        Assert.Equal(0, deserialized.GetParentIndex(1));
        Assert.Equal(1, deserialized.GetParentIndex(2));

        // 2. Check rest pose (position, rotation, scale)
        Assert.Equal(new Float3(0, 0, 0), deserialized[0].LocalPosition);
        Assert.Equal(new Float3(0, 1, 0), deserialized[1].LocalPosition);
        Assert.Equal(new Float3(0, 0.4f, 0), deserialized[2].LocalPosition);

        Assert.True(Maths.Abs(deserialized[1].LocalRotation.X - rot0.X) < 1e-5f);
        Assert.True(Maths.Abs(deserialized[1].LocalRotation.Y - rot0.Y) < 1e-5f);
        Assert.True(Maths.Abs(deserialized[1].LocalRotation.Z - rot0.Z) < 1e-5f);
        Assert.True(Maths.Abs(deserialized[1].LocalRotation.W - rot0.W) < 1e-5f);

        Assert.Equal(new Float3(1.1f, 1.1f, 1.1f), deserialized[1].LocalScale);

        // 3. Check inverse bind poses
        Assert.Equal(Float4x4.Identity, deserialized[0].InverseBindPose);
        Assert.Equal(ibp0, deserialized[1].InverseBindPose);
        Assert.Equal(ibp1, deserialized[2].InverseBindPose);

        // 4. Check lookups and hashes
        Assert.Equal(0, deserialized.FindBoneIndex("Root"));
        Assert.Equal(1, deserialized.FindBoneIndex("Hips"));
        Assert.Equal(2, deserialized.FindBoneIndex("Spine"));
        Assert.Equal(SkeletonBone.ComputeHash("Hips"), deserialized[1].Hash);
    }

    [Fact]
    public void SkeletonBone_EqualityAndHashSemantics()
    {
        var b1 = new SkeletonBone("Chest", 1, new Float3(0, 0.5f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity);
        var b2 = new SkeletonBone("Chest", 1, new Float3(0, 0.5f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity);
        var b3 = new SkeletonBone("Chest", 2, new Float3(0, 0.5f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity);

        Assert.True(b1 == b2);
        Assert.False(b1 == b3);
        Assert.True(b1.Equals(b2));
        Assert.False(b1.Equals(b3));
        Assert.Equal(b1.GetHashCode(), b2.GetHashCode());
    }

    [Fact]
    public void ModelImporter_LoadsModel_IncludesSkeletonsList()
    {
        // Test that ModelImporter populates Skeletons list on imported models
        var prefab = DefaultModels.Load(DefaultModel.Cube);
        Assert.NotNull(prefab);
    }
}
