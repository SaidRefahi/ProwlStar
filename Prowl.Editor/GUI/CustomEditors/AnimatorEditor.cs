// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Prowl.Editor.Core;
using Prowl.Editor.GUI;
using Prowl.Editor.GUI.Panels;
using Prowl.Editor.Theming;
using Prowl.OrigamiUI;
using Prowl.PaperUI;
using Prowl.Runtime;

namespace Prowl.Editor.Inspector;

/// <summary>
/// Custom inspector editor for <see cref="Animator"/> providing quick access to the
/// engine's native Animator window, real-time parameter controls, live playback, and validation.
/// </summary>
[CustomEditor(typeof(Animator))]
public class AnimatorEditor : CustomEditor
{
    private string _quickParamName = "NewParam";

    public override void OnGUI(Paper paper, string id, object target)
    {
        var animator = (Animator)target;
        var font = EditorTheme.DefaultFont;
        if (font == null) return;

        Undo.Snapshot(animator);
        animator.EnsureBaseLayer();

        // Header Action: Open the dedicated Animator Window
        Origami.Button(paper, $"{id}_open_window", $"{EditorIcons.Film}  Open in Animator Window", () =>
        {
            EditorApplication.Instance?.OpenPanel(typeof(AnimatorPanel));
        }).Show();

        Origami.Separator(paper, $"{id}_sep1").Show();

        // Native Animation System Status & Rig
        if (animator.Skeleton.Res != null)
        {
            Origami.Label(paper, $"{id}_skel_info", $"Rig: {animator.Skeleton.Res.Name} ({animator.Skeleton.Res.Bones.Count} bones)").Show();
        }
        else
        {
            Origami.Label(paper, $"{id}_skel_missing", "Warning: No SkeletonAsset assigned").TextColor(EditorTheme.Amber400).Show();
        }

        // Live Playback Controls in Inspector
        var baseLayer = animator.EnsureBaseLayer();
        string currentState = baseLayer.CurrentState != null ? baseLayer.CurrentState.Name : baseLayer.DefaultState;
        float duration = baseLayer.CurrentState?.GetDuration() ?? 0f;
        float curTime = baseLayer.CurrentTime;
        float normalized = duration > 1e-4f ? Math.Clamp(curTime / duration, 0f, 1f) : 0f;

        using (paper.Row($"{id}_playback_bar").Height(28).RowBetween(4).Enter())
        {
            if (!baseLayer.IsPlaying)
            {
                Origami.Button(paper, $"{id}_btn_play", $"{EditorIcons.Play} Play", () =>
                {
                    if (baseLayer.CurrentState == null)
                        animator.Play(baseLayer.DefaultState);
                    else
                        animator.Resume();
                }).Show();
            }
            else
            {
                Origami.Button(paper, $"{id}_btn_pause", $"{EditorIcons.Pause} Pause", () => animator.Pause()).Show();
            }

            Origami.Button(paper, $"{id}_btn_stop", $"{EditorIcons.Stop} Stop", () => animator.Stop()).Show();

            Origami.Label(paper, $"{id}_spd_lbl", "Spd:").TextColor(EditorTheme.InkDim).Show();
            Origami.Slider(paper, $"{id}_speed_sl", animator.Speed, v => animator.Speed = v, 0f, 3f).Format("F2").Width(70).Show();
        }

        // State Progress Bar
        string progressText = duration > 0f ? $"{currentState} ({curTime:F2}s / {duration:F2}s)" : currentState;
        if (baseLayer.IsInTransition && baseLayer.TargetState != null)
            progressText = $"{currentState} -> {baseLayer.TargetState.Name} ({baseLayer.TransitionProgress:P0})";

        Origami.ProgressBar(paper, $"{id}_state_prog", normalized).Label(progressText).Show();

        // Quick State Buttons
        if (baseLayer.States.Count > 0)
        {
            Origami.Separator(paper, $"{id}_st_sep").Show();
            Origami.Label(paper, $"{id}_st_hdr", $"States ({baseLayer.States.Count}):").TextColor(EditorTheme.InkDim).Show();

            using (paper.Row($"{id}_states_flow").RowBetween(4).Enter())
            {
                for (int s = 0; s < baseLayer.States.Count; s++)
                {
                    var st = baseLayer.States[s];
                    if (st == null) continue;
                    string btnLabel = st.Name == currentState ? $"▶ {st.Name}" : st.Name;
                    Origami.Button(paper, $"{id}_st_btn_{s}", btnLabel, () => animator.CrossFade(st.Name, 0.2f)).Show();
                }
            }
        }

        // Live Parameters Quick-Controls
        Origami.Separator(paper, $"{id}_sep2").Show();
        Origami.Header(paper, $"{id}_params_hdr", $"Parameters ({animator.Parameters.Count})").Underline().Show();

        for (int i = 0; i < animator.Parameters.Count; i++)
        {
            var p = animator.Parameters[i];
            if (p == null) continue;

            string rowId = $"{id}_param_{i}";
            switch (p.Type)
            {
                case AnimatorParameterType.Float:
                    float curFloat = animator.GetFloat(p.Name);
                    EditorGUI.Row(paper, rowId, p.Name, () =>
                        Origami.Slider(paper, $"{rowId}_v", curFloat, v => animator.SetFloat(p.Name, v), 0f, 10f).Format("F2").Show());
                    break;

                case AnimatorParameterType.Int:
                    int curInt = animator.GetInt(p.Name);
                    EditorGUI.Row(paper, rowId, p.Name, () =>
                        Origami.Slider(paper, $"{rowId}_v", (float)curInt, v => animator.SetInt(p.Name, (int)v), 0f, 100f).Format("F0").Show());
                    break;

                case AnimatorParameterType.Bool:
                    bool curBool = animator.GetBool(p.Name);
                    EditorGUI.Row(paper, rowId, p.Name, () =>
                        Origami.Button(paper, $"{rowId}_b", curBool ? "True" : "False", () => animator.SetBool(p.Name, !curBool)).Show());
                    break;

                case AnimatorParameterType.Trigger:
                    EditorGUI.Row(paper, rowId, p.Name, () =>
                        Origami.Button(paper, $"{rowId}_t", "Set Trigger", () => animator.SetTrigger(p.Name)).Show());
                    break;
            }
        }

        // Quick add parameter in inspector
        using (paper.Row($"{id}_add_p_row").Height(24).RowBetween(4).Enter())
        {
            Origami.TextField(paper, $"{id}_new_p_name", _quickParamName, v => _quickParamName = v).Width(80).Show();
            Origami.Button(paper, $"{id}_add_flt", "+F", () => QuickAddParam(animator, AnimatorParameterType.Float)).Tooltip("Add Float").Show();
            Origami.Button(paper, $"{id}_add_int", "+I", () => QuickAddParam(animator, AnimatorParameterType.Int)).Tooltip("Add Int").Show();
            Origami.Button(paper, $"{id}_add_bol", "+B", () => QuickAddParam(animator, AnimatorParameterType.Bool)).Tooltip("Add Bool").Show();
            Origami.Button(paper, $"{id}_add_trg", "+T", () => QuickAddParam(animator, AnimatorParameterType.Trigger)).Tooltip("Add Trigger").Show();
        }

        Origami.Separator(paper, $"{id}_sep3").Show();

        // Default properties inspector
        DrawDefaultInspector(paper, id, animator);

        // Validation Summary
        Origami.Separator(paper, $"{id}_sep4").Show();
        if (animator.Validate(out List<string> errors))
        {
            Origami.Label(paper, $"{id}_valid_lbl", "Configuration Valid (Zero errors)").TextColor(EditorTheme.Green400).Show();
        }
        else
        {
            Origami.Label(paper, $"{id}_invalid_lbl", $"Validation Warnings ({errors.Count}):").TextColor(EditorTheme.Amber400).Show();
            foreach (var err in errors)
            {
                Origami.Label(paper, $"{id}_err_{err.GetHashCode()}", $" - {err}").TextColor(EditorTheme.Amber300).Show();
            }
        }
    }

    private void QuickAddParam(Animator animator, AnimatorParameterType type)
    {
        string name = string.IsNullOrWhiteSpace(_quickParamName) ? $"Param_{animator.Parameters.Count}" : _quickParamName.Trim();
        if (animator.HasParameter(name))
            name = $"{name}_{animator.Parameters.Count}";

        animator.AddParameter(new AnimatorParameter { Name = name, Type = type });
        _quickParamName = "NewParam";
    }
}
