---
title: Índice General (MOC) - Documentación de Prowl Engine
tags: [moc, index, architecture, documentation]
category: overview
updated: 2026-09-10
---

# 🎮 Bóveda de Documentación Oficial de Prowl Engine

Bienvenido a la documentación técnica integral de **Prowl Engine** (ProwlStar), un motor de videojuegos moderno, open-source y de alto rendimiento escrito en **C# puro (.NET 10)** con una arquitectura conceptual y flujo de trabajo fuertemente inspirado en Unity.

Esta bóveda de Obsidian está estructurada como una red de conocimiento interconectada mediante wikilinks (`[[Nota]]`). Cada documento desglosa la teoría, implementación en código fuente, buenas prácticas de rendimiento y diferencias clave con motores comerciales.

---

## 🗺️ Mapa de Contenidos (MOC)

### 00. [[🧭 Qué es Prowl Engine|Inicio & Visión General]]
- [[🧭 Qué es Prowl Engine]]: Filosofía de diseño, arquitectura modular y capacidades técnicas.
- [[🔄 Comparativa y Migración desde Unity]]: Tabla de equivalencias directas, diferencias de API y guía de migración paso a paso.
- [[⚡ Arquitectura General (Runtime vs Editor)]]: Desacoplamiento total entre `Prowl.Runtime` y `Prowl.Editor`.

