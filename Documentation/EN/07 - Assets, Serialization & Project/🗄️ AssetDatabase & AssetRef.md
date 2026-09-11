---
title: AssetDatabase & AssetRef
tags: [assetdatabase, assetref, assets, guid, lazy-loading, memory-management, cache]
category: assets
updated: 2026-09-10
---

# 🗄️ AssetDatabase & AssetRef in Prowl Engine

Managing tens of thousands of textures, 3D meshes, audio clips, materials, and prefabs in commercial games without encountering memory leaks or stuttering load freezes demands a decoupled asset architecture.

Prowl Engine organizes its resource pipeline around two foundational constructs:
1. **[`AssetDatabase`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs):** The centralized registry mapping 128-bit globally unique identifiers ([`Guid`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs)) to physical assets residing on disk or resident in memory.
2. **[`AssetRef<T>`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetRef.cs):** A strongly-typed, serializable smart reference structure delivering transparent **Lazy Loading** and idle memory eviction.

---

## 🧭 Why GUID Addressing Over File System Paths?

In legacy engines, asset references often stored fragile file system strings (e.g., `"Assets/Textures/Rock.png"`). If an artist renamed a directory or reorganized subfolders, cross-references across materials and scenes shattered (*Missing References*).

In Prowl:
- Every asset file across the project is paired with an immutable `.meta` sidecar file recording an immutable 128-bit **`Guid`**.
- Developers and artists can freely rename files, reorganize folders, or restructure directory trees on disk: scenes and materials remain unbroken because they reference only the immutable GUID.

---

## 🧬 Anatomy of `AssetRef<T>`

`AssetRef<T>` is a lightweight value-like wrapper encapsulating the asset's GUID:

```mermaid
graph LR
    InspectorField[Field: AssetRef of Texture2D] --> StoreGUID[Stores Only 16-Byte GUID in Scene File]
    StoreGUID --> Request[.Res / .IsAvailable]
    Request --> CacheCheck{Resident in Memory Cache?}
    CacheCheck -->|Yes: Already in RAM| ReturnAsset[Return Instance Immediately: O(1)]
    CacheCheck -->|No: On Disk| LoadAsset[Lazy Load & Deserialization with Echo]
    LoadAsset --> ReturnAsset
```

### Primary Properties:
- **`AssetID` (`Guid`):** The persistent GUID identifier.
- **`IsAvailable` (`bool`):** Validates whether the asset exists in the database without forcing it to decompress into RAM.
- **`IsLoaded` (`bool`):** Queries whether the asset instance is currently resident in system RAM.
- **`Res` (`T`):** Direct typed access property. If the resource was not loaded, **it loads on-demand transparently**.

---

## 💻 Scripting Asset References in C#

```csharp
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

public class TurretVisuals : MonoBehaviour
{
    // Typed asset references exposed to Inspector
    [SerializeField] private AssetRef<Mesh> _barrelMesh;
    [SerializeField] private AssetRef<Material> _barrelMaterial;
    [SerializeField] private AssetRef<AudioClip> _fireSound;

    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private AudioSource _audio;

    public override void Start()
    {
        base.Start();

        // Lazy load and apply mesh geometry
        if (_barrelMesh.IsAvailable)
        {
            _renderer.Mesh = _barrelMesh.Res; // Loaded from storage only upon first access
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

## 🧹 Lock-Free Idle Eviction

A hallmark optimization of Prowl's `AssetDatabase` is its lock-free activity stamp tracking ([`AssetDatabase.cs: Stamp`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs)):
- Every access through `AssetRef.Res` updates a high-speed monotonic millisecond tick counter (`Environment.TickCount64`).
- Unused assets (such as textures belonging to completed levels or defeated enemy models) exceeding a designated idle timeout are purged from RAM automatically, capping total working-set memory footprints.

---

## 📦 Decoupled Backends: Editor vs Standalone Player

[`AssetDatabase`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs) operates over an abstract backend interface (`AssetBackendBase`):
- **Inside the Editor:** Powered by `EditorAssetDatabase`, which watches disk file changes, reimports assets, and tracks live metadata.
- **Inside Standalone Builds (Desktop Player):** Replaced with [`PlayerAssetBackend.cs`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/PlayerAssetBackend.cs), resolving assets directly from compressed package blobs without file system scan overhead.

---

## 🔗 Related Topics
- Serialization engine: [[📜 Serialization with Prowl.Echo]].
- Ingesting meshes and textures: [[📥 Asset Importers (Models, Textures, Audio)]].
- Packaging releases: [[📦 Build System & Desktop Player]].
