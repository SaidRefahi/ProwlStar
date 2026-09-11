---
title: Comparativa y Migración desde Unity
tags: [unity, migration, comparison, api]
category: getting-started
updated: 2026-09-10
---

# 🔄 Comparativa y Migración desde Unity

Prowl Engine fue diseñado específicamente para que los desarrolladores con experiencia en **Unity** se sientan inmediatamente como en casa. La mayoría de los conceptos (GameObjects, Componentes, Prefabs, Ciclo de vida, Transform, Rigidbodies, Colliders) existen con nombres y comportamientos casi idénticos.

Sin embargo, al estar construido en **C# moderno (.NET 10)** y sin las limitaciones históricas de la arquitectura C++ de Unity, existen mejoras y diferencias clave que debes conocer.

---

## 📊 Tabla de Equivalencias Directas

| Concepto | Unity | Prowl Engine | Notas Clave |
| :--- | :--- | :--- | :--- |
| **Entidad Base** | `GameObject` | `GameObject` | Ambos usan jerarquía y lista de componentes. |
| **Componente Base** | `MonoBehaviour` | `MonoBehaviour` | En Prowl los métodos son **virtuales** (`override`), no métodos mágicos basados en nombres. |
| **Transformación** | `Transform` | `Transform` (`Prowl.Vector.Transform`) | Utiliza `Float3`, `Quaternion`, `Float4x4` en lugar de `Vector3`. |
| **Vectores y Mates** | `Vector2`, `Vector3`, `Vector4` | `Float2`, `Float3`, `Float4` | Namespace `Prowl.Vector`. Soportan operadores matemáticos directos y SIMD. |
| **Matrices** | `Matrix4x4` | `Float4x4` | Operaciones optimizadas en `Prowl.Vector`. |
| **Tiempo** | `Time.deltaTime`, `Time.time` | `Time.deltaTime`, `Time.time` | Misma API estática en [`Time`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Time.cs). |
| **Física 3D** | PhysX (`Rigidbody`, `BoxCollider`) | Jitter 2 (`Rigidbody3D`, `BoxCollider`) | Jitter Physics 2 es multihilo y determinista en C#. |
| **Input** | Unity New Input System (`InputAction`) | `InputManagement` (`InputAction`, `Input`) | Soporta tanto consultas directas (`Input.GetKey`) como Action Maps desacoplados. |
| **Audio** | FMOD / Unity AudioSource | MiniAudio (`AudioSource`, `AudioListener`) | Soporte nativo de bajo nivel sin licencias restrictivas. |
| **Interfaz (UI)** | uGUI (`Canvas`, `RectTransform`) | `GameCanvas`, `RectTransform`, `UIBehaviour` | Basado en el mismo paradigma de anclas y layouts. |
| **Serialización** | Archivos `.unity`, `.prefab` (YAML) | Archivos `.prowl`, `.prefab` (**Prowl.Echo**) | Serialización binaria y de texto infinitamente más rápida y sin merge-conflicts crípticos. |
| **Gestión de Assets** | `AssetDatabase`, `Resources.Load` | `AssetDatabase`, `AssetRef<T>` | Referencias fuertes por `Guid` y descarga automática por inactividad. |

---

## ⚠️ Diferencias Críticas que Debes Recordar

### 1. Métodos de Ciclo de Vida: `override` vs Métodos Mágicos
En Unity, los métodos como `Update()` o `Start()` se detectaban por reflexión interna en C++ si el nombre coincidía:
```csharp
// ❌ ESTILO UNITY (No recomendado en Prowl)
void Update() { } 
```

En Prowl, todos los métodos del ciclo de vida son **virtuales explícitos**. Debes usar `public override void`:
```csharp
// ✅ ESTILO PROWL ENGINE
public override void Update()
{
    base.Update();
    // Tu lógica por frame aquí
}
```
> [!NOTE]
> **¿Por qué?** `SceneDispatcher` analiza los overrides en el momento de registrar el tipo y genera una máscara binaria (`SceneCallbacks`). Si tu clase no sobrescribe `Update`, el motor tiene coste cero en cada frame, evitando comprobaciones y llamadas vacías.

---

### 2. Tipos Matemáticos: `Float3` en vez de `Vector3`
El namespace `Prowl.Vector` reemplaza la biblioteca matemática de Unity:

```csharp
// En Unity:
Vector3 pos = transform.position;
transform.position += Vector3.forward * speed * Time.deltaTime;

// En Prowl:
using Prowl.Vector;

Float3 pos = Transform.Position;
Transform.Position += Transform.Forward * speed * (float)Time.deltaTime;
```

---

### 3. Operador de Comparación Nula (`== null`)
En Unity, los objetos heredados de `UnityEngine.Object` sobrecargaban `== null` para comprobar si el objeto subyacente en C++ había sido destruido.
En Prowl:
- Todos los objetos heredan de `EngineObject`.
- Tienen el método y propiedad de extensión `.IsValid()` / `.IsNotValid()`.
- Puedes comprobar `obj == null` o usar `obj.IsValid()` de forma segura.
- **Regla:** Evita la propagación de nulos (`?.`) sobre objetos de motor si necesitas garantizar el ciclo de destrucción interna.

---

### 4. Búsqueda y Adquisición de Componentes
Las APIs de componentes son prácticamente idénticas:

```csharp
// Obtener componente en el mismo GameObject
if (TryGetComponent<Rigidbody3D>(out var rb))
{
    rb.AddForce(new Float3(0, 10, 0), ForceMode.Impulse);
}

// Buscar en jerarquías
Camera cam = GetComponentInParent<Camera>();
MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>().ToArray();
```

---

### 5. Instanciación y Destrucción de Prefabs
```csharp
// Unity:
GameObject go = Instantiate(prefab, position, rotation);
Destroy(go, 2.0f);

// Prowl:
GameObject go = GameObject.Instantiate(prefab);
go.Transform.Position = position;
go.Transform.Rotation = rotation;

// Destrucción inmediata o diferida
go.Destroy(); // O go.Destroy(delayInSeconds)
```

---

## 🚀 Guía Rápida de Migración Paso a Paso

1. **Reemplaza Namespaces:**
   Cambia `using UnityEngine;` por:
   ```csharp
   using Prowl.Runtime;
   using Prowl.Vector;
   ```
2. **Convierte Vectores:**
   Reemplaza `Vector2` -> `Float2`, `Vector3` -> `Float3`, `Vector4` -> `Float4`, `Quaternion` -> `Quaternion`.
3. **Agrega `override` a tus métodos de ciclo de vida:**
   Modifica `void Start()` -> `public override void Start()`.
4. **Actualiza las llamadas de Física:**
   Cambia `Rigidbody` por `Rigidbody3D`, y usa `ForceMode.Force` o `ForceMode.Impulse`.
5. **Revisa las Corrutinas / Asincronía:**
   Prowl permite usar `Tasks` y `async/await` modernos de C# de manera nativa sin necesidad de crear enumeradores costosos con `IEnumerator`.

---

## 🔗 Continuar Aprendiendo
- Profundiza en el ciclo de vida: [[⏱️ Ciclo de Vida y Game Loop]].
- Conoce el modelo de componentes: [[🧱 GameObjects y Componentes]].
- Reglas de optimización: [[🎯 Buenas Prácticas y Rendimiento (Zero GC)]].
