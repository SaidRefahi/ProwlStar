// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Prowl.Echo;

namespace Prowl.Runtime;

/// <summary>
/// Standard humanoid bone identifiers for skeletal mapping and retargeting.
/// </summary>
public enum HumanoidBone
{
    Hips = 0,

    Spine = 1,
    Chest = 2,
    UpperChest = 3,
    Neck = 4,
    Head = 5,

    LeftShoulder = 6,
    LeftUpperArm = 7,
    LeftLowerArm = 8,
    LeftHand = 9,

    RightShoulder = 10,
    RightUpperArm = 11,
    RightLowerArm = 12,
    RightHand = 13,

    LeftUpperLeg = 14,
    LeftLowerLeg = 15,
    LeftFoot = 16,
    LeftToes = 17,

    RightUpperLeg = 18,
    RightLowerLeg = 19,
    RightFoot = 20,
    RightToes = 21,
}

/// <summary>
/// Defines a humanoid bone mapping over a <see cref="SkeletonAsset"/>.
/// Maps abstract <see cref="HumanoidBone"/> roles to concrete skeleton bone indices (O(1) lookups).
/// </summary>
[CreateAssetMenu("Humanoid Mapping", Extension = ".humanoid", Order = 101)]
public sealed class HumanoidMapping : EngineObject, ISerializable
{
    /// <summary>Total number of humanoid bones defined in <see cref="HumanoidBone"/>.</summary>
    public const int HumanoidBoneCount = 22;

    /// <summary>Reference to the target skeleton asset.</summary>
    public AssetRef<SkeletonAsset> Skeleton;

    /// <summary>Internal array mapping HumanoidBone (cast to int) to Skeleton bone index (-1 = unassigned).</summary>
    private readonly int[] _boneIndices = new int[HumanoidBoneCount];

    public HumanoidMapping()
    {
        Array.Fill(_boneIndices, -1);
    }

    /// <summary>
    /// Checks if a humanoid bone role is optional in the standard humanoid rig definition.
    /// Optional bones: LeftShoulder, RightShoulder, UpperChest, LeftToes, RightToes.
    /// </summary>
    public static bool IsOptional(HumanoidBone bone) => bone switch
    {
        HumanoidBone.LeftShoulder => true,
        HumanoidBone.RightShoulder => true,
        HumanoidBone.UpperChest => true,
        HumanoidBone.LeftToes => true,
        HumanoidBone.RightToes => true,
        _ => false
    };

    /// <summary>
    /// Checks if a humanoid bone role is required for a valid humanoid rig.
    /// </summary>
    public static bool IsRequired(HumanoidBone bone) => !IsOptional(bone);

    /// <summary>
    /// Gets the skeleton bone index mapped to the specified humanoid bone role (O(1)).
    /// Returns -1 if unassigned.
    /// </summary>
    public int Get(HumanoidBone bone)
    {
        EnsureNotDisposed();
        int idx = (int)bone;
        if (idx < 0 || idx >= HumanoidBoneCount) return -1;
        return _boneIndices[idx];
    }

    /// <summary>
    /// Tries to get the skeleton bone index mapped to the specified humanoid bone role (O(1)).
    /// Returns true if mapped (>= 0), false otherwise.
    /// </summary>
    public bool TryGet(HumanoidBone bone, out int skeletonBoneIndex)
    {
        EnsureNotDisposed();
        int idx = (int)bone;
        if (idx >= 0 && idx < HumanoidBoneCount)
        {
            skeletonBoneIndex = _boneIndices[idx];
            return skeletonBoneIndex >= 0;
        }
        skeletonBoneIndex = -1;
        return false;
    }

    /// <summary>
    /// Sets the skeleton bone index mapped to the specified humanoid bone role.
    /// Pass -1 to unassign.
    /// </summary>
    public void Set(HumanoidBone bone, int skeletonBoneIndex)
    {
        EnsureNotDisposed();
        int idx = (int)bone;
        if (idx >= 0 && idx < HumanoidBoneCount)
        {
            _boneIndices[idx] = skeletonBoneIndex;
        }
    }

    /// <summary>
    /// Clears the assignment for the specified humanoid bone role.
    /// </summary>
    public void Clear(HumanoidBone bone) => Set(bone, -1);

    /// <summary>
    /// Clears all humanoid bone assignments.
    /// </summary>
    public void ClearAll()
    {
        EnsureNotDisposed();
        Array.Fill(_boneIndices, -1);
    }

