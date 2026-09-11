---
title: Cheat Sheet de Clases Runtime
tags: [cheatsheet, api, reference, runtime, classes, namespaces]
category: reference
updated: 2026-09-10
---

# 📚 Cheat Sheet de Clases Runtime en Prowl Engine

Esta tabla resume las clases, estructuras y tipos de datos más importantes de [`Prowl.Runtime`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime), sus respectivos namespaces y su propósito fundamental dentro del motor:

---

## 🏛️ Núcleo y Grafo de Escena

| Clase / Tipo | Namespace | Descripción Breve |
| :--- | :--- | :--- |
| [`GameObject`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/GameObject.cs) | `Prowl.Runtime` | Entidad base de la escena; contenedor de componentes. |
| [`MonoBehaviour`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/GameObject/MonoBehaviour.cs) | `Prowl.Runtime` | Clase base para scripts y comportamientos en la escena. |
| [`Transform`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Math/Transform.cs) | `Prowl.Vector` | Posición, rotación, escala y relaciones de jerarquía padre-hijo. |
| [`Scene`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Scene.cs) | `Prowl.Runtime.Resources` | Colección completa de entidades y sistemas que componen un nivel. |
| [`Prefab`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Prefab.cs) | `Prowl.Runtime.Resources` | Plantilla de GameObject serializada reutilizable. |
| [`Time`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Time.cs) | `Prowl.Runtime` | Métricas de tiempo de ejecución (`deltaTime`, `time`, `frameCount`). |
| [`Debug`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Debug.cs) | `Prowl.Runtime` | Logging en consola (`Log`, `LogWarning`, `LogError`). |

---

## 🧮 Tipos Matemáticos y Espaciales

| Tipo | Namespace | Descripción Breve |
| :--- | :--- | :--- |
| `Float2` | `Prowl.Vector` | Vector 2D para coordenadas de pantalla, UVs e inputs. |
| `Float3` | `Prowl.Vector` | Vector 3D para posiciones, velocidades y normales. |
| `Float4` | `Prowl.Vector` | Vector 4D para colores RGBA y coordenadas homogéneas. |
| `Quaternion` | `Prowl.Vector` | Rotaciones 3D libres de bloqueo de cardán (*Gimbal Lock*). |
| `Float4x4` | `Prowl.Vector` | Matrices afines de proyección, vista y modelo de mundo. |
| `Ray` | `Prowl.Vector.Geometry` | Semirrecta infinita `(origin, direction)` para consultas espaciales. |
| `Bounds` | `Prowl.Vector.Geometry` | Caja delimitadora alineada con los ejes (AABB). |

---

## 🎨 Gráficos y Renderizado

| Clase | Namespace | Descripción Breve |
| :--- | :--- | :--- |
| [`Camera`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Camera.cs) | `Prowl.Runtime` | Punto de vista y cálculo de matrices de vista y proyección. |
| [`MeshRenderer`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/MeshRenderer.cs) | `Prowl.Runtime` | Dibuja una malla estática con un material asignado. |
| [`SkinnedMeshRenderer`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/SkinnedMeshRenderer.cs) | `Prowl.Runtime` | Dibuja mallas deformables por esqueleto de huesos. |
| [`DirectionalLight`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Lights/DirectionalLight.cs) | `Prowl.Runtime` | Fuente de luz solar infinita con sombras en cascada. |
| [`PointLight`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Lights/PointLight.cs) | `Prowl.Runtime` | Fuente de luz puntual esférica omnidireccional. |
| [`SpotLight`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Lights/SpotLight.cs) | `Prowl.Runtime` | Fuente de luz cónica direccional. |
| [`Material`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Material.cs) | `Prowl.Runtime.Resources` | Instancia de un shader con parámetros y texturas. |
| [`Shader`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Shader.cs) | `Prowl.Runtime.Resources` | Programa de sombreado compilado para la GPU. |
| [`Texture2D`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/Texture2D.cs) | `Prowl.Runtime.Resources` | Textura gráfica bidimensional en memoria GPU. |
| [`RenderTexture`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/RenderTexture.cs) | `Prowl.Runtime.Resources` | Búfer de dibujo fuera de pantalla para cámaras o post-procesado. |

---

## 🧊 Física 3D (Jitter Physics 2)

