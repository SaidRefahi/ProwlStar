// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Prowl.Echo;
using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// Calibrated reference pose data for a single humanoid bone.
/// Stores model-space reference position/rotation, segment direction to child, and length.
/// </summary>
public struct HumanoidBoneCalibration : ISerializable, IEquatable<HumanoidBoneCalibration>
{
    /// <summary>Index of the bone in the source <see cref="SkeletonAsset"/> (-1 if unassigned).</summary>
    public int SkeletonBoneIndex;

    /// <summary>Reference model-space position.</summary>
    public Float3 Position;

    /// <summary>Reference model-space rotation.</summary>
    public Quaternion Rotation;

    /// <summary>Normalized direction vector pointing towards the next child bone in the humanoid chain.</summary>
    public Float3 Direction;

    /// <summary>Length of the bone segment (distance to next child bone, 0 for leaf bones).</summary>
    public float Length;

    /// <summary>Whether this humanoid bone role is assigned in the mapping.</summary>
    public bool IsAssigned;

    public void Serialize(ref EchoObject compound, SerializationContext ctx)
    {
        compound.Add("SkeletonBoneIndex", new EchoObject(SkeletonBoneIndex));
        compound.Add("Position", Serializer.Serialize(Position, ctx));
        compound.Add("Rotation", Serializer.Serialize(Rotation, ctx));
        compound.Add("Direction", Serializer.Serialize(Direction, ctx));
        compound.Add("Length", new EchoObject(Length));
        compound.Add("IsAssigned", new EchoObject(IsAssigned));
    }

    public void Deserialize(EchoObject compound, SerializationContext ctx)
    {
        SkeletonBoneIndex = compound.Get("SkeletonBoneIndex")?.IntValue ?? -1;
        Position = Serializer.Deserialize<Float3>(compound.Get("Position"), ctx);
        Rotation = Serializer.Deserialize<Quaternion>(compound.Get("Rotation"), ctx);
        Direction = Serializer.Deserialize<Float3>(compound.Get("Direction"), ctx);
        Length = compound.Get("Length")?.FloatValue ?? 0f;
        IsAssigned = compound.Get("IsAssigned")?.BoolValue ?? false;
    }

    public bool Equals(HumanoidBoneCalibration other) =>
        SkeletonBoneIndex == other.SkeletonBoneIndex &&
        Position.Equals(other.Position) &&
        Rotation.Equals(other.Rotation) &&
        Direction.Equals(other.Direction) &&
        Math.Abs(Length - other.Length) < 1e-6f &&
        IsAssigned == other.IsAssigned;

    public override bool Equals(object? obj) => obj is HumanoidBoneCalibration other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(SkeletonBoneIndex, Position, Rotation, Direction, Length, IsAssigned);
}

/// <summary>
/// Precomputed calibration data derived from a <see cref="SkeletonAsset"/> and its <see cref="HumanoidMapping"/>.
/// Stores model-space reference transforms, segment lengths, and direction vectors for all humanoid bones (O(1) lookups).
/// </summary>
public sealed class HumanoidCalibration : EngineObject, ISerializable
{
    private readonly HumanoidBoneCalibration[] _bones = new HumanoidBoneCalibration[HumanoidMapping.HumanoidBoneCount];

