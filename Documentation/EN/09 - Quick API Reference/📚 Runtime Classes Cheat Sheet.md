---
title: Runtime Classes Cheat Sheet
tags: [cheatsheet, api, reference, runtime, classes, namespaces]
category: reference
updated: 2026-09-10
---

# 📚 Runtime Classes Cheat Sheet in Prowl Engine

This reference summarizes the most critical classes, structs, and interfaces across [`Prowl.Runtime`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime), noting their primary namespace and engine responsibilities:

---

## 🏛️ Core & Scene Graph Architecture

| Class / Type | Namespace | Core Responsibility |
| :--- | :--- | :--- |
| [`GameObject`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/GameObject.cs) | `Prowl.Runtime` | Base scene entity container holding components. |
| [`MonoBehaviour`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/MonoBehaviour.cs) | `Prowl.Runtime` | Base class for user behaviors and lifecycle hooks. |
| [`Transform`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Math/Transform.cs) | `Prowl.Vector` | 3D position, rotation, scale, and hierarchy parent-child tree. |
| [`Scene`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Scene.cs) | `Prowl.Runtime.Resources` | Complete scene container representing a level or world. |
| [`Prefab`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Prefab.cs) | `Prowl.Runtime.Resources` | Serialized template for entity instantiation. |
| [`Time`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Time.cs) | `Prowl.Runtime` | Frame timing metrics (`deltaTime`, `time`, `frameCount`). |
| [`Debug`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Debug.cs) | `Prowl.Runtime` | Console logging facade (`Log`, `LogWarning`, `LogError`). |

---

## 🧮 Math & Geometric Primitives

| Type | Namespace | Description |
| :--- | :--- | :--- |
| `Float2` | `Prowl.Vector` | 2D vector for screenspace, UVs, and planar inputs. |
| `Float3` | `Prowl.Vector` | 3D vector for world positions, velocities, and normals. |
| `Float4` | `Prowl.Vector` | 4D vector for RGBA colors and homogeneous coordinates. |
| `Quaternion` | `Prowl.Vector` | Continuous 3D orientation without Gimbal Lock. |
| `Float4x4` | `Prowl.Vector` | Affine transformation matrix for view/projection/model. |
| `Ray` | `Prowl.Vector.Geometry` | Directional spatial ray `(origin, direction)`. |
| `Bounds` | `Prowl.Vector.Geometry` | Axis-Aligned Bounding Box (AABB). |

---

## 🎨 Graphics & Rendering Pipeline

| Class | Namespace | Description |
| :--- | :--- | :--- |
| [`Camera`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Camera.cs) | `Prowl.Runtime` | Viewpoint calculating view and projection matrices. |
| [`MeshRenderer`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/MeshRenderer.cs) | `Prowl.Runtime` | Submits static mesh geometry with assigned materials. |
| [`SkinnedMeshRenderer`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/SkinnedMeshRenderer.cs) | `Prowl.Runtime` | Submits animated skeletal deforming meshes. |
| [`DirectionalLight`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Lights/DirectionalLight.cs) | `Prowl.Runtime` | Celestial light source with cascaded shadow mapping. |
| [`PointLight`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Lights/PointLight.cs) | `Prowl.Runtime` | Spherical point light source accelerated by LightBVH. |
| [`SpotLight`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Lights/SpotLight.cs) | `Prowl.Runtime` | Conical spotlight source. |
| [`Material`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Material.cs) | `Prowl.Runtime.Resources` | Shader parameter instance holding textures and uniforms. |
| [`Shader`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Shader.cs) | `Prowl.Runtime.Resources` | Compiled GPU shading program. |
| [`Texture2D`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Texture2D.cs) | `Prowl.Runtime.Resources` | 2D GPU image buffer. |
| [`RenderTexture`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/RenderTexture.cs) | `Prowl.Runtime.Resources` | Off-screen target buffer for post-processing and cameras. |

---

## 🧊 3D Physics (Jitter Physics 2)