### 01. [[⏱️ Ciclo de Vida y Game Loop|Scripting & Ciclo de Vida]]
- [[⏱️ Ciclo de Vida y Game Loop]]: Despacho de eventos vía `SceneDispatcher`, orden de ejecución (`Awake`, `OnEnable`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`).
- [[🧱 GameObjects y Componentes]]: Modelo de composición, tags, capas y manipulación dinámica.
- [[🧬 MonoBehaviour en Prowl]]: Clase base de comportamiento, callbacks virtuales vs métodos mágicos.
- [[📐 Transform y Jerarquías]]: Sistema de coordenadas matriciales (`Float3`, `Quaternion`, `Float4x4`) y jerarquías padre-hijo.
- [[📦 Prefabs y Sistema de Overrides]]: Instanciación, tracking de deltas y prefabs anidados.
- [[🎯 Buenas Prácticas y Rendimiento (Zero GC)]]: Optimización extrema en rutas críticas, buffers estáticos y eliminación de allocs en loops.

### 02. [[🎨 Pipeline de Renderizado (DefaultRenderPipeline)|Gráficos y Renderizado]]
- [[🎨 Pipeline de Renderizado (DefaultRenderPipeline)]]: Arquitectura del pipeline por defecto, etapas y render passes.
- [[📷 Cámaras y RenderContext]]: Configuración de proyección, viewport, culling y viewers.
- [[💡 Iluminación, Sombras y LightBVH]]: Aceleración espacial BVH para cientos de luces dinámicas y atlas de sombras.
- [[🔮 Materiales, Shaders y PropertyState]]: Shaders modulares, paso de uniformes y generación de tablas BRDF.
- [[🪞 Probes de Luz y Spherical Harmonics]]: Captura ambiental y coeficientes L2 para iluminación indirecta.
- [[✨ Efectos de Post-procesamiento y SMAA]]: Anti-aliasing subpixel morfológico y composición final.
- [[💨 Instanced & Skinned Mesh Rendering]]: Renderizado masivo por instancias GPU y deformación esquelética.

### 03. [[🌍 PhysicsWorld y Configuración|Física (Jitter Physics 2)]]
- [[🌍 PhysicsWorld y Configuración]]: Motor de simulación Jitter 2, substepping y paso temporal fijo.
- [[🧊 Rigidbodies y Modos de Fuerza]]: Cuerpos rígidos dinámicos, cinemáticos y estáticos (`Rigidbody3D`).
- [[📐 Colliders (Primitivas, Mesh, Terreno)]]: Cajas, esferas, cápsulas, cilindros, conos, mallas y altura de terrenos.
- [[🔗 Joints y Restricciones (Constraints)]]: BallSocket, Hinge, Prismatic, Universal y motores angulares/lineales.
- [[🚗 WheelCollider y Vehículos]]: Física realista de ruedas de vehículos con suspensión y curvas de agarre/deriva.
- [[🚶 CharacterController]]: Controlador cinemático para personajes, pendientes y escalones.
- [[🎯 Raycasting y Shape Queries]]: Consultas espaciales optimizadas y filtrado por capas (`LayerMask`).

### 04. [[🎮 Arquitectura del Input System|Sistema de Input]]
- [[🎮 Arquitectura del Input System]]: Acciones desacopladas (`InputAction`), esquemas y handlers.
- [[🗺️ Input Action Maps y Bindings]]: Configuración de mapas, composiciones 2D y soporte multiplataforma.
- [[🕹️ Procesadores y Composites]]: Modificadores de entrada, deadzones y sensibilidad.
- [[💻 Ejemplos de Control de Personajes]]: Implementación práctica de controladores en primera y tercera persona.

### 05. [[🔊 Motor de Audio y AudioContext|Audio (MiniAudio)]]
- [[🔊 Motor de Audio y AudioContext]]: Backend nativo multiplataforma de MiniAudio.
- [[🎧 AudioListener y AudioSource 3D]]: Espacialización sonora 3D, atenuación y Doppler.
- [[🎛️ AudioMixer y Snapshots]]: Ruteo por canales, buses y transiciones dinámicas.
- [[🧪 Efectos de Audio DSP]]: Reverb, delay, ecualización y filtros biquad en tiempo real.

### 06. [[🖼️ Sistema UI para Juegos (GameCanvas)|UI y Canvas]]
- [[🖼️ Sistema UI para Juegos (GameCanvas)]]: `GameCanvas` en espacio de pantalla y mundo.
- [[📐 RectTransform y Layouts]]: Anclas, pivotes, padding y sistemas de distribución responsiva.
- [[🔘 Componentes UI (Button, Slider, InputField, ScrollRect)]]: Controles interactivos y transiciones visuales.
- [[🖱️ EventSystem y UIRaycaster]]: Raycasting en UI, foco y navegación por teclado/gamepad.
- [[🖋️ UI Interna del Editor (Paper, Origami, Quill)]]: Sistema de renderizado inmediato y vectorial del editor.

### 07. [[🗄️ AssetDatabase y AssetRef|Assets, Serialización y Proyecto]]
- [[🗄️ AssetDatabase y AssetRef]]: Identificación por GUID, carga perezosa (`AssetRef<T>`) y desalojo por inactividad.
- [[📜 Serialización con Prowl.Echo]]: Formato de serialización binario y legible ultra veloz sin overhead de YAML.
- [[📥 Importadores de Assets (Modelos, Texturas, Audio)]]: Pipeline de ingestión y metadatos.
- [[⚙️ Project Settings y Tags-Layers]]: Configuración global del proyecto, tags y capas de colisión.
- [[📦 Build System y Exportación Standalone (Desktop Player)]]: Empaquetado y distribución multiplataforma con `Players/Desktop`.

### 08. [[🛠️ Arquitectura de Prowl.Editor|Extensibilidad del Editor]]
- [[🛠️ Arquitectura de Prowl.Editor]]: Sistema de ventanas acoplables, ciclo de edición y playmode.
- [[🎛️ Custom Editors e Inspector Personalizado]]: Atributo `[CustomEditor]` y dibujado de propiedades.
- [[🪄 Scene View Editors y Gizmos]]: Herramientas visuales y gizmos interactivos en la escena.
- [[🪟 Creación de Paneles Propios del Editor]]: Paneles modulares con layout persistente.
- [[⚡ Roslyn Hot Reload y Compilación en Caliente]]: Recarga instantánea de código C# sin reiniciar el motor.

### 09. [[📚 Cheat Sheet de Clases Runtime|Referencia de API Rápida]]
- [[📚 Cheat Sheet de Clases Runtime]]: Resumen tabular de clases, namespaces y usos frecuentes.
- [[🔍 Métodos y Propiedades Esenciales]]: Métodos de invocación rápida para desarrollo diario.
