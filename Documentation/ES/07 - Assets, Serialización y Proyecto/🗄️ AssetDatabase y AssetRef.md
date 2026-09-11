---
title: AssetDatabase y AssetRef
tags: [assetdatabase, assetref, assets, guid, lazy-loading, memory-management, cache]
category: assets
updated: 2026-09-10
---

# 🗄️ AssetDatabase y AssetRef en Prowl Engine

En videojuegos de escala comercial, gestionar miles de texturas, modelos 3D, pistas de audio, materiales y prefabs sin sufrir fugas de memoria (*Memory Leaks*) ni caídas de rendimiento por tiempos de carga exige un sistema de activos robusto y desacoplado.

Prowl Engine organiza todo su pipeline de recursos alrededor de dos conceptos centrales:
1. **[`AssetDatabase`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs):** La base de datos central que mapea identificadores universales únicos ([`Guid`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs)) con los activos reales en disco o memoria.
2. **[`AssetRef<T>`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetRef.cs):** Una referencia serializable y fuertemente tipada que permite carga perezosa (*Lazy Loading*) y descarga automática por inactividad.

---

## 🧭 ¿Por qué Identificación por GUID en vez de Rutas de Archivos?

En motores arcaicos, las referencias a assets se guardaban mediante rutas de texto (p. ej. `"Assets/Textures/Rock.png"`). Si un artista renombraba una carpeta o movía un archivo a otro directorio, todos los materiales y prefabs que lo utilizaban se rompían irremediablemente (*Missing References*).

En Prowl:
- Cada archivo dentro del proyecto tiene un archivo compañero `.meta` que contiene un **`Guid` inmutable** de 128 bits.
- Puedes renombrar, mover o reorganizar carpetas enteras en el sistema operativo: los materiales y escenas seguirán resolviendo el activo correctamente porque solo almacenan su `Guid`.

---

## 🧬 Anatomía y Uso de `AssetRef<T>`

`AssetRef<T>` es una estructura ligera que encapsula el `Guid` del activo y gestiona su ciclo de vida en memoria:

```mermaid
graph LR
    InspectorField[Campo: AssetRef de Texture2D] --> StoreGUID[Almacena solo el Guid en el Archivo de Escena]
    StoreGUID --> Request[.Res / .IsAvailable]
    Request --> CacheCheck{¿Cargado en Caché?}
    CacheCheck -->|Sí: Ya en RAM| ReturnAsset[Devolver Instancia Inmediatamente: O(1)]
    CacheCheck -->|No: Aún en Disco| LoadAsset[Carga Perezosa & Deserialización con Echo]
    LoadAsset --> ReturnAsset
```

### Propiedades Clave:
- **`AssetID` (`Guid`):** El identificador único del recurso.
- **`IsAvailable` (`bool`):** Comprueba si el asset ha sido asignado y existe en la base de datos sin forzar su carga a la RAM.
- **`IsLoaded` (`bool`):** Indica si el objeto ya está residente en la memoria RAM del sistema.
- **`Res` (`T`):** Propiedad de acceso al recurso tipado. Si el recurso no estaba cargado, **lo carga automáticamente bajo demanda de forma transparente**.

---

## 💻 Ejemplo Práctico en C#

```csharp
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

public class TurretVisuals : MonoBehaviour
{
    // Referencias tipadas expuestas al Inspector
    [SerializeField] private AssetRef<Mesh> _barrelMesh;
    [SerializeField] private AssetRef<Material> _barrelMaterial;
    [SerializeField] private AssetRef<AudioClip> _fireSound;

    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private AudioSource _audio;

    public override void Start()
    {
        base.Start();

        // Carga y asignación perezosa
        if (_barrelMesh.IsAvailable)
        {
            _renderer.Mesh = _barrelMesh.Res; // Se carga de disco solo en este momento
        }

        if (_barrelMaterial.IsAvailable)
        {
            _renderer.Material = _barrelMaterial.Res;
        }
    }

    public void PlayFireSound()
    {
        if (_fireSound.IsAvailable)
        {
            _audio.PlayOneShot(_fireSound.Res);
        }
    }
}
```

---

## 🧹 Recolección de Memoria por Inactividad (Idle Eviction)

Una de las grandes innovaciones de `AssetDatabase` en Prowl es su sistema lock-free de seguimiento de actividad ([`AssetDatabase.cs: Stamp`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs)):
- Cada vez que un script o renderizador accede a `AssetRef.Res`, el motor actualiza una marca de tiempo de microsegundos (`Environment.TickCount64`).
- Si un activo (como una textura de un nivel anterior o un audio de un jefe derrotado) no se toca durante un periodo configurable de inactividad, el motor puede desalojarlo automáticamente de la RAM para mantener el consumo de memoria al mínimo.

---

## 📦 Backends Desacoplados: Editor vs Standalone Player

[`AssetDatabase`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs) opera a través de una abstracción (`AssetBackendBase`):
- **En el Editor:** Utiliza `EditorAssetDatabase`, que vigila cambios en el explorador de archivos, reimporta assets en tiempo real y gestiona metadatos.
- **En el Juego Exportado (Standalone Desktop Player):** Se intercambia automáticamente por [`PlayerAssetBackend.cs`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/PlayerAssetBackend.cs), que resuelve activos a máxima velocidad desde archivos empaquetados comprimidos sin costo de búsqueda de archivos en disco.

---

## 🔗 Temas Relacionados
- Formato de serialización: [[📜 Serialización con Prowl.Echo]].
- Importación de modelos y texturas: [[📥 Importadores de Assets (Modelos, Texturas, Audio)]].
- Compilación del Player: [[📦 Build System y Exportación Standalone (Desktop Player)]].
