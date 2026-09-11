---
title: Materials, Shaders & PropertyState
tags: [materials, shaders, propertystate, uniforms, pbr, textures, rendering]
category: graphics
updated: 2026-09-10
---

# 🔮 Materials, Shaders & PropertyState in Prowl Engine

The shading architecture of Prowl Engine rests upon three cohesive pillars:
1. **[`Shader`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Shader.cs):** The compiled GPU program (Vertex, Geometry, and Fragment stages).
2. **[`Material`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Material.cs):** An instance of a shader holding a specific configuration of textures, numerical parameters, and tint colors.
3. **[`PropertyState`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Rendering/PropertyState.cs):** The low-level GPU uniform cache manager that validates and batches parameter uploads, eliminating redundant driver state changes.

---

## 🎨 The Standard PBR (Physically Based Rendering) Shader

Prowl delivers an industry-standard metallic/roughness PBR workflow compatible with assets authored in Blender, Maya, Substance Painter, or the Unity Standard Shader.

### Standard Shader Core Properties:
| Parameter | Type | Function |
| :--- | :--- | :--- |
| `_AlbedoTex` | `Texture2D` | Base diffuse color texture without baked lighting or ambient occlusion. |
| `_Color` | `Color` | Multiplicative color tint applied over the albedo map. |
| `_NormalTex` | `Texture2D` | Tangent-space normal map encoding microscopic surface perturbations. |
| `_NormalScale` | `float` | Bump depth and normal projection multiplier. |
| `_RoughnessTex` | `Texture2D` | Grayscale microfacet roughness map (0 = polished mirror, 1 = diffuse rough). |
| `_Roughness` | `float` | Scalar roughness multiplier. |
| `_MetallicTex` | `Texture2D` | Grayscale metallic mask (0 = dielectric insulator, 1 = pure conductive metal). |
| `_Metallic` | `float` | Scalar metallic multiplier. |
| `_OcclusionTex` | `Texture2D` | Ambient occlusion map darkening crevices and cavities. |
| `_EmissionTex` | `Texture2D` | Self-illumination color map. |
| `_EmissionColor`| `Color` | HDR emissive tint and intensity multiplier. |

---

## 💻 Scripting Materials in C#

```csharp
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

public class MaterialController : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private AssetRef<Texture2D> _rockTexture;
    [SerializeField] private AssetRef<Texture2D> _rockNormal;

    private Material _instancedMaterial;

    public override void Awake()
    {
        base.Awake();

        // 1. Clone material instance to avoid modifying the shared disk asset
        _instancedMaterial = new Material(_renderer.Material);
        _renderer.Material = _instancedMaterial;

        // 2. Assign texture maps
        if (_rockTexture.IsAvailable)
            _instancedMaterial.SetTexture("_AlbedoTex", _rockTexture.Res);

        if (_rockNormal.IsAvailable)
            _instancedMaterial.SetTexture("_NormalTex", _rockNormal.Res);

        // 3. Configure numerical and color uniforms
        _instancedMaterial.SetFloat("_Roughness", 0.35f);
        _instancedMaterial.SetFloat("_Metallic", 0.0f);
        _instancedMaterial.SetColor("_Color", new Color(0.8f, 0.85f, 0.9f, 1f));
    }

    public void TriggerDamageEffect()
    {
        // High-intensity HDR emission flash
        _instancedMaterial.SetColor("_EmissionColor", new Color(2.5f, 0.1f, 0.1f, 1f));
    }
}
```

---

## ⚡ PropertyState: Zero Redundant GPU State Uploads

In traditional render loops, every draw call pushes its complete uniform list across the graphics driver.

[`PropertyState`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Rendering/PropertyState.cs) implements a GPU state mirror:
- Computes uniform hash identities.
- If two consecutive meshes share identical uniforms or materials, **uniform upload calls are skipped entirely**.
- Manages dynamic texture unit slots (0 to 31), ensuring persistent textures are never rebound unnecessarily.

---

## 🌐 Automatic Global Uniforms

Prowl automatically populates pervasive scene uniforms accessible across all custom and standard shaders:

| Global Uniform | GLSL Type | Description |
| :--- | :--- | :--- |
| `_ViewMatrix` | `mat4` | Active camera view transformation matrix. |
| `_ProjectionMatrix` | `mat4` | Active camera perspective/orthographic projection matrix. |
| `_InvViewMatrix` | `mat4` | Inverse of the view transformation matrix. |
| `_InvProjectionMatrix` | `mat4` | Inverse of the projection matrix. |
| `_CameraPosition` | `vec3` | World-space camera eye position. |
| `_ScreenParams` | `vec4` | `(width, height, 1/width, 1/height)` in display pixels. |
| `_Time` | `vec4` | `(t/20, t, t*2, t*3)` for animated procedural shaders. |

---

## 🔗 Related Topics
- Pipeline overview: [[🎨 Rendering Pipeline (DefaultRenderPipeline)]].
- Dynamic lights: [[💡 Lighting, Shadows & LightBVH]].
- Mesh deformation and instancing: [[💨 Instanced & Skinned Mesh Rendering]].
