// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Prowl.Runtime;

namespace Prowl.Editor.Animation;

/// <summary>
/// Result of an auto-mapping attempt on a skeleton.
/// </summary>
public sealed class HumanoidAutoMappingResult
{
    /// <summary>Number of humanoid bone roles successfully mapped.</summary>
    public int MappedCount { get; set; }

    /// <summary>List of humanoid bone roles that had ambiguous candidate matches (not assigned).</summary>
    public List<HumanoidBone> AmbiguousRoles { get; } = new();

    /// <summary>List of required humanoid bone roles that could not be found.</summary>
    public List<HumanoidBone> MissingRequiredRoles { get; } = new();

    /// <summary>Validation messages/warnings encountered during auto-mapping.</summary>
    public List<string> Messages { get; } = new();

    /// <summary>Whether all required humanoid bones were successfully and unambiguously mapped.</summary>
    public bool IsSuccess => MissingRequiredRoles.Count == 0 && AmbiguousRoles.Count == 0;
}

/// <summary>
/// Editor-only utility for heuristic auto-mapping of <see cref="SkeletonAsset"/> bones
/// to <see cref="HumanoidBone"/> roles.
/// <para>
/// Never runs at runtime. Never chooses arbitrarily when candidates are ambiguous.
/// </para>
/// </summary>
public static class HumanoidAutoMapper
{
    private static readonly Dictionary<HumanoidBone, string[]> s_patterns = new()
    {
        // Torso & Head
        [HumanoidBone.Hips] = new[] { "hips", "pelvis", "root_joint", "hip" },
        [HumanoidBone.Spine] = new[] { "spine", "spine_0", "spine_01" },
        [HumanoidBone.Chest] = new[] { "chest", "spine1", "spine_02", "torso" },
        [HumanoidBone.UpperChest] = new[] { "upperchest", "upper_chest", "spine2", "spine3", "spine_03" },
        [HumanoidBone.Neck] = new[] { "neck", "neck_01" },
        [HumanoidBone.Head] = new[] { "head", "head_01" },

        // Left Arm
        [HumanoidBone.LeftShoulder] = new[] { "leftshoulder", "shoulder.l", "l_shoulder", "shoulder_l", "left_shoulder", "clavicle.l", "clavicle_l", "l_clavicle", "clavicle" },
        [HumanoidBone.LeftUpperArm] = new[] { "leftupperarm", "upperarm.l", "l_upperarm", "upperarm_l", "upper_arm.l", "upper_arm_l", "leftarm", "left_arm", "arm.l", "l_arm", "arm_l" },
        [HumanoidBone.LeftLowerArm] = new[] { "leftlowerarm", "lowerarm.l", "l_lowerarm", "lowerarm_l", "lower_arm.l", "lower_arm_l", "leftforearm", "left_forearm", "forearm.l", "l_forearm", "forearm_l" },
        [HumanoidBone.LeftHand] = new[] { "lefthand", "hand.l", "l_hand", "hand_l", "left_hand", "wrist.l", "l_wrist", "wrist_l" },

        // Right Arm
        [HumanoidBone.RightShoulder] = new[] { "rightshoulder", "shoulder.r", "r_shoulder", "shoulder_r", "right_shoulder", "clavicle.r", "clavicle_r", "r_clavicle" },
        [HumanoidBone.RightUpperArm] = new[] { "rightupperarm", "upperarm.r", "r_upperarm", "upperarm_r", "upper_arm.r", "upper_arm_r", "rightarm", "right_arm", "arm.r", "r_arm", "arm_r" },
        [HumanoidBone.RightLowerArm] = new[] { "rightlowerarm", "lowerarm.r", "r_lowerarm", "lowerarm_r", "lower_arm.r", "lower_arm_r", "rightforearm", "right_forearm", "forearm.r", "r_forearm", "forearm_r" },
        [HumanoidBone.RightHand] = new[] { "righthand", "hand.r", "r_hand", "hand_r", "right_hand", "wrist.r", "r_wrist", "wrist_r" },

        // Left Leg
        [HumanoidBone.LeftUpperLeg] = new[] { "leftupperleg", "upperleg.l", "l_upperleg", "upperleg_l", "upper_leg.l", "upper_leg_l", "leftthigh", "left_thigh", "thigh.l", "l_thigh", "thigh_l", "leftupleg", "upleg.l", "l_upleg", "upleg_l" },
        [HumanoidBone.LeftLowerLeg] = new[] { "leftlowerleg", "lowerleg.l", "l_lowerleg", "lowerleg_l", "lower_leg.l", "lower_leg_l", "leftshin", "left_shin", "shin.l", "l_shin", "shin_l", "leftcalf", "left_calf", "calf.l", "l_calf", "calf_l", "leftleg" },
        [HumanoidBone.LeftFoot] = new[] { "leftfoot", "foot.l", "l_foot", "foot_l", "left_foot", "ankle.l", "l_ankle", "ankle_l" },
        [HumanoidBone.LeftToes] = new[] { "lefttoebase", "toebase.l", "l_toebase", "toebase_l", "lefttoes", "lefttoe", "toes.l", "toe.l", "l_toes", "l_toe", "left_toe", "left_toes", "toe_l", "toes_l" },

        // Right Leg
        [HumanoidBone.RightUpperLeg] = new[] { "rightupperleg", "upperleg.r", "r_upperleg", "upperleg_r", "upper_leg.r", "upper_leg_r", "rightthigh", "right_thigh", "thigh.r", "r_thigh", "thigh_r", "rightupleg", "upleg.r", "r_upleg", "upleg_r" },
        [HumanoidBone.RightLowerLeg] = new[] { "rightlowerleg", "lowerleg.r", "r_lowerleg", "lowerleg_r", "lower_leg.r", "lower_leg_r", "rightshin", "right_shin", "shin.r", "r_shin", "shin_r", "rightcalf", "right_calf", "calf.r", "r_calf", "calf_r", "rightleg" },
        [HumanoidBone.RightFoot] = new[] { "rightfoot", "foot.r", "r_foot", "foot_r", "right_foot", "ankle.r", "r_ankle", "ankle_r" },
        [HumanoidBone.RightToes] = new[] { "righttoebase", "toebase.r", "r_toebase", "toebase_r", "righttoes", "righttoe", "toes.r", "toe.r", "r_toes", "r_toe", "right_toe", "right_toes", "toe_r", "toes_r" },
    };

