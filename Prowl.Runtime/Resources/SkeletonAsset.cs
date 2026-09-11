// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Prowl.Echo;
using Prowl.Echo.Cloning;
using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// Represents a single bone in a <see cref="SkeletonAsset"/>.
/// Stores the bone's hierarchical structure, rest pose and inverse bind pose.
/// </summary>
public struct SkeletonBone : ISerializable, IEquatable<SkeletonBone>
{
    public string Name;
    public uint Hash;
    public int ParentIndex;
    public Float3 LocalPosition;
    public Quaternion LocalRotation;
    public Float3 LocalScale;
    public Float4x4 InverseBindPose;

    public SkeletonBone()
    {
        Name = string.Empty;
        Hash = 0;
        ParentIndex = -1;
        LocalPosition = Float3.Zero;
        LocalRotation = Quaternion.Identity;
        LocalScale = Float3.One;
        InverseBindPose = Float4x4.Identity;
    }

    public SkeletonBone(
        string name,
        int parentIndex,
        Float3 localPosition,
        Quaternion localRotation,
        Float3 localScale,
        Float4x4 inverseBindPose)
    {
        Name = name ?? string.Empty;
        Hash = ComputeHash(Name);
        ParentIndex = parentIndex;
        LocalPosition = localPosition;
        LocalRotation = localRotation;
        LocalScale = localScale;
        InverseBindPose = inverseBindPose;
    }

    /// <summary>
    /// Computes a deterministic 32-bit FNV-1a hash of the bone name.
    /// </summary>
    public static uint ComputeHash(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        uint hash = 2166136261u;
        for (int i = 0; i < name.Length; i++)
        {
            hash ^= name[i];
            hash *= 16777619u;
        }
        return hash;
    }

    public void Serialize(ref EchoObject compound, SerializationContext ctx)
    {
        compound.Add("Name", new EchoObject(Name));
        compound.Add("Hash", new EchoObject((long)Hash));
        compound.Add("ParentIndex", new EchoObject(ParentIndex));
        compound.Add("LocalPosition", Serializer.Serialize(LocalPosition, ctx));
        compound.Add("LocalRotation", Serializer.Serialize(LocalRotation, ctx));
        compound.Add("LocalScale", Serializer.Serialize(LocalScale, ctx));
        compound.Add("InverseBindPose", Serializer.Serialize(InverseBindPose, ctx));
    }

    public void Deserialize(EchoObject compound, SerializationContext ctx)
    {
        Name = compound.Get("Name")?.StringValue ?? string.Empty;
        Hash = (uint)(compound.Get("Hash")?.LongValue ?? (long)ComputeHash(Name));
        ParentIndex = compound.Get("ParentIndex")?.IntValue ?? -1;
        LocalPosition = Serializer.Deserialize<Float3>(compound.Get("LocalPosition"), ctx);
        LocalRotation = Serializer.Deserialize<Quaternion>(compound.Get("LocalRotation"), ctx);
        LocalScale = Serializer.Deserialize<Float3>(compound.Get("LocalScale"), ctx);
        InverseBindPose = Serializer.Deserialize<Float4x4>(compound.Get("InverseBindPose"), ctx);
    }

    public bool Equals(SkeletonBone other)
    {
        return Name == other.Name
            && Hash == other.Hash
            && ParentIndex == other.ParentIndex
            && LocalPosition.Equals(other.LocalPosition)
            && LocalRotation.Equals(other.LocalRotation)
            && LocalScale.Equals(other.LocalScale)
            && InverseBindPose.Equals(other.InverseBindPose);
    }

    public override bool Equals(object? obj) => obj is SkeletonBone other && Equals(other);

    public override int GetHashCode() => (int)Hash;

    public static bool operator ==(SkeletonBone left, SkeletonBone right) => left.Equals(right);
    public static bool operator !=(SkeletonBone left, SkeletonBone right) => !left.Equals(right);
}

/// <summary>
/// A data asset representing a skeletal rig definition.
/// Stores bones, hierarchical relationships, rest transforms and inverse bind poses independently
/// from the live GameObject scene hierarchy.
/// </summary>
[CreateAssetMenu("Skeleton", Extension = ".skeleton", Order = 100)]
public sealed class SkeletonAsset : EngineObject, ISerializable
{
    private List<SkeletonBone> _bones = [];
    private Dictionary<string, int> _nameToIndex = new(StringComparer.Ordinal);
    private Dictionary<uint, int> _hashToIndex = [];

