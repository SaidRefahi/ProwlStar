// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Vector;

namespace Prowl.Runtime.Animation;

/// <summary>
/// Represents a sampled pose for a single bone.
/// Lightweight value type: position, rotation and scale.
/// </summary>
public struct BonePose
{
    public Float3 Position;
    public Quaternion Rotation;
    public Float3 Scale;
}