    /// <summary>
    /// Attempts to heuristically match skeleton bones to humanoid roles.
    /// Does NOT assign roles when ambiguous candidates exist.
    /// </summary>
    public static HumanoidAutoMappingResult AutoMap(SkeletonAsset skeleton, HumanoidMapping mapping)
    {
        var result = new HumanoidAutoMappingResult();
        if (skeleton == null || skeleton.BoneCount == 0)
        {
            result.Messages.Add("Skeleton is null or empty.");
            return result;
        }

        // Clean normalized names for all skeleton bones
        int boneCount = skeleton.BoneCount;
        var cleanNames = new string[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            cleanNames[i] = NormalizeName(skeleton[i].Name);
        }

        // Track candidates per humanoid role: HumanoidBone -> list of (boneIndex, score)
        var candidatesByRole = new Dictionary<HumanoidBone, List<(int boneIndex, int score)>>();

        for (int r = 0; r < HumanoidMapping.HumanoidBoneCount; r++)
        {
            var role = (HumanoidBone)r;
            if (!s_patterns.TryGetValue(role, out var patterns)) continue;

            var candidates = new List<(int, int)>();

            for (int b = 0; b < boneCount; b++)
            {
                string boneName = cleanNames[b];
                int score = ScoreMatch(boneName, patterns);
                if (score > 0)
                {
                    candidates.Add((b, score));
                }
            }

            // Sort descending by score
            candidates.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            candidatesByRole[role] = candidates;
        }

        // Assign unambiguous top matches
        var usedBones = new HashSet<int>();
        int mappedCount = 0;

        for (int r = 0; r < HumanoidMapping.HumanoidBoneCount; r++)
        {
            var role = (HumanoidBone)r;
            if (!candidatesByRole.TryGetValue(role, out var candidates) || candidates.Count == 0)
            {
                if (HumanoidMapping.IsRequired(role))
                    result.MissingRequiredRoles.Add(role);
                continue;
            }

            // Check if top candidate is tied with second candidate
            if (candidates.Count > 1 && candidates[0].Item2 == candidates[1].Item2 && candidates[0].Item1 != candidates[1].Item1)
            {
                result.AmbiguousRoles.Add(role);
                result.Messages.Add($"Ambiguous match for role '{role}': multiple bones matched with equal confidence.");
                continue;
            }

            int topBone = candidates[0].Item1;
            if (usedBones.Contains(topBone))
            {
                // Conflict: already claimed by another role
                result.AmbiguousRoles.Add(role);
                result.Messages.Add($"Conflict: bone '{skeleton[topBone].Name}' was matched to multiple roles.");
                continue;
            }

            // Successfully mapped
            mapping.Set(role, topBone);
            usedBones.Add(topBone);
            mappedCount++;
        }

        result.MappedCount = mappedCount;
        return result;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        // Strip prefixes like "mixamorig:" or namespaces
        int colon = name.LastIndexOf(':');
        if (colon >= 0 && colon < name.Length - 1)
            name = name.Substring(colon + 1);

        return name.ToLowerInvariant().Replace(" ", "").Replace("-", "_");
    }

    private static int ScoreMatch(string cleanBoneName, string[] patterns)
    {
        for (int i = 0; i < patterns.Length; i++)
        {
            string pat = patterns[i].ToLowerInvariant();

            // Exact match gets highest score
            if (cleanBoneName == pat)
                return 100 - i;

            // Contains pattern with word boundaries
            if (cleanBoneName.StartsWith(pat + "_") || cleanBoneName.EndsWith("_" + pat) || cleanBoneName.Contains("_" + pat + "_"))
                return 80 - i;

            if (cleanBoneName.Contains(pat))
                return 50 - i;
        }

        return 0;
    }
}
