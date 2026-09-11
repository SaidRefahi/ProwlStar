// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using Prowl.Echo;
using Prowl.Runtime.Animation;
using Prowl.Vector;
using Xunit;

namespace Prowl.Runtime.Test;

public class AnimatorTests : RuntimeTestBase
{
    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    private static SkeletonAsset CreateSimpleSkeleton()
    {
        var skel = new SkeletonAsset { Name = "SimpleSkeleton" };
        skel.AddBone(new SkeletonBone("Root", -1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("Bone1", 0, new Float3(0, 1, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        return skel;
    }

    private static AnimationClip CreateClipWithPosition(string clipName, string bonePath, Float3 startPos, Float3 endPos, float duration = 1.0f)
    {
        var clip = new AnimationClip
        {
            Name = clipName,
            Duration = duration,
            Wrap = AnimationWrapMode.Loop
        };

        var posCurve = new Vector.AnimationCurve(3,
            new Keyframe(0f, startPos),
            new Keyframe(duration, endPos));

        var rotCurve = new Vector.AnimationCurve(4,
            new Keyframe(0f, new Float4(0f, 0f, 0f, 1f)),
            new Keyframe(duration, new Float4(0f, 0f, 0f, 1f)));

        var scaleCurve = new Vector.AnimationCurve(3,
            new Keyframe(0f, Float3.One),
            new Keyframe(duration, Float3.One));

        clip.AddBone(new AnimationClip.AnimBone
        {
            BoneName = bonePath,
            Position = posCurve,
            Rotation = rotCurve,
            Scale = scaleCurve,
        });

        return clip;
    }

    private static AnimationClip CreateTwoBoneClip(string clipName, Float3 bone1Pos, Float3 bone2Pos, float duration = 1.0f)
    {
        var clip = new AnimationClip
        {
            Name = clipName,
            Duration = duration,
            Wrap = AnimationWrapMode.Loop
        };

        clip.AddBone(new AnimationClip.AnimBone
        {
            BoneName = "Bone1",
            Position = new Vector.AnimationCurve(3, new Keyframe(0f, bone1Pos), new Keyframe(duration, bone1Pos)),
            Rotation = new Vector.AnimationCurve(4, new Keyframe(0f, new Float4(0, 0, 0, 1)), new Keyframe(duration, new Float4(0, 0, 0, 1))),
            Scale = new Vector.AnimationCurve(3, new Keyframe(0f, Float3.One), new Keyframe(duration, Float3.One))
        });

        clip.AddBone(new AnimationClip.AnimBone
        {
            BoneName = "Bone2",
            Position = new Vector.AnimationCurve(3, new Keyframe(0f, bone2Pos), new Keyframe(duration, bone2Pos)),
            Rotation = new Vector.AnimationCurve(4, new Keyframe(0f, new Float4(0, 0, 0, 1)), new Keyframe(duration, new Float4(0, 0, 0, 1))),
            Scale = new Vector.AnimationCurve(3, new Keyframe(0f, Float3.One), new Keyframe(duration, Float3.One))
        });

        return clip;
    }

    private static (GameObject root, Transform bone1) CreateHierarchy()
    {
        var rootGO = new GameObject("Root");
        var bone1GO = new GameObject("Bone1");
        bone1GO.Transform.SetParent(rootGO.Transform);
        return (rootGO, bone1GO.Transform);
    }

    private static (GameObject root, Transform bone1, Transform bone2) CreateTwoBoneHierarchy()
    {
        var rootGO = new GameObject("Root");
        var bone1GO = new GameObject("Bone1");
        bone1GO.Transform.SetParent(rootGO.Transform);
        var bone2GO = new GameObject("Bone2");
        bone2GO.Transform.SetParent(rootGO.Transform);
        return (rootGO, bone1GO.Transform, bone2GO.Transform);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  1. Initial State Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void InitialState_StartsOnDefaultState()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipIdle = CreateClipWithPosition("Idle", "Bone1", new Float3(0, 1, 0), new Float3(0, 2, 0));
        var clipRun = CreateClipWithPosition("Run", "Bone1", new Float3(0, 5, 0), new Float3(0, 10, 0));

        var stateIdle = new AnimatorState("Idle", new AssetRef<AnimationClip>(clipIdle));
        var stateRun = new AnimatorState("Run", new AssetRef<AnimationClip>(clipRun));

        animator.AddState(stateIdle);
        animator.AddState(stateRun);
        animator.DefaultState = "Run";

        animator.OnEnable();

        Assert.NotNull(animator.CurrentState);
        Assert.Equal("Run", animator.CurrentState.Name);
        Assert.True(animator.IsPlaying);
        Assert.Equal(0f, animator.CurrentTime);
        Assert.False(animator.IsInTransition);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  2. Clip Playback Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ClipPlayback_EvaluatesAndAppliesToTransforms()
    {
        var (root, bone1) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clip = CreateClipWithPosition("Idle", "Bone1", new Float3(0, 0, 0), new Float3(0, 10, 0), 1.0f);
        var state = new AnimatorState("Idle", new AssetRef<AnimationClip>(clip));

        animator.AddState(state);
        animator.OnEnable();

        animator.Update(0.5f);

        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 5.0f) < 0.05f);
        Assert.True(MathF.Abs(animator.CurrentTime - 0.5f) < 0.001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  3. Loop and Time Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Loop_WhenTrue_WrapsTimeProperly()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clip = CreateClipWithPosition("Walk", "Bone1", Float3.Zero, Float3.One, 1.0f);
        var state = new AnimatorState("Walk", new AssetRef<AnimationClip>(clip), loop: true);

        animator.AddState(state);
        animator.OnEnable();

        animator.Update(1.25f);

        Assert.True(MathF.Abs(animator.CurrentTime - 0.25f) < 0.01f);
        Assert.True(animator.IsPlaying);
    }

    [Fact]
    public void Loop_WhenFalse_ClampsTimeAtDuration()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clip = CreateClipWithPosition("Attack", "Bone1", Float3.Zero, Float3.One, 1.0f);
        var state = new AnimatorState("Attack", new AssetRef<AnimationClip>(clip), loop: false);

        animator.AddState(state);
        animator.OnEnable();

        animator.Update(1.5f);

        Assert.True(MathF.Abs(animator.CurrentTime - 1.0f) < 0.001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  4. State Change via Play Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Play_ImmediateSwitch_ResetsTimeAndChangesState()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipA = CreateClipWithPosition("A", "Bone1", Float3.Zero, Float3.One, 1.0f);
        var clipB = CreateClipWithPosition("B", "Bone1", Float3.Zero, Float3.One, 2.0f);

        animator.AddState(new AnimatorState("StateA", new AssetRef<AnimationClip>(clipA)));
        animator.AddState(new AnimatorState("StateB", new AssetRef<AnimationClip>(clipB)));
        animator.OnEnable();

        animator.Update(0.7f);
        Assert.Equal("StateA", animator.CurrentState?.Name);
        Assert.True(MathF.Abs(animator.CurrentTime - 0.7f) < 0.001f);

        animator.Play("StateB", 0f);

        Assert.Equal("StateB", animator.CurrentState?.Name);
        Assert.Equal(0f, animator.CurrentTime);
        Assert.False(animator.IsInTransition);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  5. Transition with Duration Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Transition_WithDuration_TransitionsSmoothlyAndCompletes()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipA = CreateClipWithPosition("A", "Bone1", Float3.Zero, Float3.One, 1.0f);
        var clipB = CreateClipWithPosition("B", "Bone1", Float3.Zero, Float3.One, 1.0f);

        animator.AddState(new AnimatorState("StateA", new AssetRef<AnimationClip>(clipA)));
        animator.AddState(new AnimatorState("StateB", new AssetRef<AnimationClip>(clipB)));
        animator.OnEnable();

        // Start transition lasting 0.5s
        animator.Play("StateB", 0.5f);

        Assert.True(animator.IsInTransition);
        Assert.Equal("StateA", animator.CurrentState?.Name);
        Assert.Equal("StateB", animator.TargetState?.Name);
        Assert.Equal(0.5f, animator.TransitionDuration);

        // Step halfway
        animator.Update(0.25f);
        Assert.True(animator.IsInTransition);
        Assert.True(MathF.Abs(animator.TransitionProgress - 0.5f) < 0.05f);

        // Step past completion
        animator.Update(0.3f);
        Assert.False(animator.IsInTransition);
        Assert.Null(animator.TargetState);
        Assert.Equal("StateB", animator.CurrentState?.Name);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  6. Pose Interpolation during Transition Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Interpolation_DuringTransition_BlendsPosesCorrectly()
    {
        var (root, bone1) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        // Clip A stays at (0, 0, 0), Clip B stays at (0, 10, 0)
        var clipA = CreateClipWithPosition("A", "Bone1", new Float3(0, 0, 0), new Float3(0, 0, 0), 1.0f);
        var clipB = CreateClipWithPosition("B", "Bone1", new Float3(0, 10, 0), new Float3(0, 10, 0), 1.0f);

        animator.AddState(new AnimatorState("StateA", new AssetRef<AnimationClip>(clipA)));
        animator.AddState(new AnimatorState("StateB", new AssetRef<AnimationClip>(clipB)));
        animator.OnEnable();

        // Start 1.0s transition
        animator.Play("StateB", 1.0f);

        // Advance 0.5s -> blend factor = 0.5 -> expected Y = 5.0
        animator.Update(0.5f);

        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 5.0f) < 0.1f, $"Expected ~5.0 but got {bone1.LocalPosition.Y}");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  7. Speed Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Speed_MultipliesPlaybackTimeCorrectly()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clip = CreateClipWithPosition("Fast", "Bone1", Float3.Zero, Float3.One, 2.0f);
        var state = new AnimatorState("Fast", new AssetRef<AnimationClip>(clip), speed: 2.0f);

        animator.AddState(state);
        animator.Speed = 1.5f; // Combined speed = 1.5 * 2.0 = 3.0x
        animator.OnEnable();

        animator.Update(0.1f);

        Assert.True(MathF.Abs(animator.CurrentTime - 0.3f) < 0.01f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  8. Retargeting Compatibility Tests
    // ════════════════════════════════════════════════════════════════════════

    private static (SkeletonAsset, HumanoidMapping) CreateRetargetRig(string name, float heightScale)
    {
        var skel = new SkeletonAsset { Name = name };
        skel.AddBone(new SkeletonBone("Hips", -1, new Float3(0, 1.0f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("Spine", 0, new Float3(0, 0.2f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("Chest", 1, new Float3(0, 0.2f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("UpperChest", 2, new Float3(0, 0.2f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("Neck", 3, new Float3(0, 0.1f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("Head", 4, new Float3(0, 0.2f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));

        skel.AddBone(new SkeletonBone("LeftShoulder", 2, new Float3(-0.1f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftUpperArm", 6, new Float3(-0.2f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftLowerArm", 7, new Float3(-0.25f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftHand", 8, new Float3(-0.15f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));

        skel.AddBone(new SkeletonBone("RightShoulder", 2, new Float3(0.1f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightUpperArm", 10, new Float3(0.2f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightLowerArm", 11, new Float3(0.25f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightHand", 12, new Float3(0.15f * heightScale, 0, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));

        skel.AddBone(new SkeletonBone("LeftUpperLeg", 0, new Float3(-0.1f * heightScale, -0.4f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftLowerLeg", 14, new Float3(0, -0.4f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftFoot", 15, new Float3(0, -0.1f * heightScale, 0.1f * heightScale), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("LeftToes", 16, new Float3(0, 0, 0.1f * heightScale), Quaternion.Identity, Float3.One, Float4x4.Identity));

        skel.AddBone(new SkeletonBone("RightUpperLeg", 0, new Float3(0.1f * heightScale, -0.4f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightLowerLeg", 18, new Float3(0, -0.4f * heightScale, 0), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightFoot", 19, new Float3(0, -0.1f * heightScale, 0.1f * heightScale), Quaternion.Identity, Float3.One, Float4x4.Identity));
        skel.AddBone(new SkeletonBone("RightToes", 20, new Float3(0, 0, 0.1f * heightScale), Quaternion.Identity, Float3.One, Float4x4.Identity));

        var mapping = new HumanoidMapping { Skeleton = new AssetRef<SkeletonAsset>(skel) };
        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
            mapping.Set((HumanoidBone)i, i);

        return (skel, mapping);
    }

    [Fact]
    public void Retargeting_WhenConfigured_RetargetsPosesThroughAnimator()
    {
        var (srcSkel, srcMap) = CreateRetargetRig("SourceRig", 1.0f);
        var (tgtSkel, tgtMap) = CreateRetargetRig("TargetRig", 2.0f);

        var rootGO = new GameObject("Root");
        var hipsGO = new GameObject("Hips");
        hipsGO.Transform.SetParent(rootGO.Transform);

        var animator = rootGO.AddComponent<Animator>();
        animator.SourceSkeleton = new AssetRef<SkeletonAsset>(srcSkel);
        animator.SourceHumanoidMapping = new AssetRef<HumanoidMapping>(srcMap);
        animator.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(tgtMap);

        // Clip modifies Hips Y position from 1.0 to 1.5
        var clip = CreateClipWithPosition("Jump", "Hips", new Float3(0, 1.0f, 0), new Float3(0, 1.5f, 0), 1.0f);
        animator.AddState(new AnimatorState("Jump", new AssetRef<AnimationClip>(clip), loop: false));
        animator.OnEnable();

        animator.Update(1.0f);

        // Source translation delta = +0.5. Scale ratio = 2.0. Target rest = 2.0.
        // Expected target hips Y = 2.0 + (0.5 * 2.0) = 3.0
        Assert.True(MathF.Abs(hipsGO.Transform.LocalPosition.Y - 3.0f) < 0.1f,
            $"Expected ~3.0 but got {hipsGO.Transform.LocalPosition.Y}");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  9. Safe Failure Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SafeFailure_MissingClipOrNonexistentState_DoesNotCrash()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        // Empty state with null clip
        animator.AddState(new AnimatorState { Name = "Empty" });
        animator.OnEnable();

        // Should not throw
        animator.Update(0.1f);
        Assert.True(animator.IsPlaying);

        // Play nonexistent state
        animator.Play("NonExistent", 0f);
        animator.Update(0.1f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  10. PR10: Parameter Set & Get Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parameters_SetAndGet_AllFourTypes()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        animator.SetBool("IsRunning", true);
        animator.SetInt("WeaponId", 42);
        animator.SetFloat("Speed", 3.14f);
        animator.SetTrigger("Jump");

        Assert.True(animator.GetBool("IsRunning"));
        Assert.Equal(42, animator.GetInt("WeaponId"));
        Assert.True(MathF.Abs(animator.GetFloat("Speed") - 3.14f) < 1e-4f);

        // Reset trigger
        animator.ResetTrigger("Jump");

        // Nonexistent parameters return default
        Assert.False(animator.GetBool("NonExistentBool"));
        Assert.Equal(0, animator.GetInt("NonExistentInt"));
        Assert.Equal(0f, animator.GetFloat("NonExistentFloat"));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  11. PR10: Bool / Int / Float Conditions
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Conditions_BoolCondition_TriggersTransitionAutomatically()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipIdle = CreateClipWithPosition("Idle", "Bone1", Float3.Zero, Float3.Zero);
        var clipRun = CreateClipWithPosition("Run", "Bone1", Float3.One, Float3.One);

        animator.AddState(new AnimatorState("Idle", new AssetRef<AnimationClip>(clipIdle)));
        animator.AddState(new AnimatorState("Run", new AssetRef<AnimationClip>(clipRun)));

        // Transition: Idle -> Run when IsMoving == true
        animator.AddTransition(new AnimatorTransition("Idle", "Run", 0.2f,
            new AnimatorCondition("IsMoving", AnimatorConditionMode.If)));

        animator.OnEnable();
        Assert.Equal("Idle", animator.CurrentState?.Name);

        // Set parameter to true
        animator.SetBool("IsMoving", true);

        // Update should trigger transition
        animator.Update(0.01f);

        Assert.True(animator.IsInTransition);
        Assert.Equal("Idle", animator.CurrentState?.Name);
        Assert.Equal("Run", animator.TargetState?.Name);
    }

    [Fact]
    public void Conditions_NumericComparisons_WorkCorrectly()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipIdle = CreateClipWithPosition("Idle", "Bone1", Float3.Zero, Float3.Zero);
        var clipWalk = CreateClipWithPosition("Walk", "Bone1", Float3.One, Float3.One);

        animator.AddState(new AnimatorState("Idle", new AssetRef<AnimationClip>(clipIdle)));
        animator.AddState(new AnimatorState("Walk", new AssetRef<AnimationClip>(clipWalk)));

        // Transition when Speed > 0.5
        animator.AddTransition(new AnimatorTransition("Idle", "Walk", 0.2f,
            new AnimatorCondition("Speed", AnimatorConditionMode.Greater, 0.5f)));

        animator.OnEnable();

        // Speed = 0.2 (condition not met)
        animator.SetFloat("Speed", 0.2f);
        animator.Update(0.01f);
        Assert.False(animator.IsInTransition);

        // Speed = 0.8 (condition met)
        animator.SetFloat("Speed", 0.8f);
        animator.Update(0.01f);
        Assert.True(animator.IsInTransition);
        Assert.Equal("Walk", animator.TargetState?.Name);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  12. PR10: Trigger Consumption
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Trigger_ConsumesWhenTransitionStarts()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipIdle = CreateClipWithPosition("Idle", "Bone1", Float3.Zero, Float3.Zero);
        var clipJump = CreateClipWithPosition("Jump", "Bone1", Float3.One, Float3.One);

        animator.AddState(new AnimatorState("Idle", new AssetRef<AnimationClip>(clipIdle)));
        animator.AddState(new AnimatorState("Jump", new AssetRef<AnimationClip>(clipJump)));

        animator.AddTransition(new AnimatorTransition("Idle", "Jump", 0.1f,
            new AnimatorCondition("DoJump", AnimatorConditionMode.If)));

        animator.OnEnable();

        animator.SetTrigger("DoJump");
        animator.Update(0.01f);

        // Transition started
        Assert.True(animator.IsInTransition);
        Assert.Equal("Jump", animator.TargetState?.Name);

        // Complete the transition
        animator.Update(0.2f);
        Assert.False(animator.IsInTransition);
        Assert.Equal("Jump", animator.CurrentState?.Name);

        // Now if we have transition Jump -> Idle when DoJump is set, it shouldn't trigger
        // because DoJump was consumed
        animator.AddTransition(new AnimatorTransition("Jump", "Idle", 0.1f,
            new AnimatorCondition("DoJump", AnimatorConditionMode.If)));

        animator.Update(0.01f);
        Assert.False(animator.IsInTransition);
        Assert.Equal("Jump", animator.CurrentState?.Name);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  13. PR10: Multiple Conditions (AND Logic)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MultiConditions_RequiresAllToBeTrue()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipIdle = CreateClipWithPosition("Idle", "Bone1", Float3.Zero, Float3.Zero);
        var clipSpecial = CreateClipWithPosition("Special", "Bone1", Float3.One, Float3.One);

        animator.AddState(new AnimatorState("Idle", new AssetRef<AnimationClip>(clipIdle)));
        animator.AddState(new AnimatorState("Special", new AssetRef<AnimationClip>(clipSpecial)));

        // Transition requires IsGrounded == true AND Mana >= 10
        animator.AddTransition(new AnimatorTransition("Idle", "Special", 0.1f,
            new AnimatorCondition("IsGrounded", AnimatorConditionMode.If),
            new AnimatorCondition("Mana", AnimatorConditionMode.Greater, 9f)));

        animator.OnEnable();

        // Only IsGrounded is true
        animator.SetBool("IsGrounded", true);
        animator.SetFloat("Mana", 5f);
        animator.Update(0.01f);
        Assert.False(animator.IsInTransition);

        // Both true
        animator.SetFloat("Mana", 15f);
        animator.Update(0.01f);
        Assert.True(animator.IsInTransition);
        Assert.Equal("Special", animator.TargetState?.Name);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  14. PR10: Deterministic Evaluation Order (List Order)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MultipleTransitions_UsesDeterministicListOrder()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipIdle = CreateClipWithPosition("Idle", "Bone1", Float3.Zero, Float3.Zero);
        var clipA = CreateClipWithPosition("StateA", "Bone1", Float3.One, Float3.One);
        var clipB = CreateClipWithPosition("StateB", "Bone1", Float3.One, Float3.One);

        animator.AddState(new AnimatorState("Idle", new AssetRef<AnimationClip>(clipIdle)));
        animator.AddState(new AnimatorState("StateA", new AssetRef<AnimationClip>(clipA)));
        animator.AddState(new AnimatorState("StateB", new AssetRef<AnimationClip>(clipB)));

        // Both transitions valid when Flag == true, but StateA is defined first
        animator.AddTransition(new AnimatorTransition("Idle", "StateA", 0.1f,
            new AnimatorCondition("Flag", AnimatorConditionMode.If)));
        animator.AddTransition(new AnimatorTransition("Idle", "StateB", 0.1f,
            new AnimatorCondition("Flag", AnimatorConditionMode.If)));

        animator.OnEnable();
        animator.SetBool("Flag", true);
        animator.Update(0.01f);

        Assert.True(animator.IsInTransition);
        Assert.Equal("StateA", animator.TargetState?.Name);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  15. PR11: Blend Tree 1D (Clamping & Interpolation)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BlendTree1D_InterpolationAndClamping_WorksCorrectly()
    {
        var (root, bone1) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        // Clip 0 at (0, 0, 0), Clip 1 at (0, 10, 0)
        var clip0 = CreateClipWithPosition("Walk", "Bone1", new Float3(0, 0, 0), new Float3(0, 0, 0));
        var clip1 = CreateClipWithPosition("Run", "Bone1", new Float3(0, 10, 0), new Float3(0, 10, 0));

        var blendTree = new BlendTree(BlendTreeType.Simple1D, "Speed");
        blendTree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clip0), 0.0f));
        blendTree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clip1), 1.0f));

        var locomotionState = new AnimatorState("Locomotion", blendTree);
        animator.AddState(locomotionState);
        animator.OnEnable();

        // Test lower clamp (Speed <= 0.0) -> 100% Clip 0 -> Y = 0.0
        animator.SetFloat("Speed", -0.5f);
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 0.0f) < 0.01f);

        // Test upper clamp (Speed >= 1.0) -> 100% Clip 1 -> Y = 10.0
        animator.SetFloat("Speed", 1.5f);
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 10.0f) < 0.01f);

        // Test 50% blend (Speed = 0.5) -> Y = 5.0
        animator.SetFloat("Speed", 0.5f);
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 5.0f) < 0.05f);

        // Test 25% blend (Speed = 0.25) -> Y = 2.5
        animator.SetFloat("Speed", 0.25f);
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 2.5f) < 0.05f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  16. PR11: Blend Tree 2D (Exact Match & Multidirectional Blending)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BlendTree2D_ExactMatchAndBlending_WorksCorrectly()
    {
        var (root, bone1) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        // Center (0, 0) -> (0, 0, 0)
        // Forward (0, 1) -> (0, 10, 0)
        // Right (1, 0) -> (10, 0, 0)
        var clipCenter = CreateClipWithPosition("Center", "Bone1", new Float3(0, 0, 0), new Float3(0, 0, 0));
        var clipFwd = CreateClipWithPosition("Forward", "Bone1", new Float3(0, 10, 0), new Float3(0, 10, 0));
        var clipRight = CreateClipWithPosition("Right", "Bone1", new Float3(10, 0, 0), new Float3(10, 0, 0));

        var blendTree = new BlendTree(BlendTreeType.Simple2D, "VelX", "VelY");
        blendTree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clipCenter), new Float2(0, 0)));
        blendTree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clipFwd), new Float2(0, 1)));
        blendTree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clipRight), new Float2(1, 0)));

        animator.AddState(new AnimatorState("Move2D", blendTree));
        animator.OnEnable();

        // Exact match on Forward (0, 1) -> must be (0, 10, 0)
        animator.SetFloat("VelX", 0f);
        animator.SetFloat("VelY", 1f);
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.X - 0f) < 0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 10f) < 0.01f);

        // Exact match on Right (1, 0) -> must be (10, 0, 0)
        animator.SetFloat("VelX", 1f);
        animator.SetFloat("VelY", 0f);
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.X - 10f) < 0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 0f) < 0.01f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  17. PR11: Crossfade between BlendTree and Single Clip State
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Crossfade_BetweenBlendTreeAndClip_BlendsPosesSmoothly()
    {
        var (root, bone1) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipBT0 = CreateClipWithPosition("BT0", "Bone1", new Float3(0, 0, 0), new Float3(0, 0, 0));
        var clipBT1 = CreateClipWithPosition("BT1", "Bone1", new Float3(0, 10, 0), new Float3(0, 10, 0));
        var tree = new BlendTree(BlendTreeType.Simple1D, "Blend");
        tree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clipBT0), 0.0f));
        tree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clipBT1), 1.0f));

        var clipTarget = CreateClipWithPosition("Target", "Bone1", new Float3(0, 20, 0), new Float3(0, 20, 0));

        animator.AddState(new AnimatorState("BTState", tree));
        animator.AddState(new AnimatorState("ClipState", new AssetRef<AnimationClip>(clipTarget)));
        animator.OnEnable();

        // Set Blend = 1.0 -> BTState outputs Y = 10.0
        animator.SetFloat("Blend", 1.0f);
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 10.0f) < 0.05f);

        // Start 1.0s transition to ClipState (which has Y = 20.0)
        animator.Play("ClipState", 1.0f);

        // Step 0.5s -> blend factor 0.5 -> expected Y = 15.0
        animator.Update(0.5f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 15.0f) < 0.1f);

        // Finish transition
        animator.Update(0.6f);
        Assert.False(animator.IsInTransition);
        Assert.Equal("ClipState", animator.CurrentState?.Name);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 20.0f) < 0.05f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  18. PR12: Animator Layers (Multi-Layer & Masking)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Layers_SecondaryLayer_PlaysIndependentlyAndBlendsByWeight()
    {
        var (root, bone1) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        // Layer 0 (Base): Clip Y = 0.0
        var baseClip = CreateClipWithPosition("BaseIdle", "Bone1", Float3.Zero, Float3.Zero);
        animator.AddState(new AnimatorState("BaseIdle", new AssetRef<AnimationClip>(baseClip)));

        // Layer 1 (Upper): Clip Y = 10.0
        var layer1 = new AnimatorLayer("UpperLayer", 0.5f);
        var upperClip = CreateClipWithPosition("UpperWave", "Bone1", new Float3(0, 10, 0), new Float3(0, 10, 0));
        layer1.AddState(new AnimatorState("UpperWave", new AssetRef<AnimationClip>(upperClip)));
        animator.AddLayer(layer1);

        animator.OnEnable();

        // Layer 1 Weight = 0.5 -> 50% blend of 0.0 and 10.0 -> Y = 5.0
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 5.0f) < 0.05f);

        // Change Layer 1 Weight to 0.0 -> 100% Base Layer -> Y = 0.0
        animator.SetLayerWeight(1, 0f);
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 0.0f) < 0.01f);

        // Change Layer 1 Weight to 1.0 -> 100% Layer 1 -> Y = 10.0
        animator.SetLayerWeight(1, 1f);
        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 10.0f) < 0.01f);
    }

    [Fact]
    public void Layers_AvatarMask_RestrictsAffectedBones()
    {
        var (root, bone1, bone2) = CreateTwoBoneHierarchy();
        var animator = root.AddComponent<Animator>();

        // Base Layer: Bone1 at (0, 0, 0), Bone2 at (0, 0, 0)
        var baseClip = CreateTwoBoneClip("Base", Float3.Zero, Float3.Zero);
        animator.AddState(new AnimatorState("Base", new AssetRef<AnimationClip>(baseClip)));

        // Layer 1: Bone1 at (0, 10, 0), Bone2 at (0, 20, 0)
        var layer1 = new AnimatorLayer("MaskedLayer", 1.0f);
        var layerClip = CreateTwoBoneClip("LayerClip", new Float3(0, 10, 0), new Float3(0, 20, 0));
        layer1.AddState(new AnimatorState("LayerState", new AssetRef<AnimationClip>(layerClip)));

        // Mask: include bone 0 (Hips / Bone1), exclude bone 1 (Spine / Bone2)
        var mask = new AvatarMask();
        mask.SetIncluded((HumanoidBone)0, true, 1.0f);
        mask.SetIncluded((HumanoidBone)1, false, 0.0f);
        layer1.Mask = mask;

        animator.AddLayer(layer1);
        animator.OnEnable();

        animator.Update(0.01f);

        // Bone1 should be overridden by Layer 1 -> Y = 10.0
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 10.0f) < 0.05f, $"Expected Bone1 ~10.0 but got {bone1.LocalPosition.Y}");

        // Bone2 should be masked out and retain Base Layer -> Y = 0.0
        Assert.True(MathF.Abs(bone2.LocalPosition.Y - 0.0f) < 0.05f, $"Expected Bone2 ~0.0 but got {bone2.LocalPosition.Y}");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  19. PR13: Advanced Avatar Masks (Hierarchy, Overrides, Helpers, Blending)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AvatarMask_IndividualBoneWeight_GetAndSet()
    {
        var mask = new AvatarMask();
        mask.SetWeight(HumanoidBone.Head, 0.7f);
        Assert.True(mask.IsIncluded(HumanoidBone.Head));
        Assert.True(MathF.Abs(mask.GetWeight(HumanoidBone.Head) - 0.7f) < 1e-4f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.Head) - 0.7f) < 1e-4f);

        // Exclude bone
        mask.SetIncluded(HumanoidBone.Head, false);
        Assert.False(mask.IsIncluded(HumanoidBone.Head));
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.Head));
    }

    [Fact]
    public void AvatarMask_HierarchyWeight_AppliesToDescendants()
    {
        var mask = new AvatarMask();
        mask.SetHierarchyWeight(HumanoidBone.Spine, 0.6f);

        // Spine and its descendants should have 0.6f
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.Spine) - 0.6f) < 1e-4f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.Chest) - 0.6f) < 1e-4f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.Neck) - 0.6f) < 1e-4f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.Head) - 0.6f) < 1e-4f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.LeftHand) - 0.6f) < 1e-4f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.RightHand) - 0.6f) < 1e-4f);

        // Hips and Legs should remain at default (1.0f)
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.Hips) - 1.0f) < 1e-4f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.LeftUpperLeg) - 1.0f) < 1e-4f);
    }

    [Fact]
    public void AvatarMask_HierarchyOverride_AllowsChildOverride()
    {
        var mask = new AvatarMask();
        mask.Clear();

        // Enable full upper body via Spine hierarchy
        mask.SetHierarchyIncluded(HumanoidBone.Spine, true, 1.0f);

        // Override specific children
        mask.SetIncluded(HumanoidBone.Head, false);
        mask.SetWeight(HumanoidBone.LeftHand, 0.4f);

        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.Head));
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.LeftHand) - 0.4f) < 1e-4f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.Neck) - 1.0f) < 1e-4f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.RightHand) - 1.0f) < 1e-4f);
    }

    [Fact]
    public void AvatarMask_UpperBodyMask_FiltersCorrectly()
    {
        var mask = AvatarMask.CreateUpperBodyMask();

        // Lower body must be 0
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.Hips));
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.LeftUpperLeg));
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.RightUpperLeg));
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.LeftFoot));

        // Upper body must be 1.0
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.Spine));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.Chest));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.Neck));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.Head));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.LeftHand));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.RightHand));
    }

    [Fact]
    public void AvatarMask_LowerBodyMask_FiltersCorrectly()
    {
        var mask = AvatarMask.CreateLowerBodyMask();

        // Lower body must be 1.0
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.Hips));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.LeftUpperLeg));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.RightUpperLeg));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.LeftLowerLeg));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.LeftFoot));
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.RightFoot));

        // Upper body must be 0
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.Spine));
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.Chest));
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.Neck));
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.Head));
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.LeftHand));
    }

    [Fact]
    public void AvatarMask_WeightZeroOneIntermediate_CalculatesEffectiveWeight()
    {
        var mask = new AvatarMask();

        mask.SetWeight(HumanoidBone.Spine, 0f);
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.Spine));

        mask.SetWeight(HumanoidBone.Chest, 0.5f);
        Assert.True(MathF.Abs(mask.GetEffectiveWeight(HumanoidBone.Chest) - 0.5f) < 1e-4f);

        mask.SetWeight(HumanoidBone.Neck, 1f);
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.Neck));

        // Clamping tests
        mask.SetWeight(HumanoidBone.Head, 2.5f);
        Assert.Equal(1f, mask.GetEffectiveWeight(HumanoidBone.Head));

        mask.SetWeight(HumanoidBone.LeftShoulder, -0.5f);
        Assert.Equal(0f, mask.GetEffectiveWeight(HumanoidBone.LeftShoulder));
    }

    [Fact]
    public void AvatarMask_Validate_ReturnsExpectedStatus()
    {
        var mask = new AvatarMask();
        Assert.True(mask.Validate(out string? error));
        Assert.Null(error);

        // Cleared mask has 0 active bones -> invalid
        mask.Clear();
        Assert.False(mask.Validate(out error));
        Assert.NotNull(error);

        // Re-enable one bone -> valid
        mask.SetIncluded(HumanoidBone.Spine, true, 0.5f);
        Assert.True(mask.Validate(out error));
        Assert.Null(error);
    }

    [Fact]
    public void Animator_LayerWithHierarchyMask_BlendsSubtreeCorrectly()
    {
        var (root, bone1, bone2) = CreateTwoBoneHierarchy();
        var animator = root.AddComponent<Animator>();

        // Base Layer (Bone1 = Hips, Bone2 = Spine): Position (0, 0, 0)
        var baseClip = CreateTwoBoneClip("Base", Float3.Zero, Float3.Zero);
        animator.AddState(new AnimatorState("Base", new AssetRef<AnimationClip>(baseClip)));

        // Layer 1: Position (0, 10, 0) on both bones
        var layer1 = new AnimatorLayer("UpperBodyLayer", 1.0f);
        var layerClip = CreateTwoBoneClip("LayerClip", new Float3(0, 10, 0), new Float3(0, 10, 0));
        layer1.AddState(new AnimatorState("LayerState", new AssetRef<AnimationClip>(layerClip)));

        // Upper body mask: excludes Hips (Bone1, idx 0), includes Spine hierarchy (Bone2, idx 1)
        var mask = AvatarMask.CreateUpperBodyMask();
        layer1.Mask = mask;

        animator.AddLayer(layer1);
        animator.OnEnable();

        animator.Update(0.01f);

        // Bone1 (Hips) should remain untouched from Base Layer -> Y = 0
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 0f) < 0.05f, $"Expected Bone1 ~0 but got {bone1.LocalPosition.Y}");

        // Bone2 (Spine) should be fully applied from Layer 1 -> Y = 10
        Assert.True(MathF.Abs(bone2.LocalPosition.Y - 10f) < 0.05f, $"Expected Bone2 ~10 but got {bone2.LocalPosition.Y}");
    }

    [Fact]
    public void Animator_LayerWithMask_AndBlendTree_EvaluatesAccurately()
    {
        var (root, bone1, bone2) = CreateTwoBoneHierarchy();
        var animator = root.AddComponent<Animator>();

        var baseClip = CreateTwoBoneClip("Base", Float3.Zero, Float3.Zero);
        animator.AddState(new AnimatorState("Base", new AssetRef<AnimationClip>(baseClip)));

        // Layer 1 with 1D Blend Tree
        var layer1 = new AnimatorLayer("BlendLayer", 1.0f);
        var clipA = CreateTwoBoneClip("ClipA", new Float3(0, 10, 0), new Float3(0, 10, 0));
        var clipB = CreateTwoBoneClip("ClipB", new Float3(0, 20, 0), new Float3(0, 20, 0));

        var tree = new BlendTree(BlendTreeType.Simple1D, "BlendParam");
        tree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clipA), 0.0f));
        tree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clipB), 1.0f));

        layer1.AddState(new AnimatorState("BTState", tree));

        // Mask only Bone2 (Spine)
        var mask = AvatarMask.CreateUpperBodyMask();
        layer1.Mask = mask;
        animator.AddLayer(layer1);
        animator.OnEnable();

        animator.SetFloat("BlendParam", 0.5f); // Expected blend pose Y = 15.0
        animator.Update(0.01f);

        // Bone1 (Hips) -> Y = 0.0
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 0f) < 0.05f);

        // Bone2 (Spine) -> Y = 15.0
        Assert.True(MathF.Abs(bone2.LocalPosition.Y - 15f) < 0.1f, $"Expected Bone2 ~15 but got {bone2.LocalPosition.Y}");
    }

    [Fact]
    public void Animator_LayerWithMask_AndCrossFade_BlendsSmoothly()
    {
        var (root, bone1, bone2) = CreateTwoBoneHierarchy();
        var animator = root.AddComponent<Animator>();

        var baseClip = CreateTwoBoneClip("Base", Float3.Zero, Float3.Zero);
        animator.AddState(new AnimatorState("Base", new AssetRef<AnimationClip>(baseClip)));

        // Layer 1 with two states
        var layer1 = new AnimatorLayer("CrossFadeLayer", 1.0f);
        var clipA = CreateTwoBoneClip("ClipA", new Float3(0, 10, 0), new Float3(0, 10, 0));
        var clipB = CreateTwoBoneClip("ClipB", new Float3(0, 30, 0), new Float3(0, 30, 0));

        layer1.AddState(new AnimatorState("StateA", new AssetRef<AnimationClip>(clipA)));
        layer1.AddState(new AnimatorState("StateB", new AssetRef<AnimationClip>(clipB)));

        var mask = AvatarMask.CreateUpperBodyMask();
        layer1.Mask = mask;
        animator.AddLayer(layer1);
        animator.OnEnable();

        animator.Update(0.01f);
        Assert.True(MathF.Abs(bone2.LocalPosition.Y - 10f) < 0.05f);

        // CrossFade on Layer 1 over 1.0s
        animator.CrossFade("StateB", 1.0f, layerIndex: 1);

        // Advance 0.5s -> 50% transition from 10 to 30 = 20.0
        animator.Update(0.5f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 0f) < 0.05f);
        Assert.True(MathF.Abs(bone2.LocalPosition.Y - 20f) < 0.1f, $"Expected Bone2 ~20 but got {bone2.LocalPosition.Y}");
    }

    [Fact]
    public void Animator_LayerWithMask_AndRetargeting_PreservesMasking()
    {
        var (srcSkel, srcMap) = CreateRetargetRig("SourceRig", 1.0f);
        var (tgtSkel, tgtMap) = CreateRetargetRig("TargetRig", 2.0f);

        var rootGO = new GameObject("Root");
        var hipsGO = new GameObject("Hips");
        hipsGO.Transform.SetParent(rootGO.Transform);
        var spineGO = new GameObject("Spine");
        spineGO.Transform.SetParent(hipsGO.Transform);

        var animator = rootGO.AddComponent<Animator>();
        animator.SourceSkeleton = new AssetRef<SkeletonAsset>(srcSkel);
        animator.SourceHumanoidMapping = new AssetRef<HumanoidMapping>(srcMap);
        animator.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(tgtMap);

        // Base layer: Jump clip (modifies Hips Y from 1.0 to 1.5)
        var baseClip = CreateClipWithPosition("BaseJump", "Hips", new Float3(0, 1.0f, 0), new Float3(0, 1.5f, 0), 1.0f);
        animator.AddState(new AnimatorState("BaseJump", new AssetRef<AnimationClip>(baseClip), loop: false));

        // Layer 1: Upper body wave clip modifying Spine Y
        var layer1 = new AnimatorLayer("UpperLayer", 1.0f);
        var upperClip = CreateClipWithPosition("UpperWave", "Spine", new Float3(0, 0.2f, 0), new Float3(0, 0.6f, 0), 1.0f);
        layer1.AddState(new AnimatorState("UpperWave", new AssetRef<AnimationClip>(upperClip), loop: false));
        layer1.Mask = AvatarMask.CreateUpperBodyMask();
        animator.AddLayer(layer1);

        animator.OnEnable();
        animator.Update(1.0f);

        // Target Hips: rest = 2.0 + (0.5 * 2.0) = 3.0
        Assert.True(MathF.Abs(hipsGO.Transform.LocalPosition.Y - 3.0f) < 0.15f,
            $"Expected Hips ~3.0 but got {hipsGO.Transform.LocalPosition.Y}");
    }

    [Fact]
    public void Animator_ZeroAllocations_AcrossLayersWithAdvancedMasks()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipBase = CreateClipWithPosition("Base", "Bone1", Float3.Zero, Float3.One);
        animator.AddState(new AnimatorState("Base", new AssetRef<AnimationClip>(clipBase)));

        var layer1 = new AnimatorLayer("Layer1", 0.5f);
        var clip1 = CreateClipWithPosition("Clip1", "Bone1", Float3.One, Float3.Zero);
        layer1.AddState(new AnimatorState("Clip1", new AssetRef<AnimationClip>(clip1)));
        layer1.Mask = AvatarMask.CreateUpperBodyMask();
        animator.AddLayer(layer1);

        animator.OnEnable();

        // Warm up
        for (int i = 0; i < 5; i++)
            animator.Update(0.016f);

        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 50; i++)
        {
            animator.Update(0.016f);
        }
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, allocAfter - allocBefore);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  20. PR14: Root Motion Tests
    // ════════════════════════════════════════════════════════════════════════

    private static AnimationClip CreateClipWithRotation(string clipName, string bonePath, Quaternion startRot, Quaternion endRot, float duration = 1.0f)
    {
        var clip = new AnimationClip
        {
            Name = clipName,
            Duration = duration,
            Wrap = AnimationWrapMode.Loop
        };

        var posCurve = new Vector.AnimationCurve(3,
            new Keyframe(0f, Float3.Zero),
            new Keyframe(duration, Float3.Zero));

        var rotCurve = new Vector.AnimationCurve(4,
            new Keyframe(0f, new Float4(startRot.X, startRot.Y, startRot.Z, startRot.W)),
            new Keyframe(duration, new Float4(endRot.X, endRot.Y, endRot.Z, endRot.W)));

        var scaleCurve = new Vector.AnimationCurve(3,
            new Keyframe(0f, Float3.One),
            new Keyframe(duration, Float3.One));

        clip.AddBone(new AnimationClip.AnimBone
        {
            BoneName = bonePath,
            Position = posCurve,
            Rotation = rotCurve,
            Scale = scaleCurve,
        });

        return clip;
    }

    [Fact]
    public void RootMotion_Disabled_DoesNotMoveGameObject()
    {
        var (root, bone1) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = false;

        var clip = CreateClipWithPosition("Walk", "Bone1", Float3.Zero, new Float3(0, 0, 10), 1.0f);
        animator.AddState(new AnimatorState("Walk", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        animator.Update(0.5f);

        // GameObject transform must NOT move
        Assert.Equal(Float3.Zero, root.Transform.LocalPosition);
        // Bone transform must have moved
        Assert.True(MathF.Abs(bone1.LocalPosition.Z - 5.0f) < 0.05f);
        // Frame delta is still exposed via API
        Assert.True(MathF.Abs(animator.RootMotionPositionDelta.Z - 5.0f) < 0.05f);
    }

    [Fact]
    public void RootMotion_LinearMovement_ExtractedAndAppliedToGameObject()
    {
        var (root, bone1) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;

        var clip = CreateClipWithPosition("Walk", "Bone1", Float3.Zero, new Float3(0, 0, 10), 1.0f);
        animator.AddState(new AnimatorState("Walk", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        animator.Update(0.5f);

        // GameObject transform moves by +5 in Z
        Assert.True(MathF.Abs(root.Transform.LocalPosition.Z - 5.0f) < 0.05f, $"Expected root Z ~5.0 but got {root.Transform.LocalPosition.Z}");
        // Root bone is grounded at rest position (0, 0, 0) to prevent double motion
        Assert.True(MathF.Abs(bone1.LocalPosition.Z - 0.0f) < 0.05f, $"Expected bone Z ~0.0 but got {bone1.LocalPosition.Z}");
    }

    [Fact]
    public void RootMotion_Rotation_ExtractedAndAppliedToGameObject()
    {
        var (root, bone1) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;

        var rot0 = Quaternion.FromEuler(new Float3(0, 0, 0));
        var rot90 = Quaternion.FromEuler(new Float3(0, 90, 0));
        var clip = CreateClipWithRotation("Turn", "Bone1", rot0, rot90, 1.0f);
        animator.AddState(new AnimatorState("Turn", new AssetRef<AnimationClip>(clip), loop: false));
        animator.OnEnable();

        animator.Update(1.0f);

        // GameObject should rotate ~90 degrees
        Float3 euler = root.Transform.LocalRotation.EulerAngles;
        Assert.True(MathF.Abs(euler.Y - 90.0f) < 1.0f || MathF.Abs(euler.Y - (-270.0f)) < 1.0f,
            $"Expected Y rotation ~90 but got {euler.Y}");
    }

    [Fact]
    public void RootMotion_PerFrameDelta_CalculatedAccurately()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;

        // Moves 10 units in X over 1.0s = 10 units/s
        var clip = CreateClipWithPosition("Stride", "Bone1", Float3.Zero, new Float3(10, 0, 0), 1.0f);
        animator.AddState(new AnimatorState("Stride", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        // Step 0.1s -> expected delta = 1.0
        animator.Update(0.1f);
        Assert.True(MathF.Abs(animator.RootMotionPositionDelta.X - 1.0f) < 0.05f);

        // Step 0.2s -> expected delta = 2.0
        animator.Update(0.2f);
        Assert.True(MathF.Abs(animator.RootMotionPositionDelta.X - 2.0f) < 0.05f);
    }

    [Fact]
    public void RootMotion_LoopPosition_ContinuesWithoutJump()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;

        // Moves 10 units in Z over 1.0s, looping
        var clip = CreateClipWithPosition("RunLoop", "Bone1", Float3.Zero, new Float3(0, 0, 10), 1.0f);
        animator.AddState(new AnimatorState("RunLoop", new AssetRef<AnimationClip>(clip), loop: true));
        animator.OnEnable();

        // Advance to 0.9s -> pos ~9.0
        animator.Update(0.9f);
        Assert.True(MathF.Abs(root.Transform.LocalPosition.Z - 9.0f) < 0.1f);

        // Advance 0.2s -> time wraps from 0.9s to 0.1s.
        // Loop handling should add cycle delta (10.0), so delta = +2.0 and total pos ~11.0
        animator.Update(0.2f);
        Assert.True(MathF.Abs(root.Transform.LocalPosition.Z - 11.0f) < 0.15f,
            $"Expected ~11.0 after loop wrap but got {root.Transform.LocalPosition.Z}");
        Assert.True(animator.RootMotionPositionDelta.Z > 0f, "Delta across loop must remain forward/positive.");
    }

    [Fact]
    public void RootMotion_LoopRotation_ContinuesWithoutJump()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;

        var rot0 = Quaternion.FromEuler(new Float3(0, 0, 0));
        var rot180 = Quaternion.FromEuler(new Float3(0, 180, 0)); // 180 deg turn over 1.0s
        var clip = CreateClipWithRotation("SpinLoop", "Bone1", rot0, rot180, 1.0f);
        animator.AddState(new AnimatorState("SpinLoop", new AssetRef<AnimationClip>(clip), loop: true));
        animator.OnEnable();

        animator.Update(0.9f);
        animator.Update(0.2f); // Wrap

        // Verify no backward snap
        Assert.True(animator.RootMotionRotationDelta != Quaternion.Identity);
    }

    [Fact]
    public void RootMotion_BlendTree_ProducesCorrectDelta()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;

        // Walk = 2 units in Z over 1.0s, Run = 10 units in Z over 1.0s
        var clipWalk = CreateClipWithPosition("Walk", "Bone1", Float3.Zero, new Float3(0, 0, 2), 1.0f);
        var clipRun = CreateClipWithPosition("Run", "Bone1", Float3.Zero, new Float3(0, 0, 10), 1.0f);

        var tree = new BlendTree(BlendTreeType.Simple1D, "Speed");
        tree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clipWalk), 0.0f));
        tree.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clipRun), 1.0f));

        animator.AddState(new AnimatorState("Locomotion", tree));
        animator.OnEnable();

        // 50% blend -> speed = 6 units in Z per second
        animator.SetFloat("Speed", 0.5f);
        animator.Update(0.5f);

        // Step 0.5s -> expected displacement ~3.0
        Assert.True(MathF.Abs(root.Transform.LocalPosition.Z - 3.0f) < 0.1f,
            $"Expected ~3.0 but got {root.Transform.LocalPosition.Z}");
    }

    [Fact]
    public void RootMotion_CrossFade_ProducesContinuousDeltas()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;

        var clipWalk = CreateClipWithPosition("Walk", "Bone1", Float3.Zero, new Float3(0, 0, 2), 1.0f);
        var clipRun = CreateClipWithPosition("Run", "Bone1", Float3.Zero, new Float3(0, 0, 10), 1.0f);

        animator.AddState(new AnimatorState("Walk", new AssetRef<AnimationClip>(clipWalk)));
        animator.AddState(new AnimatorState("Run", new AssetRef<AnimationClip>(clipRun)));
        animator.OnEnable();

        animator.Update(0.1f);
        animator.CrossFade("Run", 1.0f);

        // Step through crossfade
        animator.Update(0.5f);

        // Deltas should remain positive and forward
        Assert.True(animator.RootMotionPositionDelta.Z > 0f);
        Assert.True(root.Transform.LocalPosition.Z > 0f);
    }

    [Fact]
    public void RootMotion_WithLayersAndMasks_PreservesRootMotion()
    {
        var (root, bone1, bone2) = CreateTwoBoneHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;

        // Layer 0: Walk in Z
        var baseClip = CreateTwoBoneClip("BaseWalk", Float3.Zero, Float3.Zero, 1.0f);
        baseClip.Bones[0].Position = new Vector.AnimationCurve(3, new Keyframe(0f, Float3.Zero), new Keyframe(1.0f, new Float3(0, 0, 10)));
        animator.AddState(new AnimatorState("BaseWalk", new AssetRef<AnimationClip>(baseClip)));

        // Layer 1: Upper body wave with UpperBodyMask
        var layer1 = new AnimatorLayer("UpperLayer", 1.0f);
        var upperClip = CreateTwoBoneClip("UpperWave", Float3.Zero, new Float3(0, 5, 0), 1.0f);
        layer1.AddState(new AnimatorState("UpperWave", new AssetRef<AnimationClip>(upperClip)));
        layer1.Mask = AvatarMask.CreateUpperBodyMask();
        animator.AddLayer(layer1);

        animator.OnEnable();
        animator.Update(0.5f);

        // Root motion from Layer 0 moves GameObject by ~5.0 in Z
        Assert.True(MathF.Abs(root.Transform.LocalPosition.Z - 5.0f) < 0.1f,
            $"Expected root Z ~5.0 but got {root.Transform.LocalPosition.Z}");
    }

    [Fact]
    public void RootMotion_WithRetargeting_ScalesDisplacement()
    {
        var (srcSkel, srcMap) = CreateRetargetRig("SourceRig", 1.0f);
        var (tgtSkel, tgtMap) = CreateRetargetRig("TargetRig", 2.0f);

        var rootGO = new GameObject("Root");
        var hipsGO = new GameObject("Hips");
        hipsGO.Transform.SetParent(rootGO.Transform);

        var animator = rootGO.AddComponent<Animator>();
        animator.ApplyRootMotion = true;
        animator.SourceSkeleton = new AssetRef<SkeletonAsset>(srcSkel);
        animator.SourceHumanoidMapping = new AssetRef<HumanoidMapping>(srcMap);
        animator.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(tgtMap);

        // Clip moves Hips Z by 1.0 on source
        var clip = CreateClipWithPosition("ForwardStep", "Hips", Float3.Zero, new Float3(0, 1.0f, 1.0f), 1.0f);
        animator.AddState(new AnimatorState("ForwardStep", new AssetRef<AnimationClip>(clip), loop: false));

        animator.OnEnable();
        animator.Update(1.0f);

        // Scale ratio is 2.0, so GameObject Z should be 1.0 * 2.0 = 2.0
        Assert.True(MathF.Abs(rootGO.Transform.LocalPosition.Z - 2.0f) < 0.15f,
            $"Expected Root Z ~2.0 but got {rootGO.Transform.LocalPosition.Z}");
    }

    [Fact]
    public void RootMotion_API_ExposesLastFrameDelta()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();

        var clip = CreateClipWithPosition("Move", "Bone1", Float3.Zero, new Float3(2, 0, 4), 1.0f);
        animator.AddState(new AnimatorState("Move", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        animator.Update(0.5f);

        Assert.Equal(animator.RootMotionPositionDelta, animator.RootMotionPosition);
        Assert.Equal(animator.RootMotionRotationDelta, animator.RootMotionRotation);
        Assert.True(MathF.Abs(animator.RootMotionPosition.X - 1.0f) < 0.05f);
        Assert.True(MathF.Abs(animator.RootMotionPosition.Z - 2.0f) < 0.05f);
    }

    [Fact]
    public void RootMotion_ZeroAllocations_DuringUpdate()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;

        var clip = CreateClipWithPosition("Run", "Bone1", Float3.Zero, new Float3(0, 0, 10), 1.0f);
        animator.AddState(new AnimatorState("Run", new AssetRef<AnimationClip>(clip), loop: true));
        animator.OnEnable();

        // Warm up
        for (int i = 0; i < 5; i++)
            animator.Update(0.016f);

        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 50; i++)
        {
            animator.Update(0.016f);
        }
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, allocAfter - allocBefore);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  21. PR15: Inverse Kinematics (Two-Bone IK, Hints, Targets, Retargeting, Zero Allocations)
    // ════════════════════════════════════════════════════════════════════════

    private static (GameObject root, Dictionary<string, Transform> bones) CreateHumanoidHierarchy(SkeletonAsset skel)
    {
        var root = new GameObject("CharacterRoot");
        var boneTransforms = new Dictionary<string, Transform>();
        var transformsByIndex = new Transform[skel.BoneCount];

        for (int i = 0; i < skel.BoneCount; i++)
        {
            var bone = skel[i];
            var go = new GameObject(bone.Name);
            if (bone.ParentIndex < 0)
                go.Transform.SetParent(root.Transform);
            else
                go.Transform.SetParent(transformsByIndex[bone.ParentIndex]);

            go.Transform.LocalPosition = bone.LocalPosition;
            go.Transform.LocalRotation = bone.LocalRotation;
            go.Transform.LocalScale = bone.LocalScale;

            transformsByIndex[i] = go.Transform;
            boneTransforms[bone.Name] = go.Transform;
        }

        return (root, boneTransforms);
    }

    [Fact]
    public void IKSolver_TwoBoneIK_ReachesTargetPosition()
    {
        // Root at (0, 2, 0), Mid at (0, 1, 0), End at (0, 0, 0)
        Float3 rootPos = new(0, 2, 0);
        Float3 midPos = new(0, 1, 0);
        Float3 endPos = new(0, 0, 0);

        Quaternion rootRot = Quaternion.Identity;
        Quaternion midRot = Quaternion.Identity;
        Quaternion endRot = Quaternion.Identity;
        Quaternion parentRot = Quaternion.Identity;

        // Target at (0.5, 0.5, 0)
        Float3 targetPos = new(0.5f, 0.5f, 0f);

        bool solved = IKSolver.SolveTwoBoneIK(
            in rootPos, in rootRot,
            in midPos, in midRot,
            in endPos, in endRot,
            in parentRot,
            in targetPos,
            null,
            new Float3(0.5f, 1f, 0.5f), // hint
            1.0f,
            0.0f,
            out Quaternion solvedRot0,
            out Quaternion solvedRot1,
            out Quaternion _);

        Assert.True(solved);

        // Forward kinematics to check end effector position
        Float3 v01 = midPos - rootPos;
        Float3 v12 = endPos - midPos;

        Float3 solvedP1 = rootPos + solvedRot0 * v01;
        Float3 solvedP2 = solvedP1 + (solvedRot0 * solvedRot1) * v12;

        float distToTarget = Float3.Distance(solvedP2, targetPos);
        Assert.True(distToTarget < 0.05f, $"Expected distance < 0.05 but got {distToTarget}");
    }

    [Fact]
    public void IKSolver_TwoBoneIK_PreservesSegmentLengths()
    {
        Float3 rootPos = new(0, 2, 0);
        Float3 midPos = new(0, 1, 0);
        Float3 endPos = new(0, 0, 0);

        float origLen1 = Float3.Length(midPos - rootPos);
        float origLen2 = Float3.Length(endPos - midPos);

        Float3 targetPos = new(0.3f, 0.7f, -0.4f);

        IKSolver.SolveTwoBoneIK(
            in rootPos, in Quaternion.Identity,
            in midPos, in Quaternion.Identity,
            in endPos, in Quaternion.Identity,
            in Quaternion.Identity,
            in targetPos,
            null,
            null,
            1.0f,
            0.0f,
            out Quaternion rot0,
            out Quaternion rot1,
            out Quaternion _);

        Float3 v01 = midPos - rootPos;
        Float3 v12 = endPos - midPos;

        Float3 solvedP1 = rootPos + rot0 * v01;
        Float3 solvedP2 = solvedP1 + (rot0 * rot1) * v12;

        float solvedLen1 = Float3.Length(solvedP1 - rootPos);
        float solvedLen2 = Float3.Length(solvedP2 - solvedP1);

        Assert.True(MathF.Abs(solvedLen1 - origLen1) < 1e-4f);
        Assert.True(MathF.Abs(solvedLen2 - origLen2) < 1e-4f);
    }

    [Fact]
    public void IKSolver_TwoBoneIK_HintControlsElbowBendPlane()
    {
        Float3 rootPos = new(0, 2, 0);
        Float3 midPos = new(0, 1, 0);
        Float3 endPos = new(0, 0, 0);

        Float3 targetPos = new(0, 0.5f, 0);

        // Hint forward (+Z)
        IKSolver.SolveTwoBoneIK(
            in rootPos, in Quaternion.Identity,
            in midPos, in Quaternion.Identity,
            in endPos, in Quaternion.Identity,
            in Quaternion.Identity,
            in targetPos,
            null,
            new Float3(0, 1, 1),
            1.0f,
            0.0f,
            out Quaternion rot0Fwd,
            out _,
            out _);

        // Hint backward (-Z)
        IKSolver.SolveTwoBoneIK(
            in rootPos, in Quaternion.Identity,
            in midPos, in Quaternion.Identity,
            in endPos, in Quaternion.Identity,
            in Quaternion.Identity,
            in targetPos,
            null,
            new Float3(0, 1, -1),
            1.0f,
            0.0f,
            out Quaternion rot0Bwd,
            out _,
            out _);

        Float3 v01 = midPos - rootPos;
        Float3 midSolvedFwd = rootPos + rot0Fwd * v01;
        Float3 midSolvedBwd = rootPos + rot0Bwd * v01;

        Assert.True(midSolvedFwd.Z > 0.1f, $"Expected positive Z bend for forward hint, got {midSolvedFwd.Z}");
        Assert.True(midSolvedBwd.Z < -0.1f, $"Expected negative Z bend for backward hint, got {midSolvedBwd.Z}");
    }

    [Fact]
    public void IKSolver_TwoBoneIK_HintControlsKneeBendPlane()
    {
        Float3 rootPos = new(0, 1.0f, 0);
        Float3 midPos = new(0, 0.5f, 0);
        Float3 endPos = new(0, 0, 0);

        Float3 targetPos = new(0, 0.2f, 0);

        // Hint in front of knee (+Z)
        IKSolver.SolveTwoBoneIK(
            in rootPos, in Quaternion.Identity,
            in midPos, in Quaternion.Identity,
            in endPos, in Quaternion.Identity,
            in Quaternion.Identity,
            in targetPos,
            null,
            new Float3(0, 0.5f, 1.0f),
            1.0f,
            0.0f,
            out Quaternion rot0,
            out _,
            out _);

        Float3 midSolved = rootPos + rot0 * (midPos - rootPos);
        Assert.True(midSolved.Z > 0.05f, $"Expected knee to bend forward (+Z), got {midSolved.Z}");
    }

    [Fact]
    public void IKSolver_TargetTooFar_ClampsStablyWithoutNaN()
    {
        Float3 rootPos = new(0, 2, 0);
        Float3 midPos = new(0, 1, 0);
        Float3 endPos = new(0, 0, 0);

        // Target way beyond max reach (2.0)
        Float3 targetPos = new(0, -100, 0);

        bool solved = IKSolver.SolveTwoBoneIK(
            in rootPos, in Quaternion.Identity,
            in midPos, in Quaternion.Identity,
            in endPos, in Quaternion.Identity,
            in Quaternion.Identity,
            in targetPos,
            null,
            null,
            1.0f,
            0.0f,
            out Quaternion rot0,
            out Quaternion rot1,
            out _);

        Assert.True(solved);
        Assert.False(float.IsNaN(rot0.X) || float.IsNaN(rot0.Y) || float.IsNaN(rot0.Z) || float.IsNaN(rot0.W));
        Assert.False(float.IsNaN(rot1.X) || float.IsNaN(rot1.Y) || float.IsNaN(rot1.Z) || float.IsNaN(rot1.W));
    }

    [Fact]
    public void IKSolver_TargetTooClose_ClampsStablyWithoutNaN()
    {
        Float3 rootPos = new(0, 2, 0);
        Float3 midPos = new(0, 1, 0);
        Float3 endPos = new(0, 0, 0);

        // Target right on root position
        Float3 targetPos = new(0, 2.00001f, 0);

        bool solved = IKSolver.SolveTwoBoneIK(
            in rootPos, in Quaternion.Identity,
            in midPos, in Quaternion.Identity,
            in endPos, in Quaternion.Identity,
            in Quaternion.Identity,
            in targetPos,
            null,
            null,
            1.0f,
            0.0f,
            out Quaternion rot0,
            out Quaternion rot1,
            out _);

        Assert.True(solved);
        Assert.False(float.IsNaN(rot0.X) || float.IsNaN(rot0.Y) || float.IsNaN(rot0.Z) || float.IsNaN(rot0.W));
        Assert.False(float.IsNaN(rot1.X) || float.IsNaN(rot1.Y) || float.IsNaN(rot1.Z) || float.IsNaN(rot1.W));
    }

    [Fact]
    public void Animator_IK_WeightZero_PreservesOriginalPose()
    {
        var (skel, map) = CreateRetargetRig("HumanoidRig", 1.0f);
        var (root, bones) = CreateHumanoidHierarchy(skel);

        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        var clip = CreateClipWithPosition("Pose", "Hips", new Float3(0, 1.0f, 0), new Float3(0, 1.0f, 0), 1.0f);
        animator.AddState(new AnimatorState("Pose", new AssetRef<AnimationClip>(clip)));

        // Configure LeftHand IK with Weight 0
        animator.SetIKPosition(AvatarIKGoal.LeftHand, new Float3(10, 10, 10));
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.0f);

        animator.OnEnable();
        animator.Update(0.016f);

        var handTransform = bones["LeftHand"];
        // Hand should remain at its rest local position (-0.15, 0, 0)
        Assert.True(MathF.Abs(handTransform.LocalPosition.X - (-0.15f)) < 0.05f);
    }

    [Fact]
    public void Animator_IK_WeightOne_AppliesFullIK()
    {
        var (skel, map) = CreateRetargetRig("HumanoidRig", 1.0f);
        var (root, bones) = CreateHumanoidHierarchy(skel);

        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        var clip = CreateClipWithPosition("Pose", "Hips", new Float3(0, 1.0f, 0), new Float3(0, 1.0f, 0), 1.0f);
        animator.AddState(new AnimatorState("Pose", new AssetRef<AnimationClip>(clip)));

        // Place target in reachable space for Left Hand (UpperArm is at (-0.3, 1.4, 0), reach is 0.40)
        Float3 targetWorldPos = new(-0.55f, 1.2f, 0.1f);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, targetWorldPos);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);

        animator.OnEnable();
        animator.Update(0.016f);

        var handTransform = bones["LeftHand"];
        float dist = Float3.Distance(handTransform.Position, targetWorldPos);
        Assert.True(dist < 0.08f, $"Expected LeftHand to reach target near {targetWorldPos}, dist={dist}");
    }

    [Fact]
    public void Animator_IK_TargetRotation_OrientsEndEffector()
    {
        var (skel, map) = CreateRetargetRig("HumanoidRig", 1.0f);
        var (root, bones) = CreateHumanoidHierarchy(skel);

        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        var clip = CreateClipWithPosition("Pose", "Hips", new Float3(0, 1.0f, 0), new Float3(0, 1.0f, 0), 1.0f);
        animator.AddState(new AnimatorState("Pose", new AssetRef<AnimationClip>(clip)));

        Quaternion targetRotation = Quaternion.FromEuler(new Float3(0, 90, 0));
        animator.SetIKPosition(AvatarIKGoal.LeftHand, new Float3(-0.55f, 1.2f, 0.1f));
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, targetRotation);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        animator.OnEnable();
        animator.Update(0.016f);

        var handTransform = bones["LeftHand"];
        float dot = MathF.Abs(Quaternion.Dot(handTransform.Rotation, targetRotation));
        Assert.True(dot > 0.95f, $"Expected Hand orientation to match target, dot={dot}");
    }

    [Fact]
    public void Animator_IK_WorksAfterRetargeting()
    {
        var (srcSkel, srcMap) = CreateRetargetRig("SourceRig", 1.0f);
        var (tgtSkel, tgtMap) = CreateRetargetRig("TargetRig", 2.0f);
        var (root, bones) = CreateHumanoidHierarchy(tgtSkel);

        var animator = root.AddComponent<Animator>();
        animator.SourceSkeleton = new AssetRef<SkeletonAsset>(srcSkel);
        animator.SourceHumanoidMapping = new AssetRef<HumanoidMapping>(srcMap);
        animator.Skeleton = new AssetRef<SkeletonAsset>(tgtSkel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(tgtMap);

        var clip = CreateClipWithPosition("SourcePose", "Hips", new Float3(0, 1.0f, 0), new Float3(0, 1.0f, 0), 1.0f);
        animator.AddState(new AnimatorState("SourcePose", new AssetRef<AnimationClip>(clip)));

        // TargetRig UpperArm is at (-0.6, 2.8, 0), reach is 0.80
        Float3 targetPos = new(-1.1f, 2.4f, 0.2f);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, targetPos);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);

        animator.OnEnable();
        animator.Update(0.016f);

        var handTransform = bones["LeftHand"];
        float dist = Float3.Distance(handTransform.Position, targetPos);
        Assert.True(dist < 0.15f, $"Expected Retargeted LeftHand to reach target, dist={dist}");
    }

    [Fact]
    public void Animator_IK_WorksWithBlendTree()
    {
        var (skel, map) = CreateRetargetRig("HumanoidRig", 1.0f);
        var (root, bones) = CreateHumanoidHierarchy(skel);

        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        var clip1 = CreateClipWithPosition("Idle", "Hips", new Float3(0, 1.0f, 0), new Float3(0, 1.0f, 0), 1.0f);
        var clip2 = CreateClipWithPosition("Walk", "Hips", new Float3(0, 1.1f, 0), new Float3(0, 1.1f, 0), 1.0f);

        var bt = new BlendTree
        {
            BlendType = BlendTreeType.Simple1D,
            BlendParameter = "Speed"
        };
        bt.Children.Add(new BlendTreeChild { Clip = new AssetRef<AnimationClip>(clip1), Threshold = 0.0f });
        bt.Children.Add(new BlendTreeChild { Clip = new AssetRef<AnimationClip>(clip2), Threshold = 1.0f });

        animator.AddState(new AnimatorState("BlendState", bt));
        animator.SetFloat("Speed", 0.5f);

        Float3 footTarget = new(-0.1f, 0.0f, 0.1f);
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, footTarget);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1.0f);

        animator.OnEnable();
        animator.Update(0.016f);

        var footTransform = bones["LeftFoot"];
        float dist = Float3.Distance(footTransform.Position, footTarget);
        Assert.True(dist < 0.15f, $"Expected Foot to reach target with BlendTree active, dist={dist}");
    }

    [Fact]
    public void Animator_IK_RespectsLayersAndMasks()
    {
        var (skel, map) = CreateRetargetRig("HumanoidRig", 1.0f);
        var (root, bones) = CreateHumanoidHierarchy(skel);

        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        // Base Layer
        var baseClip = CreateClipWithPosition("BaseClip", "Hips", new Float3(0, 1.0f, 0), new Float3(0, 1.0f, 0), 1.0f);
        animator.AddState(new AnimatorState("Base", new AssetRef<AnimationClip>(baseClip)));

        // Layer 1 with UpperBody mask
        var layer1 = new AnimatorLayer("Upper", 1.0f);
        var upperClip = CreateClipWithPosition("UpperClip", "Spine", new Float3(0, 0.2f, 0), new Float3(0, 0.4f, 0), 1.0f);
        layer1.AddState(new AnimatorState("UpperState", new AssetRef<AnimationClip>(upperClip)));
        layer1.Mask = AvatarMask.CreateUpperBodyMask();
        animator.AddLayer(layer1);

        // IK on Left Foot (Lower body)
        Float3 footTarget = new(-0.1f, 0.0f, 0.1f);
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, footTarget);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1.0f);

        animator.OnEnable();
        animator.Update(0.016f);

        var footTransform = bones["LeftFoot"];
        float dist = Float3.Distance(footTransform.Position, footTarget);
        Assert.True(dist < 0.15f, $"Expected Foot IK to work across multi-layer setup, dist={dist}");
    }

    [Fact]
    public void Animator_IK_DoesNotAlterRootMotion()
    {
        var (skel, map) = CreateRetargetRig("HumanoidRig", 1.0f);
        var (root, _) = CreateHumanoidHierarchy(skel);

        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        // Run clip that moves Hips Z by 4.0
        var runClip = CreateClipWithPosition("Run", "Hips", Float3.Zero, new Float3(0, 1.0f, 4.0f), 1.0f);
        animator.AddState(new AnimatorState("Run", new AssetRef<AnimationClip>(runClip), loop: false));

        // Enable Hand IK with weight 1.0
        animator.SetIKPosition(AvatarIKGoal.LeftHand, new Float3(-0.4f, 0.8f, 1.0f));
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);

        animator.OnEnable();
        animator.Update(1.0f);

        // Root motion displacement along Z should be exactly 4.0
        Assert.True(MathF.Abs(root.Transform.LocalPosition.Z - 4.0f) < 0.15f,
            $"Expected Root Z ~4.0 despite IK on Hand, got {root.Transform.LocalPosition.Z}");
    }

    [Fact]
    public void Animator_IK_IncompleteChain_FailsSafely()
    {
        var (skel, map) = CreateRetargetRig("IncompleteRig", 1.0f);
        // Unmap LeftLowerArm
        map.Set(HumanoidBone.LeftLowerArm, -1);

        var (root, _) = CreateHumanoidHierarchy(skel);
        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        var clip = CreateClipWithPosition("Clip", "Hips", Float3.Zero, Float3.Zero, 1.0f);
        animator.AddState(new AnimatorState("Clip", new AssetRef<AnimationClip>(clip)));

        animator.SetIKPosition(AvatarIKGoal.LeftHand, new Float3(1, 1, 1));
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);

        animator.OnEnable();

        // Must run safely without exceptions
        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_IK_ZeroAllocations_DuringUpdate()
    {
        var (skel, map) = CreateRetargetRig("HumanoidRig", 1.0f);
        var (root, _) = CreateHumanoidHierarchy(skel);

        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        var clip = CreateClipWithPosition("Clip", "Hips", Float3.Zero, new Float3(0, 1.0f, 1.0f), 1.0f);
        animator.AddState(new AnimatorState("Clip", new AssetRef<AnimationClip>(clip), loop: true));

        // Enable all 4 IK goals
        animator.SetIKPosition(AvatarIKGoal.LeftHand, new Float3(-0.4f, 0.8f, 0.2f));
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);

        animator.SetIKPosition(AvatarIKGoal.RightHand, new Float3(0.4f, 0.8f, 0.2f));
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);

        animator.SetIKPosition(AvatarIKGoal.LeftFoot, new Float3(-0.1f, 0.0f, 0.1f));
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1.0f);

        animator.SetIKPosition(AvatarIKGoal.RightFoot, new Float3(0.1f, 0.0f, 0.1f));
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1.0f);

        animator.OnEnable();

        // Warm up
        for (int i = 0; i < 5; i++)
            animator.Update(0.016f);

        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 50; i++)
        {
            animator.Update(0.016f);
        }
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, allocAfter - allocBefore);
    }

    [Fact]
    public void PR1_PR15_Regression_SanityCheck()
    {
        var (skel, map) = CreateRetargetRig("FullRig", 1.0f);
        var (root, _) = CreateHumanoidHierarchy(skel);

        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        var clipA = CreateClipWithPosition("StateA", "Hips", Float3.Zero, new Float3(0, 1.0f, 1.0f), 1.0f);
        var clipB = CreateClipWithPosition("StateB", "Hips", new Float3(0, 1.0f, 1.0f), new Float3(0, 1.0f, 2.0f), 1.0f);

        var stateA = new AnimatorState("StateA", new AssetRef<AnimationClip>(clipA));
        var stateB = new AnimatorState("StateB", new AssetRef<AnimationClip>(clipB));
        animator.AddState(stateA);
        animator.AddState(stateB);

        var trans = new AnimatorTransition("StateA", "StateB", 0.2f);
        trans.Conditions.Add(new AnimatorCondition("TriggerB", AnimatorConditionMode.If, 0f));
        animator.AddTransition(trans);

        animator.SetIKPosition(AvatarIKGoal.RightHand, new Float3(0.4f, 0.8f, 0.2f));
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);

        animator.OnEnable();
        animator.Update(0.5f);

        animator.SetTrigger("TriggerB");
        animator.Update(0.1f);

        Assert.True(animator.IsInTransition);
        Assert.True(animator.RootMotionPosition.Z > 0.0f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  22. PR16: Polish, Edge Cases & Comprehensive System Validation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Animator_NoStates_UpdatesSafely()
    {
        var root = new GameObject("Root");
        var animator = root.AddComponent<Animator>();
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_MissingClip_UpdatesSafely()
    {
        var root = new GameObject("Root");
        var animator = root.AddComponent<Animator>();
        animator.AddState(new AnimatorState("EmptyClipState", default(AssetRef<AnimationClip>)));
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_MissingMapping_FallsBackSafely()
    {
        var (skel, _) = CreateRetargetRig("Rig", 1.0f);
        var (root, _) = CreateHumanoidHierarchy(skel);
        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        // HumanoidMapping left unassigned
        var clip = CreateClipWithPosition("Clip", "Hips", Float3.Zero, Float3.One, 1.0f);
        animator.AddState(new AnimatorState("Clip", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_MissingSkeleton_FallsBackSafely()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        // Skeleton left null
        var clip = CreateClipWithPosition("Clip", "Bone1", Float3.Zero, Float3.One, 1.0f);
        animator.AddState(new AnimatorState("Clip", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_InvalidTransition_HandlesSafely()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        var clip = CreateClipWithPosition("Clip", "Bone1", Float3.Zero, Float3.One, 1.0f);
        animator.AddState(new AnimatorState("Clip", new AssetRef<AnimationClip>(clip)));
        animator.AddTransition(new AnimatorTransition("Clip", "NonExistentState", 0.2f));
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_EmptyBlendTree_UpdatesSafely()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        var bt = new BlendTree { BlendType = BlendTreeType.Simple1D, BlendParameter = "Speed" };
        animator.AddState(new AnimatorState("EmptyBT", bt));
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_EmptyLayer_UpdatesSafely()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        var clip = CreateClipWithPosition("Clip", "Bone1", Float3.Zero, Float3.One, 1.0f);
        animator.AddState(new AnimatorState("Clip", new AssetRef<AnimationClip>(clip)));

        // Empty secondary layer
        animator.AddLayer(new AnimatorLayer("EmptySecondary", 0.5f));
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_LayerWeightZeroAndOne_HandlesAccurately()
    {
        var (root, bone1, bone2) = CreateTwoBoneHierarchy();
        var animator = root.AddComponent<Animator>();

        var clipA = CreateTwoBoneClip("ClipA", Float3.Zero, Float3.Zero);
        animator.AddState(new AnimatorState("StateA", new AssetRef<AnimationClip>(clipA)));

        var layer1 = new AnimatorLayer("Layer1", 0.0f);
        var clipB = CreateTwoBoneClip("ClipB", new Float3(0, 10, 0), new Float3(0, 10, 0));
        layer1.AddState(new AnimatorState("StateB", new AssetRef<AnimationClip>(clipB)));
        animator.AddLayer(layer1);

        animator.OnEnable();
        animator.Update(0.016f);

        // Weight 0: layer has 0 effect
        Assert.True(MathF.Abs(bone1.LocalPosition.Y) < 1e-4f);

        // Weight 1: layer overrides fully
        layer1.Weight = 1.0f;
        animator.Update(0.016f);
        Assert.True(MathF.Abs(bone1.LocalPosition.Y - 10.0f) < 0.05f);
    }

    [Fact]
    public void Animator_ClipZeroDuration_HandlesSafely()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        var clip = CreateClipWithPosition("ZeroDur", "Bone1", Float3.Zero, Float3.One, 0.0f);
        animator.AddState(new AnimatorState("ZeroDurState", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
        Assert.Equal(0f, animator.CurrentTime);
    }

    [Fact]
    public void Animator_SingleBoneSkeleton_HandlesSafely()
    {
        var skel = new SkeletonAsset { Name = "SingleBoneRig" };
        skel.AddBone(new SkeletonBone("RootBone", -1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));

        var root = new GameObject("Root");
        var boneGO = new GameObject("RootBone");
        boneGO.Transform.SetParent(root.Transform);

        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);

        var clip = CreateClipWithPosition("Single", "RootBone", Float3.Zero, new Float3(1, 2, 3), 1.0f);
        animator.AddState(new AnimatorState("Single", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.5f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_IncompleteHumanoid_HandlesSafely()
    {
        var (skel, map) = CreateRetargetRig("IncompleteRig", 1.0f);
        // Clear half the bones
        map.Clear(HumanoidBone.LeftUpperArm);
        map.Clear(HumanoidBone.RightLowerLeg);
        map.Clear(HumanoidBone.Neck);

        var (root, _) = CreateHumanoidHierarchy(skel);
        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        var clip = CreateClipWithPosition("Pose", "Hips", Float3.Zero, Float3.One, 1.0f);
        animator.AddState(new AnimatorState("Pose", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_InvalidIKTarget_HandlesSafely()
    {
        var (skel, map) = CreateRetargetRig("Rig", 1.0f);
        var (root, _) = CreateHumanoidHierarchy(skel);
        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map);

        var clip = CreateClipWithPosition("Pose", "Hips", Float3.Zero, Float3.One, 1.0f);
        animator.AddState(new AnimatorState("Pose", new AssetRef<AnimationClip>(clip)));

        // IK target has weight 1.0 but no Position or Target Transform assigned
        animator.LeftHandIK.Weight = 1.0f;
        animator.LeftHandIK.Target = null;
        animator.LeftHandIK.Position = null;

        animator.OnEnable();
        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_RootMotion_NoRootBone_HandlesSafely()
    {
        var root = new GameObject("Root");
        var animator = root.AddComponent<Animator>();
        animator.ApplyRootMotion = true;
        animator.OnEnable();

        var ex = Record.Exception(() => animator.Update(0.016f));
        Assert.Null(ex);
        Assert.Equal(Float3.Zero, animator.RootMotionPositionDelta);
    }

    [Fact]
    public void Animator_InvalidCrossFade_HandlesSafely()
    {
        var (root, _) = CreateHierarchy();
        var animator = root.AddComponent<Animator>();
        var clip = CreateClipWithPosition("Clip", "Bone1", Float3.Zero, Float3.One, 1.0f);
        animator.AddState(new AnimatorState("Clip", new AssetRef<AnimationClip>(clip)));
        animator.OnEnable();

        // CrossFade to non-existent state
        var ex = Record.Exception(() => animator.CrossFade("NonExistent", 0.25f));
        Assert.Null(ex);
        Assert.False(animator.IsInTransition);
    }

    [Fact]
    public void Animator_MissingParameter_ReturnsDefaults()
    {
        var root = new GameObject("Root");
        var animator = root.AddComponent<Animator>();

        Assert.Equal(0f, animator.GetFloat("NonExistentFloat"));
        Assert.Equal(0, animator.GetInt("NonExistentInt"));
        Assert.False(animator.GetBool("NonExistentBool"));

        var ex = Record.Exception(() => animator.ResetTrigger("NonExistentTrigger"));
        Assert.Null(ex);
    }

    [Fact]
    public void Animator_RuntimeReconfiguration_RebindsCleanly()
    {
        var (skel1, map1) = CreateRetargetRig("Rig1", 1.0f);
        var (skel2, map2) = CreateRetargetRig("Rig2", 1.5f);
        var (root, _) = CreateHumanoidHierarchy(skel1);

        var animator = root.AddComponent<Animator>();
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel1);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map1);

        var clipA = CreateClipWithPosition("ClipA", "Hips", Float3.Zero, Float3.One, 1.0f);
        var clipB = CreateClipWithPosition("ClipB", "Hips", Float3.One, Float3.Zero, 1.0f);

        animator.AddState(new AnimatorState("ClipA", new AssetRef<AnimationClip>(clipA)));
        animator.OnEnable();
        animator.Update(0.5f);

        // Reconfigure Skeleton, Mapping, and State at runtime
        animator.Skeleton = new AssetRef<SkeletonAsset>(skel2);
        animator.HumanoidMapping = new AssetRef<HumanoidMapping>(map2);
        animator.AddState(new AnimatorState("ClipB", new AssetRef<AnimationClip>(clipB)));
        animator.Play("ClipB");

        var ex = Record.Exception(() => animator.Update(0.1f));
        Assert.Null(ex);
        Assert.Equal("ClipB", animator.CurrentState?.Name);
    }

    [Fact]
    public void SkeletonAsset_Validation_DetectsCyclesAndDuplicates()
    {
        // 1. Empty skeleton
        var emptySkel = new SkeletonAsset();
        Assert.False(emptySkel.Validate(out List<string> emptyErrors));
        Assert.NotEmpty(emptyErrors);

        // 2. Cyclic skeleton
        var cycleSkel = new SkeletonAsset();
        cycleSkel.AddBone(new SkeletonBone("BoneA", 1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        cycleSkel.AddBone(new SkeletonBone("BoneB", 0, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        Assert.False(cycleSkel.Validate(out List<string> cycleErrors));
        Assert.Contains(cycleErrors, e => e.Contains("Cyclic") || e.Contains("no root"));

        // 3. Valid skeleton
        var validSkel = new SkeletonAsset();
        validSkel.AddBone(new SkeletonBone("Root", -1, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        validSkel.AddBone(new SkeletonBone("Child", 0, Float3.Zero, Quaternion.Identity, Float3.One, Float4x4.Identity));
        Assert.True(validSkel.Validate(out string? validError));
        Assert.Null(validError);
    }

    [Fact]
    public void BlendTree_Validation_DetectsEmptyAndInvalidChildren()
    {
        var bt = new BlendTree();
        Assert.False(bt.Validate(out List<string> errors));
        Assert.NotEmpty(errors);

        bt.BlendParameter = "Speed";
        var clip = CreateClipWithPosition("ValidClip", "Bone1", Float3.Zero, Float3.One);
        bt.AddChild(new BlendTreeChild(new AssetRef<AnimationClip>(clip), 0f));

        Assert.True(bt.Validate(out string? error));
        Assert.Null(error);
    }

    [Fact]
    public void Animator_Validation_DetectsLayerAndStateIssues()
    {
        var root = new GameObject("Root");
        var animator = root.AddComponent<Animator>();

        // Empty animator
        Assert.False(animator.Validate(out List<string> errors));
        Assert.NotEmpty(errors);

        // Configured valid animator
        var clip = CreateClipWithPosition("Walk", "Bone1", Float3.Zero, Float3.One);
        animator.AddState(new AnimatorState("Walk", new AssetRef<AnimationClip>(clip)));
        Assert.True(animator.Validate(out string? err));
        Assert.Null(err);
    }
}


