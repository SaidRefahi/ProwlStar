---
title: Essential Methods & Properties
tags: [reference, api, methods, properties, quick-reference, snippets]
category: reference
updated: 2026-09-10
---

# 🔍 Essential Methods & Properties in Prowl Engine

This quick reference guide provides copy-paste ready C# code snippets covering the most ubiquitous API calls in daily gameplay programming with Prowl Engine.

---

## 🧱 1. GameObjects & Components

```csharp
// Instantiate a GameObject or Prefab instance
GameObject clone = GameObject.Instantiate(prefabAsset.Res);
clone.Transform.Position = new Float3(0, 10, 0);

// Destroy a GameObject (instantaneous or with delay in seconds)
targetGo.Destroy();
bulletGo.Destroy(2.5f);

// Attach a component
Rigidbody3D rb = playerGo.AddComponent<Rigidbody3D>();

// Zero-allocation component lookup
if (playerGo.TryGetComponent<AudioSource>(out var audio))
{
    audio.Play();
}

// Hierarchical queries
Camera cam = playerGo.GetComponentInParent<Camera>();
MeshRenderer[] renderers = playerGo.GetComponentsInChildren<MeshRenderer>().ToArray();

// High-speed tag comparison (Zero heap allocation)
if (other.CompareTag("Player")) { ... }
```

---

## 📐 2. Transform & Spatial Vector Math

```csharp
using Prowl.Vector;

// World and Local position modification
Transform.Position = new Float3(0, 5, 10); // Absolute world coordinates
Transform.LocalPosition = new Float3(0, 0, 1); // Relative to parent

// Quaternion orientation
Transform.Rotation = Quaternion.Euler(0, 90f, 0);

// Directional unit vectors
Float3 forward = Transform.Forward;
Float3 right = Transform.Right;
Float3 up = Transform.Up;

// Continuous motion
Transform.Translate(forward * speed * (float)Time.deltaTime, isWorldSpace: true);
Transform.Rotate(new Float3(0, rotationSpeed * (float)Time.deltaTime, 0));

// Orient heading toward target
Transform.LookAt(targetWorldPosition, Float3.Up);

// Coordinate space transformations
Float3 worldPoint = Transform.TransformPoint(new Float3(0, 1.5f, 2.0f));
Float3 localPoint = Transform.InverseTransformPoint(worldTarget);
```

---

## ⏱️ 3. Execution Time Metrics (Time)

```csharp
// Delta duration between rendered frames
float dt = (float)Time.deltaTime;

// Fixed delta timestep between physics simulation ticks
float fixedDt = (float)Time.fixedDeltaTime;

// Elapsed game runtime in seconds
float totalTime = (float)Time.time;

// Monotonic frame counter
long frames = Time.frameCount;

// Time dilation scale (1.0 = standard, 0.5 = slow motion, 0.0 = paused)
Time.timeScale = 0.5f;
```

---

## 🧊 4. 3D Physics & Raycasting (Zero GC)

```csharp
// Apply physical impulse to Rigidbody3D
rb.AddForce(Float3.Up * jumpPower, ForceMode.Impulse);

// Apply continuous force in FixedUpdate
rb.AddForce(Transform.Forward * engineThrust, ForceMode.Force);

// Direct linear velocity assignment
rb.LinearVelocity = new Float3(0, 10f, 0);

// Single raycast against layer mask
if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, 50f, layerMask))
{
    Debug.Log($"Hit surface at {hit.Point} with normal {hit.Normal}");
}

// Bulk raycast using preallocated buffer (Zero GC)
private static readonly RaycastHit[] s_hits = new RaycastHit[16];
int count = Physics.RaycastNonAlloc(rayOrigin, rayDir, s_hits, 50f, layerMask);
for (int i = 0; i < count; i++)
{
    ref readonly RaycastHit h = ref s_hits[i];
    // Process contact
}
```

---

## 🎮 5. Direct and Action-Based Input

```csharp
// Direct hardware polling
bool isHoldingW = Input.GetKey(Key.W);
bool justPressedSpace = Input.GetKeyDown(Key.Space);

// Mouse input
Int2 mousePos = Input.MousePosition;
Float2 mouseDelta = Input.MouseDelta;
bool leftClicked = Input.GetMouseButtonDown(0);

// Confine cursor
Input.CursorLocked = true;
Input.CursorVisible = false;

// Decoupled InputAction reading
Float2 moveVec = _moveAction.ReadValue<Float2>();
bool jumped = _jumpAction.WasPressedThisFrame();
```

---

## 🔊 6. Audio Playback

```csharp
// Playback active clip
audioSource.Play();
audioSource.Stop();

// One-shot audio trigger (Zero channel interruption)
audioSource.PlayOneShot(soundClip.Res, volumeScale: 0.75f);

// Sub-bus decibel level modulation
audioMixer.SetFloat("MasterVolume", -6.0f); // Half amplitude
audioMixer.SetFloat("MusicVolume", -80.0f); // Muted
```

---

## 🗄️ 7. Assets & Resource Ingestion

```csharp
// Transparent on-demand dereferencing
if (_textureRef.IsAvailable)
{
    Texture2D tex = _textureRef.Res; // Lazy-loaded from storage or cache
}

// Direct GUID resolution
EngineObject obj = AssetDatabase.Get(assetGuid);
```

---

## 🔗 Main Navigation
- Master table of contents: [[🏠 Master Index (MOC)]].
- Unity migration guide: [[🔄 Unity Migration & Comparison Guide]].
- Class reference: [[📚 Runtime Classes Cheat Sheet]].
