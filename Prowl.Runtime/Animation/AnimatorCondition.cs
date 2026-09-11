// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

namespace Prowl.Runtime;

/// <summary>
/// Comparison operator for an <see cref="AnimatorCondition"/>.
/// </summary>
public enum AnimatorConditionMode
{
    If,        // Bool is true, Trigger is active
    IfNot,     // Bool is false
    Greater,   // Float/Int > Threshold
    Less,      // Float/Int < Threshold
    Equal,     // Float/Int == Threshold (with float epsilon)
    NotEqual   // Float/Int != Threshold
}

/// <summary>
/// A condition on an <see cref="AnimatorTransition"/> evaluated against an <see cref="Animator"/> parameter.
/// </summary>
public sealed class AnimatorCondition
{
    /// <summary>Name of the parameter to evaluate.</summary>
    public string Parameter { get; set; } = "";

    /// <summary>Comparison mode to apply.</summary>
    public AnimatorConditionMode Mode { get; set; } = AnimatorConditionMode.If;

    /// <summary>Comparison threshold value used by numeric and equality comparisons.</summary>
    public float Threshold { get; set; }

    public AnimatorCondition() { }

    public AnimatorCondition(string parameter, AnimatorConditionMode mode, float threshold = 0f)
    {
        Parameter = parameter;
        Mode = mode;
        Threshold = threshold;
    }
}
