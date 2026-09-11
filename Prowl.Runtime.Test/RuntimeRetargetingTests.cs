// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using Prowl.Echo;
using Prowl.Runtime.Animation;
using Prowl.Vector;
using Xunit;

namespace Prowl.Runtime.Test;

public class RuntimeRetargetingTests : RuntimeTestBase
{
    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    private static (SkeletonAsset, HumanoidMapping) CreateHumanoidRig(string name, float heightScale = 1.0f)
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

        return (skel, mapping);
    }

    private GameObject BuildHierarchyFromSkeleton(SkeletonAsset skeleton)
    {
        var rootGO = CreateGameObject(skeleton.Name);
        var boneGOs = new GameObject[skeleton.BoneCount];

        for (int i = 0; i < skeleton.BoneCount; i++)
        {
            var bone = skeleton[i];
            var go = CreateGameObject(bone.Name);
            boneGOs[i] = go;

            if (bone.ParentIndex < 0)
                go.Transform.SetParent(rootGO.Transform);
            else
                go.Transform.SetParent(boneGOs[bone.ParentIndex].Transform);

            go.Transform.LocalPosition = bone.LocalPosition;
            go.Transform.LocalRotation = bone.LocalRotation;
            go.Transform.LocalScale = bone.LocalScale;
        }

        return rootGO;
    }

    private static AnimationClip CreateArmRaiseClip(SkeletonAsset sourceSkel)
    {
        var clip = new AnimationClip
        {
            Name = "ArmRaise",
            Duration = 1.0f,
            Wrap = AnimationWrapMode.Once
        };

        var armRot = Quaternion.AngleAxis(45f, Float3.UnitZ);

        for (int i = 0; i < sourceSkel.BoneCount; i++)
        {
            string bonePath = AnimationRetargeter.GetSkeletonBonePath(sourceSkel, i);
            var rot = (i == 7) ? armRot : sourceSkel[i].LocalRotation; // Bone 7 = LeftUpperArm

            clip.AddBone(new AnimationClip.AnimBone
            {
                BoneName = bonePath,
                Position = new Vector.AnimationCurve(3,
                    new Keyframe(0f, sourceSkel[i].LocalPosition),
                    new Keyframe(1.0f, sourceSkel[i].LocalPosition)),
                Rotation = new Vector.AnimationCurve(4,
                    new Keyframe(0f, new Float4(rot.X, rot.Y, rot.Z, rot.W)),
                    new Keyframe(1.0f, new Float4(rot.X, rot.Y, rot.Z, rot.W))),
                Scale = new Vector.AnimationCurve(3,
                    new Keyframe(0f, Float3.One),
                    new Keyframe(1.0f, Float3.One))
            });
        }

        return clip;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Runtime Retargeting Integration Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Playback_LegacyDirect_WhenNoRetargetingConfigured()
    {
        var scene = CreateScene(enable: true);
        var (skel, _) = CreateHumanoidRig("Character");
        var rootGO = BuildHierarchyFromSkeleton(skel);

        var clip = CreateArmRaiseClip(skel);
        var animComp = rootGO.AddComponent<AnimationComponent>();
        animComp.Skeleton = new AssetRef<SkeletonAsset>(skel);
        // SourceSkeleton and HumanoidMapping left unassigned
        animComp.Play(clip);

        animComp.Update();

        // LeftUpperArm (bone 7) should receive the clip rotation
        var armGO = rootGO.Transform.Find("Hips/Spine/Chest/LeftShoulder/LeftUpperArm");
        Assert.NotNull(armGO);
        Assert.Equal(Quaternion.AngleAxis(45f, Float3.UnitZ), armGO!.LocalRotation);
    }

    [Fact]
    public void Playback_RetargetedToDifferentSkeleton()
    {
        var scene = CreateScene(enable: true);
        var (srcSkel, srcMap) = CreateHumanoidRig("SourceRig", heightScale: 1.0f);
        var (tgtSkel, tgtMap) = CreateHumanoidRig("TargetRig", heightScale: 1.5f); // 1.5x scale

        var tgtRootGO = BuildHierarchyFromSkeleton(tgtSkel);
        var clip = CreateArmRaiseClip(srcSkel);

        var animComp = tgtRootGO.AddComponent<AnimationComponent>();
        animComp.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel);
        animComp.HumanoidMapping = new AssetRef<HumanoidMapping>(tgtMap);
        animComp.SourceSkeleton = new AssetRef<SkeletonAsset>(srcSkel);
        animComp.SourceHumanoidMapping = new AssetRef<HumanoidMapping>(srcMap);

        animComp.Play(clip);
        animComp.Update();

        // LeftUpperArm on target should have the 45 degree rotation retargeted
        var armGO = tgtRootGO.Transform.Find("Hips/Spine/Chest/LeftShoulder/LeftUpperArm");
        Assert.NotNull(armGO);
        Assert.Equal(Quaternion.AngleAxis(45f, Float3.UnitZ), armGO!.LocalRotation);

        // Hips position should be scaled (1.5x)
        var hipsGO = tgtRootGO.Transform.Find("Hips");
        Assert.NotNull(hipsGO);
        Assert.Equal(new Float3(0, 1.5f, 0), hipsGO!.LocalPosition);
    }

    [Fact]
    public void Playback_ClipChange_RebuildsBindings()
    {
        var scene = CreateScene(enable: true);
        var (srcSkel, srcMap) = CreateHumanoidRig("SourceRig");
        var (tgtSkel, tgtMap) = CreateHumanoidRig("TargetRig");

        var tgtRootGO = BuildHierarchyFromSkeleton(tgtSkel);
        var clip1 = CreateArmRaiseClip(srcSkel);

        // Create a second clip with head rotated
        var clip2 = new AnimationClip { Name = "HeadTurn", Duration = 1.0f, Wrap = AnimationWrapMode.Once };
        var headRot = Quaternion.AngleAxis(30f, Float3.UnitY);
        for (int i = 0; i < srcSkel.BoneCount; i++)
        {
            string bonePath = AnimationRetargeter.GetSkeletonBonePath(srcSkel, i);
            var rot = (i == 5) ? headRot : srcSkel[i].LocalRotation; // Bone 5 = Head
            clip2.AddBone(new AnimationClip.AnimBone
            {
                BoneName = bonePath,
                Position = new Vector.AnimationCurve(3, new Keyframe(0f, srcSkel[i].LocalPosition), new Keyframe(1f, srcSkel[i].LocalPosition)),
                Rotation = new Vector.AnimationCurve(4, new Keyframe(0f, new Float4(rot.X, rot.Y, rot.Z, rot.W)), new Keyframe(1f, new Float4(rot.X, rot.Y, rot.Z, rot.W))),
                Scale = new Vector.AnimationCurve(3, new Keyframe(0f, Float3.One), new Keyframe(1f, Float3.One))
            });
        }

        var animComp = tgtRootGO.AddComponent<AnimationComponent>();
        animComp.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel);
        animComp.HumanoidMapping = new AssetRef<HumanoidMapping>(tgtMap);
        animComp.SourceSkeleton = new AssetRef<SkeletonAsset>(srcSkel);
        animComp.SourceHumanoidMapping = new AssetRef<HumanoidMapping>(srcMap);

        // Play clip1
        animComp.Play(clip1);
        animComp.Update();

        var armGO = tgtRootGO.Transform.Find("Hips/Spine/Chest/LeftShoulder/LeftUpperArm");
        Assert.Equal(Quaternion.AngleAxis(45f, Float3.UnitZ), armGO!.LocalRotation);

        // Switch to clip2
        animComp.Play(clip2);
        animComp.Update();

        var headGO = tgtRootGO.Transform.Find("Hips/Spine/Chest/UpperChest/Neck/Head");
        Assert.Equal(headRot, headGO!.LocalRotation);
    }

    [Fact]
    public void Playback_SkeletonOrMappingChange_RebuildsRetargeter()
    {
        var scene = CreateScene(enable: true);
        var (srcSkel, srcMap) = CreateHumanoidRig("SourceRig", heightScale: 1.0f);
        var (tgtSkel1, tgtMap1) = CreateHumanoidRig("TargetRig1", heightScale: 1.0f);
        var (tgtSkel2, tgtMap2) = CreateHumanoidRig("TargetRig2", heightScale: 2.0f);

        var tgtRootGO = BuildHierarchyFromSkeleton(tgtSkel1);
        var clip = CreateArmRaiseClip(srcSkel);

        var animComp = tgtRootGO.AddComponent<AnimationComponent>();
        animComp.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel1);
        animComp.HumanoidMapping = new AssetRef<HumanoidMapping>(tgtMap1);
        animComp.SourceSkeleton = new AssetRef<SkeletonAsset>(srcSkel);
        animComp.SourceHumanoidMapping = new AssetRef<HumanoidMapping>(srcMap);

        animComp.Play(clip);
        animComp.Update();

        var hipsGO = tgtRootGO.Transform.Find("Hips");
        Assert.Equal(new Float3(0, 1.0f, 0), hipsGO!.LocalPosition);

        // Change target skeleton & mapping to the 2.0x scale rig
        animComp.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel2);
        animComp.HumanoidMapping = new AssetRef<HumanoidMapping>(tgtMap2);
        animComp.Update();

        Assert.Equal(new Float3(0, 2.0f, 0), hipsGO.LocalPosition);
    }

    [Fact]
    public void Playback_IdenticalRigs_ProducesIdenticalPose()
    {
        var scene = CreateScene(enable: true);
        var (srcSkel, srcMap) = CreateHumanoidRig("Rig");
        var (tgtSkel, tgtMap) = CreateHumanoidRig("Rig");

        var rootGO = BuildHierarchyFromSkeleton(tgtSkel);
        var clip = CreateArmRaiseClip(srcSkel);

        var animComp = rootGO.AddComponent<AnimationComponent>();
        animComp.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel);
        animComp.HumanoidMapping = new AssetRef<HumanoidMapping>(tgtMap);
        animComp.SourceSkeleton = new AssetRef<SkeletonAsset>(srcSkel);
        animComp.SourceHumanoidMapping = new AssetRef<HumanoidMapping>(srcMap);

        animComp.Play(clip);
        animComp.Update();

        var armGO = rootGO.Transform.Find("Hips/Spine/Chest/LeftShoulder/LeftUpperArm");
        Assert.Equal(Quaternion.AngleAxis(45f, Float3.UnitZ), armGO!.LocalRotation);
    }

    [Fact]
    public void Playback_InvalidMapping_FailsSafelyWithoutThrowing()
    {
        var scene = CreateScene(enable: true);
        var (srcSkel, _) = CreateHumanoidRig("SourceRig");
        var (tgtSkel, _) = CreateHumanoidRig("TargetRig");

        var rootGO = BuildHierarchyFromSkeleton(tgtSkel);
        var clip = CreateArmRaiseClip(srcSkel);

        var animComp = rootGO.AddComponent<AnimationComponent>();
        animComp.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel);
        // Provide empty unassigned mapping
        animComp.HumanoidMapping = new AssetRef<HumanoidMapping>(new HumanoidMapping());
        animComp.SourceSkeleton = new AssetRef<SkeletonAsset>(srcSkel);
        animComp.SourceHumanoidMapping = new AssetRef<HumanoidMapping>(new HumanoidMapping());

        animComp.Play(clip);

        // Must not throw an exception; falls back safely
        var exception = Record.Exception(() => animComp.Update());
        Assert.Null(exception);
    }
}
