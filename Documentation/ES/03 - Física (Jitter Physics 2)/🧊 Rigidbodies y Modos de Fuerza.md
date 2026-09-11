---
title: Rigidbodies y Modos de Fuerza
tags: [rigidbody, physics, jitter2, forces, forcemode, interpolation, velocity]
category: physics
updated: 2026-09-10
---

# 🧊 Rigidbodies y Modos de Fuerza en Prowl Engine

El componente [`Rigidbody3D`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Rigidbody3D.cs) dota a un `GameObject` de masa, inercia y capacidad de respuesta ante fuerzas físicas, gravedad y colisiones según las leyes de la mecánica clásica simuladas por **Jitter Physics 2**.

---

## 🏃 Tipos de Movimiento (MotionType)

Un `Rigidbody3D` puede operar en uno de tres modos:

1. **`MotionType.Dynamic`:**
   El cuerpo responde a todas las fuerzas, gravedad, torques y colisiones de otros objetos. Es el modo predeterminado para cajas, vehículos, pelotas o escombros.
2. **`MotionType.Kinematic`:**
   El cuerpo no es afectado por la gravedad ni por los impactos de otros objetos. Su movimiento es controlado exclusivamente por código o animaciones mediante `rb.LinearVelocity` o modificando su `Transform`. Sin embargo, puede empujar y aplastar cuerpos dinámicos. Ideal para plataformas móviles, puertas y ascensores.
3. **`MotionType.Static`:**
   El cuerpo tiene masa infinita y no se mueve jamás. Se utiliza para paredes, edificios y suelos.

---

## 🎛️ Propiedades Clave de Rigidbody3D

| Propiedad | Tipo | Descripción |
| :--- | :--- | :--- |
| `Mass` | `float` | Masa del objeto en kilogramos (predeterminado: 1.0 kg). |
| `UseGravity` | `bool` | Determina si el cuerpo es acelerado por la gravedad del mundo. |
| `LinearDamping` | `float` | Resistencia del aire al movimiento lineal (frena la velocidad con el tiempo). |
| `AngularDamping` | `float` | Resistencia del aire al giro y rotación. |
| `Friction` | `float` | Coeficiente de fricción o rozamiento superficial (0 = hielo resbaladizo, 1 = goma rugosa). |
| `Restitution` | `float` | Coeficiente de restitución o rebote elástico (0 = plastilina sin rebote, 1 = pelota súper elástica). |
| `LinearVelocity` | `Float3` | Velocidad lineal instantánea en espacio de mundo ($m/s$). |
| `AngularVelocity`| `Float3` | Velocidad angular instantánea en radianes por segundo. |
| `Interpolation` | `RigidbodyInterpolation` | Suavizado visual entre pasos de física (`None`, `Interpolate`, `Extrapolate`). |
| `Constraints` | `RigidbodyConstraints` | Bloqueo de ejes de posición o rotación (`FreezePositionY`, `FreezeRotationX`, etc.). |

---

## 🚀 Modos de Aplicación de Fuerza (`ForceMode`)

Al invocar `AddForce`, el parámetro [`ForceMode`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Physics/ForceMode.cs) define cómo se interpreta matemáticamente el vector:

| Modo de Fuerza | ¿Depende de la Masa? | ¿Depende de $\Delta t$? | Uso Común |
| :--- | :---: | :---: | :--- |
| **`ForceMode.Force`** | **Sí** ($a = F / m$) | **Sí** ($v += a \cdot \Delta t$) | Fuerzas continuas: propulsión de cohetes, motores, empuje de viento en `FixedUpdate`. |
| **`ForceMode.Acceleration`** | **No** | **Sí** ($v += a \cdot \Delta t$) | Aceleraciones continuas independientes del peso: campos de gravedad personalizados. |
| **`ForceMode.Impulse`** | **Sí** ($\Delta v = J / m$) | **No** (Instantáneo) | Golpes o impactos instantáneos: explosiones, disparos, saltos de personajes. |
| **`ForceMode.VelocityChange`** | **No** | **No** (Instantáneo) | Cambio directo de velocidad ignorando masa: saltos arcade, teletransporte de velocidad. |

---

## 💻 Ejemplos Prácticos en C#

### 1. Salto y Propulsión Física
```csharp
using Prowl.Runtime;
using Prowl.Vector;

public class BallPhysicsController : MonoBehaviour
{
    [SerializeField] private float _jumpImpulse = 10.0f;
    [SerializeField] private float _engineForce = 25.0f;

    private Rigidbody3D _rb;
    private bool _jumpRequested;

    public override void Awake()
    {
        base.Awake();
        TryGetComponent(out _rb);
    }

    public override void Update()
    {
        base.Update();
        // El input se captura en Update
        if (Input.GetKeyDown(Key.Space))
        {
            _jumpRequested = true;
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // 1. Aplicar fuerza continua hacia adelante
        Float3 forwardForce = Transform.Forward * _engineForce;
        _rb.AddForce(forwardForce, ForceMode.Force);

        // 2. Aplicar impulso instantáneo de salto
        if (_jumpRequested)
        {
            _rb.AddForce(Float3.Up * _jumpImpulse, ForceMode.Impulse);
            _jumpRequested = false;
        }
    }
}
```

### 2. Aplicar Fuerza en un Punto Descentralizado (Genera Torque)
```csharp
public class DestructibleObject : MonoBehaviour
{
    private Rigidbody3D _rb;

    public void ReceiveImpact(Float3 hitPoint, Float3 hitDirection, float power)
    {
        if (TryGetComponent(out _rb))
        {
            // Aplica fuerza lineal y torque rotacional simultáneo
            _rb.AddForceAtPosition(hitDirection * power, hitPoint, ForceMode.Impulse);
        }
    }
}
```

---

## 📺 Interpolación Visual: Adiós a los Tirones

Dado que la física se ejecuta a una frecuencia fija (p. ej., 50 Hz) y los monitores modernos refrescan a 60, 144 o 240 Hz, los objetos físicos pueden verse entrecortados si no se interpolan.

- **`RigidbodyInterpolation.Interpolate`:**
  Interpola suavemente la posición visual de la malla entre las dos últimas poses físicas calculadas. Es la opción recomendada para el jugador principal y vehículos.
- **`RigidbodyInterpolation.None`:**
  Opción de menor costo computacional, ideal para objetos secundarios o escombros pequeños.

---

## 🔗 Temas Relacionados
- Configuración del mundo físico: [[🌍 PhysicsWorld y Configuración]].
- Colisionadores y formas: [[📐 Colliders (Primitivas, Mesh, Terreno)]].
- Restricciones y uniones mecánicas: [[🔗 Joints y Restricciones (Constraints)]].
