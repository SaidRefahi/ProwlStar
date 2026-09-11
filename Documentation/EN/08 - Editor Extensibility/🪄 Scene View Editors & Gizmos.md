---
title: Scene View Editors & Gizmos
tags: [gizmos, sceneview, debug, handles, 3d-tools, editor]
category: editor
updated: 2026-09-10
---

# 🪄 Scene View Editors & Gizmos in Prowl Engine

**Gizmos** are visual overlays and interactive manipulation handles rendered in the 3D [`SceneViewPanel`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Editor/GUI/Panels/SceneViewPanel.cs) for debugging and level design. They never appear in the standalone game or the `GameViewPanel`, making them indispensable for visualizing AI patrol zones, spawn volumes, audio reach boundaries, and vision cones.

In Prowl Engine, gizmos are authored via the virtual `DrawGizmos()` hook declared on [`MonoBehaviour`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/MonoBehaviour.cs) using the static `Gizmos` utility class.

---

## 🎨 Primary Gizmos Drawing Methods

| Method | Key Arguments | Typical Application |
| :--- | :--- | :--- |
| `Gizmos.DrawLine` | `Float3 from, Float3 to` | Pathfinding nodes, projectile trajectories, ray sensors. |
| `Gizmos.DrawWireCube` | `Float3 center, Float3 size` | Bounding volumes, room trigger extents, spawn zones. |
| `Gizmos.DrawWireSphere` | `Float3 center, float radius` | Explosion radius, 3D audio reach, player detection ranges. |
| `Gizmos.DrawRay` | `Float3 origin, Float3 direction` | Sensor heading, muzzle trajectories. |
| `Gizmos.DrawFrustum` | `Float4x4 viewProjMatrix` | Camera view frustums, spot light projection boundaries. |
| `Gizmos.DrawIcon` | `Float3 pos, string iconName` | Billboard 2D glyphs (e.g. Player Start, NavMesh Waypoints). |

---

## 💻 Practical C# Implementation: AI Vision Cone & Patrol Range

```csharp
using Prowl.Runtime;
using Prowl.Vector;

public class EnemyPatrolRoute : MonoBehaviour
{
    [SerializeField] private float _patrolRadius = 12.0f;
    [SerializeField] private float _visionRange = 8.0f;
    [SerializeField] private float _visionAngle = 60.0f;

#if PROWL_EDITOR
    // Executed exclusively inside the Editor environment
    public override void DrawGizmos()
    {
        base.DrawGizmos();

        // 1. Draw circular ground patrol boundary
        Gizmos.Color = new Color(0.2f, 0.8f, 0.2f, 0.75f); // Translucent green
        Gizmos.DrawWireSphere(Transform.Position, _patrolRadius);

        // 2. Draw directional vision frustum
        Gizmos.Color = new Color(1.0f, 0.2f, 0.2f, 0.9f); // Solid red
        Float3 forward = Transform.Forward;
        Float3 eyePos = Transform.Position + new Float3(0, 1.6f, 0);

        // Center line of sight
        Gizmos.DrawRay(eyePos, forward * _visionRange);

        // Peripheral cone boundaries
        float halfAngle = _visionAngle * 0.5f;
        Float3 leftRay = Quaternion.Euler(0, -halfAngle, 0) * forward;
        Float3 rightRay = Quaternion.Euler(0, halfAngle, 0) * forward;

        Gizmos.DrawRay(eyePos, leftRay * _visionRange);
        Gizmos.DrawRay(eyePos, rightRay * _visionRange);

        // Connect boundaries
        Gizmos.DrawLine(eyePos + (leftRay * _visionRange), eyePos + (forward * _visionRange));
        Gizmos.DrawLine(eyePos + (rightRay * _visionRange), eyePos + (forward * _visionRange));
    }
#endif
}
```

---

## 🎛️ Interactive Viewport Handles

Beyond static wireframe drawings, Prowl enables custom interactive viewport handles:
- **Translation Handles:** RGB axial arrows (X-Red, Y-Green, Z-Blue) supporting direct mouse dragging.
- **Rotation Rings:** Planar arc rings facilitating intuitive angular orientation.
- **Scale Handles:** Cuboid scale handles.

These tools allow level designers to drag waypoints or resize volumes visually directly inside the 3D viewport without entering numeric coordinates manually in the Inspector.

---

## ⚡ Performance Optimization
- Gizmos submit lines to a GPU batch buffer, rendering all editor wireframes in a single draw pass.
- Wrapping gizmos with `#if PROWL_EDITOR` ensures zero assembly bloat in the standalone player.

---

## 🔗 Related Topics
- Editor architecture: [[🛠️ Prowl.Editor Architecture]].
- Custom inspector widgets: [[🎛️ Custom Editors & Inspector Customization]].
- Custom editor windows: [[🪟 Custom Editor Panels]].
