---
title: Scene View Editors y Gizmos
tags: [gizmos, sceneview, debug, handles, 3d-tools, editor]
category: editor
updated: 2026-09-10
---

# 🪄 Scene View Editors y Gizmos en Prowl Engine

Los **Gizmos** son representaciones visuales y herramientas de manipulación tridimensionales que se dibujan en el [`SceneViewPanel`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Editor/GUI/Panels/SceneViewPanel.cs) para depuración y diseño de niveles. No son visibles en el juego compilado ni en la `GameView`, lo que los convierte en la herramienta perfecta para visualizar zonas de disparo, rutas de patrulla de IA, radios de luz o volúmenes de colisión.

En Prowl Engine, los gizmos se dibujan mediante el método virtual `DrawGizmos()` en cualquier [`MonoBehaviour`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/MonoBehaviour.cs) utilizando la clase estática `Gizmos`.

---

## 🎨 Métodos Principales de Dibujo con Gizmos

| Método | Argumentos Clave | Uso Típico |
| :--- | :--- | :--- |
| `Gizmos.DrawLine` | `Float3 from, Float3 to` | Trazar rayos, trayectorias de proyectiles o líneas de visión. |
| `Gizmos.DrawWireCube` | `Float3 center, Float3 size` | Visualizar cajas delimitadoras (AABB) o áreas de spawn. |
| `Gizmos.DrawWireSphere` | `Float3 center, float radius` | Radios de explosión, zonas de audio 3D o detección de IA. |
| `Gizmos.DrawRay` | `Float3 origin, Float3 direction` | Visualizar la orientación de sensores o cañones. |
| `Gizmos.DrawFrustum` | `Float4x4 viewProjMatrix` | Dibujar el tronco de visión de cámaras o focos de luz. |
| `Gizmos.DrawIcon` | `Float3 pos, string iconName` | Colocar un icono 2D flotante en la escena (p. ej. spawn de jugador). |

---

## 💻 Ejemplo: Visualizador de Radio de Patrulla y Ruta de IA

```csharp
using Prowl.Runtime;
using Prowl.Vector;

public class EnemyPatrolRoute : MonoBehaviour
{
    [SerializeField] private float _patrolRadius = 12.0f;
    [SerializeField] private float _visionRange = 8.0f;
    [SerializeField] private float _visionAngle = 60.0f;

#if PROWL_EDITOR
    // Este método solo se ejecuta dentro del Editor
    public override void DrawGizmos()
    {
        base.DrawGizmos();

        // 1. Dibujar el radio de patrulla circular en el suelo
        Gizmos.Color = new Color(0.2f, 0.8f, 0.2f, 0.75f); // Verde translúcido
        Gizmos.DrawWireSphere(Transform.Position, _patrolRadius);

        // 2. Dibujar el cono de visión del enemigo
        Gizmos.Color = new Color(1.0f, 0.2f, 0.2f, 0.9f); // Rojo
        Float3 forward = Transform.Forward;
        Float3 eyePos = Transform.Position + new Float3(0, 1.6f, 0);

        // Línea central de visión
        Gizmos.DrawRay(eyePos, forward * _visionRange);

        // Bordes del cono de visión
        float halfAngle = _visionAngle * 0.5f;
        Float3 leftRay = Quaternion.Euler(0, -halfAngle, 0) * forward;
        Float3 rightRay = Quaternion.Euler(0, halfAngle, 0) * forward;

        Gizmos.DrawRay(eyePos, leftRay * _visionRange);
        Gizmos.DrawRay(eyePos, rightRay * _visionRange);

        // Conectar los extremos
        Gizmos.DrawLine(eyePos + (leftRay * _visionRange), eyePos + (forward * _visionRange));
        Gizmos.DrawLine(eyePos + (rightRay * _visionRange), eyePos + (forward * _visionRange));
    }
#endif
}
```

---

## 🎛️ Manipuladores de Escena (Scene Handles)

Además de gizmos pasivos de alambre, Prowl permite crear herramientas de manipulación interactivas arrastrables con el ratón:
- **Translation Handles:** Flechas de traslación tridimensional en los ejes X (rojo), Y (verde) y Z (azul).
- **Rotation Rings:** Anillos de rotación angular para cuaterniones.
- **Scale Handles:** Cubos de escala volumétrica.

Estas herramientas permiten a los diseñadores ajustar radios de patrulla, puntos de ruta (*Waypoints*) o límites de áreas arrastrando directamente en la vista de escena 3D sin tener que adivinar números en el Inspector.

---

## ⚡ Rendimiento de Gizmos
- Los gizmos utilizan una cola de buffers de líneas GPU compacta que se dibuja en un único pase masivo por frame.
- Se compilan condicionalmente mediante `#if PROWL_EDITOR`, garantizando **cero bytes y cero llamadas** en el juego final exportado (*Desktop Player*).

---

## 🔗 Temas Relacionados
- Arquitectura del editor: [[🛠️ Arquitectura de Prowl.Editor]].
- Personalización del inspector: [[🎛️ Custom Editors e Inspector Personalizado]].
- Ventanas personalizadas: [[🪟 Creación de Paneles Propios del Editor]].