| Class | Namespace | Description |
| :--- | :--- | :--- |
| [`Rigidbody3D`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Rigidbody3D.cs) | `Prowl.Runtime` | Dynamic, kinematic, or static physical body. |
| [`BoxCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Colliders/BoxCollider.cs) | `Prowl.Runtime` | Analytical cuboid collider. |
| [`SphereCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Colliders/SphereCollider.cs) | `Prowl.Runtime` | Ultra-fast spherical collider. |
| [`CapsuleCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Colliders/CapsuleCollider.cs) | `Prowl.Runtime` | Capsule shape for character avatars. |
| [`MeshCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Colliders/MeshCollider.cs) | `Prowl.Runtime` | Concave static mesh or dynamic convex hull collider. |
| [`TerrainCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/TerrainCollider.cs) | `Prowl.Runtime` | Fast heightmap surface collider. |
| [`WheelCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/WheelCollider.cs) | `Prowl.Runtime` | Raycast vehicle wheel with suspension spring and tire slip. |
| [`CharacterController`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/CharacterController.cs) | `Prowl.Runtime` | Kinematic capsule controller handling stairs and slopes. |
| [`Physics`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Physics/PhysicsWorld.cs) | `Prowl.Runtime` | Static facade for Raycasts, ShapeCasts, and overlap queries. |

---

## 🔊 Audio Subsystem (MiniAudio)

| Class | Namespace | Description |
| :--- | :--- | :--- |
| [`AudioSource`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Audio/AudioSource.cs) | `Prowl.Runtime` | 3D spatialized or 2D stereo sound emitter. |
| [`AudioListener`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Audio/AudioListener.cs) | `Prowl.Runtime` | Virtual microphone receiving acoustic signals. |
| [`AudioClip`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/AudioClip.cs) | `Prowl.Runtime.Resources` | Memory-resident or streaming audio stream (WAV, MP3, OGG, FLAC). |
| [`AudioMixer`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Audio/AudioMixer.cs) | `Prowl.Runtime.Audio` | Hierarchical sub-mix routing with decibel level control. |

---

## 🖼️ User Interface (UI)

| Class | Namespace | Description |
| :--- | :--- | :--- |
| [`GameCanvas`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/GameCanvas.cs) | `Prowl.Runtime` | Root canvas managing layout projection and batch trees. |
| [`RectTransform`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/RectTransform.cs) | `Prowl.Runtime.UI` | 2D responsive rectangular bounding transform. |
| [`UIImage`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/UIImage.cs) | `Prowl.Runtime.UI` | Sprite renderer supporting 9-slice and radial fill. |
| [`TextComponent`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/TextComponent.cs) | `Prowl.Runtime.UI` | Vector font renderer with Rich Text markup. |
| [`UIButton`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/UIButton.cs) | `Prowl.Runtime.UI` | Clickable button with state color transitions. |
| [`UISlider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/UISlider.cs) | `Prowl.Runtime.UI` | Continuous or discrete integer range slider. |
| [`UIInputField`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/UIInputField.cs) | `Prowl.Runtime.UI` | Interactive text field supporting caret editing. |
| [`UIScrollRect`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/UIScrollRect.cs) | `Prowl.Runtime.UI` | Scrolling viewport container with elastic inertia. |
| [`EventSystem`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/EventSystem.cs) | `Prowl.Runtime.UI` | Central pointer and gamepad event router. |

---

## 🗄️ Asset Pipeline & Serialization

| Class | Namespace | Description |
| :--- | :--- | :--- |
| [`AssetDatabase`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs) | `Prowl.Runtime` | Central asset registry indexed by 128-bit GUIDs. |
| [`AssetRef<T>`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetRef.cs) | `Prowl.Runtime` | Smart typed reference supporting transparent lazy-loading. |
| `Serializer` | `Prowl.Echo` | Binary and text serialization engine with deep cloning. |

---

## 🔗 Related Topics
- Essential syntax and properties: [[🔍 Essential Methods & Properties]].
- Unity migration: [[🔄 Unity Migration & Comparison Guide]].
- Performance guidelines: [[🎯 Best Practices & Performance (Zero GC)]].
