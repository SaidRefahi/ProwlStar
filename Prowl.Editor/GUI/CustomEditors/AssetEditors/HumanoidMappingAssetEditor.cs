// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Prowl.Echo;
using Prowl.Editor.Animation;
using Prowl.Editor.GUI;
using Prowl.Editor.Projects;
using Prowl.Editor.Theming;
using Prowl.OrigamiUI;
using Prowl.PaperUI;
using Prowl.Runtime;
using Prowl.Vector;

namespace Prowl.Editor.Inspector;

/// <summary>
/// Custom inspector for <see cref="HumanoidMapping"/> assets.
/// Provides visual bone role assignment, auto-mapping heuristics, live validation, and calibration inspection.
/// </summary>
[CustomAssetEditor(typeof(HumanoidMapping))]
public class HumanoidMappingAssetEditor : AssetImporterEditor
{
    private static readonly HumanoidBone[] s_spineBones =
    {
        HumanoidBone.Hips,
        HumanoidBone.Spine,
        HumanoidBone.Chest,
        HumanoidBone.UpperChest,
        HumanoidBone.Neck,
        HumanoidBone.Head
    };

    private static readonly HumanoidBone[] s_leftArmBones =
    {
        HumanoidBone.LeftShoulder,
        HumanoidBone.LeftUpperArm,
        HumanoidBone.LeftLowerArm,
        HumanoidBone.LeftHand
    };

    private static readonly HumanoidBone[] s_rightArmBones =
    {
        HumanoidBone.RightShoulder,
        HumanoidBone.RightUpperArm,
        HumanoidBone.RightLowerArm,
        HumanoidBone.RightHand
    };

    private static readonly HumanoidBone[] s_leftLegBones =
    {
        HumanoidBone.LeftUpperLeg,
        HumanoidBone.LeftLowerLeg,
        HumanoidBone.LeftFoot,
        HumanoidBone.LeftToes
    };

    private static readonly HumanoidBone[] s_rightLegBones =
    {
        HumanoidBone.RightUpperLeg,
        HumanoidBone.RightLowerLeg,
        HumanoidBone.RightFoot,
        HumanoidBone.RightToes
    };

    protected override EchoObject? CaptureState(AssetEntry entry, EngineObject? asset)
        => asset is HumanoidMapping mapping && mapping.IsValid() ? Serializer.Serialize(typeof(object), mapping) : null;

    public override void OnGUI(Paper paper, string id, AssetEntry entry, EngineObject? asset)
    {
        if (asset is not HumanoidMapping mapping || !mapping.IsValid()) return;

        var font = EditorTheme.DefaultFont;
        if (font == null) return;

        Origami.Header(paper, $"{id}_header", "Humanoid Bone Mapping").Line().Show();

        // 1. Target Skeleton Asset reference field
        var skel = mapping.Skeleton.Res;
        string skelName = skel != null ? skel.Name : "(None)";
        EditorGUI.Row(paper, $"{id}_skel", "Skeleton", () =>
        {
            Origami.Label(paper, $"{id}_skel_txt", skelName).Show();
        });

        // 2. Action toolbar (Auto-Map, Clear All)
        if (skel != null && skel.BoneCount > 0)
        {
            Origami.Button(paper, $"{id}_btn_automap", "Auto-Map", () =>
            {
                var result = HumanoidAutoMapper.AutoMap(skel, mapping);
                if (result.IsSuccess)
                    Runtime.Debug.Log($"[HumanoidMapping] Auto-mapped {result.MappedCount} bones successfully.");
                else
                    Runtime.Debug.LogWarning($"[HumanoidMapping] Auto-mapped {result.MappedCount} bones. {result.AmbiguousRoles.Count} ambiguous roles.");
            }).Show();

            Origami.Button(paper, $"{id}_btn_clear", "Clear All", () =>
            {
                mapping.ClearAll();
            }).Show();
        }

        Origami.Separator(paper, $"{id}_sep1").Show();

        // 3. Bone Role Groups
        DrawBoneGroup(paper, id, "Spine & Head", s_spineBones, mapping, skel);
        DrawBoneGroup(paper, id, "Left Arm", s_leftArmBones, mapping, skel);
        DrawBoneGroup(paper, id, "Right Arm", s_rightArmBones, mapping, skel);
        DrawBoneGroup(paper, id, "Left Leg", s_leftLegBones, mapping, skel);
        DrawBoneGroup(paper, id, "Right Leg", s_rightLegBones, mapping, skel);

        Origami.Separator(paper, $"{id}_sep2").Show();

        // 4. Validation section
        Origami.Header(paper, $"{id}_val_header", "Validation Status").Show();
        if (skel == null)
        {
            Origami.Label(paper, $"{id}_val_noskel", "No Skeleton assigned.").Show();
        }
        else if (mapping.Validate(skel, out var errors))
        {
            Origami.Label(paper, $"{id}_val_ok", "Rig Valid and Calibrated").Show();

            // 5. Calibration preview
            if (HumanoidCalibration.Build(skel, mapping, out var cal, out _))
            {
                Origami.Header(paper, $"{id}_cal_header", "Bone Measurements").Show();
                for (int i = 0; i < HumanoidMapping.HumanoidBoneCount; i++)
                {
                    var bone = (HumanoidBone)i;
                    if (cal!.TryGetBoneLength(bone, out float len) && len > 0.001f)
                    {
                        EditorGUI.Row(paper, $"{id}_len_{i}", bone.ToString(), () =>
                        {
                            Origami.Label(paper, $"{id}_lentxt_{i}", $"{len:F3} m").Show();
                        });
                    }
                }
            }
        }
        else
        {
            foreach (var err in errors)
            {
                Origami.Label(paper, $"{id}_err_{err.GetHashCode()}", $"• {err}").Show();
            }
        }
    }

    private static void DrawBoneGroup(Paper paper, string id, string groupTitle, HumanoidBone[] roles, HumanoidMapping mapping, SkeletonAsset? skel)
    {
        Origami.Header(paper, $"{id}_grp_{groupTitle}", groupTitle).Show();

        foreach (var role in roles)
        {
            string roleLabel = $"{role} {(HumanoidMapping.IsRequired(role) ? "*" : "(opt)")}";
            int mappedIndex = mapping.Get(role);
            string boneDisplay = (skel != null && mappedIndex >= 0 && mappedIndex < skel.BoneCount)
                ? $"{mappedIndex}: {skel[mappedIndex].Name}"
                : "(Unassigned)";

            EditorGUI.Row(paper, $"{id}_role_{role}", roleLabel, () =>
            {
                Origami.Label(paper, $"{id}_txt_{role}", boneDisplay).Show();
            });
        }
    }
}
