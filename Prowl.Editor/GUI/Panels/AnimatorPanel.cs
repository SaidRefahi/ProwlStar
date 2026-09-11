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
using Prowl.PaperUI.LayoutEngine;
using Prowl.Runtime;
using Prowl.Runtime.Animation;
using Prowl.Runtime.Resources;
using Prowl.Vector;

namespace Prowl.Editor.GUI.Panels;

/// <summary>
/// Dedicated dockable editor panel for the native engine Animation System (<see cref="Animator"/>).
/// Provides live inspection and playback testing for layers, blend trees, parameters,
/// condition-based transitions, humanoid retargeting, Inverse Kinematics (IK), and Root Motion.
/// </summary>
public class AnimatorPanel : DockPanel
{
    [MenuItem("Window/Animation/Animator", priority: 10)]
    static void Open() => EditorApplication.Instance?.OpenPanel(typeof(AnimatorPanel));

    public override string Title => "Animator";
    public override string Icon => EditorIcons.Film;

    private enum SidebarTab { Parameters, Layers, IKAndMotion, Rig }
    private SidebarTab _currentSidebarTab = SidebarTab.Parameters;

    private enum MainView { StateMachine, BlendTree, Diagnostics }
    private MainView _currentMainView = MainView.StateMachine;

    private int _selectedLayerIndex = 0;
    private string _selectedStateName = "";
    private string _newParameterName = "NewParam";
    private string _newStateName = "NewState";
    private string _newLayerName = "New Layer";
    private string _paramSearchText = "";

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

        animator.EnsureBaseLayer();
        if (_selectedLayerIndex >= animator.Layers.Count)
            _selectedLayerIndex = Math.Max(0, animator.Layers.Count - 1);

        Undo.Snapshot(animator);

        using (paper.Column("animator_main").Size(width, height).Padding(6).RowBetween(4).Enter())
        {
            // 1. Top Transport Toolbar
            DrawTopToolbar(paper, animator, width);

            Origami.Separator(paper, "anim_sep_top").Show();

            // 2. Main Two-Column Layout (Sidebar + Center Workspace)
            float sidebarWidth = Math.Min(310f, Math.Max(240f, width * 0.28f));
            float workspaceWidth = Math.Max(300f, width - sidebarWidth - 18f);
            float bodyHeight = Math.Max(200f, height - 52f);

            using (paper.Row("anim_body_row").Size(width - 12f, bodyHeight).RowBetween(8).Enter())
            {
                // Left Sidebar
                using (paper.Column("anim_sidebar").Width(sidebarWidth).Height(bodyHeight).RowBetween(4).Enter())
                {
                    DrawSidebarHeader(paper);
                    Origami.Separator(paper, "sb_sep").Show();

                    Origami.ScrollView(paper, "sb_scroll", sidebarWidth, bodyHeight - 40f).Body(() =>
                    {
                        switch (_currentSidebarTab)
                        {
                            case SidebarTab.Parameters:
                                DrawParametersTab(paper, animator);
                                break;
                            case SidebarTab.Layers:
                                DrawLayersTab(paper, animator);
                                break;
                            case SidebarTab.IKAndMotion:
                                DrawIKAndMotionTab(paper, animator);
                                break;
                            case SidebarTab.Rig:
                                DrawRigTab(paper, animator);
                                break;
                        }
                    });
                }

                // Right Workspace
                using (paper.Column("anim_workspace").Width(workspaceWidth).Height(bodyHeight).RowBetween(4).Enter())
                {
                    DrawWorkspaceHeader(paper, animator);
                    Origami.Separator(paper, "ws_sep").Show();

                    Origami.ScrollView(paper, "ws_scroll", workspaceWidth, bodyHeight - 40f).Body(() =>
                    {
                        switch (_currentMainView)
                        {
                            case MainView.StateMachine:
                                DrawStateMachineView(paper, animator, workspaceWidth);
                                break;
                            case MainView.BlendTree:
                                DrawBlendTreeEditor(paper, animator, workspaceWidth);
                                break;
                            case MainView.Diagnostics:
                                DrawDiagnosticsView(paper, animator);
                                break;
                        }
                    });
                }
            }
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
                "Select any animated character or GameObject in the Hierarchy to inspect its state machine, Blend Trees, parameters, retargeting, and IK.")
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

    // ════════════════════════════════════════════════════════════════════════
    //  TOP TRANSPORT TOOLBAR
    // ════════════════════════════════════════════════════════════════════════

