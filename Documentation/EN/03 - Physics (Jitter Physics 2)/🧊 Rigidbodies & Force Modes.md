---
title: Rigidbodies & Force Modes
tags: [rigidbody, physics, jitter2, forces, forcemode, interpolation, velocity]
category: physics
updated: 2026-09-10
---

# 🧊 Rigidbodies & Force Modes in Prowl Engine

The [`Rigidbody3D`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Rigidbody3D.cs) component imbues a `GameObject` with physical mass, inertia tensors, and physical responsiveness to forces, gravity, and contacts governed by **Jitter Physics 2**.

---

## 🏃 Motion Types (MotionType)

A `Rigidbody3D` operates under one of three motion modes:

1. **`MotionType.Dynamic`:**
   Fully simulated. Responds to gravity, applied external forces, contact impulses, and collisions from surrounding geometry. The default mode for crates, vehicles, debris, and ragdoll bones.
2. **`MotionType.Kinematic`:**
   Immune to external impacts and gravity. Position is driven directly by scripts via `LinearVelocity` or explicit `Transform` positioning. Crucially, kinematic bodies exert infinite mass upon dynamic bodies, making them ideal for moving platforms, blast doors, and elevators.
3. **`MotionType.Static`:**
   Infinite mass and immobile. Assigned to terrain, architecture, and static world bounds.

---

## 🎛️ Essential Rigidbody3D Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Mass` | `float` | Body mass in kilograms (default: 1.0 kg). |
| `UseGravity` | `bool` | Toggles gravitational acceleration on this body. |
| `LinearDamping` | `float` | Linear drag coefficient opposing directional velocity. |
| `AngularDamping` | `float` | Rotational drag coefficient dampening angular spin. |
| `Friction` | `float` | Surface friction coefficient (0 = frictionless ice, 1 = high-grip rubber). |
| `Restitution` | `float` | Surface bounciness coefficient (0 = inelastic clay, 1 = elastic superball). |
| `LinearVelocity` | `Float3` | Instantaneous world-space linear velocity vector ($m/s$). |
| `AngularVelocity`| `Float3` | Instantaneous rotational velocity in radians/second. |
| `Interpolation` | `RigidbodyInterpolation` | Transform smoothing between physics steps (`None`, `Interpolate`, `Extrapolate`). |
| `Constraints` | `RigidbodyConstraints` | Axis constraints (`FreezePositionY`, `FreezeRotationX`, etc.). |

---

## 🚀 Force Application Modes (`ForceMode`)

When invoking `AddForce`, the [`ForceMode`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Physics/ForceMode.cs) parameter dictates mathematical integration:

| Force Mode | Considers Mass? | Scaled by $\Delta t$? | Typical Application |
| :--- | :---: | :---: | :--- |
| **`ForceMode.Force`** | **Yes** ($a = F / m$) | **Yes** ($v += a \cdot \Delta t$) | Continuous forces: rocket thrusters, wind, water buoyancy in `FixedUpdate`. |
| **`ForceMode.Acceleration`** | **No** | **Yes** ($v += a \cdot \Delta t$) | Continuous acceleration independent of mass: directional gravity fields. |
| **`ForceMode.Impulse`** | **Yes** ($\Delta v = J / m$) | **No** (Instantaneous) | Instantaneous shocks: explosions, bullet impacts, character jump impulses. |
| **`ForceMode.VelocityChange`** | **No** | **No** (Instantaneous) | Direct velocity override bypassing mass: arcade maneuvers, jump pads. |

---

## 💻 Practical C# Implementation

### 1. Directional Thrust and Jump Impulses
```csharp
using Prowl.Runtime;
using Prowl.Vector;

public class BallPhysicsController : MonoBehaviour
{
    [SerializeField] private float _jumpImpulse = 10.0f;
    [SerializeField] private float _engineForce = 25.0f;

    private Rigidbody3D _rb;
    private bool _jumpRequested;

    public override void Awake()
    {
        base.Awake();
        TryGetComponent(out _rb);
    }

    public override void Update()
    {
        base.Update();
        // Capture input in Update hook
        if (Input.GetKeyDown(Key.Space))
        {
            _jumpRequested = true;
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // 1. Continuous propulsion force
        Float3 forwardForce = Transform.Forward * _engineForce;
        _rb.AddForce(forwardForce, ForceMode.Force);

        // 2. Instantaneous jump impulse
        if (_jumpRequested)
        {
            _rb.AddForce(Float3.Up * _jumpImpulse, ForceMode.Impulse);
            _jumpRequested = false;
        }
    }
}
```

### 2. Off-Center Force Application (Producing Rotational Torque)
```csharp
public class DestructibleObject : MonoBehaviour
{
    private Rigidbody3D _rb;

    public void ReceiveImpact(Float3 hitPoint, Float3 hitDirection, float power)
    {
        if (TryGetComponent(out _rb))
        {
            // Imparts both linear acceleration and rotational spin
            _rb.AddForceAtPosition(hitDirection * power, hitPoint, ForceMode.Impulse);
        }
    }
}
```

---

## 📺 Visual Interpolation: Eliminating Micro-Stutter

Because physics solves at fixed intervals (e.g., 50 Hz) while displays refresh at 60, 144, or 240 Hz, dynamic entities can exhibit visual jitter if unbuffered.

- **`RigidbodyInterpolation.Interpolate`:**
  Smoothly blends the visual transform between the two most recent physics step poses. Strongly recommended for the player character and camera targets.
- **`RigidbodyInterpolation.None`:**
  Direct unbuffered transform sync, optimal for background debris and minor props.

---

## 🔗 Related Topics
- Physics setup: [[🌍 PhysicsWorld & Configuration]].
- Collider shapes: [[📐 Colliders (Primitives, Mesh, Terrain)]].
- Mechanical constraints: [[🔗 Joints & Constraints]].
