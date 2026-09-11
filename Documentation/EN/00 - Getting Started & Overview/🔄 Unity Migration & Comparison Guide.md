---
title: Unity Migration & Comparison Guide
tags: [unity, migration, comparison, api]
category: getting-started
updated: 2026-09-10
---

# 🔄 Unity Migration & Comparison Guide

Prowl Engine was deliberately architected so that developers with a background in **Unity** feel immediately at home. Core mental models—GameObjects, Components, Prefabs, Lifecycle methods, Transforms, Rigidbodies, and Colliders—exist with nearly identical naming and behaviors.

However, because Prowl is constructed in **pure modern C# (.NET 10)** without the historical limitations and marshaling layers of Unity's C++ native codebase, several key differences and architectural upgrades exist.

---

## 📊 Direct API Equivalencies Table

| Concept | Unity | Prowl Engine | Key Notes |
| :--- | :--- | :--- | :--- |
| **Base Entity** | `GameObject` | `GameObject` | Both use component lists and scene graph hierarchies. |
| **Base Component** | `MonoBehaviour` | `MonoBehaviour` | In Prowl, lifecycle hooks are explicit **`virtual` methods** (`override`), not reflection-based magic methods. |
| **Transform** | `Transform` | `Transform` (`Prowl.Vector.Transform`) | Uses `Float3`, `Quaternion`, `Float4x4` instead of `Vector3`. |
| **Vector Math** | `Vector2`, `Vector3`, `Vector4` | `Float2`, `Float3`, `Float4` | In namespace `Prowl.Vector`. Built-in SIMD operators. |
| **Matrices** | `Matrix4x4` | `Float4x4` | Direct matrix math in `Prowl.Vector`. |
| **Time System** | `Time.deltaTime`, `Time.time` | `Time.deltaTime`, `Time.time` | Static access in [`Time`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Time.cs). |
| **3D Physics** | PhysX (`Rigidbody`, `BoxCollider`) | Jitter 2 (`Rigidbody3D`, `BoxCollider`) | Jitter Physics 2 is multithreaded, deterministic, and 100% managed C#. |
| **Input** | Unity New Input System (`InputAction`) | `InputManagement` (`InputAction`, `Input`) | Supports both direct polling (`Input.GetKey`) and decoupled action maps. |
| **Audio** | FMOD / Unity AudioSource | MiniAudio (`AudioSource`, `AudioListener`) | Direct low-level DSP pipeline without restrictive licensing. |
| **User Interface** | uGUI (`Canvas`, `RectTransform`) | `GameCanvas`, `RectTransform`, `UIBehaviour` | Same anchor, pivot, and responsive layout paradigm. |
| **Serialization** | `.unity`, `.prefab` (YAML) | `.prowl`, `.prefab` (**Prowl.Echo**) | High-performance binary and text formats without merge conflicts or YAML bloat. |
| **Asset Pipeline** | `AssetDatabase`, `Resources.Load` | `AssetDatabase`, `AssetRef<T>` | Strong GUID referencing, lazy loading, and idle memory eviction. |

---

## ⚠️ Critical Differences to Keep in Mind

### 1. Lifecycle Methods: `override` vs Magic Methods
In Unity, engine loops invoked methods like `Update()` or `Start()` via internal C++ reflection based on method name matching:
```csharp
// ❌ UNITY STYLE (Will NOT be called in Prowl)
void Update() { } 
```

In Prowl, all component lifecycle callbacks are **explicit virtual methods**. You must declare `public override void`:
```csharp
// ✅ PROWL ENGINE STYLE
public override void Update()
{
    base.Update();
    // Per-frame logic here
}
```
> [!NOTE]
> **Why?** `SceneDispatcher` inspects virtual overrides when a component type is first registered and builds an execution bitmask (`SceneCallbacks`). If a class does not override `Update`, the engine completely skips it every frame at zero CPU cost.

---

### 2. Math Types: `Float3` instead of `Vector3`
The `Prowl.Vector` namespace replaces Unity's math library:

```csharp
// In Unity:
Vector3 pos = transform.position;
transform.position += Vector3.forward * speed * Time.deltaTime;

// In Prowl:
using Prowl.Vector;

Float3 pos = Transform.Position;
Transform.Position += Transform.Forward * speed * (float)Time.deltaTime;
```

---

### 3. Null Checking (`== null` and `.IsValid()`)
In Unity, objects inheriting from `UnityEngine.Object` overloaded the `==` operator to test whether the C++ native peer had been destroyed.
In Prowl:
- All engine types inherit from `EngineObject`.
- Extension methods `.IsValid()` and `.IsNotValid()` provide fast, safe checks.
- You can check `obj == null` or `obj.IsValid()`.
- **Rule:** Avoid null-coalescing (`??`) or null-conditional (`?.`) operators on `EngineObject` types to prevent bypassing lifecycle disposal flags.

---

### 4. Component Queries & Caching
Querying components is virtually identical to Unity:

```csharp
// Query on current GameObject
if (TryGetComponent<Rigidbody3D>(out var rb))
{
    rb.AddForce(new Float3(0, 10, 0), ForceMode.Impulse);
}

// Hierarchical queries
Camera cam = GetComponentInParent<Camera>();
MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>().ToArray();
```

---

### 5. Prefab Instantiation and Destruction
```csharp
// Unity:
GameObject go = Instantiate(prefab, position, rotation);
Destroy(go, 2.0f);

// Prowl:
GameObject go = GameObject.Instantiate(prefab);
go.Transform.Position = position;
go.Transform.Rotation = rotation;

// Immediate or deferred destruction
go.Destroy(); // Or go.Destroy(delayInSeconds)
```

---

## 🚀 Step-by-Step Migration Checklist

1. **Update Namespaces:**
   Replace `using UnityEngine;` with:
   ```csharp
   using Prowl.Runtime;
   using Prowl.Vector;
   ```
2. **Convert Math Types:**
   Map `Vector2` -> `Float2`, `Vector3` -> `Float3`, `Vector4` -> `Float4`, `Quaternion` -> `Quaternion`.
3. **Add `override` to Lifecycle Hooks:**
   Change `void Start()` to `public override void Start()`.
4. **Update Physics APIs:**
   Use `Rigidbody3D` instead of `Rigidbody`, and use `ForceMode.Force` or `ForceMode.Impulse`.
5. **Modern Async instead of Coroutines:**
   Prowl supports standard modern C# `Task` / `ValueTask` and `async/await` patterns with zero allocation overhead compared to legacy `IEnumerator` coroutines.

---

## 🔗 Next Steps
- Deep-dive into execution order: [[⏱️ Lifecycle & Game Loop]].
- Learn the component architecture: [[🧱 GameObjects & Components]].
- Review performance standards: [[🎯 Best Practices & Performance (Zero GC)]].