    private void DrawTopToolbar(Paper paper, Animator animator, float width)
    {
        using (paper.Row("anim_toolbar").Height(34).RowBetween(6).Enter())
        {
            string charName = animator.GameObject.IsValid() ? animator.GameObject.Name : "Character";
            Origami.Label(paper, "char_name_lbl", $"[{charName}]").TextColor(EditorTheme.AccentText).Show();

            if (animator.Skeleton.Res != null)
                Origami.Label(paper, "skel_lbl", $"Rig: {animator.Skeleton.Res.Name}").TextColor(EditorTheme.InkDim).Show();
            else
                Origami.Label(paper, "skel_missing_lbl", "No Rig").TextColor(EditorTheme.Amber400).Show();

            // Layer Selector Dropdown
            var layerNames = animator.Layers.Select((l, idx) => $"Layer {idx}: {l.Name}").ToArray();
            if (layerNames.Length > 0)
            {
                Origami.Dropdown(paper, "top_layer_select", _selectedLayerIndex, idx => _selectedLayerIndex = idx, layerNames)
                    .Width(140).Show();
            }

            // Transport Buttons
            var currentLayer = animator.GetLayer(_selectedLayerIndex) ?? animator.EnsureBaseLayer();
            bool isPlaying = currentLayer.IsPlaying;

            if (!isPlaying)
            {
                Origami.Button(paper, "btn_play", $"{EditorIcons.Play} Play", () =>
                {
                    if (currentLayer.CurrentState == null)
                        animator.Play(currentLayer.DefaultState, _selectedLayerIndex);
                    else
                        animator.Resume();
                }).Show();
            }
            else
            {
                Origami.Button(paper, "btn_pause", $"{EditorIcons.Pause} Pause", () => animator.Pause()).Show();
            }

            Origami.Button(paper, "btn_stop", $"{EditorIcons.Stop} Stop", () => animator.Stop()).Show();

            // Global Speed Slider
            Origami.Label(paper, "spd_lbl", "Speed:").TextColor(EditorTheme.InkDim).Show();
            Origami.Slider(paper, "anim_speed_slider", animator.Speed, v => animator.Speed = v, 0f, 3f)
                .Format("F2").Width(90).Show();

            // Active State & Time Progress Bar
            string stateName = currentLayer.CurrentState != null ? currentLayer.CurrentState.Name : (string.IsNullOrEmpty(currentLayer.DefaultState) ? "(None)" : currentLayer.DefaultState);
            float duration = currentLayer.CurrentState?.GetDuration() ?? 0f;
            float curTime = currentLayer.CurrentTime;
            float normalized = duration > 1e-4f ? Math.Clamp(curTime / duration, 0f, 1f) : 0f;

            string progressText = duration > 0f
                ? $"{stateName} ({curTime:F2}s / {duration:F2}s)"
                : stateName;

            if (currentLayer.IsInTransition && currentLayer.TargetState != null)
            {
                progressText = $"{stateName} -> {currentLayer.TargetState.Name} ({currentLayer.TransitionTime / Math.Max(0.01f, currentLayer.TransitionDuration):P0})";
            }

            Origami.ProgressBar(paper, "playback_progress", normalized)
                .Label(progressText)
                .Width(180)
                .Show();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  SIDEBAR: TABS & PARAMETERS & LAYERS & IK
    // ════════════════════════════════════════════════════════════════════════

    private void DrawSidebarHeader(Paper paper)
    {
        using (paper.Row("sb_tabs").Height(26).RowBetween(2).Enter())
        {
            TabButton(paper, "tab_params", "Params", SidebarTab.Parameters);
            TabButton(paper, "tab_layers", "Layers", SidebarTab.Layers);
            TabButton(paper, "tab_ik", "IK", SidebarTab.IKAndMotion);
            TabButton(paper, "tab_rig", "Rig", SidebarTab.Rig);
        }
    }

    private void TabButton(Paper paper, string id, string label, SidebarTab tab)
    {
        bool active = _currentSidebarTab == tab;
        string text = active ? $"[{label}]" : label;
        Origami.Button(paper, id, text, () => _currentSidebarTab = tab).Show();
    }

    private void DrawParametersTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "params_head", $"Parameters ({animator.Parameters.Count})").Underline().Show();

        // Parameter Search Filter
        Origami.SearchField(paper, "param_search", _paramSearchText, v => _paramSearchText = v, "Search parameters...").Show();

        // Add Parameter Bar
        using (paper.Row("add_param_bar").Height(26).RowBetween(4).Enter())
        {
            Origami.TextField(paper, "new_param_name", _newParameterName, v => _newParameterName = v).Width(100).Show();
            Origami.Button(paper, "add_flt", "+F", () => AddParam(animator, AnimatorParameterType.Float)).Tooltip("Add Float Parameter").Show();
            Origami.Button(paper, "add_int", "+I", () => AddParam(animator, AnimatorParameterType.Int)).Tooltip("Add Int Parameter").Show();
            Origami.Button(paper, "add_bol", "+B", () => AddParam(animator, AnimatorParameterType.Bool)).Tooltip("Add Bool Parameter").Show();
            Origami.Button(paper, "add_trg", "+T", () => AddParam(animator, AnimatorParameterType.Trigger)).Tooltip("Add Trigger Parameter").Show();
        }

        Origami.Separator(paper, "params_div").Show();

        // Parameter List
        for (int i = 0; i < animator.Parameters.Count; i++)
        {
            var p = animator.Parameters[i];
            if (p == null) continue;

            if (!string.IsNullOrEmpty(_paramSearchText) && !p.Name.Contains(_paramSearchText, StringComparison.OrdinalIgnoreCase))
                continue;

            string pid = $"param_row_{i}";
            using (paper.Row(pid).Height(26).RowBetween(4).Enter())
            {
                // Type badge
                string typeBadge = p.Type switch
                {
                    AnimatorParameterType.Float => "[F]",
                    AnimatorParameterType.Int => "[I]",
                    AnimatorParameterType.Bool => "[B]",
                    AnimatorParameterType.Trigger => "[T]",
                    _ => "[?]"
                };
                Origami.Label(paper, $"{pid}_badge", typeBadge).TextColor(EditorTheme.AccentText).Width(24).Show();

                // Name
                Origami.Label(paper, $"{pid}_name", p.Name).Width(80).Show();

                // Interactive Value Control
                switch (p.Type)
                {
                    case AnimatorParameterType.Float:
                        float curFloat = animator.GetFloat(p.Name);
                        Origami.Slider(paper, $"{pid}_f", curFloat, v => animator.SetFloat(p.Name, v), 0f, 10f)
                            .Format("F2").Width(110).Show();
                        break;

                    case AnimatorParameterType.Int:
                        int curInt = animator.GetInt(p.Name);
                        Origami.Slider(paper, $"{pid}_i", (float)curInt, v => animator.SetInt(p.Name, (int)v), 0f, 50f)
                            .Format("F0").Width(110).Show();
                        break;

                    case AnimatorParameterType.Bool:
                        bool curBool = animator.GetBool(p.Name);
                        Origami.Button(paper, $"{pid}_b", curBool ? "True" : "False", () => animator.SetBool(p.Name, !curBool))
                            .Width(60).Show();
                        break;

                    case AnimatorParameterType.Trigger:
                        Origami.Button(paper, $"{pid}_t", "Fire", () => animator.SetTrigger(p.Name))
                            .Width(60).Show();
                        break;
                }

                // Delete Button
                Origami.Button(paper, $"{pid}_del", "X", () =>
                {
                    animator.RemoveParameter(p.Name);
                }).Tooltip($"Delete parameter '{p.Name}'").Width(22).Show();
            }
        }
    }

