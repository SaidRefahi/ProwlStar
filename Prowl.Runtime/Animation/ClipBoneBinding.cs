// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Vector;

namespace Prowl.Runtime.Animation;

/// <summary>
/// Resolved mapping from one clip bone to one skeleton bone and its scene Transform.
/// Built once per clip+skeleton pair, consumed every frame via indexed array access.
/// </summary>
internal struct ClipBoneBinding
{
    /// <summary>Index into <see cref="AnimationClip.Bones"/>.</summary>
    public int ClipBoneIndex;

    /// <summary>Index into <see cref="SkeletonAsset.Bones"/> (-1 = unresolved).</summary>
    public int SkeletonBoneIndex;

    /// <summary>Cached scene Transform to write the pose into. Null if unresolved.</summary>
    public Transform? Target;
}
