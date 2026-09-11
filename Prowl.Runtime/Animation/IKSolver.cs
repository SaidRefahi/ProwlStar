// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using Prowl.Vector;

namespace Prowl.Runtime.Animation;

/// <summary>
/// Stateless analytical Inverse Kinematics solver for skeletal animation.
/// Solves Two-Bone IK chains (arms, legs) with zero allocations in the hot path.
/// </summary>
public static class IKSolver
{
    /// <summary>
    /// Computes the shortest-arc rotation from one vector to another using pure Quaternion arithmetic.
    /// </summary>
    public static Quaternion FromToRotation(Float3 from, Float3 to)
    {
        float fromSq = Float3.LengthSquared(from);
        float toSq = Float3.LengthSquared(to);
        if (fromSq < 1e-8f || toSq < 1e-8f)
            return Quaternion.Identity;

        Float3 a = Float3.Normalize(from);
        Float3 b = Float3.Normalize(to);
        float d = Float3.Dot(a, b);

        if (d >= 0.999999f)
            return Quaternion.Identity;

        if (d <= -0.999999f)
        {
            Float3 axis = Float3.Cross(a, Float3.UnitX);
            if (Float3.LengthSquared(axis) < 1e-6f)
                axis = Float3.Cross(a, Float3.UnitY);
            return Quaternion.AxisAngle(Float3.Normalize(axis), MathF.PI);
        }

        Float3 c = Float3.Cross(a, b);
        return Quaternion.NormalizeSafe(new Quaternion(c.X, c.Y, c.Z, 1f + d));
    }

