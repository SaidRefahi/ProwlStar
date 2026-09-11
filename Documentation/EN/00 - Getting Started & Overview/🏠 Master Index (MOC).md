---
title: Master Index (MOC) - Prowl Engine Documentation
tags: [moc, index, architecture, documentation]
category: overview
updated: 2026-09-10
---

# 🎮 Official Prowl Engine Documentation Vault

Welcome to the comprehensive technical documentation for **Prowl Engine** (ProwlStar), a modern, high-performance, open-source game engine developed entirely in **pure C# (.NET 10)** with an architecture and workflow heavily inspired by Unity.

This Obsidian vault is structured as an interconnected knowledge graph using wikilinks (`[[Note]]`). Every note breaks down engine concepts, source code implementation, zero-GC performance guidelines, and direct comparisons with commercial engines.

---

## 🗺️ Content Map (MOC)

### 00. [[🧭 What is Prowl Engine|Getting Started & Overview]]
- [[🧭 What is Prowl Engine]]: Design philosophy, modular architecture, and technical capabilities.
- [[🔄 Unity Migration & Comparison Guide]]: Direct equivalencies table, API differences, and migration walkthrough.
- [[⚡ Core Architecture (Runtime vs Editor)]]: Complete decoupling between `Prowl.Runtime` and `Prowl.Editor`.

