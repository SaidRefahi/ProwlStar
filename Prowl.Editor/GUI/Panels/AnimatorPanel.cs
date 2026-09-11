// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Prowl.Editor.Core;
using Prowl.Editor.GUI;
using Prowl.Editor.Theming;
using Prowl.OrigamiUI;
using Prowl.PaperUI;
using Prowl.Runtime;
using Prowl.Runtime.Animation;
using Prowl.Runtime.Resources;
using Prowl.Vector;

namespace Prowl.Editor.GUI.Panels;

/// <summary>
/// Dedicated dockable editor panel for the native engine Animation System (<see cref="Animator"/>).
/// Provides live inspection and playback testing for layers, blend trees, parameters,
/// humanoid retargeting, Inverse Kinematics (IK), and Root Motion.
/// </summary>
public class AnimatorPanel : DockPanel
{
    [MenuItem("Window/Animation/Animator", priority: 10)]
    static void Open() => EditorApplication.Instance?.OpenPanel(typeof(AnimatorPanel));

    public override string Title => "Animator";
    public override string Icon => EditorIcons.Film;

    private enum Tab { Parameters, LayersAndStates, BlendTrees, Humanoid, IKAndRootMotion, Diagnostics }
    private Tab _currentTab = Tab.Parameters;

    private string _newParameterName = "NewParam";

    public override void OnGUI(Paper paper, float width, float height)
    {
        var font = EditorTheme.DefaultFont;
        if (font == null) return;

        // Resolve active animator: prefer selected GameObject, then search active scene
        var selectedGO = Selection.GetSelected<GameObject>().FirstOrDefault();
        Animator? animator = null;
        if (selectedGO.IsValid())
            animator = selectedGO.GetComponent<Animator>();

        if (animator.IsNotValid())
        {
            var all = Scene.Current.FindObjectsOfType<Animator>();
            if (all != null && all.Length > 0 && all[0].IsValid())
                animator = all[0];
        }

        if (animator == null)
        {
            DrawEmptyState(paper, width, height);
            return;
        }

        Undo.Snapshot(animator);

        using (paper.Column("animator_main").Size(width, height).Padding(8).RowBetween(6).Enter())
        {
            // Top Toolbar: Character info & playback controls
            DrawTopToolbar(paper, animator);

            // Tab bar
            DrawTabBar(paper);

            Origami.Separator(paper, "anim_sep").Show();

            // Active Tab Content
            Origami.ScrollView(paper, "anim_tab_scroll", width - 16, height - 120).Body(() =>
            {
                switch (_currentTab)
                {
                    case Tab.Parameters:
                        DrawParametersTab(paper, animator);
                        break;
                    case Tab.LayersAndStates:
                        DrawLayersAndStatesTab(paper, animator);
                        break;
                    case Tab.BlendTrees:
                        DrawBlendTreesTab(paper, animator);
                        break;
                    case Tab.Humanoid:
                        DrawHumanoidTab(paper, animator);
                        break;
                    case Tab.IKAndRootMotion:
                        DrawIKAndRootMotionTab(paper, animator);
                        break;
                    case Tab.Diagnostics:
                        DrawDiagnosticsTab(paper, animator);
                        break;
                }
            });
        }
    }

    private void DrawEmptyState(Paper paper, float width, float height)
    {
        using (paper.Column("anim_empty").Size(width, height).Padding(24).RowBetween(12).Enter())
        {
            Origami.Header(paper, "empty_hdr", "Animator — Native Animation Engine").Show();
            Origami.Label(paper, "empty_sub", "No active Animator found in the current selection.")
                .TextColor(EditorTheme.InkDim).Show();
            Origami.Label(paper, "empty_desc",
                "In Prowl, imported characters and skinned models automatically include their native Animator.\n" +
                "Select any animated character or GameObject in the Hierarchy to test its state machine, Blend Trees, parameters, retargeting, and IK.")
                .TextColor(EditorTheme.InkFaint).Show();

            var allAnimators = Scene.Current.FindObjectsOfType<Animator>();
            if (allAnimators != null && allAnimators.Length > 0)
            {
                Origami.Separator(paper, "empty_sep").Show();
                Origami.Label(paper, "empty_list_hdr", $"Available Animators in scene ({allAnimators.Length}):").Show();
                for (int i = 0; i < allAnimators.Length; i++)
                {
                    var a = allAnimators[i];
                    if (a.IsNotValid() || a.GameObject.IsNotValid()) continue;
                    Origami.Button(paper, $"select_anim_{i}", $"Inspect: {a.GameObject.Name} ({a.Layers.Count} Layers)", () =>
                    {
                        Selection.Select(a.GameObject);
                    }).Show();
                }
            }
        }
    }

