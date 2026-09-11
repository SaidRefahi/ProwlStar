// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Prowl.Runtime;

/// <summary>
/// Represents an animation layer within an <see cref="Animator"/> with its own states,
/// transitions, weight, and optional <see cref="AvatarMask"/>.
/// </summary>
public sealed class AnimatorLayer
{
    /// <summary>Name of this layer.</summary>
    public string Name { get; set; } = "Base Layer";

    /// <summary>Blend weight of this layer (0..1).</summary>
    public float Weight { get; set; } = 1f;

    /// <summary>Optional mask restricting which bones are affected by this layer.</summary>
    public AvatarMask? Mask { get; set; }

    /// <summary>Default state to play on start/enable for this layer.</summary>
    public string DefaultState { get; set; } = "";

    /// <summary>All states defined on this layer.</summary>
    public List<AnimatorState> States { get; set; } = new();

    /// <summary>All transitions defined on this layer.</summary>
    public List<AnimatorTransition> Transitions { get; set; } = new();

    // ── Runtime State Tracked Per Layer ────────────────────────────────────
    public AnimatorState? CurrentState { get; internal set; }
    public AnimatorState? TargetState { get; internal set; }
    public bool IsInTransition { get; internal set; }
    public bool IsPlaying { get; internal set; } = true;
    public float CurrentTime { get; internal set; }
    public float TargetTime { get; internal set; }
    public float TransitionTime { get; internal set; }
    public float TransitionDuration { get; internal set; }

    public float TransitionProgress
    {
        get
        {
            if (!IsInTransition || TransitionDuration <= 0f) return 1f;
            return Math.Clamp(TransitionTime / TransitionDuration, 0f, 1f);
        }
    }

    public AnimatorLayer() { }

    public AnimatorLayer(string name, float weight = 1f, AvatarMask? mask = null)
    {
        Name = name;
        Weight = weight;
        Mask = mask;
    }

