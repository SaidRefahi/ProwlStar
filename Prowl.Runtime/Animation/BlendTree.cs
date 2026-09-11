// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// Type of blend tree evaluation.
/// </summary>
public enum BlendTreeType
{
    Simple1D,
    Simple2D
}

/// <summary>
/// A child motion node within a <see cref="BlendTree"/>.
/// </summary>
public sealed class BlendTreeChild
{
    /// <summary>The animation clip for this child node.</summary>
    public AssetRef<AnimationClip> Clip;

    /// <summary>Threshold value for 1D blend trees.</summary>
    public float Threshold { get; set; }

    /// <summary>Position in 2D space (X, Y) for 2D blend trees.</summary>
    public Float2 Position { get; set; }

    /// <summary>Playback speed multiplier for this child clip.</summary>
    public float Speed { get; set; } = 1f;

    public BlendTreeChild() { }

    public BlendTreeChild(AssetRef<AnimationClip> clip, float threshold, float speed = 1f)
    {
        Clip = clip;
        Threshold = threshold;
        Speed = speed;
    }

    public BlendTreeChild(AssetRef<AnimationClip> clip, Float2 position, float speed = 1f)
    {
        Clip = clip;
        Position = position;
        Speed = speed;
    }
}

/// <summary>
/// Blend tree allowing multi-clip animation blending driven by parameter values.
/// </summary>
public sealed class BlendTree
{
    /// <summary>Type of blend tree (1D or 2D).</summary>
    public BlendTreeType BlendType { get; set; } = BlendTreeType.Simple1D;

    /// <summary>Name of the parameter driving 1D blending or the X-axis for 2D blending.</summary>
    public string BlendParameter { get; set; } = "";

    /// <summary>Name of the parameter driving the Y-axis for 2D blending.</summary>
    public string BlendParameterY { get; set; } = "";

    /// <summary>Child animation clips in the blend tree.</summary>
    public List<BlendTreeChild> Children { get; set; } = new();

    public BlendTree() { }

    public BlendTree(BlendTreeType blendType, string blendParameter, string blendParameterY = "")
    {
        BlendType = blendType;
        BlendParameter = blendParameter;
        BlendParameterY = blendParameterY;
    }

    public void AddChild(BlendTreeChild child) => Children.Add(child);

    /// <summary>
    /// Validates the blend tree configuration.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(BlendParameter))
            errors.Add("Blend parameter name is missing or empty.");
        if (BlendType == BlendTreeType.Simple2D && string.IsNullOrWhiteSpace(BlendParameterY))
            errors.Add("BlendParameterY is missing or empty for 2D blend tree.");
        if (Children.Count == 0)
            errors.Add("Blend tree has no child motion nodes.");

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child == null)
                errors.Add($"Child at index {i} is null.");
            else if (child.Clip.Res == null)
                errors.Add($"Child at index {i} has no valid AnimationClip assigned.");
        }
        return errors.Count == 0;
    }

    /// <summary>
    /// Validates the blend tree configuration.
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        bool valid = Validate(out List<string> errors);
        errorMessage = valid ? null : string.Join("; ", errors);
        return valid;
    }
}
