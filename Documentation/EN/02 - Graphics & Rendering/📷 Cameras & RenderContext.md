---
title: Cameras & RenderContext
tags: [camera, rendercontext, frustum, projection, rendertexture, raycast]
category: graphics
updated: 2026-09-10
---

# 📷 Cameras & RenderContext in Prowl Engine

The [`Camera`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Camera.cs) component defines the spatial viewpoint from which the engine renders the virtual world. It calculates view and projection matrices, builds the view frustum, culls out-of-view entities, and presents rendered pixels either directly to the application window or into an offscreen [`RenderTexture`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/RenderTexture.cs).

Meanwhile, [`RenderContext`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Rendering/RenderContext.cs) is the ephemeral context structure passed through render stages and image post-processing effects to provide pooled framebuffer access.

---

## 🎛️ Primary Camera Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `ProjectionType` | `CameraProjectionType` | `Perspective` (standard 3D with vanishing point) or `Orthographic` (2D/isometric). |
| `FieldOfView` | `float` | Vertical Field of View in degrees (default: 60°). |
| `OrthographicSize` | `float` | Half-height of the orthographic view volume in world units. |
| `NearClipPlane` | `float` | Distance to the near clipping plane (e.g., 0.1 units). |
| `FarClipPlane` | `float` | Distance to the far clipping plane (e.g., 1000 units). |
| `CullingMask` | `LayerMask` | Bitmask filtering which GameObject layers are rendered by this camera. |
| `ClearColor` | `Color` | Color used to clear the framebuffer before drawing. |
| `HDR` | `bool` | Enables 16-bit floating-point render targets (RGBA16F). |
| `Target` | `RenderTexture?` | Render target texture. When `null`, draws directly to the window framebuffer. |

---

## 📐 Coordinate Projection & Screen Raycasting

Translating screen mouse coordinates into 3D world space is a core gameplay programming pattern:

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

        // 1. Acquire mouse pixel coordinates
        Int2 mousePixel = Input.MousePosition;

        // 2. Generate a 3D ray through the camera frustum
        Ray ray = _cam.ScreenPointToRay(new Float2(mousePixel.x, mousePixel.y));

        // 3. Query world physics
        if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 100f))
        {
            Debug.Log($"Hit surface at {hit.Point} on {hit.Collider.GameObject.Name}");
        }
    }
}
```

### Projection Methods:
- `cam.ScreenPointToRay(Float2 screenPos)`: Generates a 3D world ray originating at the camera and passing through the specified screen pixel.
- `cam.WorldToScreenPoint(Float3 worldPos)`: Projects a 3D world coordinate into a 2D screen coordinate.
- `cam.ScreenToWorldPoint(Float3 screenPosWithDepth)`: Reconstructs a 3D world coordinate using screen XY and depth Z.

---

## 🖼️ Render-to-Texture Workflows

Direct a camera's rendering pipeline into a `RenderTexture` for in-game security monitors, mirrors, or portal views:

```csharp
public class SecurityMonitor : MonoBehaviour
{
    [SerializeField] private Camera _securityCam;
    [SerializeField] private MeshRenderer _tvScreenRenderer;

    private RenderTexture _rt;

    public override void Awake()
    {
        base.Awake();

        // Allocate 512x512 render target
        _rt = new RenderTexture(512, 512);

        // Assign to camera
        _securityCam.Target = _rt;

        // Route color buffer to TV material
        _tvScreenRenderer.Material.SetTexture("_MainTex", _rt.Color);
    }
}
```

---

## 🧪 RenderContext & Pooled Texture Management

Inside post-processing passes ([`ImageEffect`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Rendering/ImageEffect.cs)), [`RenderContext`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Rendering/RenderContext.cs) manages pooled textures to prevent GPU allocation stalls:

```csharp
public class CustomGrayscaleEffect : ImageEffect
{
    [SerializeField] private Material _grayscaleMaterial;

    public override void OnRenderEffect(RenderContext context)
    {
        // Acquire pooled temporary render texture
        RenderTexture tempRT = context.GetTemporaryRT();

        // Process fullscreen shader pass
        Graphics.Blit(context.ColorBuffer, tempRT, _grayscaleMaterial);

        // Copy back to context color buffer
        Graphics.Blit(tempRT, context.ColorBuffer);

        // Release temporary RT back to pool (Zero GC / GPU re-allocations)
        context.ReleaseTemporaryRT(tempRT);
    }
}
```

---

## 🔗 Related Topics
- Pipeline stages: [[🎨 Rendering Pipeline (DefaultRenderPipeline)]].
- Post-processing effects: [[✨ Post-Processing & SMAA]].
- Mouse and touch input: [[🎮 Input System Architecture]].