    private void DrawTopToolbar(Paper paper, Animator animator)
    {
        using (paper.Row("anim_toolbar").Height(32).RowBetween(8).Enter())
        {
            string charName = animator.GameObject.IsValid() ? animator.GameObject.Name : "Character";
            Origami.Label(paper, "char_name_lbl", $"Character: {charName}").Show();

            if (animator.Skeleton.Res != null)
                Origami.Label(paper, "skel_lbl", $"Rig: {animator.Skeleton.Res.Name}").TextColor(EditorTheme.InkDim).Show();

            string state = animator.CurrentState != null ? animator.CurrentState.Name : animator.DefaultState;
            Origami.Label(paper, "state_lbl", $"[State: {state}]").TextColor(EditorTheme.AccentText).Show();

            Origami.Button(paper, "btn_play_default", "Play", () => animator.Play(animator.DefaultState)).Show();
            Origami.Button(paper, "btn_stop", "Stop", () => animator.Stop()).Show();

            EditorGUI.Row(paper, "anim_speed_row", "Speed", () =>
                Origami.Slider(paper, "anim_speed_slider", animator.Speed, v => animator.Speed = v, 0f, 3f).Format("F2").Show());
        }
    }

    private void DrawTabBar(Paper paper)
    {
        using (paper.Row("anim_tabs").Height(28).RowBetween(4).Enter())
        {
            TabButton(paper, Tab.Parameters, "Parameters");
            TabButton(paper, Tab.LayersAndStates, "Layers & States");
            TabButton(paper, Tab.BlendTrees, "Blend Trees");
            TabButton(paper, Tab.Humanoid, "Humanoid & Retargeting");
            TabButton(paper, Tab.IKAndRootMotion, "IK & Root Motion");
            TabButton(paper, Tab.Diagnostics, "Diagnostics");
        }
    }

    private void TabButton(Paper paper, Tab tab, string label)
    {
        bool isActive = _currentTab == tab;
        string btnText = isActive ? $"[ {label} ]" : label;
        Origami.Button(paper, $"tab_{tab}", btnText, () => _currentTab = tab).Show();
    }

