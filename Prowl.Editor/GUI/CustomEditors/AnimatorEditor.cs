// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

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
/// engine's native Animator window, real-time parameter controls, and live validation.
/// </summary>
[CustomEditor(typeof(Animator))]
public class AnimatorEditor : CustomEditor
{
    public override void OnGUI(Paper paper, string id, object target)
    {
        var animator = (Animator)target;
        var font = EditorTheme.DefaultFont;
        if (font == null) return;

        Undo.Snapshot(animator);

        // Header Action: Open the dedicated Animator Window
        Origami.Button(paper, $"{id}_open_window", $"{EditorIcons.Film}  Open in Animator Window", () =>
        {
            EditorApplication.Instance?.OpenPanel(typeof(AnimatorPanel));
        }).Show();

        Origami.Separator(paper, $"{id}_sep1").Show();

        // Native Animation System Status
        if (animator.Skeleton.Res != null)
        {
            Origami.Label(paper, $"{id}_skel_info", $"Skeleton: {animator.Skeleton.Res.Name} ({animator.Skeleton.Res.Bones.Count} bones)").Show();
        }
        else
        {
            Origami.Label(paper, $"{id}_skel_missing", "Warning: No SkeletonAsset assigned to this Animator").TextColor(EditorTheme.Amber400).Show();
        }

        if (animator.Layers.Count > 0)
        {
            string currentState = animator.CurrentState != null ? animator.CurrentState.Name : animator.DefaultState;
            Origami.Label(paper, $"{id}_state_info", $"Active State: {currentState}").Show();
        }

        // Live Parameters Quick-Controls
        if (animator.Parameters.Count > 0)
        {
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
        }

        Origami.Separator(paper, $"{id}_sep2").Show();

        // Default properties inspector
        DrawDefaultInspector(paper, id, animator);

        // Validation Summary
        Origami.Separator(paper, $"{id}_sep3").Show();
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
}
