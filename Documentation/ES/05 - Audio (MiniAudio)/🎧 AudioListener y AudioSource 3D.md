---
title: AudioListener y AudioSource 3D
tags: [audiosource, audiolistener, 3d-audio, spatialization, doppler, miniaudio]
category: audio
updated: 2026-09-10
---

# 🎧 AudioListener y AudioSource 3D en Prowl Engine

Para crear una atmósfera sonora creíble en un entorno tridimensional, el motor debe calcular la atenuación de volumen con la distancia, la panorámica estéreo basada en la orientación y la variación de tono por movimiento (**Efecto Doppler**).

Prowl Engine resuelve esto mediante dos componentes complementarios:
1. **[`AudioListener`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Audio/AudioListener.cs):** Representa los oídos virtuales del jugador en el mundo.
2. **[`AudioSource`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Audio/AudioSource.cs):** Representa un emisor de sonido ubicado en un punto del espacio 3D o en modo plano 2D.

---

## 👂 1. AudioListener (El Receptor)

El componente `AudioListener` debe colocarse en el objeto que represente la cabeza del jugador o en la [`Camera`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Camera.cs) principal activa.

### Comportamiento:
- En cada fotograma, sincroniza su posición de mundo (`Transform.Position`) y sus vectores direccionales (`Transform.Forward` y `Transform.Up`) con el motor MiniAudio.
- Solo debe haber **un único AudioListener activo** por escena. Si hay más de uno, el motor tomará el primero encontrado.

---

## 📢 2. AudioSource (El Emisor)

`AudioSource` es el componente que reproduce clips de audio ([`AudioClip`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/AudioClip.cs)):

### Propiedades Principales:
| Propiedad | Tipo | Descripción |
| :--- | :--- | :--- |
| `Clip` | `AssetRef<AudioClip>` | El archivo de audio asignado. |
| `Volume` | `float` | Amplitud lineal de salida (0.0f silencio total a 1.0f volumen normal). |
| `Pitch` | `float` | Tono y velocidad de reproducción (1.0f = normal, 2.0f = doble velocidad / una octava más aguda). |
| `Loop` | `bool` | Si está activo, el clip se reinicia automáticamente al llegar al final. |
| `PlayOnAwake` | `bool` | Inicia la reproducción inmediatamente en el momento en que se carga el objeto. |
| `Spatialize` | `bool` | `true` activa el cálculo 3D posicional. `false` reproduce en estéreo directo 2D (música de fondo o UI). |
| `MinDistance` | `float` | Distancia hasta la cual el volumen se mantiene al 100% sin decaimiento. |
| `MaxDistance` | `float` | Distancia máxima más allá de la cual el sonido deja de escucharse por completo. |
| `RolloffMode` | `AudioRolloffMode` | Modelo matemático de caída (`Logarithmic` o `Linear`). |
| `DopplerFactor` | `float` | Factor de escala para el desplazamiento de frecuencia por velocidad relativa. |

---

## 💻 Ejemplos de Reproducción en C#

### 1. Reproducción de Sonidos Únicos con `PlayOneShot`
Para sonidos rápidos que pueden ocurrir varias veces en rápida sucesión (como disparos de ametralladora o pisadas) sin cortar el sonido anterior:

```csharp
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

public class FootstepSystem : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AssetRef<AudioClip> _footstepWood;
    [SerializeField] private AssetRef<AudioClip> _footstepStone;

    public void PlayFootstep(bool onWood)
    {
        AssetRef<AudioClip> clip = onWood ? _footstepWood : _footstepStone;
        if (!clip.IsAvailable) return;

        // Variación sutil de pitch para evitar monotonía sonora (±10%)
        _audioSource.Pitch = 1.0f + ((Random.Shared.NextSingle() - 0.5f) * 0.2f);

        // PlayOneShot mezcla el clip sin reiniciar la fuente
        _audioSource.PlayOneShot(clip.Res, volumeScale: 0.8f);
    }
}
```

### 2. Sonido Posicional 3D en Entornos (Fuego de Hoguera)
```csharp
public class CampfireAudio : MonoBehaviour
{
    public override void Awake()
    {
        base.Awake();

        if (TryGetComponent<AudioSource>(out var src))
        {
            src.Spatialize = true; // Activar espacialización 3D
            src.Loop = true;
            src.MinDistance = 2.0f;  // Se escucha al 100% hasta a 2 metros
            src.MaxDistance = 25.0f; // Silencio total a partir de 25 metros
            src.Play();
        }
    }
}
```

---

## 🌪️ Simulación del Efecto Doppler

Cuando una sirena de ambulancia o un misil a alta velocidad pasa rozando al jugador:
- Si el emisor se acerca rápidamente al `AudioListener`, la longitud de onda sonora percibida se comprime, elevando el tono (*Pitch* más agudo).
- Al alejarse, la longitud de onda se expande, provocando una caída pronunciada del tono (*Pitch* grave).
- Prowl calcula este efecto automáticamente basándose en la velocidad de los cuerpos rígidos (`Rigidbody3D.LinearVelocity`) o en la derivada de posición de los `Transforms`.

---

## 🔗 Temas Relacionados
- Backend de audio: [[🔊 Motor de Audio y AudioContext]].
- Ruteo a buses de mezcla: [[🎛️ AudioMixer y Snapshots]].
- Procesamiento y reverberación: [[🧪 Efectos de Audio DSP]].
