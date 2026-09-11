---
title: MonoBehaviour en Prowl
tags: [monobehaviour, scripting, callbacks, serialization, attributes]
category: scripting
updated: 2026-09-10
---

# 🧬 MonoBehaviour en Prowl Engine

La clase base para todo script de comportamiento que se adjunta a un [`GameObject`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/GameObject.cs) en Prowl Engine es [`MonoBehaviour`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/MonoBehaviour.cs). Hereda de [`EngineObject`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/EngineObject.cs) e implementa `ISerializationCallbackReceiver`.

En Prowl, `MonoBehaviour` no depende de la magia de nombres de Unity: proporciona métodos virtuales explícitos que deben ser sobreescritos (`override`), lo que permite que el compilador de C# verifique firmas en tiempo de compilación y que el despachador de la escena (`SceneDispatcher`) optimice su ejecución.

---

## 📋 Propiedades Principales de MonoBehaviour

| Propiedad | Tipo | Descripción |
| :--- | :--- | :--- |
| `GameObject` | `GameObject` | Referencia al `GameObject` al que está asociado este componente. |
| `Transform` | `Transform` | Acceso directo al componente `Transform` del `GameObject`. |
| `Tag` | `string` | Devuelve la etiqueta asignada al `GameObject` propietario. |
| `Enabled` | `bool` | Activa o desactiva este componente individualmente. |
| `EnabledInHierarchy` | `bool` | `true` solo si este componente y todos sus ancestros están activos. |
| `HasStarted` | `bool` | Indica si el método `Start()` ya ha sido invocado. |
| `HideFlags` | `HideFlags` | Controla la visibilidad del componente en el Inspector o la jerarquía. |

---

## 🎯 Lista Completa de Métodos Virtuales Sobrescribibles

```csharp
public abstract class MonoBehaviour : EngineObject, ISerializationCallbackReceiver
{
    // ---- Ciclo de Vida General ----
    public virtual void Awake() { }
    public virtual void OnEnable() { }
    public virtual void Start() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void LateUpdate() { }
    public virtual void OnDisable() { }
    protected override void OnDispose() { }

    // ---- Gráficos, Editor y UI ----
    public virtual void OnRenderCollect() { }
    public virtual void DrawGizmos() { }
    public virtual void OnGui() { }

    // ---- Eventos Físicos de Jitter 2 ----
    public virtual void OnCollisionBegin(in Collision collision) { }
    public virtual void OnCollisionEnd(in Collision collision) { }
    public virtual void OnTriggerEnter(Rigidbody3D other) { }
    public virtual void OnTriggerStay(Rigidbody3D other) { }
    public virtual void OnTriggerExit(Rigidbody3D other) { }

    // ---- Serialización ----
    public virtual void OnBeforeSerialize() { }
    public virtual void OnAfterDeserialize() { }
}
```

---

## 🏷️ Atributos Especiales de Componente

Prowl proporciona atributos para enriquecer la experiencia en el editor y controlar la ejecución:

### 1. `[AddComponentMenu("Categoría/Nombre")]`
Permite organizar tu script dentro del menú interactivo "Add Component" del Inspector.
```csharp
[AddComponentMenu("Gameplay/Player Controller")]
public class PlayerController : MonoBehaviour { }
```

### 2. `[ComponentIcon("\uf1b2")]` o `[ComponentIcon("icono")]`
Define un icono personalizado en el Inspector y Hierarchy utilizando fuentes de iconos como FontAwesome o SVG.
```csharp
[ComponentIcon("\uf0e7")] // Icono de rayo
public class LightningPower : MonoBehaviour { }
```

### 3. `[ExecuteAlways]`
Permite que un componente se ejecute en la vista de escena del Editor incluso cuando el juego no está en modo Play. Esencial para scripts de edición de niveles, generadores de terreno o interfaces en tiempo de diseño.
```csharp
[ExecuteAlways]
public class ProceduralRoadBuilder : MonoBehaviour
{
    public override void Update()
    {
        base.Update();
        // Se ejecuta tanto en el editor como durante el juego
    }
}
```

---

## 📦 Serialización de Campos (`[SerializeField]` vs `public`)

El serializador de Prowl (**Echo**) sigue reglas estrictas de serialización:
- Los campos marcados con `[SerializeField]` se serializan independientemente de si son `private` o `protected`.
- Por convención y reglas de encapsulación, los campos deben ser privados:
```csharp
public class Weapon : MonoBehaviour
{
    // ✅ Serializado en el inspector y encapsulado
    [SerializeField] private float _fireRate = 0.25f;
    [SerializeField] private int _magazineCapacity = 30;
    [SerializeField] private AssetRef<AudioClip> _fireSound;

    // Propiedad pública de solo lectura
    public float FireRate => _fireRate;
    public int MagazineCapacity => _magazineCapacity;

    // Campo ignorado por el serializador
    [SerializeIgnore]
    private float _nextFireTime;
}
```

---

## 💥 Callbacks de Física y Detección de Colisiones

Los eventos físicos se reciben cuando el GameObject tiene un `Collider` y/o `Rigidbody3D`:

```csharp
public class Projectile : MonoBehaviour
{
    // Colisión física con contacto (usa 'in Collision' para Zero Copy)
    public override void OnCollisionBegin(in Collision collision)
    {
        base.OnCollisionBegin(collision);

        // collision.OtherRigidbody contiene el cuerpo impactado
        if (collision.OtherRigidbody.IsValid())
        {
            Debug.Log($"Impacto con {collision.OtherRigidbody.GameObject.Name}");
        }

        GameObject.Destroy();
    }

    // Entrada en un volumen Trigger
    public override void OnTriggerEnter(Rigidbody3D other)
    {
        base.OnTriggerEnter(other);
        if (other.CompareTag("Player"))
        {
            // Daño al jugador
        }
    }
}
```

---

## 🔗 Temas Relacionados
- Ciclo de ejecución: [[⏱️ Ciclo de Vida y Game Loop]].
- Física Jitter 2: [[🧊 Rigidbodies y Modos de Fuerza]].
- Creación de editores a medida: [[🎛️ Custom Editors e Inspector Personalizado]].
