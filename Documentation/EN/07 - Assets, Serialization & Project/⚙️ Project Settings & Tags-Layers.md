---
title: Project Settings & Tags-Layers
tags: [project-settings, tags, layers, layermask, configuration, settings]
category: assets
updated: 2026-09-10
---

# ⚙️ Project Settings & Tags-Layers in Prowl Engine

Every video game or standalone application built in Prowl Engine maintains centralized project configuration manifests known as **Project Settings** ([`PlayerSettingsFiles.cs`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/PlayerSettingsFiles.cs)), managed inside the Editor via the [`ProjectSettingsPanel.cs`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Editor/GUI/Panels/ProjectSettingsPanel.cs).

Among these configuration domains, the global semantic tag and physical layer registry ([`TagLayerManager.cs`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/TagLayerManager.cs)) is foundational to gameplay mechanics.

---

## 🏷️ The Semantic Tags System

A **Tag** is an arbitrary identifier string associated with a [`GameObject`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/GameObject.cs) declaring an entity's semantic role without relying on fragile hierarchy path strings or expensive type probes.

### Built-in System Tags:
- `Untagged`: Default fallback for all newly spawned entities.
- `Player`: Reserved for the primary user avatar.
- `MainCamera`: Designates the primary viewport rendering camera.
- `EditorOnly`: Entities automatically stripped during production standalone packaging.

### High-Performance Tag Comparison:
```csharp
using Prowl.Runtime;

public class BulletImpact : MonoBehaviour
{
    public override void OnTriggerEnter(Rigidbody3D other)
    {
        base.OnTriggerEnter(other);

        // ❌ WRONG: Generates string garbage and allocation overhead
        // if (other.Tag == "Enemy") { ... }

        // ✅ RIGHT: Optimized internal ordinal comparison (Zero GC)
        if (other.CompareTag("Enemy"))
        {
            ApplyDamage(other.GameObject);
        }
    }
}
```

---

## 🥞 The Layers & LayerMask System

**Layers** are integer indices ranging from `0` to `31` (a total of 32 addressable bit flags matching the 32 bits of a standard unsigned integer `uint`):

### Reserved System Layers (0 to 7):
- `0: Default`: Base layer for generic world geometry.
- `1: TransparentFX`: Alpha-blended visual effects ignoring directional shadow passes.
- `2: Ignore Raycast`: Geometry bypassed by spatial physics ray queries.
- `4: Water`: Fluid bodies and surface buoyancy sensors.
- `5: UI`: Retained screen-space widgets belonging to [`GameCanvas`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/GameCanvas.cs).

### User Layers (8 to 31):
Developers configure custom named layers within Project Settings (e.g. `Player`, `Enemy`, `Debris`, `Ground`, `Interactable`).

---

## 🧮 Bitwise LayerMask Operations in C#

The [`LayerMask`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/LayerMask.cs) struct manages multi-layer bitmask evaluation:

```csharp
using Prowl.Runtime;
using Prowl.Vector;

public class GroundSensor : MonoBehaviour
{
    // Exposed as a multi-selection dropdown inside the Inspector
    [SerializeField] private LayerMask _walkableLayers;
    [SerializeField] private float _sensorDistance = 1.1f;

    public bool IsGrounded()
    {
        // 1. Retrieve integer bitmask representation
        int maskValue = _walkableLayers.Value;

        // 2. Query physics using the bitmask
        return Physics.Raycast(Transform.Position, Float3.Down, out _, _sensorDistance, maskValue);
    }

    public static void LayerMaskUtilities()
    {
        // Synthesize combined bitmask from string labels
        LayerMask mask = LayerMask.GetMask("Ground", "Obstacles", "Vehicles");

        // Convert layer name to integer index (0 to 31)
        int layerIndex = LayerMask.NameToLayer("Player");

        // Convert index to name
        string name = LayerMask.LayerToName(8);

        // Validate whether an entity matches the mask
        GameObject go = ...;
        bool isInMask = ((1 << go.Layer) & mask.Value) != 0;
    }
}
```

---

## ⚙️ Project Settings Storage Format

All project settings are serialized via **Prowl.Echo** inside the project's `ProjectSettings/` directory:
- **`PhysicsSettings.echo`:** Gravity vector, `FixedDeltaTime`, friction materials, and the layer collision matrix.
- **`GraphicsSettings.echo`:** Active render pipeline reference, shadow map resolution limits, and SMAA anti-aliasing defaults.
- **`TimeSettings.echo`:** Global `Time.timeScale` and frame step delta caps.
- **`TagLayerSettings.echo`:** Tag dictionary and the 32 layer label mappings.

---

## 🔗 Related Topics
- Layered raycasting: [[🎯 Raycasting & Shape Queries]].
- Layer collision matrix: [[🌍 PhysicsWorld & Configuration]].
- Serialization: [[📜 Serialization with Prowl.Echo]].
