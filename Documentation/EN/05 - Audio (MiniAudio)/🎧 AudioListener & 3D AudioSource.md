---
title: AudioListener & 3D AudioSource
tags: [audiosource, audiolistener, 3d-audio, spatialization, doppler, miniaudio]
category: audio
updated: 2026-09-10
---

# 🎧 AudioListener & 3D AudioSource in Prowl Engine

To construct an immersive soundscape across a 3D environment, the engine must evaluate distance attenuation, stereo pan panning based on orientation, and acoustic frequency shifts caused by relative velocity (**Doppler Effect**).

Prowl Engine achieves this via two complementary components:
1. **[`AudioListener`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Audio/AudioListener.cs):** Represents the player's virtual ears in 3D world space.
2. **[`AudioSource`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Audio/AudioSource.cs):** Represents an acoustic emitter located at a specific 3D spatial coordinate or operating in 2D stereo mode.

---

## 👂 1. AudioListener (The Receiver)

The `AudioListener` component belongs on the player character's head entity or attached directly to the active [`Camera`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Camera.cs).

### Behavior:
- Every frame, it synchronizes its world position (`Transform.Position`) and directional vectors (`Transform.Forward` and `Transform.Up`) directly into the MiniAudio backend.
- Exactly **one active AudioListener** should exist per scene. If multiple exist, the first encountered listener is selected.

---

## 📢 2. AudioSource (The Emitter)

`AudioSource` plays back assigned audio assets ([`AudioClip`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/AudioClip.cs)):

### Core Properties:
| Property | Type | Description |
| :--- | :--- | :--- |
| `Clip` | `AssetRef<AudioClip>` | Reference to the audio asset on disk. |
| `Volume` | `float` | Linear playback amplitude (0.0f = muted, 1.0f = full volume). |
| `Pitch` | `float` | Playback speed and frequency multiplier (1.0f = normal, 2.0f = 2x speed / octave higher). |
| `Loop` | `bool` | Automatically loops the clip back to the start when reaching EOF. |
| `PlayOnAwake` | `bool` | Commences playback as soon as the GameObject initializes. |
| `Spatialize` | `bool` | `true` activates 3D attenuation and panning; `false` plays flat 2D stereo (music/UI). |
| `MinDistance` | `float` | Inner distance boundary where volume remains at 100%. |
| `MaxDistance` | `float` | Outer attenuation boundary beyond which sound is completely inaudible. |
| `RolloffMode` | `AudioRolloffMode` | Falloff curve (`Logarithmic` or `Linear`). |
| `DopplerFactor` | `float` | Intensity scale of the pitch shift caused by relative velocities. |

---

## 💻 Scripting Audio in C#

### 1. Rapid Sound Effects with `PlayOneShot`
For rapid, overlapping sound triggers (automatic gunfire, footsteps, impact sounds) without interrupting currently active playback:

```csharp
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

public class FootstepSystem : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AssetRef<AudioClip> _footstepWood;
    [SerializeField] private AssetRef<AudioClip> _footstepStone;

    public void PlayFootstep(bool onWood)
    {
        AssetRef<AudioClip> clip = onWood ? _footstepWood : _footstepStone;
        if (!clip.IsAvailable) return;

        // Randomize pitch subtly (±10%) to prevent auditory fatigue
        _audioSource.Pitch = 1.0f + ((Random.Shared.NextSingle() - 0.5f) * 0.2f);

        // Mix in sound without restarting or interrupting other instances
        _audioSource.PlayOneShot(clip.Res, volumeScale: 0.8f);
    }
}
```

### 2. Positional 3D Environmental Emitters (Campfire)
```csharp
public class CampfireAudio : MonoBehaviour
{
    public override void Awake()
    {
        base.Awake();

        if (TryGetComponent<AudioSource>(out var src))
        {
            src.Spatialize = true; // Enable 3D spatial calculations
            src.Loop = true;
            src.MinDistance = 2.0f;  // Audibility at 100% up to 2 meters
            src.MaxDistance = 25.0f; // Silent beyond 25 meters
            src.Play();
        }
    }
}
```

---

## 🌪️ Doppler Shift Simulation

When a high-speed projectile or racing vehicle passes the player:
- As the emitter approaches the `AudioListener`, the perceived wavelength compresses, shifting the pitch higher.
- As it recedes, the wavelength stretches, triggering a drop in perceived pitch.
- Prowl computes this transition automatically based on attached `Rigidbody3D.LinearVelocity` or `Transform` motion derivatives.

---

## 🔗 Related Topics
- Engine audio context: [[🔊 Audio Engine & AudioContext]].
- Mixer buses: [[🎛️ AudioMixer & Snapshots]].
- Audio DSP: [[🧪 DSP Audio Effects]].
