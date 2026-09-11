---
title: MonoBehaviour in Prowl
tags: [monobehaviour, scripting, callbacks, serialization, attributes]
category: scripting
updated: 2026-09-10
---

# 🧬 MonoBehaviour in Prowl Engine

The foundational base class for all behavior scripts attached to a [`GameObject`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/GameObject.cs) in Prowl Engine is [`MonoBehaviour`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/MonoBehaviour.cs). It derives from [`EngineObject`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/EngineObject.cs) and implements `ISerializationCallbackReceiver`.

In Prowl, `MonoBehaviour` does not rely on Unity's string-based magic methods. It provides explicit `virtual` methods designed to be overridden (`override`), allowing compile-time signature validation by the C# compiler and bitmask-driven execution dispatching by `SceneDispatcher`.

---

## 📋 Core Properties of MonoBehaviour

| Property | Type | Description |
| :--- | :--- | :--- |
| `GameObject` | `GameObject` | Direct reference to the owning `GameObject`. |
| `Transform` | `Transform` | Quick access to the owner's `Transform` component. |
| `Tag` | `string` | Returns the tag assigned to the owning `GameObject`. |
| `Enabled` | `bool` | Enables or disables this specific component instance. |
| `EnabledInHierarchy` | `bool` | `true` only if this component and all ancestral GameObjects are active. |
| `HasStarted` | `bool` | Indicates whether the `Start()` hook has completed. |
| `HideFlags` | `HideFlags` | Governs visibility in the Inspector and Scene Hierarchy. |

---

## 🎯 Complete List of Virtual Lifecycle Overrides

```csharp
public abstract class MonoBehaviour : EngineObject, ISerializationCallbackReceiver
{
    // ---- General Lifecycle ----
    public virtual void Awake() { }
    public virtual void OnEnable() { }
    public virtual void Start() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void LateUpdate() { }
    public virtual void OnDisable() { }
    protected override void OnDispose() { }

    // ---- Graphics, Editor & GUI ----
    public virtual void OnRenderCollect() { }
    public virtual void DrawGizmos() { }
    public virtual void OnGui() { }

    // ---- Jitter Physics 2 Contact Events ----
    public virtual void OnCollisionBegin(in Collision collision) { }
    public virtual void OnCollisionEnd(in Collision collision) { }
    public virtual void OnTriggerEnter(Rigidbody3D other) { }
    public virtual void OnTriggerStay(Rigidbody3D other) { }
    public virtual void OnTriggerExit(Rigidbody3D other) { }

    // ---- Serialization Callbacks ----
    public virtual void OnBeforeSerialize() { }
    public virtual void OnAfterDeserialize() { }
}
```

---

## 🏷️ Essential Component Attributes

Prowl provides attributes to streamline editor integration and runtime execution:

### 1. `[AddComponentMenu("Category/Name")]`
Organizes your script within the interactive "Add Component" menu inside the Inspector.
```csharp
[AddComponentMenu("Gameplay/Player Controller")]
public class PlayerController : MonoBehaviour { }
```

### 2. `[ComponentIcon("\uf1b2")]` or `[ComponentIcon("icon")]`
Assigns a distinct glyph/icon in the Inspector header and hierarchy view.
```csharp
[ComponentIcon("\uf0e7")] // Lightning glyph
public class LightningPower : MonoBehaviour { }
```

### 3. `[ExecuteAlways]`
Enables the component to execute inside the Editor Scene View even when PlayMode is not active. Crucial for level-building tools, procedural geometry generators, and design-time layouts.
```csharp
[ExecuteAlways]
public class ProceduralRoadBuilder : MonoBehaviour
{
    public override void Update()
    {
        base.Update();
        // Executes both in the Editor viewport and during gameplay
    }
}
```

---

## 📦 Field Serialization (`[SerializeField]` vs `public`)

Prowl's **Echo** serialization engine adheres to strict encapsulation principles:
- Fields marked with `[SerializeField]` are serialized regardless of private/protected access modifiers.
- Encapsulation best practice: Keep fields private and expose read-only properties:
```csharp
public class Weapon : MonoBehaviour
{
    // ✅ Serialized in inspector while strictly encapsulated
    [SerializeField] private float _fireRate = 0.25f;
    [SerializeField] private int _magazineCapacity = 30;
    [SerializeField] private AssetRef<AudioClip> _fireSound;

    // Public read-only accessors
    public float FireRate => _fireRate;
    public int MagazineCapacity => _magazineCapacity;

    // Ignored by the serializer
    [SerializeIgnore]
    private float _nextFireTime;
}
```

---

## 💥 Physics Callbacks & Collision Detection

Contact events are fired when the GameObject is equipped with a `Collider` and/or `Rigidbody3D`:

```csharp
public class Projectile : MonoBehaviour
{
    // Physical contact collision (uses 'in Collision' for zero copies)
    public override void OnCollisionBegin(in Collision collision)
    {
        base.OnCollisionBegin(collision);

        // collision.OtherRigidbody references the hit body
        if (collision.OtherRigidbody.IsValid())
        {
            Debug.Log($"Hit entity: {collision.OtherRigidbody.GameObject.Name}");
        }

        GameObject.Destroy();
    }

    // Trigger volume overlap
    public override void OnTriggerEnter(Rigidbody3D other)
    {
        base.OnTriggerEnter(other);
        if (other.CompareTag("Player"))
        {
            // Inflict player damage
        }
    }
}
```

---

## 🔗 Related Topics
- Engine execution flow: [[⏱️ Lifecycle & Game Loop]].
- 3D Physics with Jitter 2: [[🧊 Rigidbodies & Force Modes]].
- Customizing component inspectors: [[🎛️ Custom Editors & Inspector Customization]].
