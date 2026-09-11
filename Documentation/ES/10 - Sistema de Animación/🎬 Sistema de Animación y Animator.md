---
title: Sistema de Animación y Animator
tags: [animation, animator, skeleton, humanoid, retargeting, ik, root-motion, blend-trees]
category: animation
updated: 2026-09-11
---

# 🎬 Sistema de Animación y Animator en Prowl Engine

En **Prowl Engine**, el **Sistema de Animación** es una característica nativa fundamental del motor que gestiona el ciclo completo de vida de los personajes y mallas esqueléticas (Skinned Meshes). El `Animator` constituye el núcleo evaluador en tiempo de ejecución del motor, integrando esqueletos, clips, árboles de mezcla, capas, retargeting humanoide en tiempo real, cinemática inversa (IK) y movimiento raíz (Root Motion) con **cero asignaciones de memoria (Zero GC)** en la ruta crítica.

---

## 🏗️ Flujo Conceptual de Animación

El pipeline nativo de animación de Prowl sigue un flujo desacoplado, determinista y de alto rendimiento:

```
[ Modelo 3D con SkeletonAsset ] + [ AnimationClips (Mocopi / Mixamo / FBX) ]
                         │
                         ▼
             [ Evaluador Animator ]
                         │
      ┌──────────────────┼──────────────────┐
      ▼                  ▼                  ▼
[ Capas & BlendTrees ] [ Retargeting Humanoide ] [ Two-Bone IK ]
      │                  │                  │
      └──────────────────┼──────────────────┘
                         ▼
             [ Root Motion Extractor ]
                         │
                         ▼
     [ Transform GameObject + SkinnedMeshRenderer ]
```

1. **Importación y SkeletonAsset**: Al importar un modelo esquelético, Prowl genera su `SkeletonAsset` nativo con la jerarquía indexada de huesos y poses de referencia.
2. **Clips de Animación**: Los `AnimationClip` contienen curvas y tracks de rotación, posición y escala.
3. **Animator**: Evalúa el grafo de estados, parámetros y condiciones, muestreando los clips necesarios.
4. **Blend Trees y Capas**: Combina poses 1D/2D y mezcla capas ponderadas mediante `AvatarMask` jerárquicos.
5. **Retargeting Humanoide en Tiempo Real**: Si el clip proviene de un esqueleto distinto, `AnimationRetargeter` adapta las rotaciones y escalas relativas preservando el contacto con el suelo.
6. **Inverse Kinematics (IK)**: `IKSolver` ajusta analíticamente extremidades (brazos/piernas) hacia objetivos en el espacio de mundo.
7. **Root Motion**: Extrae el desplazamiento y giro de la raíz para aplicarlo al `Transform` del actor en la escena.

---

## 🧬 Componentes del Sistema

### 1. SkeletonAsset
Define la estructura del esqueleto:
- Matriz de binds y nombres únicos por hueso.
- Detección automática de jerarquías y validación de ciclos.
- Indexación numérica directa para evitar búsquedas de cadenas por frame.

### 2. HumanoidMapping y Calibración
Permite el retargeting universal entre cualquier personaje bípedo:
- Mapeo de `HumanoidBone` (Hips, Spine, Head, Arms, Legs, etc.) a los huesos del esqueleto.
- Calibración automática de pose T / pose A y corrección de orientaciones locales.

### 3. Animator (Runtime Evaluator)
El evaluador por excelencia en tiempo de ejecución:
- **Parámetros**: Float, Int, Bool, Trigger.
- **Transiciones**: Evaluación basada en condiciones y cross-fades de duración configurable.
- **Capas Múltiples**: Base Layer + Capas aditivas/override con máscaras de avatar.
- **Blend Trees 1D y 2D**: Mezcla continua según velocidad, dirección o ángulos de movimiento.
- **Two-Bone IK**: Manos y pies con hints de codos/rodillas y ponderación de peso `[0, 1]`.
- **Root Motion**: Aplicación precisa de velocidad lineal y angular al actor.

---

## 💻 Ejemplo de Uso e Integración en Código

```csharp
using Prowl.Runtime;
using Prowl.Vector;

public class CharacterController : MonoBehaviour
{
    // El Animator nativo asociado al personaje
    private Animator _animator;

    public override void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public override void Update()
    {
        // 1. Control de parámetros para Blend Trees y transiciones
        float moveForward = Input.GetKey(Key.W) ? 1f : 0f;
        _animator.SetFloat("Speed", moveForward);

        // 2. Disparo de transiciones por Trigger
        if (Input.GetKeyDown(Key.Space))
        {
            _animator.SetTrigger("Jump");
        }

        // 3. Ajuste de Two-Bone IK para fijar pies al terreno
        if (Physics.Raycast(transform.position + Float3.Up, Float3.Down, out var hit, 2f))
        {
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, hit.Point);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1.0f);
        }
    }
}
```

---

## ⚡ Garantías de Rendimiento (Zero GC)

- **Cero Asignaciones en `Animator.Update()`**: Todos los buffers de poses (`BonePose[]`), matrices globales y arrays de mezcla están pre-asignados y reutilizados.
- **Sin LINQ ni Búsquedas por String**: Las transiciones, parámetros y huesos se resuelven mediante identificadores numéricos e índices directos.
- **Alineación Vectorial**: Matemáticas optimizadas con `System.Numerics` y tipos vectoriales de Prowl.