    /// <summary>
    /// Indexer for getting or setting humanoid bone mappings.
    /// </summary>
    public int this[HumanoidBone bone]
    {
        get => Get(bone);
        set => Set(bone, value);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  VALIDATION
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates the mapping against its assigned <see cref="Skeleton"/>.
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        bool isValid = Validate(out List<string> errors);
        errorMessage = isValid ? null : string.Join("; ", errors);
        return isValid;
    }

    /// <summary>
    /// Validates the mapping against its assigned <see cref="Skeleton"/>, returning all error messages.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        EnsureNotDisposed();
        return Validate(Skeleton.Res, out errors);
    }

    /// <summary>
    /// Validates the mapping against a specified <see cref="SkeletonAsset"/>, returning all error messages.
    /// </summary>
    public bool Validate(SkeletonAsset? skeleton, out List<string> errors)
    {
        errors = new List<string>();

        if (skeleton == null)
        {
            errors.Add("SkeletonAsset is missing or null.");
            return false;
        }

        int skelBoneCount = skeleton.BoneCount;

        // 1. Check bounds for assigned indices
        for (int i = 0; i < HumanoidBoneCount; i++)
        {
            int skelIdx = _boneIndices[i];
            if (skelIdx >= skelBoneCount)
            {
                errors.Add($"HumanoidBone '{(HumanoidBone)i}' points to out-of-range skeleton index {skelIdx} (Skeleton has {skelBoneCount} bones).");
            }
        }

        // 2. Check required bones are assigned
        for (int i = 0; i < HumanoidBoneCount; i++)
        {
            var bone = (HumanoidBone)i;
            if (IsRequired(bone) && _boneIndices[i] < 0)
            {
                errors.Add($"Required humanoid bone '{bone}' is not assigned.");
            }
        }

        // 3. Check for duplicates (two humanoid roles mapped to same skeleton bone)
        var usedIndices = new Dictionary<int, HumanoidBone>();
        for (int i = 0; i < HumanoidBoneCount; i++)
        {
            int skelIdx = _boneIndices[i];
            if (skelIdx < 0 || skelIdx >= skelBoneCount) continue;

            if (usedIndices.TryGetValue(skelIdx, out HumanoidBone existingBone))
            {
                string boneName = skeleton[skelIdx].Name;
                errors.Add($"Skeleton bone {skelIdx} ('{boneName}') is mapped to multiple humanoid roles: '{existingBone}' and '{(HumanoidBone)i}'.");
            }
            else
            {
                usedIndices[skelIdx] = (HumanoidBone)i;
            }
        }

        // If there are fundamental assignment errors, skip hierarchy validation
        if (errors.Count > 0)
            return false;

        // 4. Hierarchy validation using SkeletonAsset.ParentIndex
        ValidateHierarchyChain(skeleton, HumanoidBone.Hips, HumanoidBone.Spine, errors);
        ValidateHierarchyChain(skeleton, HumanoidBone.Spine, HumanoidBone.Chest, errors);

        if (TryGet(HumanoidBone.UpperChest, out _))
        {
            ValidateHierarchyChain(skeleton, HumanoidBone.Chest, HumanoidBone.UpperChest, errors);
            ValidateHierarchyChain(skeleton, HumanoidBone.UpperChest, HumanoidBone.Neck, errors);
        }
        else
        {
            ValidateHierarchyChain(skeleton, HumanoidBone.Chest, HumanoidBone.Neck, errors);
        }

        ValidateHierarchyChain(skeleton, HumanoidBone.Neck, HumanoidBone.Head, errors);

        // Left Leg
        ValidateHierarchyChain(skeleton, HumanoidBone.Hips, HumanoidBone.LeftUpperLeg, errors);
        ValidateHierarchyChain(skeleton, HumanoidBone.LeftUpperLeg, HumanoidBone.LeftLowerLeg, errors);
        ValidateHierarchyChain(skeleton, HumanoidBone.LeftLowerLeg, HumanoidBone.LeftFoot, errors);
        if (TryGet(HumanoidBone.LeftToes, out _))
            ValidateHierarchyChain(skeleton, HumanoidBone.LeftFoot, HumanoidBone.LeftToes, errors);

        // Right Leg
        ValidateHierarchyChain(skeleton, HumanoidBone.Hips, HumanoidBone.RightUpperLeg, errors);
        ValidateHierarchyChain(skeleton, HumanoidBone.RightUpperLeg, HumanoidBone.RightLowerLeg, errors);
        ValidateHierarchyChain(skeleton, HumanoidBone.RightLowerLeg, HumanoidBone.RightFoot, errors);
        if (TryGet(HumanoidBone.RightToes, out _))
            ValidateHierarchyChain(skeleton, HumanoidBone.RightFoot, HumanoidBone.RightToes, errors);

        // Left Arm
        if (TryGet(HumanoidBone.LeftShoulder, out _))
        {
            ValidateHierarchyChain(skeleton, HumanoidBone.Chest, HumanoidBone.LeftShoulder, errors);
            ValidateHierarchyChain(skeleton, HumanoidBone.LeftShoulder, HumanoidBone.LeftUpperArm, errors);
        }
        else
        {
            ValidateHierarchyChain(skeleton, HumanoidBone.Chest, HumanoidBone.LeftUpperArm, errors);
        }
        ValidateHierarchyChain(skeleton, HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm, errors);
        ValidateHierarchyChain(skeleton, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand, errors);

        // Right Arm
        if (TryGet(HumanoidBone.RightShoulder, out _))
        {
            ValidateHierarchyChain(skeleton, HumanoidBone.Chest, HumanoidBone.RightShoulder, errors);
            ValidateHierarchyChain(skeleton, HumanoidBone.RightShoulder, HumanoidBone.RightUpperArm, errors);
        }
        else
        {
            ValidateHierarchyChain(skeleton, HumanoidBone.Chest, HumanoidBone.RightUpperArm, errors);
        }
        ValidateHierarchyChain(skeleton, HumanoidBone.RightUpperArm, HumanoidBone.RightLowerArm, errors);
        ValidateHierarchyChain(skeleton, HumanoidBone.RightLowerArm, HumanoidBone.RightHand, errors);

        return errors.Count == 0;
    }

    private void ValidateHierarchyChain(SkeletonAsset skeleton, HumanoidBone parentRole, HumanoidBone childRole, List<string> errors)
    {
        if (!TryGet(parentRole, out int parentSkelIdx) || !TryGet(childRole, out int childSkelIdx))
            return;

        if (!IsDescendantOf(skeleton, childSkelIdx, parentSkelIdx))
        {
            errors.Add($"Hierarchy mismatch: '{childRole}' (skeleton index {childSkelIdx}) is not a descendant of '{parentRole}' (skeleton index {parentSkelIdx}).");
        }
    }

    /// <summary>
    /// Checks whether <paramref name="childIndex"/> is a descendant of <paramref name="ancestorIndex"/>
    /// by traversing the <see cref="SkeletonAsset.GetParentIndex"/> chain.
    /// </summary>
    public static bool IsDescendantOf(SkeletonAsset skeleton, int childIndex, int ancestorIndex)
    {
        if (childIndex < 0 || ancestorIndex < 0 || childIndex == ancestorIndex)
            return false;

        int current = skeleton.GetParentIndex(childIndex);
        while (current >= 0)
        {
            if (current == ancestorIndex)
                return true;
            current = skeleton.GetParentIndex(current);
        }

        return false;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  SERIALIZATION
    // ════════════════════════════════════════════════════════════════════════

    public void Serialize(ref EchoObject compound, SerializationContext ctx)
    {
        compound.Add("Name", new EchoObject(Name));
        compound.Add("Skeleton", Serializer.Serialize(Skeleton, ctx));

        var boneList = EchoObject.NewList();
        for (int i = 0; i < HumanoidBoneCount; i++)
        {
            if (_boneIndices[i] >= 0)
            {
                var entry = EchoObject.NewCompound();
                entry.Add("Bone", new EchoObject(i));
                entry.Add("Index", new EchoObject(_boneIndices[i]));
                boneList.ListAdd(entry);
            }
        }
        compound.Add("Bones", boneList);
    }

    public void Deserialize(EchoObject compound, SerializationContext ctx)
    {
        Name = compound.Get("Name")?.StringValue ?? "HumanoidMapping";
        Skeleton = Serializer.Deserialize<AssetRef<SkeletonAsset>>(compound.Get("Skeleton"), ctx);

        Array.Fill(_boneIndices, -1);
        var bonesList = compound.Get("Bones");
        if (bonesList != null && bonesList.TagType == EchoType.List)
        {
            foreach (var item in bonesList.List)
            {
                if (item.TagType == EchoType.Compound)
                {
                    int bone = item.Get("Bone")?.IntValue ?? -1;
                    int index = item.Get("Index")?.IntValue ?? -1;
                    if (bone >= 0 && bone < HumanoidBoneCount)
                    {
                        _boneIndices[bone] = index;
                    }
                }
            }
        }
    }
}