    public HumanoidCalibration()
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            _bones[i].SkeletonBoneIndex = -1;
            _bones[i].Position = Float3.Zero;
            _bones[i].Rotation = Quaternion.Identity;
            _bones[i].Direction = Float3.Zero;
            _bones[i].Length = 0f;
            _bones[i].IsAssigned = false;
        }
    }

    /// <summary>
    /// Builds a <see cref="HumanoidCalibration"/> from a skeleton and mapping.
    /// </summary>
    public static bool Build(SkeletonAsset? skeleton, HumanoidMapping? mapping, out HumanoidCalibration? calibration) =>
        Build(skeleton, mapping, out calibration, out _);

    /// <summary>
    /// Builds a <see cref="HumanoidCalibration"/> from a skeleton and mapping, providing detailed validation errors if failed.
    /// </summary>
    public static bool Build(SkeletonAsset? skeleton, HumanoidMapping? mapping, out HumanoidCalibration? calibration, out List<string> errors)
    {
        calibration = null;
        errors = new List<string>();

        if (skeleton == null)
        {
            errors.Add("SkeletonAsset is null or missing.");
            return false;
        }

        if (mapping == null)
        {
            errors.Add("HumanoidMapping is null or missing.");
            return false;
        }

        if (!mapping.Validate(skeleton, out errors))
        {
            return false;
        }

        int skelCount = skeleton.BoneCount;
        var worldMatrices = new Float4x4[skelCount];
        var worldPositions = new Float3[skelCount];
        var worldRotations = new Quaternion[skelCount];

        // 1. Compute model-space transforms for all skeleton bones top-down
        for (int i = 0; i < skelCount; i++)
        {
            var bone = skeleton[i];
            var localTRS = Float4x4.CreateTRS(bone.LocalPosition, bone.LocalRotation, bone.LocalScale);

            if (bone.ParentIndex < 0 || bone.ParentIndex >= i)
            {
                worldMatrices[i] = localTRS;
                worldRotations[i] = bone.LocalRotation;
            }
            else
            {
                worldMatrices[i] = worldMatrices[bone.ParentIndex] * localTRS;
                worldRotations[i] = worldRotations[bone.ParentIndex] * bone.LocalRotation;
            }

            worldPositions[i] = Float4x4.TransformPoint(Float3.Zero, worldMatrices[i]);
        }

        var result = new HumanoidCalibration();

        // 2. Assign reference positions and rotations
        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            var hb = (HumanoidBone)i;
            if (mapping.TryGet(hb, out int skelIdx))
            {
                result._bones[i].SkeletonBoneIndex = skelIdx;
                result._bones[i].Position = worldPositions[skelIdx];
                result._bones[i].Rotation = worldRotations[skelIdx];
                result._bones[i].IsAssigned = true;
            }
        }

        // 3. Compute directions and lengths toward target child bones
        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            if (!result._bones[i].IsAssigned) continue;

            var hb = (HumanoidBone)i;
            var targetChild = GetTargetChild(hb, mapping);

            if (targetChild.HasValue && mapping.TryGet(targetChild.Value, out int childSkelIdx))
            {
                Float3 delta = worldPositions[childSkelIdx] - result._bones[i].Position;
                float length = Float3.Length(delta);
                result._bones[i].Length = length;

                if (length > 1e-5f)
                {
                    result._bones[i].Direction = delta / length;
                }
                else
                {
                    result._bones[i].Direction = Float3.Zero;
                    errors.Add($"Degenerate zero-length segment: bone '{hb}' and child '{targetChild.Value}' are at identical positions.");
                }
            }
            else
            {
                // Leaf bone: length is 0, direction continues from parent if available
                result._bones[i].Length = 0f;
                var targetParent = GetTargetParent(hb, mapping);
                if (targetParent.HasValue && mapping.TryGet(targetParent.Value, out int parentSkelIdx))
                {
                    Float3 parentDelta = result._bones[i].Position - worldPositions[parentSkelIdx];
                    float parentLen = Float3.Length(parentDelta);
                    result._bones[i].Direction = parentLen > 1e-5f ? parentDelta / parentLen : Float3.UnitY;
                }
                else
                {
                    result._bones[i].Direction = Float3.UnitY;
                }
            }
        }

        // 4. Validate finiteness
        for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
        {
            if (!result._bones[i].IsAssigned) continue;
            ref var b = ref result._bones[i];

            if (!IsFinite(b.Position) || !IsFinite(b.Rotation) || !IsFinite(b.Direction) || !float.IsFinite(b.Length))
            {
                errors.Add($"Non-finite calibration data detected in bone '{(HumanoidBone)i}'.");
            }
        }

        if (errors.Count > 0)
            return false;

        calibration = result;
        return true;
    }

    /// <summary>
    /// Gets the next logical child in the humanoid hierarchy for computing segment vectors.
    /// </summary>
    public static HumanoidBone? GetTargetChild(HumanoidBone bone, HumanoidMapping mapping) => bone switch
    {
        HumanoidBone.Hips => HumanoidBone.Spine,
        HumanoidBone.Spine => HumanoidBone.Chest,
        HumanoidBone.Chest => mapping.TryGet(HumanoidBone.UpperChest, out _) ? HumanoidBone.UpperChest : HumanoidBone.Neck,
        HumanoidBone.UpperChest => HumanoidBone.Neck,
        HumanoidBone.Neck => HumanoidBone.Head,
        HumanoidBone.Head => null, // Leaf

        HumanoidBone.LeftShoulder => HumanoidBone.LeftUpperArm,
        HumanoidBone.LeftUpperArm => HumanoidBone.LeftLowerArm,
        HumanoidBone.LeftLowerArm => HumanoidBone.LeftHand,
        HumanoidBone.LeftHand => null, // Leaf

        HumanoidBone.RightShoulder => HumanoidBone.RightUpperArm,
        HumanoidBone.RightUpperArm => HumanoidBone.RightLowerArm,
        HumanoidBone.RightLowerArm => HumanoidBone.RightHand,
        HumanoidBone.RightHand => null, // Leaf

        HumanoidBone.LeftUpperLeg => HumanoidBone.LeftLowerLeg,
        HumanoidBone.LeftLowerLeg => HumanoidBone.LeftFoot,
        HumanoidBone.LeftFoot => mapping.TryGet(HumanoidBone.LeftToes, out _) ? HumanoidBone.LeftToes : null,
        HumanoidBone.LeftToes => null, // Leaf

        HumanoidBone.RightUpperLeg => HumanoidBone.RightLowerLeg,
        HumanoidBone.RightLowerLeg => HumanoidBone.RightFoot,
        HumanoidBone.RightFoot => mapping.TryGet(HumanoidBone.RightToes, out _) ? HumanoidBone.RightToes : null,
        HumanoidBone.RightToes => null, // Leaf

        _ => null
    };

    /// <summary>
    /// Gets the parent in the humanoid hierarchy for leaf direction continuation.
    /// </summary>
    public static HumanoidBone? GetTargetParent(HumanoidBone bone, HumanoidMapping mapping) => bone switch
    {
        HumanoidBone.Head => HumanoidBone.Neck,
        HumanoidBone.LeftHand => HumanoidBone.LeftLowerArm,
        HumanoidBone.RightHand => HumanoidBone.RightLowerArm,
        HumanoidBone.LeftToes => HumanoidBone.LeftFoot,
        HumanoidBone.RightToes => HumanoidBone.RightFoot,
        _ => null
    };

    // ════════════════════════════════════════════════════════════════════════
    //  ACCESS API (O(1), zero allocations)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tries to get the reference model-space position and rotation for the specified humanoid bone.
    /// </summary>
    public bool TryGetReferencePose(HumanoidBone bone, out Float3 position, out Quaternion rotation)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < _bones.Length && _bones[idx].IsAssigned)
        {
            position = _bones[idx].Position;
            rotation = _bones[idx].Rotation;
            return true;
        }
        position = Float3.Zero;
        rotation = Quaternion.Identity;
        return false;
    }

    /// <summary>
    /// Tries to get the segment length for the specified humanoid bone.
    /// </summary>
    public bool TryGetBoneLength(HumanoidBone bone, out float length)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < _bones.Length && _bones[idx].IsAssigned)
        {
            length = _bones[idx].Length;
            return true;
        }
        length = 0f;
        return false;
    }

    /// <summary>
    /// Tries to get the normalized bone direction for the specified humanoid bone.
    /// </summary>
    public bool TryGetBoneDirection(HumanoidBone bone, out Float3 direction)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < _bones.Length && _bones[idx].IsAssigned)
        {
            direction = _bones[idx].Direction;
            return true;
        }
        direction = Float3.Zero;
        return false;
    }

    /// <summary>
    /// Gets the full calibration struct for the specified humanoid bone (O(1)).
    /// </summary>
    public HumanoidBoneCalibration Get(HumanoidBone bone)
    {
        int idx = (int)bone;
        if (idx >= 0 && idx < _bones.Length)
            return _bones[idx];
        return default;
    }

    /// <summary>
    /// Indexer for accessing bone calibration data.
    /// </summary>
    public HumanoidBoneCalibration this[HumanoidBone bone] => Get(bone);

    /// <summary>
    /// Validates internal calibration data consistency.
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        bool valid = Validate(out List<string> errors);
        errorMessage = valid ? null : string.Join("; ", errors);
        return valid;
    }

    /// <summary>
    /// Validates internal calibration data consistency, returning all errors.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        for (int i = 0; i < _bones.Length; i++)
        {
            if (!_bones[i].IsAssigned) continue;
            ref var b = ref _bones[i];
            if (!IsFinite(b.Position) || !IsFinite(b.Rotation) || !IsFinite(b.Direction) || !float.IsFinite(b.Length))
            {
                errors.Add($"Non-finite calibration data in bone '{(HumanoidBone)i}'.");
            }
        }
        return errors.Count == 0;
    }

    private static bool IsFinite(Float3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    private static bool IsFinite(Quaternion q) =>
        float.IsFinite(q.X) && float.IsFinite(q.Y) && float.IsFinite(q.Z) && float.IsFinite(q.W);

    // ════════════════════════════════════════════════════════════════════════
    //  SERIALIZATION
    // ════════════════════════════════════════════════════════════════════════

    public void Serialize(ref EchoObject compound, SerializationContext ctx)
    {
        compound.Add("Name", new EchoObject(Name));
        var boneList = EchoObject.NewList();
        for (int i = 0; i < _bones.Length; i++)
        {
            var boneCompound = EchoObject.NewCompound();
            var b = _bones[i];
            b.Serialize(ref boneCompound, ctx);
            boneList.ListAdd(boneCompound);
        }
        compound.Add("Bones", boneList);
    }

    public void Deserialize(EchoObject compound, SerializationContext ctx)
    {
        Name = compound.Get("Name")?.StringValue ?? "HumanoidCalibration";
        var boneList = compound.Get("Bones");
        if (boneList != null && boneList.TagType == EchoType.List)
        {
            int count = Math.Min(boneList.List.Count, _bones.Length);
            for (int i = 0; i < count; i++)
            {
                var item = boneList.List[i];
                if (item.TagType == EchoType.Compound)
                {
                    _bones[i].Deserialize(item, ctx);
                }
            }
        }
    }
}
