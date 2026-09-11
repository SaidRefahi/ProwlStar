---
title: Métodos y Propiedades Esenciales
tags: [reference, api, methods, properties, quick-reference, snippets]
category: reference
updated: 2026-09-10
---

# 🔍 Métodos y Propiedades Esenciales en Prowl Engine

Esta guía rápida proporciona fragmentos de código listos para copiar y pegar con las llamadas de API más utilizadas en el desarrollo diario de mecánicas en Prowl Engine.

---

## 🧱 1. GameObjects y Componentes

```csharp
// Instanciar un GameObject o Prefab
GameObject clone = GameObject.Instantiate(prefabAsset.Res);
clone.Transform.Position = new Float3(0, 10, 0);

// Destruir un GameObject (inmediato o con retraso en segundos)
targetGo.Destroy();
bulletGo.Destroy(2.5f);

// Añadir un componente
Rigidbody3D rb = playerGo.AddComponent<Rigidbody3D>();

// Obtención de componentes con asignación segura (Zero Alloc)
if (playerGo.TryGetComponent<AudioSource>(out var audio))
{
    audio.Play();
}

// Búsqueda en ancestros o descendientes
Camera cam = playerGo.GetComponentInParent<Camera>();
MeshRenderer[] renderers = playerGo.GetComponentsInChildren<MeshRenderer>().ToArray();

// Comparación eficiente de etiquetas (Sin asignación en el Heap)
if (other.CompareTag("Player")) { ... }
```

---

## 📐 2. Transform y Matemáticas Espaciales

```csharp
using Prowl.Vector;

// Modificar posición y rotación
Transform.Position = new Float3(0, 5, 10); // Coordenadas globales de mundo
Transform.LocalPosition = new Float3(0, 0, 1); // Relativo al padre

// Rotación con Cuaterniones
Transform.Rotation = Quaternion.Euler(0, 90f, 0);

// Vectores direccionales
Float3 forward = Transform.Forward;
Float3 right = Transform.Right;
Float3 up = Transform.Up;

// Traslación y rotación continuas
Transform.Translate(forward * speed * (float)Time.deltaTime, isWorldSpace: true);
Transform.Rotate(new Float3(0, rotationSpeed * (float)Time.deltaTime, 0));

// Orientar frontal hacia un objetivo
Transform.LookAt(targetWorldPosition, Float3.Up);

// Transformación de coordenadas locales a mundo
Float3 worldPoint = Transform.TransformPoint(new Float3(0, 1.5f, 2.0f));
Float3 localPoint = Transform.InverseTransformPoint(worldTarget);
```

---

## ⏱️ 3. Métricas de Tiempo (Time)

```csharp
// Delta time entre cuadros de renderizado
float dt = (float)Time.deltaTime;

// Delta time fijo del bucle de física
float fixedDt = (float)Time.fixedDeltaTime;

// Tiempo total transcurrido desde el inicio del juego en segundos
float totalTime = (float)Time.time;

// Contador de fotogramas acumulados
long frames = Time.frameCount;

// Escala de tiempo (1.0 = normal, 0.5 = cámara lenta, 0.0 = pausa)
Time.timeScale = 0.5f;
```

---

## 🧊 4. Física y Raycasting (Zero GC)

```csharp
// Aplicar impulso físico a un Rigidbody3D
rb.AddForce(Float3.Up * jumpPower, ForceMode.Impulse);

// Aplicar fuerza continua en FixedUpdate
rb.AddForce(Transform.Forward * engineThrust, ForceMode.Force);

// Modificar velocidad lineal directamente
rb.LinearVelocity = new Float3(0, 10f, 0);

// Raycast lineal con máscara de capa
if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, 50f, layerMask))
{
    Debug.Log($"Impacto en: {hit.Point} con normal: {hit.Normal}");
}

// Raycast masivo reutilizando buffer estático (Zero GC)
private static readonly RaycastHit[] s_hits = new RaycastHit[16];
int count = Physics.RaycastNonAlloc(rayOrigin, rayDir, s_hits, 50f, layerMask);
for (int i = 0; i < count; i++)
{
    ref readonly RaycastHit h = ref s_hits[i];
    // Procesar impacto
}
```

---

## 🎮 5. Input Directo y Desacoplado

```csharp
// Consulta directa de teclas
bool isHoldingW = Input.GetKey(Key.W);
bool justPressedSpace = Input.GetKeyDown(Key.Space);

// Ratón
Int2 mousePos = Input.MousePosition;
Float2 mouseDelta = Input.MouseDelta;
bool leftClicked = Input.GetMouseButtonDown(0);

// Bloquear el cursor
Input.CursorLocked = true;
Input.CursorVisible = false;

// Lectura de InputAction desacoplada
Float2 moveVec = _moveAction.ReadValue<Float2>();
bool jumped = _jumpAction.WasPressedThisFrame();
```

---

## 🔊 6. Reproducción de Audio

```csharp
// Reproducir clip asignado
audioSource.Play();
audioSource.Stop();

// Disparar efecto puntual superpuesto (Zero Interruption)
audioSource.PlayOneShot(soundClip.Res, volumeScale: 0.75f);

// Control de volumen en el AudioMixer (Decibelios)
audioMixer.SetFloat("MasterVolume", -6.0f); // Mitad de volumen
audioMixer.SetFloat("MusicVolume", -80.0f); // Silencio
```

---

## 🗄️ 7. Assets y Recursos

```csharp
// Acceso transparente bajo demanda a un AssetRef
if (_textureRef.IsAvailable)
{
    Texture2D tex = _textureRef.Res; // Carga perezosa desde disco o memoria
}

// Resolución directa por GUID
EngineObject obj = AssetDatabase.Get(assetGuid);
```

---

## 🔗 Navegación Principal
- Regresar al índice general: [[🏠 Indice General (MOC)]].
- Comparativa completa con Unity: [[🔄 Comparativa y Migración desde Unity]].
- Hoja de clases: [[📚 Cheat Sheet de Clases Runtime]].
