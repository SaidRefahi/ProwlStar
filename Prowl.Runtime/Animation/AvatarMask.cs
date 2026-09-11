// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;

namespace Prowl.Runtime;

/// <summary>
/// Defines which humanoid bones are affected by an <see cref="AnimatorLayer"/>,
/// with per-bone inclusion flags, blend weights, and hierarchical cascading.
/// </summary>
public sealed class AvatarMask
{
    private static readonly HumanoidBone[][] s_descendants = new HumanoidBone[HumanoidMapping.HumanoidBoneCount][]
    {
        // 0: Hips
        [
            HumanoidBone.Spine, HumanoidBone.Chest, HumanoidBone.UpperChest, HumanoidBone.Neck, HumanoidBone.Head,
            HumanoidBone.LeftShoulder, HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand,
            HumanoidBone.RightShoulder, HumanoidBone.RightUpperArm, HumanoidBone.RightLowerArm, HumanoidBone.RightHand,
            HumanoidBone.LeftUpperLeg, HumanoidBone.LeftLowerLeg, HumanoidBone.LeftFoot, HumanoidBone.LeftToes,
            HumanoidBone.RightUpperLeg, HumanoidBone.RightLowerLeg, HumanoidBone.RightFoot, HumanoidBone.RightToes
        ],
        // 1: Spine
        [
            HumanoidBone.Chest, HumanoidBone.UpperChest, HumanoidBone.Neck, HumanoidBone.Head,
            HumanoidBone.LeftShoulder, HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand,
            HumanoidBone.RightShoulder, HumanoidBone.RightUpperArm, HumanoidBone.RightLowerArm, HumanoidBone.RightHand
        ],
        // 2: Chest
        [
            HumanoidBone.UpperChest, HumanoidBone.Neck, HumanoidBone.Head,
            HumanoidBone.LeftShoulder, HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand,
            HumanoidBone.RightShoulder, HumanoidBone.RightUpperArm, HumanoidBone.RightLowerArm, HumanoidBone.RightHand
        ],
        // 3: UpperChest
        [
            HumanoidBone.Neck, HumanoidBone.Head
        ],
        // 4: Neck
        [
            HumanoidBone.Head
        ],
        // 5: Head
        [],
        // 6: LeftShoulder
        [
            HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand
        ],
        // 7: LeftUpperArm
        [
            HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand
        ],
        // 8: LeftLowerArm
        [
            HumanoidBone.LeftHand
        ],
        // 9: LeftHand
        [],
        // 10: RightShoulder
        [
            HumanoidBone.RightUpperArm, HumanoidBone.RightLowerArm, HumanoidBone.RightHand
        ],
        // 11: RightUpperArm
        [
            HumanoidBone.RightLowerArm, HumanoidBone.RightHand
        ],
        // 12: RightLowerArm
        [
            HumanoidBone.RightHand
        ],
        // 13: RightHand
        [],
        // 14: LeftUpperLeg
        [
            HumanoidBone.LeftLowerLeg, HumanoidBone.LeftFoot, HumanoidBone.LeftToes
        ],
        // 15: LeftLowerLeg
        [
            HumanoidBone.LeftFoot, HumanoidBone.LeftToes
        ],
        // 16: LeftFoot
        [
            HumanoidBone.LeftToes
        ],
        // 17: LeftToes
        [],
        // 18: RightUpperLeg
        [
            HumanoidBone.RightLowerLeg, HumanoidBone.RightFoot, HumanoidBone.RightToes
        ],
        // 19: RightLowerLeg
        [
            HumanoidBone.RightFoot, HumanoidBone.RightToes
        ],
        // 20: RightFoot
        [
            HumanoidBone.RightToes
        ],
        // 21: RightToes
        []
    };

    private static readonly HumanoidBone?[] s_parents = new HumanoidBone?[HumanoidMapping.HumanoidBoneCount]
    {
        null, // Hips
        HumanoidBone.Hips, // Spine
        HumanoidBone.Spine, // Chest
        HumanoidBone.Chest, // UpperChest
        HumanoidBone.UpperChest, // Neck
        HumanoidBone.Neck, // Head
        HumanoidBone.Chest, // LeftShoulder
        HumanoidBone.LeftShoulder, // LeftUpperArm
        HumanoidBone.LeftUpperArm, // LeftLowerArm
        HumanoidBone.LeftLowerArm, // LeftHand
        HumanoidBone.Chest, // RightShoulder
        HumanoidBone.RightShoulder, // RightUpperArm
        HumanoidBone.RightUpperArm, // RightLowerArm
        HumanoidBone.RightLowerArm, // RightHand
        HumanoidBone.Hips, // LeftUpperLeg
        HumanoidBone.LeftUpperLeg, // LeftLowerLeg
        HumanoidBone.LeftLowerLeg, // LeftFoot
        HumanoidBone.LeftFoot, // LeftToes
        HumanoidBone.Hips, // RightUpperLeg
        HumanoidBone.RightUpperLeg, // RightLowerLeg
        HumanoidBone.RightLowerLeg, // RightFoot
        HumanoidBone.RightFoot // RightToes
    };

    private readonly bool[] _boneIncluded = new bool[HumanoidMapping.HumanoidBoneCount];
    private readonly float[] _boneWeights = new float[HumanoidMapping.HumanoidBoneCount];