| Clase | Namespace | Descripción Breve |
| :--- | :--- | :--- |
| [`Rigidbody3D`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Rigidbody3D.cs) | `Prowl.Runtime` | Cuerpo rígido dinámico, cinemático o estático. |
| [`BoxCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Colliders/BoxCollider.cs) | `Prowl.Runtime` | Colisionador geométrico en forma de caja. |
| [`SphereCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Colliders/SphereCollider.cs) | `Prowl.Runtime` | Colisionador esférico ultrarrápido. |
| [`CapsuleCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Colliders/CapsuleCollider.cs) | `Prowl.Runtime` | Colisionador de cápsula para bípedos y personajes. |
| [`MeshCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/Colliders/MeshCollider.cs) | `Prowl.Runtime` | Colisionador basado en mallas poligonales o convex hulls. |
| [`TerrainCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/TerrainCollider.cs) | `Prowl.Runtime` | Colisionador de mapa de alturas para terrenos. |
| [`WheelCollider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/WheelCollider.cs) | `Prowl.Runtime` | Simulación realista de rueda de vehículo con suspensión y agarre. |
| [`CharacterController`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/CharacterController.cs) | `Prowl.Runtime` | Controlador cinemático para personajes con escaleras y pendientes. |
| [`Physics`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Physics/PhysicsWorld.cs) | `Prowl.Runtime` | Fachada estática para Raycasts, ShapeCasts y solapamientos. |

---

## 🔊 Audio (MiniAudio)

| Clase | Namespace | Descripción Breve |
| :--- | :--- | :--- |
| [`AudioSource`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Audio/AudioSource.cs) | `Prowl.Runtime` | Emisor de audio espacial 3D o estéreo 2D. |
| [`AudioListener`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Audio/AudioListener.cs) | `Prowl.Runtime` | Receptor de sonido del jugador. |
| [`AudioClip`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Resources/AudioClip.cs) | `Prowl.Runtime.Resources` | Archivo de audio cargado o en streaming (WAV, MP3, OGG, FLAC). |
| [`AudioMixer`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Audio/AudioMixer.cs) | `Prowl.Runtime.Audio` | Ruteo de señales sonoras en buses con volumen en decibelios. |

---

## 🖼️ Interfaz de Usuario (UI)

| Clase | Namespace | Descripción Breve |
| :--- | :--- | :--- |
| [`GameCanvas`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/GameCanvas.cs) | `Prowl.Runtime` | Lienzo raíz para elementos de UI en pantalla o en el mundo. |
| [`RectTransform`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/RectTransform.cs) | `Prowl.Runtime.UI` | Transformación bidimensional con anclas, márgenes y pivotes. |
| [`UIImage`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/UIImage.cs) | `Prowl.Runtime.UI` | Renderizado de sprites con soporte 9-slice y radial. |
| [`TextComponent`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/TextComponent.cs) | `Prowl.Runtime.UI` | Renderizado tipográfico vectorial nítido con Rich Text. |
| [`UIButton`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/UIButton.cs) | `Prowl.Runtime.UI` | Botón interactivo con evento `OnClick`. |
| [`UISlider`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/UISlider.cs) | `Prowl.Runtime.UI` | Barra deslizante de rangos numéricos continuos o enteros. |
| [`UIInputField`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/UIInputField.cs) | `Prowl.Runtime.UI` | Caja interactiva de texto editable con soporte para contraseñas. |
| [`UIScrollRect`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/UIScrollRect.cs) | `Prowl.Runtime.UI` | Contenedor con desplazamiento e inercia elástica. |
| [`EventSystem`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/UI/Input/EventSystem.cs) | `Prowl.Runtime.UI` | Despacho de clics, arrastres y foco de mandos en la UI. |

---

## 🗄️ Gestión de Assets y Serialización

| Clase | Namespace | Descripción Breve |
| :--- | :--- | :--- |
| [`AssetDatabase`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetDatabase.cs) | `Prowl.Runtime` | Registro global de activos por identificador GUID. |
| [`AssetRef<T>`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/AssetRef.cs) | `Prowl.Runtime` | Referencia inteligente tipada con carga perezosa automática. |
| `Serializer` | `Prowl.Echo` | Motor de serialización binaria, textual y clonación profunda. |

---

## 🔗 Temas Relacionados
- Métodos rápidos: [[🔍 Métodos y Propiedades Esenciales]].
- Guía para desarrolladores de Unity: [[🔄 Comparativa y Migración desde Unity]].
- Buenas prácticas de rendimiento: [[🎯 Buenas Prácticas y Rendimiento (Zero GC)]].
