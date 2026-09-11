---
title: Materiales, Shaders y PropertyState
tags: [materials, shaders, propertystate, uniforms, pbr, textures, rendering]
category: graphics
updated: 2026-09-10
---

# 🔮 Materiales, Shaders y PropertyState en Prowl Engine

El sistema de sombreado de Prowl Engine está estructurado en tres pilares fundamentales:
1. **[`Shader`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Shader.cs):** El programa que se compila y ejecuta en la GPU (Vertex, Geometry y Fragment shaders).
2. **[`Material`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Material.cs):** Una instancia de un shader que encapsula una configuración específica de texturas, colores y parámetros numéricos.
3. **[`PropertyState`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Rendering/PropertyState.cs):** El administrador de bajo nivel que agrupa, compara y sube uniformes y texturas a la GPU, evitando envíos redundantes que degraden el rendimiento.

---

## 🎨 El Shader Estándar PBR (Physically Based Rendering)

Prowl implementa un modelo de sombreado PBR metálico/rugoso compatible con los estándares de la industria (Substance Painter, Blender, Unity Standard Shader).

### Parámetros Principales del Shader Estándar:
| Parámetro | Tipo | Función |
| :--- | :--- | :--- |
| `_AlbedoTex` | `Texture2D` | Textura de color base / difusión sin sombras ni luces cocinadas. |
| `_Color` | `Color` | Tinte multiplicativo sobre el albedo. |
| `_NormalTex` | `Texture2D` | Mapa de normales en espacio tangente para detalles superficiales. |
| `_NormalScale` | `float` | Intensidad o relieve del mapa de normales. |
| `_RoughnessTex` | `Texture2D` | Mapa en escala de grises de microfaceta (0 = liso/espejo, 1 = rugoso/difuso). |
| `_Roughness` | `float` | Multiplicador escalar de rugosidad. |
| `_MetallicTex` | `Texture2D` | Mapa en escala de grises de metalicidad (0 = dieléctrico/plástico, 1 = metal puro). |
| `_Metallic` | `float` | Multiplicador escalar de metalicidad. |
| `_OcclusionTex` | `Texture2D` | Mapa de oclusión ambiental (AO) que oscurece grietas y rincones. |
| `_EmissionTex` | `Texture2D` | Textura de emisión propia de luz (autoiluminación). |
| `_EmissionColor`| `Color` | Color e intensidad HDR de la emisión de luz. |

---

## 💻 Creación y Manipulación de Materiales en C#

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

        // 1. Instanciar el material para no modificar el asset compartido en disco
        _instancedMaterial = new Material(_renderer.Material);
        _renderer.Material = _instancedMaterial;

        // 2. Asignar texturas
        if (_rockTexture.IsAvailable)
            _instancedMaterial.SetTexture("_AlbedoTex", _rockTexture.Res);

        if (_rockNormal.IsAvailable)
            _instancedMaterial.SetTexture("_NormalTex", _rockNormal.Res);

        // 3. Modificar propiedades escalares y vectoriales
        _instancedMaterial.SetFloat("_Roughness", 0.35f);
        _instancedMaterial.SetFloat("_Metallic", 0.0f);
        _instancedMaterial.SetColor("_Color", new Color(0.8f, 0.85f, 0.9f, 1f));
    }

    public void TriggerDamageEffect()
    {
        // Pulso de emisión roja
        _instancedMaterial.SetColor("_EmissionColor", new Color(2.5f, 0.1f, 0.1f, 1f)); // HDR > 1.0
    }
}
```

---

## ⚡ PropertyState: Evitando Subidas Redundantes

En motores convencionales, cada vez que se dibuja un objeto se vuelven a enviar todos sus uniformes a la GPU a través de llamadas costosas del driver.

[`PropertyState`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Rendering/PropertyState.cs) mantiene un espejo del estado actual del contexto gráfico:
- Compara valores hash de los uniformes.
- Si dos objetos consecutivos comparten el mismo material o parámetros idénticos, **omite las llamadas de asignación**.
- Administra la asignación dinámica de ranuras de textura (*Texture Units* 0 a 31), garantizando que las texturas comunes no se vuelvan a enlazar.

---

## 🌐 Uniformes Globales de Cámara y Tiempo

Prowl alimenta automáticamente una serie de variables globales accesibles desde cualquier shader sin necesidad de asignarlas manualmente por material:

| Uniforme Global | Tipo | Descripción |
| :--- | :--- | :--- |
| `_ViewMatrix` | `mat4` | Matriz de vista de la cámara activa. |
| `_ProjectionMatrix` | `mat4` | Matriz de proyección de la cámara. |
| `_InvViewMatrix` | `mat4` | Matriz inversa de vista. |
| `_InvProjectionMatrix` | `mat4` | Matriz inversa de proyección. |
| `_CameraPosition` | `vec3` | Coordenadas de mundo del ojo de la cámara. |
| `_ScreenParams` | `vec4` | Ancho, Alto, 1/Ancho, 1/Alto en píxeles. |
| `_Time` | `vec4` | `(t/20, t, t*2, t*3)` para animaciones en shaders. |

---

## 🔗 Temas Relacionados
- Pipeline gráfico: [[🎨 Pipeline de Renderizado (DefaultRenderPipeline)]].
- Sombras y luces dinámicas: [[💡 Iluminación, Sombras y LightBVH]].
- Optimización de mallas y batching: [[💨 Instanced & Skinned Mesh Rendering]].
