// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;

namespace Prowl.Runtime;

/// <summary>
/// Defines a transition between two animation states in an <see cref="Animator"/>.
/// </summary>
public sealed class AnimatorTransition
{
    /// <summary>Name of the source state.</summary>
    public string SourceState { get; set; } = "";

    /// <summary>Name of the destination target state.</summary>
    public string TargetState { get; set; } = "";

    /// <summary>Duration of the transition cross-fade in seconds.</summary>
    public float Duration { get; set; } = 0.25f;

    /// <summary>List of conditions required to trigger this transition automatically.</summary>
    public List<AnimatorCondition> Conditions { get; set; } = new();

    public AnimatorTransition() { }

    public AnimatorTransition(string sourceState, string targetState, float duration = 0.25f)
    {
        SourceState = sourceState;
        TargetState = targetState;
        Duration = duration;
    }

    public AnimatorTransition(string sourceState, string targetState, float duration, params AnimatorCondition[] conditions)
    {
        SourceState = sourceState;
        TargetState = targetState;
        Duration = duration;
        if (conditions != null && conditions.Length > 0)
            Conditions.AddRange(conditions);
    }

    /// <summary>
    /// Validates the transition configuration.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(SourceState))
            errors.Add("Transition SourceState is missing or empty.");
        if (string.IsNullOrWhiteSpace(TargetState))
            errors.Add("Transition TargetState is missing or empty.");
        if (Duration < 0f)
            errors.Add($"Transition duration {Duration} cannot be negative.");

        for (int i = 0; i < Conditions.Count; i++)
        {
            var c = Conditions[i];
            if (c == null)
                errors.Add($"Condition at index {i} is null.");
            else if (string.IsNullOrWhiteSpace(c.Parameter))
                errors.Add($"Condition at index {i} has empty Parameter.");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Validates the transition configuration.
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        bool valid = Validate(out List<string> errors);
        errorMessage = valid ? null : string.Join("; ", errors);
        return valid;
    }
}
