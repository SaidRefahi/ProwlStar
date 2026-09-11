// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using Prowl.Echo;
using Prowl.Editor.Animation;
using Prowl.Runtime;
using Prowl.Runtime.Animation;
using Prowl.Vector;
using Xunit;

namespace Prowl.Editor.Test;

public class HumanoidEditorToolingTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    private static SkeletonAsset CreateMixamoStyleSkeleton()
    {
        var skel = new SkeletonAsset { Name = "MixamoCharacter" };

        skel.AddBone(new SkeletonBone("mixamorig:Hips", -1, new Float3(0, 1.0f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));       // 0
        skel.AddBone(new SkeletonBone("mixamorig:Spine", 0, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));      // 1
        skel.AddBone(new SkeletonBone("mixamorig:Spine1", 1, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));     // 2 (Chest)
        skel.AddBone(new SkeletonBone("mixamorig:Spine2", 2, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));     // 3 (UpperChest)
        skel.AddBone(new SkeletonBone("mixamorig:Neck", 3, new Float3(0, 0.1f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));       // 4
        skel.AddBone(new SkeletonBone("mixamorig:Head", 4, new Float3(0, 0.2f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));       // 5

        skel.AddBone(new SkeletonBone("mixamorig:LeftShoulder", 3, new Float3(-0.1f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 6
        skel.AddBone(new SkeletonBone("mixamorig:LeftArm", 6, new Float3(-0.2f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));     // 7 (UpperArm)
        skel.AddBone(new SkeletonBone("mixamorig:LeftForeArm", 7, new Float3(-0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 8 (LowerArm)
        skel.AddBone(new SkeletonBone("mixamorig:LeftHand", 8, new Float3(-0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 9

        skel.AddBone(new SkeletonBone("mixamorig:RightShoulder", 3, new Float3(0.1f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 10
        skel.AddBone(new SkeletonBone("mixamorig:RightArm", 10, new Float3(0.2f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 11 (UpperArm)
        skel.AddBone(new SkeletonBone("mixamorig:RightForeArm", 11, new Float3(0.25f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 12 (LowerArm)
        skel.AddBone(new SkeletonBone("mixamorig:RightHand", 12, new Float3(0.15f, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));  // 13

        skel.AddBone(new SkeletonBone("mixamorig:LeftUpLeg", 0, new Float3(-0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 14 (UpperLeg)
        skel.AddBone(new SkeletonBone("mixamorig:LeftLeg", 14, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));    // 15 (LowerLeg)
        skel.AddBone(new SkeletonBone("mixamorig:LeftFoot", 15, new Float3(0, -0.1f, 0.1f), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 16
        skel.AddBone(new SkeletonBone("mixamorig:LeftToeBase", 16, new Float3(0, 0, 0.1f), Quaternion.Identity, Float3.One, Float4x4.Identity));  // 17 (Toes)

        skel.AddBone(new SkeletonBone("mixamorig:RightUpLeg", 0, new Float3(0.1f, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));// 18 (UpperLeg)
        skel.AddBone(new SkeletonBone("mixamorig:RightLeg", 18, new Float3(0, -0.4f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));   // 19 (LowerLeg)
        skel.AddBone(new SkeletonBone("mixamorig:RightFoot", 19, new Float3(0, -0.1f, 0.1f), Quaternion.Identity, Float3.One, Float4x4.Identity));// 20
        skel.AddBone(new SkeletonBone("mixamorig:RightToeBase", 20, new Float3(0, 0, 0.1f), Quaternion.Identity, Float3.One, Float4x4.Identity)); // 21 (Toes)

        return skel;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Auto-Mapping Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AutoMapper_MixamoRig_MapsAllBonesSuccessfully()
    {
        var skeleton = CreateMixamoStyleSkeleton();
        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skeleton) };

        var result = HumanoidAutoMapper.AutoMap(skeleton, mapping);

        Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
        Assert.Equal(22, result.MappedCount);
        Assert.Empty(result.AmbiguousRoles);
        Assert.Empty(result.MissingRequiredRoles);

        // Verify key roles mapped to expected indices
        Assert.Equal(0, mapping.Get(HumanoidBone.Hips));
        Assert.Equal(5, mapping.Get(HumanoidBone.Head));
        Assert.Equal(7, mapping.Get(HumanoidBone.LeftUpperArm));
        Assert.Equal(14, mapping.Get(HumanoidBone.LeftUpperLeg));

        // Mapping is valid and passes validation
        Assert.True(mapping.Validate(skeleton, out var errors), string.Join("; ", errors));
    }

    [Fact]
    public void AutoMapper_AmbiguousBones_DoesNotAssignArbitrarily()
    {
        var skeleton = new SkeletonAsset { Name = "AmbiguousRig" };
        // Two bones with identical matching scores for LeftUpperArm
        skeleton.AddBone(new SkeletonBone("Hips", -1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        skeleton.AddBone(new SkeletonBone("Arm_L_CandidateA", 0, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        skeleton.AddBone(new SkeletonBone("Arm_L_CandidateB", 0, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));

        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skeleton) };
        var result = HumanoidAutoMapper.AutoMap(skeleton, mapping);

        // LeftUpperArm should be flagged as ambiguous and left unassigned (-1)
        Assert.Contains(HumanoidBone.LeftUpperArm, result.AmbiguousRoles);
        Assert.Equal(-1, mapping.Get(HumanoidBone.LeftUpperArm));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Editor Validation & Calibration Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EditorMapping_ManualAssignmentAndValidation()
    {
        var skeleton = CreateMixamoStyleSkeleton();
        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skeleton) };

        // Before assignment: validation fails with missing required bones
        Assert.False(mapping.Validate(skeleton, out var errorsBefore));
        Assert.NotEmpty(errorsBefore);

        // Manually assign each bone
        for (int i = 0; i < 22; i++)
            mapping.Set((HumanoidBone)i, i);

        // Now validation passes
        Assert.True(mapping.Validate(skeleton, out var errorsAfter));
        Assert.Empty(errorsAfter);
    }

    [Fact]
    public void EditorCalibration_DerivesAccurateMeasurements()
    {
        var skeleton = CreateMixamoStyleSkeleton();
        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skeleton) };
        HumanoidAutoMapper.AutoMap(skeleton, mapping);

        bool success = HumanoidCalibration.Build(skeleton, mapping, out var calibration, out var errors);
        Assert.True(success, string.Join("; ", errors));

        // Arm segment lengths
        Assert.True(calibration!.TryGetBoneLength(HumanoidBone.LeftUpperArm, out float upperArmLen));
        Assert.Equal(0.25f, upperArmLen, 3);

        Assert.True(calibration.TryGetBoneLength(HumanoidBone.LeftLowerArm, out float lowerArmLen));
        Assert.Equal(0.15f, lowerArmLen, 3);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Preview Tooling Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RetargetPreview_SamplesAndRetargetsSuccessfully()
    {
        var srcSkel = CreateMixamoStyleSkeleton();
        var srcMap = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(srcSkel) };
        HumanoidAutoMapper.AutoMap(srcSkel, srcMap);

        var tgtSkel = CreateMixamoStyleSkeleton();
        tgtSkel.Name = "TargetRig";
        var tgtMap = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(tgtSkel) };
        HumanoidAutoMapper.AutoMap(tgtSkel, tgtMap);

        var preview = new HumanoidRetargetPreview();
        bool setupOk = preview.Setup(srcSkel, srcMap, tgtSkel, tgtMap, out var errors);
        Assert.True(setupOk, string.Join("; ", errors));
        Assert.True(preview.IsValid);

        // Create simple clip
        var clip = new AnimationClip { Name = "TestClip", Duration = 1.0f, Wrap = AnimationWrapMode.Once };
        var armRot = Quaternion.AngleAxis(30f, Float3.UnitZ);
        for (int i = 0; i < srcSkel.BoneCount; i++)
        {
            string path = AnimationRetargeter.GetSkeletonBonePath(srcSkel, i);
            var rot = (i == 7) ? armRot : srcSkel[i].LocalRotation;
            clip.AddBone(new AnimationClip.AnimBone
            {
                BoneName = path,
                Position = new Vector.AnimationCurve(3, new Keyframe(0f, srcSkel[i].LocalPosition), new Keyframe(1f, srcSkel[i].LocalPosition)),
                Rotation = new Vector.AnimationCurve(4, new Keyframe(0f, new Float4(rot.X, rot.Y, rot.Z, rot.W)), new Keyframe(1f, new Float4(rot.X, rot.Y, rot.Z, rot.W))),
                Scale = new Vector.AnimationCurve(3, new Keyframe(0f, Float3.One), new Keyframe(1f, Float3.One))
            });
        }

        // Sample at t=0.5
        bool sampleOk = preview.SampleRetargetedPose(clip, 0.5f, out var targetPoses);
        Assert.True(sampleOk);
        Assert.Equal(tgtSkel.BoneCount, targetPoses.Length);
        Assert.Equal(armRot, targetPoses[7].Rotation);
    }
}