    /// <summary>
    /// Solves an analytical Two-Bone IK chain (e.g. UpperArm -> LowerArm -> Hand or UpperLeg -> LowerLeg -> Foot).
    /// </summary>
    public static bool SolveTwoBoneIK(
        in Float3 rootWorldPos, in Quaternion rootWorldRot,
        in Float3 midWorldPos, in Quaternion midWorldRot,
        in Float3 endWorldPos, in Quaternion endWorldRot,
        in Quaternion parentWorldRot,
        in Float3 targetWorldPos,
        in Quaternion? targetWorldRot,
        in Float3? hintWorldPos,
        float posWeight,
        float rotWeight,
        out Quaternion solvedLocalRot0,
        out Quaternion solvedLocalRot1,
        out Quaternion solvedLocalRot2)
    {
        Quaternion invParentRot = Quaternion.Inverse(parentWorldRot);
        Quaternion origLocalRot0 = invParentRot * rootWorldRot;
        Quaternion origLocalRot1 = Quaternion.Inverse(rootWorldRot) * midWorldRot;
        Quaternion origLocalRot2 = Quaternion.Inverse(midWorldRot) * endWorldRot;

        posWeight = Math.Clamp(posWeight, 0f, 1f);
        rotWeight = Math.Clamp(rotWeight, 0f, 1f);

        if (posWeight <= 1e-5f && rotWeight <= 1e-5f)
        {
            solvedLocalRot0 = origLocalRot0;
            solvedLocalRot1 = origLocalRot1;
            solvedLocalRot2 = origLocalRot2;
            return false;
        }

        Float3 p0 = rootWorldPos;
        Float3 p1 = midWorldPos;
        Float3 p2 = endWorldPos;

        Float3 v01 = p1 - p0;
        Float3 v12 = p2 - p1;

        float len1 = Float3.Length(v01);
        float len2 = Float3.Length(v12);

        if (len1 < 1e-5f || len2 < 1e-5f)
        {
            solvedLocalRot0 = origLocalRot0;
            solvedLocalRot1 = origLocalRot1;
            solvedLocalRot2 = origLocalRot2;
            return false;
        }

        Float3 tVec = targetWorldPos - p0;
        float d = Float3.Length(tVec);

        float maxReach = (len1 + len2) * 0.99999f;
        float minReach = Math.Abs(len1 - len2) + 1e-4f;
        float dClamped = Math.Clamp(d, Math.Max(1e-4f, minReach), Math.Max(1e-4f, maxReach));

        Float3 tDir;
        if (d < 1e-5f)
        {
            tDir = Float3.LengthSquared(v01) > 1e-6f ? Float3.Normalize(v01) : Float3.UnitZ;
        }
        else
        {
            tDir = tVec / d;
        }

        // Law of Cosines
        float cosAlpha = (len1 * len1 + dClamped * dClamped - len2 * len2) / (2f * len1 * dClamped);
        cosAlpha = Math.Clamp(cosAlpha, -1f, 1f);
        float sinAlpha = MathF.Sqrt(Math.Max(0f, 1f - cosAlpha * cosAlpha));

        // Bend plane normal: from target and hint (or animated mid pos)
        Float3 hintDir = hintWorldPos.HasValue ? (hintWorldPos.Value - p0) : v01;
        Float3 bendNormal = Float3.Cross(tDir, hintDir);

        if (Float3.LengthSquared(bendNormal) < 1e-6f)
        {
            bendNormal = Float3.Cross(tDir, v01);
            if (Float3.LengthSquared(bendNormal) < 1e-6f)
            {
                bendNormal = Float3.Cross(tDir, v12);
                if (Float3.LengthSquared(bendNormal) < 1e-6f)
                {
                    Float3 perp = Float3.Cross(tDir, Float3.UnitX);
                    if (Float3.LengthSquared(perp) < 1e-6f)
                        perp = Float3.Cross(tDir, Float3.UnitY);
                    bendNormal = perp;
                }
            }
        }
        bendNormal = Float3.Normalize(bendNormal);

        // Vector perpendicular to tDir in the bend plane pointing towards the bend
        Float3 bendDir = Float3.Cross(bendNormal, tDir);
        if (Float3.LengthSquared(bendDir) > 1e-6f)
            bendDir = Float3.Normalize(bendDir);

        // Ensure bendDir points in direction of hint (or animated mid pos)
        Float3 referenceHint = hintWorldPos.HasValue ? (hintWorldPos.Value - p0) : v01;
        if (Float3.Dot(bendDir, referenceHint) < 0f)
        {
            bendDir = -bendDir;
            bendNormal = -bendNormal;
        }

        // Solved mid joint position in world space
        Float3 p1Solved = p0 + (tDir * cosAlpha + bendDir * sinAlpha) * len1;
        Float3 targetClamped = p0 + tDir * dClamped;

        Float3 v01Solved = p1Solved - p0;
        Float3 v12Solved = targetClamped - p1Solved;

        // Orientation changes
        Quaternion q0 = FromToRotation(v01, v01Solved);
        Quaternion newWorldRot0 = q0 * rootWorldRot;

        Float3 v12Intermediate = q0 * v12;
        Quaternion q1 = FromToRotation(v12Intermediate, v12Solved);
        Quaternion newWorldRot1 = q1 * (q0 * midWorldRot);

        // Local rotations
        Quaternion targetLocalRot0 = invParentRot * newWorldRot0;
        Quaternion targetLocalRot1 = Quaternion.Inverse(newWorldRot0) * newWorldRot1;

        solvedLocalRot0 = Quaternion.Slerp(origLocalRot0, targetLocalRot0, posWeight);
        solvedLocalRot1 = Quaternion.Slerp(origLocalRot1, targetLocalRot1, posWeight);

        // End-effector rotation
        if (targetWorldRot.HasValue && rotWeight > 1e-5f)
        {
            Quaternion solvedTargetWorldRot = targetWorldRot.Value;
            Quaternion blendedWorldRot2 = Quaternion.Slerp(endWorldRot, solvedTargetWorldRot, rotWeight);
            Quaternion targetLocalRot2 = Quaternion.Inverse(newWorldRot1) * blendedWorldRot2;
            solvedLocalRot2 = Quaternion.Slerp(origLocalRot2, targetLocalRot2, posWeight > 0f ? posWeight : rotWeight);
        }
        else
        {
            Quaternion solvedWorldRot2 = q1 * (q0 * endWorldRot);
            Quaternion targetLocalRot2 = Quaternion.Inverse(newWorldRot1) * solvedWorldRot2;
            solvedLocalRot2 = Quaternion.Slerp(origLocalRot2, targetLocalRot2, posWeight);
        }

        return true;
    }
}