    private void AddParam(AnimatorParameterType type)
    {
        var selectedGO = Selection.GetSelected<GameObject>().FirstOrDefault();
        Animator? anim = null;
        if (selectedGO.IsValid())
            anim = selectedGO.GetComponent<Animator>();
        AddParam(anim, type);
    }

    private void AddParam(Animator? animator, AnimatorParameterType type)
    {
        if (animator == null) return;
        string name = string.IsNullOrWhiteSpace(_newParameterName) ? $"Param_{animator.Parameters.Count}" : _newParameterName.Trim();
        if (animator.HasParameter(name))
            name = $"{name}_{animator.Parameters.Count}";

        animator.AddParameter(new AnimatorParameter { Name = name, Type = type });
        _newParameterName = "NewParam";
    }

    private void DrawLayersTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "layers_head", $"Layers ({animator.Layers.Count})").Underline().Show();

        // Add Layer row
        using (paper.Row("add_layer_bar").Height(26).RowBetween(4).Enter())
        {
            Origami.TextField(paper, "new_layer_name_tf", _newLayerName, v => _newLayerName = v).Width(140).Show();
            Origami.Button(paper, "btn_add_layer", "+ Add Layer", () =>
            {
                string lname = string.IsNullOrWhiteSpace(_newLayerName) ? $"Layer {animator.Layers.Count}" : _newLayerName.Trim();
                animator.AddLayer(new AnimatorLayer(lname, 1f));
                _newLayerName = "New Layer";
            }).Show();
        }

        Origami.Separator(paper, "layers_list_sep").Show();

        for (int i = 0; i < animator.Layers.Count; i++)
        {
            var layer = animator.Layers[i];
            if (layer == null) continue;

            int layerIdx = i;
            bool isSelected = _selectedLayerIndex == i;
            string lid = $"layer_card_{i}";

            using (paper.Column(lid).Padding(4).RowBetween(4).Enter())
            {
                using (paper.Row($"{lid}_top").Height(24).RowBetween(6).Enter())
                {
                    string selectLabel = isSelected ? $"▶ {layer.Name}" : layer.Name;
                    Origami.Button(paper, $"{lid}_sel", selectLabel, () =>
                    {
                        _selectedLayerIndex = layerIdx;
                    }).Width(150).Show();

                    if (i > 0)
                    {
                        Origami.Button(paper, $"{lid}_del", "Delete", () =>
                        {
                            animator.RemoveLayer(layerIdx);
                            if (_selectedLayerIndex >= animator.Layers.Count)
                                _selectedLayerIndex = Math.Max(0, animator.Layers.Count - 1);
                        }).Width(50).Show();
                    }
                }

                // Layer Weight
                if (i > 0)
                {
                    EditorGUI.Row(paper, $"{lid}_w_row", "Weight", () =>
                        Origami.Slider(paper, $"{lid}_w", layer.Weight, v => animator.SetLayerWeight(layerIdx, v), 0f, 1f)
                            .Format("F2").Show());
                }

                // Default State Selector
                var stateNames = layer.States.Select(s => s.Name).ToArray();
                if (stateNames.Length > 0)
                {
                    int curDefIdx = Math.Max(0, Array.IndexOf(stateNames, layer.DefaultState));
                    EditorGUI.Row(paper, $"{lid}_def_row", "Default State", () =>
                        Origami.Dropdown(paper, $"{lid}_def_dd", curDefIdx, idx => layer.DefaultState = stateNames[idx], stateNames).Show());
                }

                // AvatarMask
                using (paper.Row($"{lid}_mask_row").Height(22).RowBetween(4).Enter())
                {
                    string maskDesc = layer.Mask != null ? "Mask: Attached" : "Mask: None";
                    Origami.Label(paper, $"{lid}_mask_lbl", maskDesc).TextColor(EditorTheme.InkDim).Width(100).Show();

                    Origami.Button(paper, $"{lid}_m_up", "Upper", () => layer.Mask = AvatarMask.CreateUpperBodyMask()).Tooltip("Set Upper Body Mask").Show();
                    Origami.Button(paper, $"{lid}_m_low", "Lower", () => layer.Mask = AvatarMask.CreateLowerBodyMask()).Tooltip("Set Lower Body Mask").Show();
                    Origami.Button(paper, $"{lid}_m_clr", "Clear", () => layer.Mask = null).Tooltip("Remove Mask").Show();
                }

                Origami.Separator(paper, $"{lid}_sep").Show();
            }
        }
    }

    private void DrawIKAndMotionTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "rm_head", "Root Motion").Underline().Show();

        Origami.Button(paper, "rm_toggle", $"Apply Root Motion: {(animator.ApplyRootMotion ? "ON" : "OFF")}", () =>
        {
            animator.ApplyRootMotion = !animator.ApplyRootMotion;
        }).Show();

        Origami.Label(paper, "rm_pos", $"Position Delta: {animator.RootMotionPosition}").TextColor(EditorTheme.InkDim).Show();
        Origami.Label(paper, "rm_rot", $"Rotation Delta: {animator.RootMotionRotation}").TextColor(EditorTheme.InkDim).Show();

        Origami.Separator(paper, "ik_div").Show();
        Origami.Header(paper, "ik_head", "Two-Bone Inverse Kinematics").Underline().Show();

        DrawIKRow(paper, animator, AvatarIKGoal.LeftHand, "Left Hand");
        DrawIKRow(paper, animator, AvatarIKGoal.RightHand, "Right Hand");
        DrawIKRow(paper, animator, AvatarIKGoal.LeftFoot, "Left Foot");
        DrawIKRow(paper, animator, AvatarIKGoal.RightFoot, "Right Foot");
    }

    private void DrawIKRow(Paper paper, Animator animator, AvatarIKGoal goal, string label)
    {
        string gid = $"ik_{goal}";
        var ik = animator.GetIKTarget(goal);

        Origami.Label(paper, $"{gid}_hdr", label).TextColor(EditorTheme.AccentText).Show();
        EditorGUI.Row(paper, $"{gid}_pw_r", "Pos Weight", () =>
            Origami.Slider(paper, $"{gid}_pw", ik.PositionWeight, v => animator.SetIKPositionWeight(goal, v), 0f, 1f).Format("F2").Show());
        EditorGUI.Row(paper, $"{gid}_rw_r", "Rot Weight", () =>
            Origami.Slider(paper, $"{gid}_rw", ik.RotationWeight, v => animator.SetIKRotationWeight(goal, v), 0f, 1f).Format("F2").Show());
    }

    private void DrawRigTab(Paper paper, Animator animator)
    {
        Origami.Header(paper, "rig_head", "Skeleton & Retargeting").Underline().Show();

        PropertyGridUtils.DrawField(paper, "rig_skel", "Target Skeleton", typeof(AssetRef<SkeletonAsset>), animator.Skeleton, v =>
        {
            if (v is AssetRef<SkeletonAsset> aref) animator.Skeleton = aref;
        });

        PropertyGridUtils.DrawField(paper, "rig_map", "Humanoid Mapping", typeof(AssetRef<HumanoidMapping>), animator.HumanoidMapping, v =>
        {
            if (v is AssetRef<HumanoidMapping> aref) animator.HumanoidMapping = aref;
        });

        Origami.Separator(paper, "rig_src_sep").Show();
        Origami.Label(paper, "rig_src_info", "Authoring Rig (For Retargeting from different skeletons):").TextColor(EditorTheme.InkDim).Show();

        PropertyGridUtils.DrawField(paper, "rig_src_skel", "Source Skeleton", typeof(AssetRef<SkeletonAsset>), animator.SourceSkeleton, v =>
        {
            if (v is AssetRef<SkeletonAsset> aref) animator.SourceSkeleton = aref;
        });

        PropertyGridUtils.DrawField(paper, "rig_src_map", "Source Mapping", typeof(AssetRef<HumanoidMapping>), animator.SourceHumanoidMapping, v =>
        {
            if (v is AssetRef<HumanoidMapping> aref) animator.SourceHumanoidMapping = aref;
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    //  WORKSPACE: STATE MACHINE GRAPH & BLEND TREE EDITOR & DIAGNOSTICS
    // ════════════════════════════════════════════════════════════════════════

    private void DrawWorkspaceHeader(Paper paper, Animator animator)
    {
        using (paper.Row("ws_mode_bar").Height(26).RowBetween(4).Enter())
        {
            ViewButton(paper, "btn_vm_sm", "State Machine", MainView.StateMachine);
            ViewButton(paper, "btn_vm_bt", "Blend Tree Editor", MainView.BlendTree);
            ViewButton(paper, "btn_vm_diag", "Diagnostics", MainView.Diagnostics);

            var layer = animator.GetLayer(_selectedLayerIndex);
            if (layer != null)
            {
                Origami.Label(paper, "cur_layer_name", $"[Active: {layer.Name} ({layer.States.Count} States)]")
                    .TextColor(EditorTheme.InkDim).Show();
            }
        }
    }

    private void ViewButton(Paper paper, string id, string label, MainView view)
    {
        bool active = _currentMainView == view;
        string text = active ? $"▶ {label}" : label;
        Origami.Button(paper, id, text, () => _currentMainView = view).Show();
    }

    // ── 1. STATE MACHINE GRAPH VIEW ───────────────────────────────────────

    private void DrawStateMachineView(Paper paper, Animator animator, float width)
    {
        var layer = animator.GetLayer(_selectedLayerIndex) ?? animator.EnsureBaseLayer();

        // Canvas Action Bar
        using (paper.Row("sm_actions_bar").Height(28).RowBetween(6).Enter())
        {
            Origami.TextField(paper, "new_st_name_tf", _newStateName, v => _newStateName = v).Width(120).Show();

            Origami.Button(paper, "btn_add_st", "+ New State", () =>
            {
                string sName = string.IsNullOrWhiteSpace(_newStateName) ? $"State_{layer.States.Count}" : _newStateName.Trim();
                layer.AddState(new AnimatorState { Name = sName });
                _selectedStateName = sName;
                _newStateName = "NewState";
            }).Show();

            Origami.Button(paper, "btn_add_bt_st", "+ BlendTree State", () =>
            {
                string sName = string.IsNullOrWhiteSpace(_newStateName) ? $"Blend_{layer.States.Count}" : _newStateName.Trim();
                var tree = new BlendTree(BlendTreeType.Simple1D, "Speed");
                layer.AddState(new AnimatorState(sName, tree));
                _selectedStateName = sName;
                _currentMainView = MainView.BlendTree;
                _newStateName = "NewState";
            }).Show();
        }

        Origami.Separator(paper, "sm_cards_sep").Show();

        // Visual State Cards Grid
        Origami.Header(paper, "sm_nodes_hdr", $"States in '{layer.Name}'").Underline().Show();

        using (paper.Row("sm_nodes_flow").RowBetween(8).Enter())
        {
            // Entry Node badge
            using (paper.Column("node_entry").Width(110).Padding(6).RowBetween(2).Enter())
            {
                Origami.Label(paper, "ne_title", "▶ ENTRY").TextColor(EditorTheme.Green400).Show();
                Origami.Label(paper, "ne_sub", $"-> {layer.DefaultState}").TextColor(EditorTheme.InkDim).Show();
            }

            // Any State Node badge
            using (paper.Column("node_anystate").Width(110).Padding(6).RowBetween(2).Enter())
            {
                Origami.Label(paper, "nas_title", "★ ANY STATE").TextColor(EditorTheme.Blue400).Show();
                int anyTransCount = layer.Transitions.Count(t => string.IsNullOrEmpty(t.SourceState) || t.SourceState == "Any State");
                Origami.Label(paper, "nas_sub", $"{anyTransCount} Transitions").TextColor(EditorTheme.InkDim).Show();
            }
        }

        // List of State Cards
        for (int s = 0; s < layer.States.Count; s++)
        {
            var state = layer.States[s];
            if (state == null) continue;

            string sid = $"state_card_{s}";
            bool isSelected = state.Name == _selectedStateName;
            bool isDefault = state.Name == layer.DefaultState;
            bool isCurrentPlaying = layer.CurrentState == state;

            float duration = state.GetDuration();
            float curTime = isCurrentPlaying ? layer.CurrentTime : 0f;
            float progress = duration > 1e-4f ? Math.Clamp(curTime / duration, 0f, 1f) : 0f;

            using (paper.Column(sid).Padding(6).RowBetween(3).Enter())
            {
                using (paper.Row($"{sid}_row").Height(24).RowBetween(6).Enter())
                {
                    // Select Button / Title
                    string prefix = isSelected ? "▶ " : "";
                    string defaultBadge = isDefault ? " (Default)" : "";
                    string titleText = $"{prefix}{state.Name}{defaultBadge}";

                    Origami.Button(paper, $"{sid}_sel", titleText, () =>
                    {
                        _selectedStateName = state.Name;
                    }).Width(160).Show();

                    // Motion Type Info
                    string motionDesc = state.BlendTree != null
                        ? $"[BlendTree: {state.BlendTree.BlendType}]"
                        : (state.Clip.Res != null ? $"[Clip: {state.Clip.Res.Name}]" : "[No Motion]");
                    Origami.Label(paper, $"{sid}_mdesc", motionDesc).TextColor(EditorTheme.InkDim).Width(180).Show();

                    // Stats: Speed, Loop, Duration
                    string statsDesc = $"{duration:F2}s | {state.Speed:F1}x | {(state.Loop ? "Loop" : "Once")}";
                    Origami.Label(paper, $"{sid}_stats", statsDesc).TextColor(EditorTheme.InkFaint).Width(120).Show();

                    // Quick Actions
                    Origami.Button(paper, $"{sid}_play", "Play", () => animator.Play(state.Name, _selectedLayerIndex)).Show();
                    Origami.Button(paper, $"{sid}_fade", "CrossFade", () => animator.CrossFade(state.Name, 0.25f, _selectedLayerIndex)).Show();

                    if (!isDefault)
                    {
                        Origami.Button(paper, $"{sid}_mkdef", "Set Default", () => layer.DefaultState = state.Name).Show();
                    }

                    Origami.Button(paper, $"{sid}_del", "Delete", () =>
                    {
                        layer.RemoveState(state.Name);
                        if (_selectedStateName == state.Name) _selectedStateName = "";
                    }).Show();
                }

                // Live Progress Bar for active playing state
                if (isCurrentPlaying)
                {
                    Origami.ProgressBar(paper, $"{sid}_prog", progress).Show();
                }
            }

            Origami.Separator(paper, $"{sid}_sep").Show();
        }

        // ── Selected State Inspector & Transitions ─────────────────────────
        var selectedState = layer.GetState(_selectedStateName);
        if (selectedState != null)
        {
            DrawSelectedStateInspector(paper, animator, layer, selectedState);
        }
    }

    private void DrawSelectedStateInspector(Paper paper, Animator animator, AnimatorLayer layer, AnimatorState state)
    {
        Origami.Header(paper, "sel_st_hdr", $"State Details: '{state.Name}'").Underline().Show();

        using (paper.Column("sel_st_body").Padding(6).RowBetween(4).Enter())
        {
            // Name
            EditorGUI.Row(paper, "st_name_r", "State Name", () =>
                Origami.TextField(paper, "st_name_tf", state.Name, v =>
                {
                    if (!string.IsNullOrWhiteSpace(v) && v != state.Name)
                    {
                        string old = state.Name;
                        state.Name = v.Trim();
                        // Update transitions referencing this state
                        for (int t = 0; t < layer.Transitions.Count; t++)
                        {
                            if (layer.Transitions[t].SourceState == old) layer.Transitions[t].SourceState = state.Name;
                            if (layer.Transitions[t].TargetState == old) layer.Transitions[t].TargetState = state.Name;
                        }
                        if (layer.DefaultState == old) layer.DefaultState = state.Name;
                        _selectedStateName = state.Name;
                    }
                }).Show());

            // Speed
            EditorGUI.Row(paper, "st_spd_r", "Speed", () =>
                Origami.Slider(paper, "st_spd_s", state.Speed, v => state.Speed = v, 0.1f, 3f).Format("F2").Show());

            // Loop & Wrap
            EditorGUI.Row(paper, "st_loop_r", "Loop", () =>
                Origami.Checkbox(paper, "st_loop_cb", state.Loop, v => state.Loop = v).Show());

            EditorGUI.Row(paper, "st_wrap_r", "Wrap Mode", () =>
                Origami.EnumDropdown(paper, "st_wrap_dd", state.Wrap, v => state.Wrap = v).Show());

            // Motion Clip / BlendTree
            if (state.BlendTree == null)
            {
                PropertyGridUtils.DrawField(paper, "st_clip_fg", "Animation Clip", typeof(AssetRef<AnimationClip>), state.Clip, v =>
                {
                    if (v is AssetRef<AnimationClip> aref) state.Clip = aref;
                });

                Origami.Button(paper, "st_conv_bt", "Convert to Blend Tree", () =>
                {
                    state.SetBlendTree(new BlendTree(BlendTreeType.Simple1D, "Speed"));
                    _currentMainView = MainView.BlendTree;
                }).Show();
            }
            else
            {
                Origami.Label(paper, "st_bt_info", $"Uses BlendTree ({state.BlendTree.BlendType}) with {state.BlendTree.Children.Count} motions.").Show();
                Origami.Button(paper, "st_edit_bt", "Open in Blend Tree Editor ▶", () => _currentMainView = MainView.BlendTree).Show();
                Origami.Button(paper, "st_conv_clip", "Convert to Single Clip", () => state.SetClip(default)).Show();
            }

            // ── Outgoing Transitions List ──────────────────────────────────
            Origami.Separator(paper, "st_trans_sep").Show();
            Origami.Header(paper, "st_trans_hdr", "Transitions").Underline().Show();

            // Add Transition Row
            var otherStates = layer.States.Where(s => s.Name != state.Name).Select(s => s.Name).ToArray();
            if (otherStates.Length > 0)
            {
                using (paper.Row("add_trans_bar").Height(24).RowBetween(6).Enter())
                {
                    Origami.Label(paper, "add_trans_lbl", "Add Transition to:").Show();
                    for (int o = 0; o < otherStates.Length; o++)
                    {
                        string targetName = otherStates[o];
                        Origami.Button(paper, $"btn_trans_to_{o}", targetName, () =>
                        {
                            layer.AddTransition(new AnimatorTransition(state.Name, targetName, 0.25f));
                        }).Show();
                    }
                }
            }

            // Show outgoing transitions from this state (and Any State)
            for (int t = 0; t < layer.Transitions.Count; t++)
            {
                var trans = layer.Transitions[t];
                if (trans == null) continue;
                if (trans.SourceState != state.Name && trans.SourceState != "Any State") continue;

                int transIdx = t;
                string tid = $"trans_item_{t}";

                using (paper.Column(tid).Padding(4).RowBetween(3).Enter())
                {
                    using (paper.Row($"{tid}_h").Height(22).RowBetween(6).Enter())
                    {
                        string fromText = string.IsNullOrEmpty(trans.SourceState) ? "Any State" : trans.SourceState;
                        Origami.Label(paper, $"{tid}_flow", $"{fromText} -> {trans.TargetState}").TextColor(EditorTheme.AccentText).Width(160).Show();

                        Origami.Button(paper, $"{tid}_del", "Remove", () =>
                        {
                            layer.RemoveTransition(trans);
                        }).Show();
                    }

                    // Duration
                    EditorGUI.Row(paper, $"{tid}_dur_r", "Duration (s)", () =>
                        Origami.Slider(paper, $"{tid}_dur", trans.Duration, v => trans.Duration = v, 0f, 1f).Format("F2").Show());

                    // HasExitTime & ExitTime
                    EditorGUI.Row(paper, $"{tid}_exit_r", "Has Exit Time", () =>
                        Origami.Checkbox(paper, $"{tid}_exit_cb", trans.HasExitTime, v => trans.HasExitTime = v).Show());

                    if (trans.HasExitTime)
                    {
                        EditorGUI.Row(paper, $"{tid}_exit_val_r", "Exit Time (norm)", () =>
                            Origami.Slider(paper, $"{tid}_exit_val", trans.ExitTime, v => trans.ExitTime = v, 0f, 2f).Format("F2").Show());
                    }

                    // Conditions
                    Origami.Label(paper, $"{tid}_cond_lbl", $"Conditions ({trans.Conditions.Count}):").TextColor(EditorTheme.InkDim).Show();

                    for (int c = 0; c < trans.Conditions.Count; c++)
                    {
                        var cond = trans.Conditions[c];
                        int condIdx = c;
                        string cid = $"{tid}_cond_{c}";

                        using (paper.Row(cid).Height(24).RowBetween(4).Enter())
                        {
                            // Parameter Selector
                            var paramNames = animator.Parameters.Select(p => p.Name).ToArray();
                            if (paramNames.Length > 0)
                            {
                                int curPIdx = Math.Max(0, Array.IndexOf(paramNames, cond.Parameter));
                                Origami.Dropdown(paper, $"{cid}_param", curPIdx, idx => cond.Parameter = paramNames[idx], paramNames).Width(90).Show();
                            }
                            else
                            {
                                Origami.TextField(paper, $"{cid}_param_tf", cond.Parameter, v => cond.Parameter = v).Width(90).Show();
                            }

                            // Mode Selector
                            Origami.EnumDropdown(paper, $"{cid}_mode", cond.Mode, v => cond.Mode = v).Width(85).Show();

                            // Threshold
                            if (cond.Mode != AnimatorConditionMode.If && cond.Mode != AnimatorConditionMode.IfNot)
                            {
                                Origami.NumericField<float>(paper, $"{cid}_thresh", cond.Threshold, v => cond.Threshold = v).Width(60).Show();
                            }

                            // Delete Condition
                            Origami.Button(paper, $"{cid}_del", "X", () => trans.Conditions.RemoveAt(condIdx)).Width(22).Show();
                        }
                    }

                    // Add Condition Button
                    if (animator.Parameters.Count > 0)
                    {
                        Origami.Button(paper, $"{tid}_add_cond", "+ Add Condition", () =>
                        {
                            string firstParam = animator.Parameters[0].Name;
                            trans.Conditions.Add(new AnimatorCondition(firstParam, AnimatorConditionMode.If));
                        }).Show();
                    }

                    Origami.Separator(paper, $"{tid}_sep").Show();
                }
            }
        }
    }

    // ── 2. BLEND TREE EDITOR ──────────────────────────────────────────────

    private void DrawBlendTreeEditor(Paper paper, Animator animator, float width)
    {
        var layer = animator.GetLayer(_selectedLayerIndex) ?? animator.EnsureBaseLayer();
        var state = layer.GetState(_selectedStateName);

        if (state == null || state.BlendTree == null)
        {
            using (paper.Column("no_bt_box").Padding(12).RowBetween(8).Enter())
            {
                Origami.Label(paper, "no_bt_title", "No Blend Tree currently selected.").TextColor(EditorTheme.InkDim).Show();
                Origami.Label(paper, "no_bt_hint", "Select any state with a BlendTree from the State Machine view, or create a new one:").Show();

                Origami.Button(paper, "btn_create_bt", "+ Create New BlendTree State", () =>
                {
                    string sName = $"Blend_{layer.States.Count}";
                    var tree = new BlendTree(BlendTreeType.Simple1D, "Speed");
                    layer.AddState(new AnimatorState(sName, tree));
                    _selectedStateName = sName;
                }).Show();

                Origami.Button(paper, "btn_back_sm", "◀ Back to State Machine", () => _currentMainView = MainView.StateMachine).Show();
            }
            return;
        }

        var bt = state.BlendTree;

        using (paper.Row("bt_top_bar").Height(28).RowBetween(8).Enter())
        {
            Origami.Button(paper, "bt_back", "◀ Back to State Machine", () => _currentMainView = MainView.StateMachine).Show();
            Origami.Label(paper, "bt_state_title", $"Editing State: '{state.Name}'").TextColor(EditorTheme.AccentText).Show();
        }

        Origami.Separator(paper, "bt_sep1").Show();

        // BlendTree Type & Parameter Setup
        EditorGUI.Row(paper, "bt_type_r", "Blend Type", () =>
            Origami.EnumDropdown(paper, "bt_type_dd", bt.BlendType, v => bt.BlendType = v).Show());

        var floatParams = animator.Parameters.Where(p => p.Type == AnimatorParameterType.Float).Select(p => p.Name).ToArray();

        EditorGUI.Row(paper, "bt_param_r", "Parameter X", () =>
        {
            if (floatParams.Length > 0)
            {
                int curPIdx = Math.Max(0, Array.IndexOf(floatParams, bt.BlendParameter));
                Origami.Dropdown(paper, "bt_param_dd", curPIdx, idx => bt.BlendParameter = floatParams[idx], floatParams).Show();
            }
            else
            {
                Origami.TextField(paper, "bt_param_tf", bt.BlendParameter, v => bt.BlendParameter = v).Show();
            }
        });

        if (bt.BlendType == BlendTreeType.Simple2D)
        {
            EditorGUI.Row(paper, "bt_param_y_r", "Parameter Y", () =>
            {
                if (floatParams.Length > 0)
                {
                    int curPIdx = Math.Max(0, Array.IndexOf(floatParams, bt.BlendParameterY));
                    Origami.Dropdown(paper, "bt_param_y_dd", curPIdx, idx => bt.BlendParameterY = floatParams[idx], floatParams).Show();
                }
                else
                {
                    Origami.TextField(paper, "bt_param_y_tf", bt.BlendParameterY, v => bt.BlendParameterY = v).Show();
                }
            });
        }

        Origami.Separator(paper, "bt_sep2").Show();
        Origami.Header(paper, "bt_motions_hdr", $"Motions ({bt.Children.Count})").Underline().Show();

        // Sample current weights for live visualization
        float curX = animator.GetFloat(bt.BlendParameter);
        float curY = animator.GetFloat(bt.BlendParameterY);

        for (int c = 0; c < bt.Children.Count; c++)
        {
            var child = bt.Children[c];
            int childIdx = c;
            string cid = $"bt_child_{c}";

            using (paper.Column(cid).Padding(4).RowBetween(3).Enter())
            {
                using (paper.Row($"{cid}_r").Height(24).RowBetween(6).Enter())
                {
                    Origami.Label(paper, $"{cid}_idx", $"#{c}").Width(24).Show();

                    // Clip selector
                    PropertyGridUtils.DrawField(paper, $"{cid}_clip", "", typeof(AssetRef<AnimationClip>), child.Clip, v =>
                    {
                        if (v is AssetRef<AnimationClip> aref) child.Clip = aref;
                    });

                    // 1D Threshold or 2D Position
                    if (bt.BlendType == BlendTreeType.Simple1D)
                    {
                        EditorGUI.Row(paper, $"{cid}_th_r", "Threshold", () =>
                            Origami.NumericField<float>(paper, $"{cid}_th", child.Threshold, v => child.Threshold = v).Width(70).Show());
                    }
                    else
                    {
                        EditorGUI.Row(paper, $"{cid}_pos_r", "Pos (X,Y)", () =>
                            Origami.Float2Field(paper, $"{cid}_pos", child.Position, v => child.Position = v).Show());
                    }

                    // Speed
                    EditorGUI.Row(paper, $"{cid}_spd_r", "Speed", () =>
                        Origami.NumericField<float>(paper, $"{cid}_spd", child.Speed, v => child.Speed = v).Width(50).Show());

                    // Delete
                    Origami.Button(paper, $"{cid}_del", "X", () => bt.Children.RemoveAt(childIdx)).Width(24).Show();
                }
            }

            Origami.Separator(paper, $"{cid}_sep").Show();
        }

        Origami.Button(paper, "bt_add_motion", "+ Add Motion Child", () =>
        {
            float nextThresh = bt.Children.Count > 0 ? bt.Children[^1].Threshold + 1f : 0f;
            bt.AddChild(new BlendTreeChild(default, nextThresh));
        }).Show();
    }

    // ── 3. DIAGNOSTICS VIEW ───────────────────────────────────────────────

    private void DrawDiagnosticsView(Paper paper, Animator animator)
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