    private void DrawParametersTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "params_head", $"Parameters ({animator.Parameters.Count})").Underline().Show();

        for (int i = 0; i < animator.Parameters.Count; i++)
        {
            var p = animator.Parameters[i];
            if (p == null) continue;

            string pid = $"param_row_{i}";
            using (paper.Row(pid).Height(24).RowBetween(8).Enter())
            {
                Origami.Label(paper, $"{pid}_name", $"[{p.Type}] {p.Name}").Width(140).Show();

                switch (p.Type)
                {
                    case AnimatorParameterType.Float:
                        float curFloat = animator.GetFloat(p.Name);
                        Origami.Slider(paper, $"{pid}_f", curFloat, v => animator.SetFloat(p.Name, v), 0f, 10f).Format("F2").Show();
                        break;

                    case AnimatorParameterType.Int:
                        int curInt = animator.GetInt(p.Name);
                        Origami.Slider(paper, $"{pid}_i", (float)curInt, v => animator.SetInt(p.Name, (int)v), 0f, 100f).Format("F0").Show();
                        break;

                    case AnimatorParameterType.Bool:
                        bool curBool = animator.GetBool(p.Name);
                        Origami.Button(paper, $"{pid}_b", curBool ? "True" : "False", () => animator.SetBool(p.Name, !curBool)).Show();
                        break;

                    case AnimatorParameterType.Trigger:
                        Origami.Button(paper, $"{pid}_t", "Fire Trigger", () => animator.SetTrigger(p.Name)).Show();
                        break;
                }
            }
        }

        Origami.Separator(paper, "add_param_sep").Show();
        using (paper.Row("add_param_bar").Height(28).RowBetween(6).Enter())
        {
            Origami.TextField(paper, "new_param_name", _newParameterName, v => _newParameterName = v).Width(140).Show();
            Origami.Button(paper, "add_float_btn", "+ Float", () => animator.Parameters.Add(new AnimatorParameter { Name = _newParameterName, Type = AnimatorParameterType.Float })).Show();
            Origami.Button(paper, "add_int_btn", "+ Int", () => animator.Parameters.Add(new AnimatorParameter { Name = _newParameterName, Type = AnimatorParameterType.Int })).Show();
            Origami.Button(paper, "add_bool_btn", "+ Bool", () => animator.Parameters.Add(new AnimatorParameter { Name = _newParameterName, Type = AnimatorParameterType.Bool })).Show();
            Origami.Button(paper, "add_trig_btn", "+ Trigger", () => animator.Parameters.Add(new AnimatorParameter { Name = _newParameterName, Type = AnimatorParameterType.Trigger })).Show();
        }
    }

    private void DrawLayersAndStatesTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "layers_head", $"Layers ({animator.Layers.Count})").Underline().Show();

        for (int l = 0; l < animator.Layers.Count; l++)
        {
            var layer = animator.Layers[l];
            if (layer == null) continue;

            string lid = $"layer_{l}";
            using (paper.Column(lid).Padding(4).RowBetween(4).Enter())
            {
                Origami.Label(paper, $"{lid}_title", $"Layer {l}: {layer.Name}").Show();
                EditorGUI.Row(paper, $"{lid}_w_row", "Weight", () =>
                    Origami.Slider(paper, $"{lid}_w", layer.Weight, v => animator.SetLayerWeight(l, v), 0f, 1f).Format("F2").Show());

                if (layer.Mask != null)
                    Origami.Label(paper, $"{lid}_mask", $"Avatar Mask: Attached").TextColor(EditorTheme.InkDim).Show();

                Origami.Label(paper, $"{lid}_states_title", $"States ({layer.States.Count}):").Show();
                for (int s = 0; s < layer.States.Count; s++)
                {
                    var state = layer.States[s];
                    if (state == null) continue;

                    string sid = $"{lid}_state_{s}";
                    using (paper.Row(sid).Height(24).RowBetween(8).Enter())
                    {
                        string motionInfo = state.BlendTree != null ? "[BlendTree]" : (state.Clip.Res != null ? $"[Clip: {state.Clip.Res.Name}]" : "[No Motion]");
                        Origami.Label(paper, $"{sid}_lbl", $"{state.Name} {motionInfo}").Width(200).Show();

                        Origami.Button(paper, $"{sid}_play", "Play", () => animator.Play(state.Name, l)).Show();
                        Origami.Button(paper, $"{sid}_fade", "CrossFade (0.2s)", () => animator.CrossFade(state.Name, 0.2f, l)).Show();
                    }
                }
            }
            Origami.Separator(paper, $"{lid}_div").Show();
        }
    }

    private void DrawBlendTreesTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "bt_head", "Blend Trees").Underline().Show();

        bool anyBt = false;
        foreach (var layer in animator.Layers)
        {
            if (layer == null) continue;
            foreach (var state in layer.States)
            {
                if (state?.BlendTree == null) continue;
                anyBt = true;
                var bt = state.BlendTree;

                Origami.Label(paper, $"bt_name_{state.Name}", $"State '{state.Name}' -> BlendTree ({bt.BlendType})").Show();
                Origami.Label(paper, $"bt_param_{state.Name}", $"Parameter X: '{bt.BlendParameter}', Parameter Y: '{bt.BlendParameterY}'").TextColor(EditorTheme.InkDim).Show();
                Origami.Label(paper, $"bt_child_count_{state.Name}", $"Motions: {bt.Children.Count}").Show();

                for (int c = 0; c < bt.Children.Count; c++)
                {
                    var child = bt.Children[c];
                    string clipName = child?.Clip.Res != null ? child.Clip.Res.Name : "None";
                    Origami.Label(paper, $"bt_{state.Name}_c_{c}", $" - Motion {c}: {clipName} (Threshold: {child?.Threshold:F2})").Show();
                }
                Origami.Separator(paper, $"bt_sep_{state.Name}").Show();
            }
        }

        if (!anyBt)
        {
            Origami.Label(paper, "no_bt_lbl", "No Blend Trees found in active Animator states.").TextColor(EditorTheme.InkDim).Show();
        }
    }

    private void DrawHumanoidTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "hum_head", "Humanoid & Retargeting").Underline().Show();

        if (animator.HumanoidMapping.Res != null)
        {
            var mapping = animator.HumanoidMapping.Res;
            Origami.Label(paper, "hum_active", "Humanoid Retargeting: ENABLED").TextColor(EditorTheme.Green400).Show();
            Origami.Label(paper, "hum_map_name", $"Mapping Asset: {mapping.Name}").Show();
            Origami.Label(paper, "hum_source_skel", $"Source Skeleton: {(animator.SourceSkeleton.Res != null ? animator.SourceSkeleton.Res.Name : "Using Target Skeleton")}").Show();
            Origami.Label(paper, "hum_target_skel", $"Target Skeleton: {(animator.Skeleton.Res != null ? animator.Skeleton.Res.Name : "None")}").Show();
        }
        else
        {
            Origami.Label(paper, "hum_inactive", "Humanoid Retargeting: Direct Rig (No HumanoidMapping)").TextColor(EditorTheme.InkDim).Show();
            Origami.Label(paper, "hum_desc", "Direct playback maps AnimationClip bone tracks directly to the SkeletonAsset hierarchy.").Show();
        }
    }

    private void DrawIKAndRootMotionTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "ik_head", "Inverse Kinematics (IK)").Underline().Show();
        Origami.Label(paper, "ik_desc", "Analytic Two-Bone IK for humanoid limbs:").TextColor(EditorTheme.InkDim).Show();

        DrawIKGoalRow(paper, animator, AvatarIKGoal.LeftHand, "Left Hand");
        DrawIKGoalRow(paper, animator, AvatarIKGoal.RightHand, "Right Hand");
        DrawIKGoalRow(paper, animator, AvatarIKGoal.LeftFoot, "Left Foot");
        DrawIKGoalRow(paper, animator, AvatarIKGoal.RightFoot, "Right Foot");

        Origami.Separator(paper, "rm_sep").Show();
        Origami.Header(paper, "rm_head", "Root Motion").Underline().Show();
        Origami.Button(paper, "rm_apply_btn", $"Apply Root Motion: {(animator.ApplyRootMotion ? "ON" : "OFF")}", () =>
            animator.ApplyRootMotion = !animator.ApplyRootMotion).Show();
        Origami.Label(paper, "rm_delta_pos", $"Root Motion Pos Delta: {animator.RootMotionPosition}").Show();
        Origami.Label(paper, "rm_delta_rot", $"Root Motion Rot Delta: {animator.RootMotionRotation}").Show();
    }

    private void DrawIKGoalRow(Paper paper, Animator animator, AvatarIKGoal goal, string label)
    {
        string gid = $"ik_{goal}";
        var ikTarget = animator.GetIKTarget(goal);
        float posWeight = ikTarget.PositionWeight;
        float rotWeight = ikTarget.RotationWeight;

        using (paper.Row(gid).Height(24).RowBetween(8).Enter())
        {
            Origami.Label(paper, $"{gid}_lbl", label).Width(90).Show();
            Origami.Slider(paper, $"{gid}_pw", posWeight, v => animator.SetIKPositionWeight(goal, v), 0f, 1f).Format("F2").Show();
            Origami.Slider(paper, $"{gid}_rw", rotWeight, v => animator.SetIKRotationWeight(goal, v), 0f, 1f).Format("F2").Show();
        }
    }

    private void DrawDiagnosticsTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "diag_head", "System Validation").Underline().Show();

        if (animator.Validate(out List<string> errors))
        {
            Origami.Label(paper, "diag_ok", "STATUS: All Animator checks passed successfully.").TextColor(EditorTheme.Green400).Show();
            Origami.Label(paper, "diag_ok_desc", "Skeleton, Layers, States, Transitions, Parameters, and IK configurations are valid.").TextColor(EditorTheme.InkDim).Show();
        }
        else
        {
            Origami.Label(paper, "diag_warn", $"STATUS: Found {errors.Count} validation warnings:").TextColor(EditorTheme.Amber400).Show();
            for (int i = 0; i < errors.Count; i++)
            {
                Origami.Label(paper, $"diag_err_{i}", $" - {errors[i]}").TextColor(EditorTheme.Amber300).Show();
            }
        }
    }
}