    public AvatarMask()
    {
        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            _boneIncluded[i] = true;
            _boneWeights[i] = 1f;
        }
    }

    /// <summary>
    /// Gets all humanoid descendants of the specified bone in canonical humanoid hierarchy.
    /// </summary>
    public static ReadOnlySpan<HumanoidBone> GetDescendants(HumanoidBone bone)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < HumanoidMapping.HumanoidBoneCount)
            return s_descendants[idx];
        return ReadOnlySpan<HumanoidBone>.Empty;
    }

    /// <summary>
    /// Gets the parent humanoid bone in canonical humanoid hierarchy, or null if root (Hips).
    /// </summary>
    public static HumanoidBone? GetParent(HumanoidBone bone)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < HumanoidMapping.HumanoidBoneCount)
            return s_parents[idx];
        return null;
    }

    /// <summary>
    /// Clears the mask, setting all bones to excluded with 0 weight.
    /// </summary>
    public void Clear()
    {
        Array.Fill(_boneIncluded, false);
        Array.Fill(_boneWeights, 0f);
    }

    /// <summary>
    /// Sets whether a humanoid bone is included in this mask and its relative blend weight.
    /// </summary>
    public void SetIncluded(HumanoidBone bone, bool included, float weight = 1f)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < HumanoidMapping.HumanoidBoneCount)
        {
            _boneIncluded[idx] = included;
            _boneWeights[idx] = included ? Math.Clamp(weight, 0f, 1f) : 0f;
        }
    }

    /// <summary>
    /// Sets the blend weight (0..1) for a specific humanoid bone.
    /// If weight > 0, also marks the bone as included.
    /// </summary>
    public void SetWeight(HumanoidBone bone, float weight)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < HumanoidMapping.HumanoidBoneCount)
        {
            float clamped = Math.Clamp(weight, 0f, 1f);
            _boneWeights[idx] = clamped;
            if (clamped > 0f)
                _boneIncluded[idx] = true;
        }
    }

    /// <summary>
    /// Returns true if the humanoid bone is included in this mask.
    /// </summary>
    public bool IsIncluded(HumanoidBone bone)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < HumanoidMapping.HumanoidBoneCount)
            return _boneIncluded[idx];
        return false;
    }

    /// <summary>
    /// Returns the raw blend weight (0..1) for this humanoid bone. Returns 0 if excluded.
    /// </summary>
    public float GetWeight(HumanoidBone bone)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < HumanoidMapping.HumanoidBoneCount)
            return _boneIncluded[idx] ? _boneWeights[idx] : 0f;
        return 0f;
    }

    /// <summary>
    /// Returns the effective blend weight (0..1) for this humanoid bone. Returns 0 if excluded or weight &lt;= 0.
    /// </summary>
    public float GetEffectiveWeight(HumanoidBone bone)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < HumanoidMapping.HumanoidBoneCount)
            return _boneIncluded[idx] ? Math.Clamp(_boneWeights[idx], 0f, 1f) : 0f;
        return 0f;
    }

    /// <summary>
    /// Sets inclusion and weight hierarchically for a bone and all of its descendants.
    /// Specific child bones can subsequently be overridden.
    /// </summary>
    public void SetHierarchyIncluded(HumanoidBone rootBone, bool included, float weight = 1f)
    {
        SetIncluded(rootBone, included, weight);
        var descendants = GetDescendants(rootBone);
        for (int i = 0; i < descendants.Length; i++)
        {
            SetIncluded(descendants[i], included, weight);
        }
    }

    /// <summary>
    /// Sets the blend weight hierarchically for a bone and all of its descendants.
    /// Specific child bones can subsequently be overridden.
    /// </summary>
    public void SetHierarchyWeight(HumanoidBone rootBone, float weight)
    {
        SetWeight(rootBone, weight);
        var descendants = GetDescendants(rootBone);
        for (int i = 0; i < descendants.Length; i++)
        {
            SetWeight(descendants[i], weight);
        }
    }

    /// <summary>
    /// Validates the AvatarMask configuration, ensuring at least one bone has effective weight &gt; 0.
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        bool valid = Validate(out List<string> errors);
        errorMessage = valid ? null : string.Join("; ", errors);
        return valid;
    }

    /// <summary>
    /// Validates the AvatarMask configuration, returning any validation errors.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        bool hasActiveBone = false;
        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            if (_boneIncluded[i] && _boneWeights[i] > 1e-4f)
            {
                hasActiveBone = true;
                break;
            }
        }

        if (!hasActiveBone)
        {
            errors.Add("AvatarMask has no active bones (all weights are 0 or excluded).");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Creates an AvatarMask pre-configured for Upper Body animations (Spine, Chest, Head, and Arms).
    /// </summary>
    public static AvatarMask CreateUpperBodyMask()
    {
        var mask = new AvatarMask();
        mask.Clear();
        mask.SetHierarchyIncluded(HumanoidBone.Spine, true, 1f);
        return mask;
    }

    /// <summary>
    /// Creates an AvatarMask pre-configured for Lower Body animations (Hips, Legs, and Feet).
    /// </summary>
    public static AvatarMask CreateLowerBodyMask()
    {
        var mask = new AvatarMask();
        mask.Clear();
        mask.SetIncluded(HumanoidBone.Hips, true, 1f);
        mask.SetHierarchyIncluded(HumanoidBone.LeftUpperLeg, true, 1f);
        mask.SetHierarchyIncluded(HumanoidBone.RightUpperLeg, true, 1f);
        return mask;
    }
}
