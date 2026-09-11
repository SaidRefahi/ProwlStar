---
title: Project Settings y Tags-Layers
tags: [project-settings, tags, layers, layermask, configuration, settings]
category: assets
updated: 2026-09-10
---

# ⚙️ Project Settings y Tags-Layers en Prowl Engine

Toda aplicación o videojuego construido en Prowl Engine posee un conjunto de archivos de configuración centralizados conocidos como **Project Settings** ([`PlayerSettingsFiles.cs`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/PlayerSettingsFiles.cs)), gestionados desde el panel [`ProjectSettingsPanel.cs`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Editor/GUI/Panels/ProjectSettingsPanel.cs) en el Editor.

Entre estos ajustes, uno de los subsistemas más utilizados en la programación diaria de mecánicas es el gestor global de etiquetas y capas ([`TagLayerManager.cs`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/TagLayerManager.cs)).

---

## 🏷️ Sistema de Tags (Etiquetas Semánticas)

Un **Tag** es una cadena de texto arbitraria asociada a un [`GameObject`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/GameObject.cs) que permite identificar semánticamente el rol de una entidad sin depender de su nombre exacto en la jerarquía ni de comprobaciones de tipo pesadas.

### Tags Nativos del Motor:
- `Untagged`: Valor predeterminado de todo objeto nuevo.
- `Player`: Reservado para el avatar del usuario.
- `MainCamera`: Reservado para la cámara principal activa.
- `EditorOnly`: Entidades que se excluyen automáticamente al compilar el juego final.

### Buenas Prácticas con Tags:
```csharp
using Prowl.Runtime;

public class BulletImpact : MonoBehaviour
{
    public override void OnTriggerEnter(Rigidbody3D other)
    {
        base.OnTriggerEnter(other);

        // ❌ INCORRECTO: Genera copias de strings y allocations innecesarias
        // if (other.Tag == "Enemy") { ... }

        // ✅ CORRECTO: Comparación interna optimizada (Zero GC)
        if (other.CompareTag("Enemy"))
        {
            ApplyDamage(other.GameObject);
        }
    }
}
```

---

## 🥞 Sistema de Layers y LayerMask (Capas de Colisión y Render)

Las **Capas (Layers)** son índices enteros que van desde el `0` hasta el `31` (un total de 32 capas posibles, mapeadas a los 32 bits de un entero `int` o `uint`):

### Capas Predeterminadas (0 a 7):
- `0: Default`: Capa por defecto para geometría general.
- `1: TransparentFX`: Efectos visuales con transparencia que ignoran ciertas sombras.
- `2: Ignore Raycast`: Objetos que los raycasts deben atravesar sin colisionar.
- `4: Water`: Masas de agua y simulaciones de fluidos.
- `5: UI`: Elementos de interfaz de usuario de [`GameCanvas`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/GameCanvas.cs).

### Capas Personalizadas (8 a 31):
El desarrollador puede asignar nombres a las capas 8 a 31 en `Project Settings` (p. ej. `Player`, `Enemies`, `Debris`, `Ground`, `Interactable`).

---

## 🧮 Uso Matemático de LayerMask en C#

Un [`LayerMask`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/LayerMask.cs) almacena una máscara de bits donde cada bit encendido representa una capa incluida:

```csharp
using Prowl.Runtime;
using Prowl.Vector;

public class GroundSensor : MonoBehaviour
{
    // Campo editable en el Inspector con un selector desplegable de casillas
    [SerializeField] private LayerMask _walkableLayers;
    [SerializeField] private float _sensorDistance = 1.1f;

    public bool IsGrounded()
    {
        // 1. Obtener el valor entero de la máscara de bits
        int maskValue = _walkableLayers.Value;

        // 2. Comprobar si un rayo impacta exclusivamente contra las capas seleccionadas
        return Physics.Raycast(Transform.Position, Float3.Down, out _, _sensorDistance, maskValue);
    }

    public static void LayerMaskUtilities()
    {
        // Convertir nombres a máscara combinada
        LayerMask mask = LayerMask.GetMask("Ground", "Obstacles", "Vehicles");

        // Convertir nombre a índice de capa individual (0 a 31)
        int layerIndex = LayerMask.NameToLayer("Player");

        // Convertir índice a nombre
        string name = LayerMask.LayerToName(8);

        // Comprobar si un GameObject pertenece a una máscara
        GameObject go = ...;
        bool isInMask = ((1 << go.Layer) & mask.Value) != 0;
    }
}
```

---

## ⚙️ Archivos de Configuración del Proyecto

Todos los ajustes se serializan mediante **Prowl.Echo** dentro de la carpeta `ProjectSettings/`:
- **`PhysicsSettings.echo`:** Gravedad, `FixedDeltaTime`, fricción global y la matriz bidimensional de colisiones.
- **`GraphicsSettings.echo`:** Pipeline de renderizado activo, calidad de sombras y anti-aliasing SMAA.
- **`TimeSettings.echo`:** Escala de tiempo (`Time.timeScale`) y umbrales de paso máximo de cuadros.
- **`TagLayerSettings.echo`:** Diccionario de nombres de tags y las 32 capas del proyecto.

---

## 🔗 Temas Relacionados
- Consultas de física por capa: [[🎯 Raycasting y Shape Queries]].
- Matriz de capas: [[🌍 PhysicsWorld y Configuración]].
- Serialización de ajustes: [[📜 Serialización con Prowl.Echo]].
