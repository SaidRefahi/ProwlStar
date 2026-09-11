// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Prowl.Runtime;

/// <summary>
/// Represents a single animation state within an <see cref="Animator"/>.
/// Can play a single <see cref="AnimationClip"/> or evaluate a <see cref="BlendTree"/>.
/// </summary>
public sealed class AnimatorState
{
    /// <summary>Unique name/identifier of this state.</summary>
    public string Name { get; set; } = "";

    /// <summary>The animation clip played by this state when not using a blend tree.</summary>
    public AssetRef<AnimationClip> Clip;

    /// <summary>Optional blend tree for multi-clip blending.</summary>
    public BlendTree? BlendTree { get; set; }

    /// <summary>Playback speed multiplier specific to this state.</summary>
    public float Speed { get; set; } = 1f;

    /// <summary>Whether this state should loop continuously.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Optional tag for categorization and gameplay queries.</summary>
    public string Tag { get; set; } = "";

    /// <summary>Wrap mode configuration. If not explicitly changed, defaults according to <see cref="Loop"/>.</summary>
    public AnimationWrapMode Wrap { get; set; } = AnimationWrapMode.Loop;

    public void SetClip(AssetRef<AnimationClip> clip)
    {
        Clip = clip;
        BlendTree = null;
    }

    public void SetBlendTree(BlendTree tree)
    {
        BlendTree = tree;
        Clip = default;
    }

    public AnimatorState() { }

    public AnimatorState(string name, AssetRef<AnimationClip> clip, float speed = 1f, bool loop = true)
    {
        Name = name;
        Clip = clip;
        Speed = speed;
        Loop = loop;
        Wrap = loop ? AnimationWrapMode.Loop : AnimationWrapMode.Once;
    }

    public AnimatorState(string name, BlendTree blendTree, float speed = 1f, bool loop = true)
    {
        Name = name;
        BlendTree = blendTree;
        Speed = speed;
        Loop = loop;
        Wrap = loop ? AnimationWrapMode.Loop : AnimationWrapMode.Once;
    }

    /// <summary>
    /// Computes the effective duration of this state from its clip or blend tree children.
    /// </summary>
    public float GetDuration()
    {
        if (BlendTree != null && BlendTree.Children.Count > 0)
        {
            float maxDuration = 0f;
            var children = CollectionsMarshal.AsSpan(BlendTree.Children);
            for (int i = 0; i < children.Length; i++)
            {
                var clip = children[i].Clip.Res;
                if (clip.IsValid() && clip.Duration > maxDuration)
                    maxDuration = clip.Duration;
            }
            return maxDuration;
        }

        var directClip = Clip.Res;
        return directClip.IsValid() ? directClip.Duration : 0f;
    }

    /// <summary>
    /// Validates the state configuration.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("State name is missing or empty.");

        if (BlendTree != null)
        {
            if (!BlendTree.Validate(out List<string> btErrors))
                errors.AddRange(btErrors);
        }
        else if (Clip.Res == null)
        {
            errors.Add($"State '{Name}' has no valid AnimationClip assigned.");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Validates the state configuration.
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        bool valid = Validate(out List<string> errors);
        errorMessage = valid ? null : string.Join("; ", errors);
        return valid;
    }
}
