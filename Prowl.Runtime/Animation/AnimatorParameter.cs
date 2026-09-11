// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

namespace Prowl.Runtime;

/// <summary>
/// Supported data types for <see cref="Animator"/> parameters.
/// </summary>
public enum AnimatorParameterType
{
    Float,
    Int,
    Bool,
    Trigger
}

/// <summary>
/// Declares an animator parameter with its name, type, and default values.
/// </summary>
public sealed class AnimatorParameter
{
    /// <summary>Unique name of the parameter.</summary>
    public string Name { get; set; } = "";

    /// <summary>Type of the parameter.</summary>
    public AnimatorParameterType Type { get; set; } = AnimatorParameterType.Float;

    /// <summary>Default float value.</summary>
    public float DefaultFloat { get; set; }

    /// <summary>Default int value.</summary>
    public int DefaultInt { get; set; }

    /// <summary>Default bool value.</summary>
    public bool DefaultBool { get; set; }

    public AnimatorParameter() { }

    public AnimatorParameter(string name, AnimatorParameterType type)
    {
        Name = name;
        Type = type;
    }
}
