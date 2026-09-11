---
title: Cámaras y RenderContext
tags: [camera, rendercontext, frustum, projection, rendertexture, raycast]
category: graphics
updated: 2026-09-10
---

# 📷 Cámaras y RenderContext en Prowl Engine

El componente [`Camera`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Camera.cs) define el punto de vista desde el cual el motor renderiza el mundo virtual. Es el responsable de calcular las matrices de vista y proyección, definir el volumen de visión (*Frustum*), descartar geometrías no visibles y enviar la escena a la pantalla o a un [`RenderTexture`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/RenderTexture.cs).

Por su parte, [`RenderContext`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Rendering/RenderContext.cs) es la estructura temporal que transfiere los buffers de color, profundidad y metadatos de resolución a través de las diferentes etapas del pipeline de renderizado y los efectos de post-procesado.

---

## 🎛️ Propiedades Principales de la Cámara

| Propiedad | Tipo | Descripción |
| :--- | :--- | :--- |
| `ProjectionType` | `CameraProjectionType` | `Perspective` (3D estándar con fuga) u `Orthographic` (isométrico/2D). |
| `FieldOfView` | `float` | Campo de visión vertical en grados (predeterminado: 60°). |
| `OrthographicSize` | `float` | Altura del volumen de visión ortográfico en unidades de mundo. |
| `NearClipPlane` | `float` | Distancia del plano de corte cercano (p. ej. 0.1 unidades). |
| `FarClipPlane` | `float` | Distancia del plano de corte lejano (p. ej. 1000 unidades). |
| `CullingMask` | `LayerMask` | Máscara de capas que determina qué GameObjects son dibujados por esta cámara. |
| `ClearColor` | `Color` | Color de fondo con el que se limpia el búfer antes de dibujar la escena. |
| `HDR` | `bool` | Si está activo, utiliza un buffer de renderizado con formato de coma flotante de 16 bits (RGBA16F). |
| `Target` | `RenderTexture?` | Destino del renderizado. Si es `null`, se dibuja directamente en la ventana principal. |

---

## 📐 Transformación de Coordenadas y Raycasting de Pantalla

Una de las tareas más comunes al programar mecánicas de juego es convertir coordenadas entre el espacio de pantalla del ratón y el espacio tridimensional del mundo:

```csharp
using Prowl.Runtime;
using Prowl.Vector;

public class MouseRaycastController : MonoBehaviour
{
    private Camera _cam;

    public override void Awake()
    {
        base.Awake();
        _cam = GetComponent<Camera>();
    }

    public override void Update()
    {
        base.Update();

        // 1. Obtener la posición del ratón en píxeles de pantalla
        Int2 mousePixel = Input.MousePosition;

        // 2. Generar un Rayo 3D desde la cámara hacia el mundo
        Ray ray = _cam.ScreenPointToRay(new Float2(mousePixel.x, mousePixel.y));

        // 3. Lanzar un raycast contra la física del mundo
        if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 100f))
        {
            Debug.Log($"El ratón apunta a: {hit.Point} sobre el objeto: {hit.Collider.GameObject.Name}");
        }
    }
}
```

### Métodos de Conversión Disponibles:
- `cam.ScreenPointToRay(Float2 screenPos)`: Genera un rayo espacial `(origin, direction)` que atraviesa la posición de pantalla indicada.
- `cam.WorldToScreenPoint(Float3 worldPos)`: Proyecta una posición 3D de mundo a coordenadas de píxel 2D de la pantalla.
- `cam.ScreenToWorldPoint(Float3 screenPosWithDepth)`: Convierte un punto de pantalla a posición de mundo usando una profundidad Z específica.

---

## 🖼️ Renderizado a Textura (Render to Texture)

Las cámaras en Prowl pueden dirigir su salida hacia un `RenderTexture` para crear cámaras de seguridad, retrovisores de vehículos, portales o minimapas:

```csharp
public class SecurityMonitor : MonoBehaviour
{
    [SerializeField] private Camera _securityCam;
    [SerializeField] private MeshRenderer _tvScreenRenderer;

    private RenderTexture _rt;

    public override void Awake()
    {
        base.Awake();

        // Crear una textura de renderizado de 512x512
        _rt = new RenderTexture(512, 512);

        // Asignar a la cámara
        _securityCam.Target = _rt;

        // Asignar al material de la pantalla de TV
        _tvScreenRenderer.Material.SetTexture("_MainTex", _rt.Color);
    }
}
```

---

## 🧪 RenderContext y Gestión de Buffers Temporales

Dentro del pipeline de renderizado y los efectos de post-procesamiento (`ImageEffect`), [`RenderContext`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Rendering/RenderContext.cs) proporciona acceso seguro y libre de fugas de memoria a los buffers de dibujo:

```csharp
public class CustomGrayscaleEffect : ImageEffect
{
    [SerializeField] private Material _grayscaleMaterial;

    public override void OnRenderEffect(RenderContext context)
    {
        // Obtener un Render Texture temporal del pool del contexto
        RenderTexture tempRT = context.GetTemporaryRT();

        // Aplicar shader de escala de grises desde el buffer de origen al temporal
        Graphics.Blit(context.ColorBuffer, tempRT, _grayscaleMaterial);

        // Volcar de nuevo al buffer del contexto
        Graphics.Blit(tempRT, context.ColorBuffer);

        // Devolver la textura temporal al pool (evita allocs de GPU)
        context.ReleaseTemporaryRT(tempRT);
    }
}
```

---

## 🔗 Temas Relacionados
- Flujo del pipeline: [[🎨 Pipeline de Renderizado (DefaultRenderPipeline)]].
- Post-procesamiento: [[✨ Efectos de Post-procesamiento y SMAA]].
- Entrada del ratón: [[🎮 Arquitectura del Input System]].