    public AnimatorState? GetState(string name)
    {
        var span = CollectionsMarshal.AsSpan(States);
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Name == name)
                return span[i];
        }
        return null;
    }

    public void AddState(AnimatorState state) => States.Add(state);

    public bool RemoveState(string name)
    {
        for (int i = 0; i < States.Count; i++)
        {
            if (States[i].Name == name)
            {
                var st = States[i];
                States.RemoveAt(i);
                Transitions.RemoveAll(t => t.SourceState == name || t.TargetState == name);
                if (CurrentState == st) CurrentState = null;
                if (TargetState == st) TargetState = null;
                if (DefaultState == name) DefaultState = States.Count > 0 ? States[0].Name : "";
                return true;
            }
        }
        return false;
    }

    public void AddTransition(AnimatorTransition transition) => Transitions.Add(transition);

    public bool RemoveTransition(AnimatorTransition transition) => Transitions.Remove(transition);

    public List<AnimatorTransition> GetTransitionsFrom(string stateName)
    {
        var list = new List<AnimatorTransition>();
        for (int i = 0; i < Transitions.Count; i++)
        {
            if (Transitions[i].SourceState == stateName)
                list.Add(Transitions[i]);
        }
        return list;
    }

    public AnimatorState? ResolveDefaultState()
    {
        if (!string.IsNullOrEmpty(DefaultState))
        {
            var st = GetState(DefaultState);
            if (st != null) return st;
        }

        if (States.Count > 0)
            return States[0];

        return null;
    }

    public void Play(string stateName, float transitionDuration = -1f)
    {
        var targetState = GetState(stateName);
        if (targetState == null) return;

        float duration = transitionDuration;
        if (duration < 0f)
        {
            duration = FindTransitionDuration(CurrentState?.Name, stateName);
        }

        var currentClip = GetPrimaryClip(CurrentState);
        var targetClip = GetPrimaryClip(targetState);

        if (duration <= 0f || CurrentState == null || (currentClip == null && CurrentState.BlendTree == null) || (targetClip == null && targetState.BlendTree == null) || !IsPlaying)
        {
            PlayStateImmediate(targetState);
        }
        else
        {
            TargetState = targetState;
            TargetTime = 0f;
            TransitionTime = 0f;
            TransitionDuration = duration;
            IsInTransition = true;
            IsPlaying = true;
        }
    }

    public void PlayStateImmediate(AnimatorState state)
    {
        CurrentState = state;
        CurrentTime = 0f;
        TargetState = null;
        TargetTime = 0f;
        IsInTransition = false;
        TransitionTime = 0f;
        TransitionDuration = 0f;
        IsPlaying = true;
    }

    private float FindTransitionDuration(string? fromState, string toState)
    {
        if (string.IsNullOrEmpty(fromState)) return 0f;

        var transitions = CollectionsMarshal.AsSpan(Transitions);
        for (int i = 0; i < transitions.Length; i++)
        {
            if (transitions[i].SourceState == fromState && transitions[i].TargetState == toState)
                return transitions[i].Duration;
        }
        return 0f;
    }

    private static AnimationClip? GetPrimaryClip(AnimatorState? state)
    {
        if (state == null) return null;
        if (state.BlendTree != null && state.BlendTree.Children.Count > 0)
        {
            for (int i = 0; i < state.BlendTree.Children.Count; i++)
            {
                var clip = state.BlendTree.Children[i].Clip.Res;
                if (clip != null) return clip;
            }
        }
        return state.Clip.Res;
    }

    /// <summary>
    /// Validates the layer configuration, checking states, transitions, and mask.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Layer name is missing or empty.");
        if (Weight < 0f || Weight > 1f)
            errors.Add($"Layer weight {Weight} is outside the valid range [0, 1].");

        var stateNames = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < States.Count; i++)
        {
            var st = States[i];
            if (st == null)
            {
                errors.Add($"State at index {i} is null.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(st.Name))
                errors.Add($"State at index {i} has an empty name.");
            else if (!stateNames.Add(st.Name))
                errors.Add($"Duplicate state name '{st.Name}' on layer '{Name}'.");

            if (st.BlendTree != null)
            {
                if (!st.BlendTree.Validate(out List<string> btErrors))
                {
                    foreach (var err in btErrors)
                        errors.Add($"State '{st.Name}' BlendTree: {err}");
                }
            }
            else if (st.Clip.Res == null)
            {
                errors.Add($"State '{st.Name}' has no valid AnimationClip or BlendTree assigned.");
            }
        }

        for (int i = 0; i < Transitions.Count; i++)
        {
            var tr = Transitions[i];
            if (tr == null)
            {
                errors.Add($"Transition at index {i} is null.");
                continue;
            }
            bool isAnyState = string.IsNullOrWhiteSpace(tr.SourceState) || tr.SourceState == "Any State";
            if (!isAnyState && !stateNames.Contains(tr.SourceState))
                errors.Add($"Transition SourceState '{tr.SourceState}' does not exist on layer '{Name}'.");

            if (string.IsNullOrWhiteSpace(tr.TargetState))
                errors.Add($"Transition at index {i} has empty TargetState.");
            else if (!stateNames.Contains(tr.TargetState))
                errors.Add($"Transition TargetState '{tr.TargetState}' does not exist on layer '{Name}'.");

            if (tr.Duration < 0f)
                errors.Add($"Transition from '{tr.SourceState}' to '{tr.TargetState}' has negative duration {tr.Duration}.");
        }

        if (Mask != null && !Mask.Validate(out List<string> maskErrors))
        {
            foreach (var err in maskErrors)
                errors.Add($"Mask on layer '{Name}': {err}");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Validates the layer configuration.
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        bool valid = Validate(out List<string> errors);
        errorMessage = valid ? null : string.Join("; ", errors);
        return valid;
    }
}