### 01. [[⏱️ Lifecycle & Game Loop|Scripting & Lifecycle]]
- [[⏱️ Lifecycle & Game Loop]]: Event dispatching via `SceneDispatcher`, execution order (`Awake`, `OnEnable`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`).
- [[🧱 GameObjects & Components]]: Composition model, tags, layers, and dynamic manipulation.
- [[🧬 MonoBehaviour in Prowl]]: Behavioral base class, virtual callbacks vs magic methods.
- [[📐 Transform & Hierarchies]]: Matrix coordinate system (`Float3`, `Quaternion`, `Float4x4`) and parent-child hierarchies.
- [[📦 Prefabs & Override System]]: Instantiation, delta tracking, and nested prefabs.
- [[🎯 Best Practices & Performance (Zero GC)]]: Extreme optimization in critical paths, static buffers, and eliminating loop allocations.

### 02. [[🎨 Rendering Pipeline (DefaultRenderPipeline)|Graphics & Rendering]]
- [[🎨 Rendering Pipeline (DefaultRenderPipeline)]]: Default pipeline architecture, stages, and render passes.
- [[📷 Cameras & RenderContext]]: Projection setup, viewports, frustum culling, and viewers.
- [[💡 Lighting, Shadows & LightBVH]]: Spatial BVH acceleration for hundreds of dynamic lights and shadow atlases.
- [[🔮 Materials, Shaders & PropertyState]]: Modular shaders, uniform uploads, and BRDF LUT generation.
- [[🪞 Light Probes & Spherical Harmonics]]: Environment capture and L2 spherical harmonics for indirect lighting.
- [[✨ Post-Processing & SMAA]]: Subpixel Morphological Anti-Aliasing and final composition.
- [[💨 Instanced & Skinned Mesh Rendering]]: Mass GPU instancing and skeletal deformation.

### 03. [[🌍 PhysicsWorld & Configuration|Physics (Jitter Physics 2)]]
- [[🌍 PhysicsWorld & Configuration]]: Jitter 2 simulation engine, substepping, and fixed timesteps.
- [[🧊 Rigidbodies & Force Modes]]: Dynamic, kinematic, and static rigidbodies (`Rigidbody3D`).
- [[📐 Colliders (Primitives, Mesh, Terrain)]]: Boxes, spheres, capsules, cylinders, cones, arbitrary meshes, and heightmap terrains.
- [[🔗 Joints & Constraints]]: BallSocket, Hinge, Prismatic, Universal, and linear/angular motors.
- [[🚗 WheelCollider & Vehicles]]: Realistic vehicle raycast wheels with suspension and slip-grip curves.
- [[🚶 CharacterController]]: Kinematic controller for smooth character movement, slopes, and stepping.
- [[🎯 Raycasting & Shape Queries]]: Optimized spatial queries and collision layer filtering (`LayerMask`).

### 04. [[🎮 Input System Architecture|Input System]]
- [[🎮 Input System Architecture]]: Decoupled action architecture (`InputAction`), schemes, and handlers.
- [[🗺️ Input Action Maps & Bindings]]: Action maps, 2D composite bindings, and multi-device support.
- [[🕹️ Processors & Composites]]: Input modifiers, deadzones, and sensitivity curves.
- [[💻 Character Controller Input Examples]]: Practical implementation of first-person and third-person controllers.

### 05. [[🔊 Audio Engine & AudioContext|Audio (MiniAudio)]]
- [[🔊 Audio Engine & AudioContext]]: Cross-platform native MiniAudio backend.
- [[🎧 AudioListener & 3D AudioSource]]: 3D sound spatialization, attenuation curves, and Doppler effect.
- [[🎛️ AudioMixer & Snapshots]]: Channel routing, bus hierarchies, and dynamic snapshot transitions.
- [[🧪 DSP Audio Effects]]: Real-time reverb, delay, EQ, and biquad filters.

### 06. [[🖼️ Game Canvas UI System|UI & Canvas]]
- [[🖼️ Game Canvas UI System]]: `GameCanvas` in screen space and world space.
- [[📐 RectTransform & Layouts]]: Anchors, pivots, padding, and responsive layout distribution.
- [[🔘 UI Components (Button, Slider, InputField, ScrollRect)]]: Interactive widgets and visual transitions.
- [[🖱️ EventSystem & UIRaycaster]]: UI raycasting, focus control, and gamepad/keyboard navigation.
- [[🖋️ Editor UI Internals (Paper, Origami, Quill)]]: Immediate mode and vector rendering systems powering the editor.

### 07. [[🗄️ AssetDatabase & AssetRef|Assets, Serialization & Project]]
- [[🗄️ AssetDatabase & AssetRef]]: GUID-based indexing, lazy loading (`AssetRef<T>`), and idle eviction.
- [[📜 Serialization with Prowl.Echo]]: High-speed binary/text serialization format without Unity YAML overhead.
- [[📥 Asset Importers (Models, Textures, Audio)]]: Asset ingestion pipeline and `.meta` metadata.
- [[⚙️ Project Settings & Tags-Layers]]: Global project settings, tags, and physics layer masks.
- [[📦 Build System & Desktop Player]]: Standalone packaging and multiplatform distribution via `Players/Desktop`.

### 08. [[🛠️ Prowl.Editor Architecture|Editor Extensibility]]
- [[🛠️ Prowl.Editor Architecture]]: Dockable panel system, edit/play loops, and selection system.
- [[🎛️ Custom Editors & Inspector Customization]]: `[CustomEditor]` attribute and custom property drawers.
- [[🪄 Scene View Editors & Gizmos]]: Scene visual tools and interactive 3D handles.
- [[🪟 Custom Editor Panels]]: Creating modular docked panels with persistent layouts.
- [[⚡ Roslyn Hot Reload & Live Compilation]]: Live C# script compilation and state transfer without restarting the engine.

### 09. [[📚 Runtime Classes Cheat Sheet|Quick API Reference]]
- [[📚 Runtime Classes Cheat Sheet]]: Tabular overview of classes, namespaces, and core methods.
- [[🔍 Essential Methods & Properties]]: Fast-lookup reference for daily development.

### 10. [[🎬 Animation System & Animator|Animation System]]
- [[🎬 Animation System & Animator]]: Native skeletal animation pipeline, `SkeletonAsset`, `Animator`, Blend Trees, Layers, Humanoid Retargeting, Two-Bone IK, and Root Motion.