    /// <summary>Number of bones in the skeleton.</summary>
    public int BoneCount
    {
        get
        {
            EnsureNotDisposed();
            return _bones.Count;
        }
    }

    /// <summary>Read-only access to the bones list.</summary>
    public IReadOnlyList<SkeletonBone> Bones
    {
        get
        {
            EnsureNotDisposed();
            return _bones;
        }
    }

    /// <summary>Get or set bone by index.</summary>
    public SkeletonBone this[int index]
    {
        get
        {
            EnsureNotDisposed();
            return _bones[index];
        }
        set
        {
            EnsureNotDisposed();
            _bones[index] = value;
            RebuildLookup();
        }
    }

    /// <summary>Adds a bone to the skeleton.</summary>
    public void AddBone(SkeletonBone bone)
    {
        EnsureNotDisposed();
        int index = _bones.Count;
        _bones.Add(bone);
        if (!string.IsNullOrEmpty(bone.Name))
            _nameToIndex.TryAdd(bone.Name, index);
        _hashToIndex.TryAdd(bone.Hash, index);
    }

    /// <summary>Replaces the bones list.</summary>
    public void SetBones(IEnumerable<SkeletonBone> bones)
    {
        EnsureNotDisposed();
        _bones = new List<SkeletonBone>(bones);
        RebuildLookup();
    }

    /// <summary>Finds a bone index by exact name (O(1)). Returns -1 if not found.</summary>
    public int FindBoneIndex(string name)
    {
        EnsureNotDisposed();
        if (string.IsNullOrEmpty(name)) return -1;
        return _nameToIndex.TryGetValue(name, out int index) ? index : -1;
    }

    /// <summary>Finds a bone index by name hash (O(1)). Returns -1 if not found.</summary>
    public int FindBoneIndex(uint hash)
    {
        EnsureNotDisposed();
        return _hashToIndex.TryGetValue(hash, out int index) ? index : -1;
    }

    /// <summary>Gets a bone by name, or null if not found.</summary>
    public SkeletonBone? GetBone(string name)
    {
        int idx = FindBoneIndex(name);
        return idx >= 0 ? _bones[idx] : null;
    }

    /// <summary>Gets a bone by hash, or null if not found.</summary>
    public SkeletonBone? GetBone(uint hash)
    {
        int idx = FindBoneIndex(hash);
        return idx >= 0 ? _bones[idx] : null;
    }

    /// <summary>Returns the parent index for the given bone index, or -1 for root/invalid.</summary>
    public int GetParentIndex(int boneIndex)
    {
        EnsureNotDisposed();
        if (boneIndex < 0 || boneIndex >= _bones.Count) return -1;
        return _bones[boneIndex].ParentIndex;
    }

    private void RebuildLookup()
    {
        _nameToIndex.Clear();
        _hashToIndex.Clear();
        for (int i = 0; i < _bones.Count; i++)
        {
            if (!string.IsNullOrEmpty(_bones[i].Name))
                _nameToIndex.TryAdd(_bones[i].Name, i);
            _hashToIndex.TryAdd(_bones[i].Hash, i);
        }
    }

    public void Serialize(ref EchoObject compound, SerializationContext ctx)
    {
        compound.Add("Name", new EchoObject(Name));
        var boneList = EchoObject.NewList();
        foreach (var bone in _bones)
        {
            var boneCompound = EchoObject.NewCompound();
            var b = bone;
            b.Serialize(ref boneCompound, ctx);
            boneList.ListAdd(boneCompound);
        }
        compound.Add("Bones", boneList);
    }

    public void Deserialize(EchoObject compound, SerializationContext ctx)
    {
        Name = compound.Get("Name")?.StringValue ?? "Skeleton";
        _bones.Clear();
        var boneList = compound.Get("Bones");
        if (boneList != null)
        {
            foreach (var item in boneList.List)
            {
                var bone = new SkeletonBone();
                bone.Deserialize(item, ctx);
                _bones.Add(bone);
            }
        }
        RebuildLookup();
    }
}
