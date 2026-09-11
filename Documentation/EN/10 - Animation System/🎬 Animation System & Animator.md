---
title: Animation System & Animator
tags: [animation, animator, skeleton, humanoid, retargeting, ik, root-motion, blend-trees]
category: animation
updated: 2026-09-11
---

# 🎬 Animation System & Animator in Prowl Engine

In **Prowl Engine**, the **Animation System** is a core engine-native feature that manages the entire lifecycle of skinned characters and skeletal meshes. The `Animator` serves as the runtime evaluation and playback engine, integrating skeletons, clips, blend trees, multi-layer blending, real-time humanoid retargeting, Inverse Kinematics (IK), and Root Motion with **Zero GC allocations** in hot execution paths.

---

## 🏗️ Conceptual Animation Pipeline

The native Prowl animation pipeline is decoupled, deterministic, and optimized for maximum runtime performance:

```
[ 3D Model with SkeletonAsset ] + [ AnimationClips (Mocopi / Mixamo / FBX) ]
                         │
                         ▼
               [ Animator Evaluator ]
                         │
      ┌──────────────────┼──────────────────┐
      ▼                  ▼                  ▼
[ Layers & BlendTrees ] [ Humanoid Retargeting ] [ Two-Bone IK ]
      │                  │                  │
      └──────────────────┼──────────────────┘
                         ▼
             [ Root Motion Extractor ]
                         │
                         ▼
     [ GameObject Transform + SkinnedMeshRenderer ]
```

1. **Import & SkeletonAsset**: Importing a skinned model generates an engine-native `SkeletonAsset` containing indexed bone hierarchies, bind poses, and inverse bind matrices.
2. **Animation Clips**: `AnimationClip` assets contain tracks and curves for rotation, translation, and scale.
3. **Animator**: Evaluates state machines, parameters, and condition graphs, sampling clips efficiently.
4. **Blend Trees & Layers**: Blends 1D/2D motion nodes and combines weighted layers via hierarchical `AvatarMask`s.
5. **Real-time Humanoid Retargeting**: `AnimationRetargeter` adapts joint rotations and proportions on the fly between different rigs.
6. **Inverse Kinematics (IK)**: `IKSolver` analytically solves two-bone limbs (arms/legs) to reach world-space targets with elbow/knee hints.
7. **Root Motion**: Extracts velocity and angular deltas from root tracks and transfers motion directly to the actor's `Transform`.

---

## 💻 Scripting Example

```csharp
using Prowl.Runtime;
using Prowl.Vector;

public class CharacterController : MonoBehaviour
{
    private Animator _animator;

    public override void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public override void Update()
    {
        // 1. Feed parameters for Blend Trees and state transitions
        float speed = Input.GetKey(Key.W) ? 1.0f : 0.0f;
        _animator.SetFloat("Speed", speed);

        // 2. Fire triggers for reactive actions
        if (Input.GetKeyDown(Key.Space))
        {
            _animator.SetTrigger("Jump");
        }

        // 3. Drive foot IK to adapt to terrain slopes
        if (Physics.Raycast(transform.position + Float3.Up, Float3.Down, out var hit, 2f))
        {
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, hit.Point);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1.0f);
        }
    }
}
```

---

## ⚡ Zero GC Guarantees

- **No Allocations in `Animator.Update()`**: Pre-allocated internal buffers for `BonePose[]`, world matrices, and blending scratchpads.
- **Index-Based Fast Paths**: Direct integer indexing for parameters, bones, and transitions.
- **SIMD / Vectorized Math**: Optimized using hardware-accelerated math operations.
