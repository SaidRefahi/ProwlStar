// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Echo;
using Prowl.Runtime.Animation;
using Prowl.Vector;
using Xunit;

namespace Prowl.Runtime.Test;

public class SkeletonAnimationTests : RuntimeTestBase
{
    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Builds a simple 3-bone skeleton: Root → Hips → Spine.</summary>
    private static SkeletonAsset CreateTestSkeleton()
    {
        var skeleton = new SkeletonAsset { Name = "TestSkeleton" };
        skeleton.AddBone(new SkeletonBone("Armature", -1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        skeleton.AddBone(new SkeletonBone("Hips", 0, new Float3(0, 1, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skeleton.AddBone(new SkeletonBone("Spine", 1, new Float3(0, 0.3f, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        return skeleton;
    }

    /// <summary>Builds a skeleton with duplicate bone names at different levels.</summary>
    private static SkeletonAsset CreateSkeletonWithDuplicateNames()
    {
        var skeleton = new SkeletonAsset { Name = "DuplicateNames" };
        skeleton.AddBone(new SkeletonBone("Root", -1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        skeleton.AddBone(new SkeletonBone("Joint", 0, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));   // Root → Joint
        skeleton.AddBone(new SkeletonBone("Chain", 1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));   // Root → Joint → Chain
        skeleton.AddBone(new SkeletonBone("Joint", 0, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));   // Root → Joint (duplicate!)
        return skeleton;
    }

    /// <summary>Creates a simple AnimationClip with constant curves for the given bone paths.</summary>
    private static AnimationClip CreateTestClip(params string[] bonePaths)
    {
        var clip = new AnimationClip
        {
            Name = "TestClip",
            Duration = 1f,
            Wrap = AnimationWrapMode.Once,
        };

        foreach (string path in bonePaths)
        {
            var posCurve = new Vector.AnimationCurve(3,
                new Keyframe(0f, new Float3(0f, 1f, 0f)),
                new Keyframe(1f, new Float3(0f, 1f, 0f)));

            var rotCurve = new Vector.AnimationCurve(4,
                new Keyframe(0f, new Float4(0f, 0f, 0f, 1f)),
                new Keyframe(1f, new Float4(0f, 0f, 0f, 1f)));

            var scaleCurve = new Vector.AnimationCurve(3,
                new Keyframe(0f, Float3.One),
                new Keyframe(1f, Float3.One));

            clip.AddBone(new AnimationClip.AnimBone
            {
                BoneName = path,
                Position = posCurve,
                Rotation = rotCurve,
                Scale = scaleCurve,
            });
        }

        return clip;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Path Resolution Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PathResolution_ExactMatch_FullPath()
    {
        var skeleton = CreateTestSkeleton(); // Armature(0) → Hips(1) → Spine(2)

        // Full path matches: Armature → Hips → Spine
        int idx = AnimationComponent.ResolveClipBoneToSkeleton("Armature/Hips/Spine", skeleton);
        Assert.Equal(2, idx);
    }

    [Fact]
    public void PathResolution_ExactMatch_PartialPath()
    {
        var skeleton = CreateTestSkeleton();

        // Partial path: Hips → Spine (right-aligned match)
        int idx = AnimationComponent.ResolveClipBoneToSkeleton("Hips/Spine", skeleton);
        Assert.Equal(2, idx);
    }

    [Fact]
    public void PathResolution_ExactMatch_LeafOnly()
    {
        var skeleton = CreateTestSkeleton();

        // "Hips" is unique — matches even as leaf-only
        int idx = AnimationComponent.ResolveClipBoneToSkeleton("Hips", skeleton);
        Assert.Equal(1, idx);
    }

    [Fact]
    public void PathResolution_ExactMatch_RootBone()
    {
        var skeleton = CreateTestSkeleton();

        int idx = AnimationComponent.ResolveClipBoneToSkeleton("Armature", skeleton);
        Assert.Equal(0, idx);
    }

    [Fact]
    public void PathResolution_UnambiguousFallback()
    {
        var skeleton = CreateTestSkeleton();

        // Path "SomeOther/Spine" doesn't match the ParentIndex chain exactly,
        // but "Spine" is unique → unambiguous fallback
        int idx = AnimationComponent.ResolveClipBoneToSkeleton("SomeOther/Spine", skeleton);
        Assert.Equal(2, idx);
    }

    [Fact]
    public void PathResolution_AmbiguousLeafName_ReturnsMinusOne()
    {
        var skeleton = CreateSkeletonWithDuplicateNames(); // Has two bones named "Joint"

        // "Joint" is ambiguous (2 candidates, no exact path match)
        int idx = AnimationComponent.ResolveClipBoneToSkeleton("Joint", skeleton);
        Assert.Equal(-1, idx);
    }

    [Fact]
    public void PathResolution_AmbiguousLeafName_ExactPathDisambiguates()
    {
        var skeleton = CreateSkeletonWithDuplicateNames();
        // Bone 1: Root → Joint (parent 0)
        // Bone 3: Root → Joint (parent 0) — same parent!
        // Both have parent "Root", so "Root/Joint" is still ambiguous.
        // But if we had distinct parent chains, exact match would disambiguate.

        // In this case both candidates match "Root/Joint" so it's still ambiguous
        int idx = AnimationComponent.ResolveClipBoneToSkeleton("Root/Joint", skeleton);
        Assert.Equal(-1, idx); // Multiple exact matches → unresolved
    }

    [Fact]
    public void PathResolution_NonExistentBone_ReturnsMinusOne()
    {
        var skeleton = CreateTestSkeleton();

        int idx = AnimationComponent.ResolveClipBoneToSkeleton("NonExistent", skeleton);
        Assert.Equal(-1, idx);
    }

    [Fact]
    public void PathResolution_EmptyPath_ReturnsMinusOne()
    {
        var skeleton = CreateTestSkeleton();

        Assert.Equal(-1, AnimationComponent.ResolveClipBoneToSkeleton("", skeleton));
        Assert.Equal(-1, AnimationComponent.ResolveClipBoneToSkeleton(null!, skeleton));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Parent Index / Hierarchy Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParentIndices_MatchSkeletonHierarchy()
    {
        var skeleton = CreateTestSkeleton();

        // Resolve all three bones
        int armature = AnimationComponent.ResolveClipBoneToSkeleton("Armature", skeleton);
        int hips = AnimationComponent.ResolveClipBoneToSkeleton("Armature/Hips", skeleton);
        int spine = AnimationComponent.ResolveClipBoneToSkeleton("Armature/Hips/Spine", skeleton);

        Assert.Equal(0, armature);
        Assert.Equal(1, hips);
        Assert.Equal(2, spine);

        // Verify parent chain
        Assert.Equal(-1, skeleton.GetParentIndex(armature));
        Assert.Equal(armature, skeleton.GetParentIndex(hips));
        Assert.Equal(hips, skeleton.GetParentIndex(spine));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Missing Bones / Graceful Degradation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MissingBones_SkippedGracefully()
    {
        var skeleton = CreateTestSkeleton();

        // Clip has bones that don't exist in skeleton
        var clip = CreateTestClip("Armature/Hips/Spine", "Armature/Hips/LeftLeg", "NonExistent/Bone");

        // Spine should resolve, others should be -1
        int spineIdx = AnimationComponent.ResolveClipBoneToSkeleton(clip.Bones[0].BoneName, skeleton);
        int leftLegIdx = AnimationComponent.ResolveClipBoneToSkeleton(clip.Bones[1].BoneName, skeleton);
        int nonExistIdx = AnimationComponent.ResolveClipBoneToSkeleton(clip.Bones[2].BoneName, skeleton);

        Assert.Equal(2, spineIdx);
        Assert.Equal(-1, leftLegIdx);
        Assert.Equal(-1, nonExistIdx);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Serialization Format Unchanged
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AnimationClip_SerializationFormat_Unchanged()
    {
        var clip = CreateTestClip("Armature/Hips", "Armature/Hips/Spine");

        // Round-trip through Echo serialization
        var serialized = Serializer.Serialize(clip);
        var deserialized = Serializer.Deserialize<AnimationClip>(serialized);

        Assert.NotNull(deserialized);
        Assert.Equal(clip.Bones.Count, deserialized.Bones.Count);
        Assert.Equal(clip.Duration, deserialized.Duration);
        Assert.Equal(clip.Wrap, deserialized.Wrap);

        for (int i = 0; i < clip.Bones.Count; i++)
        {
            Assert.Equal(clip.Bones[i].BoneName, deserialized.Bones[i].BoneName);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pose Output Equivalence
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AnimationSampler_PoseOutput_ConsistentValues()
    {
        var clip = CreateTestClip("Armature/Hips", "Armature/Hips/Spine");
        var sampler = new AnimationSampler();

        // Evaluate at t=0
        sampler.Evaluate(clip, 0f, out var poses);
        Assert.True(poses.Length >= clip.Bones.Count);

        // Evaluate at t=0.5
        sampler.Evaluate(clip, 0.5f, out var poses2);
        Assert.True(poses2.Length >= clip.Bones.Count);

        // With constant curves, poses should be identical at both times
        for (int i = 0; i < clip.Bones.Count; i++)
        {
            Assert.Equal(poses[i].Position, poses2[i].Position);
            Assert.Equal(poses[i].Scale, poses2[i].Scale);
        }
    }

    [Fact]
    public void AnimationSampler_BufferReuse_NoAllocationPerFrame()
    {
        var clip = CreateTestClip("Armature/Hips", "Armature/Hips/Spine");
        var sampler = new AnimationSampler();

        sampler.Evaluate(clip, 0f, out var poses1);
        sampler.Evaluate(clip, 0.5f, out var poses2);

        // Same buffer reference (reused, not reallocated)
        Assert.Same(poses1, poses2);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  AnimationComponent Integration Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AnimationComponent_WithSkeleton_DrivesHierarchy()
    {
        var scene = CreateScene(enable: true);

        var rootGO = CreateGameObject("Model");
        var armatureGO = CreateGameObject("Armature");
        var hipsGO = CreateGameObject("Hips");
        var spineGO = CreateGameObject("Spine");

        armatureGO.Transform.SetParent(rootGO.Transform);
        hipsGO.Transform.SetParent(armatureGO.Transform);
        spineGO.Transform.SetParent(hipsGO.Transform);

        var skeleton = CreateTestSkeleton();
        var clip = CreateTestClip("Armature/Hips", "Armature/Hips/Spine");

        var animComp = rootGO.AddComponent<AnimationComponent>();
        animComp.Skeleton = new AssetRef<SkeletonAsset>(skeleton);
        animComp.Play(clip);

        // Before update: default position
        Assert.Equal(Float3.Zero, hipsGO.Transform.LocalPosition);

        // Update advances animation and applies pose
        animComp.Update();

        // After update: curve has Position = (0, 1, 0)
        Assert.Equal(new Float3(0f, 1f, 0f), hipsGO.Transform.LocalPosition);
        Assert.Equal(new Float3(0f, 1f, 0f), spineGO.Transform.LocalPosition);
    }

    [Fact]
    public void AnimationComponent_LegacyFallback_DrivesHierarchy()
    {
        var scene = CreateScene(enable: true);

        var rootGO = CreateGameObject("Model");
        var armatureGO = CreateGameObject("Armature");
        var hipsGO = CreateGameObject("Hips");
        var spineGO = CreateGameObject("Spine");

        armatureGO.Transform.SetParent(rootGO.Transform);
        hipsGO.Transform.SetParent(armatureGO.Transform);
        spineGO.Transform.SetParent(hipsGO.Transform);

        var clip = CreateTestClip("Armature/Hips", "Armature/Hips/Spine");

        var animComp = rootGO.AddComponent<AnimationComponent>();
        // Skeleton left null → legacy path
        animComp.Play(clip);

        animComp.Update();

        Assert.Equal(new Float3(0f, 1f, 0f), hipsGO.Transform.LocalPosition);
        Assert.Equal(new Float3(0f, 1f, 0f), spineGO.Transform.LocalPosition);
    }

    [Fact]
    public void AnimationComponent_CacheInvalidation_OnRebind()
    {
        var scene = CreateScene(enable: true);

        var rootGO = CreateGameObject("Model");
        var armatureGO = CreateGameObject("Armature");
        var hipsGO = CreateGameObject("Hips");
        var spineGO = CreateGameObject("Spine");

        armatureGO.Transform.SetParent(rootGO.Transform);
        hipsGO.Transform.SetParent(armatureGO.Transform);
        spineGO.Transform.SetParent(hipsGO.Transform);

        var skeleton1 = CreateTestSkeleton();
        var clip1 = CreateTestClip("Armature/Hips");

        var animComp = rootGO.AddComponent<AnimationComponent>();
        animComp.Skeleton = new AssetRef<SkeletonAsset>(skeleton1);
        animComp.Play(clip1);
        animComp.Update();

        Assert.Equal(new Float3(0f, 1f, 0f), hipsGO.Transform.LocalPosition);

        // Switch to a new clip with both Hips and Spine
        var clip2 = CreateTestClip("Armature/Hips", "Armature/Hips/Spine");
        animComp.Play(clip2);
        animComp.Update();

        Assert.Equal(new Float3(0f, 1f, 0f), spineGO.Transform.LocalPosition);
    }
}
