---
title: Ejemplos de Control de Personajes
tags: [input, charactercontroller, fps, thirdperson, gameplay, examples, movement]
category: input
updated: 2026-09-10
---

# 💻 Ejemplos de Control de Personajes en Prowl Engine

Esta guía proporciona dos implementaciones completas, listas para producción y optimizadas (**Zero GC**) de controladores de personajes en Prowl Engine:
1. **Controlador en Primera Persona (FPS Controller):** Cámara atada a la vista, control de rotación de ratón (*Pitch & Yaw*) con bloqueo de ángulos y desplazamiento cinemático.
2. **Controlador en Tercera Persona (Third-Person Controller):** Cámara orbital independiente y rotación suave del personaje orientada hacia la dirección de movimiento.

---

## 🎮 1. Controlador en Primera Persona (FPS)

### Requisitos de Escena:
- Un `GameObject` para el jugador con el componente [`CharacterController`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/CharacterController.cs).
- Un `GameObject` hijo que contenga el componente [`Camera`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Camera.cs) a la altura de los ojos (`Y = 1.6m`).

```csharp
using System;
using Prowl.Runtime;
using Prowl.Vector;

[AddComponentMenu("Gameplay/First Person Controller")]
public class FirstPersonController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform _cameraTransform;

    [Header("Ajustes de Movimiento")]
    [SerializeField] private float _walkSpeed = 5.0f;
    [SerializeField] private float _sprintSpeed = 8.5f;
    [SerializeField] private float _jumpHeight = 1.6f;
    [SerializeField] private float _gravity = -22.0f;

    [Header("Sensibilidad del Ratón")]
    [SerializeField] private float _mouseSensitivity = 0.12f;
    [SerializeField] private float _minPitch = -85f;
    [SerializeField] private float _maxPitch = 85f;

    private CharacterController _controller;
    private float _pitch = 0.0f;
    private Float3 _verticalVelocity = Float3.Zero;

    public override void Awake()
    {
        base.Awake();
        TryGetComponent(out _controller);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        // Bloquear y ocultar el cursor
        Input.CursorLocked = true;
        Input.CursorVisible = false;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        Input.CursorLocked = false;
        Input.CursorVisible = true;
    }

    public override void Update()
    {
        base.Update();

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        Float2 mouseDelta = Input.MouseDelta;

        // 1. Rotación horizontal (Yaw) sobre el cuerpo del jugador
        float yaw = mouseDelta.X * _mouseSensitivity;
        Transform.Rotate(new Float3(0, yaw, 0), isWorldSpace: false);

        // 2. Rotación vertical (Pitch) exclusivamente sobre la cámara
        _pitch -= mouseDelta.Y * _mouseSensitivity;
        _pitch = Math.Clamp(_pitch, _minPitch, _maxPitch);

        if (_cameraTransform.IsValid())
        {
            _cameraTransform.LocalRotation = Quaternion.Euler(_pitch, 0, 0);
        }
    }

    private void HandleMovement()
    {
        bool grounded = _controller.IsGrounded;
        if (grounded && _verticalVelocity.Y < 0)
        {
            _verticalVelocity.Y = -2.0f; // Adherencia en rampas
        }

        // Lectura de teclas
        float inputX = (Input.GetKey(Key.D) ? 1 : 0) - (Input.GetKey(Key.A) ? 1 : 0);
        float inputZ = (Input.GetKey(Key.W) ? 1 : 0) - (Input.GetKey(Key.S) ? 1 : 0);
        bool sprinting = Input.GetKey(Key.LeftShift);

        Float3 moveDir = (Transform.Right * inputX) + (Transform.Forward * inputZ);
        if (moveDir.sqrMagnitude > 1.0f)
            moveDir = moveDir.normalized;

        float speed = sprinting ? _sprintSpeed : _walkSpeed;
        Float3 horizontalMotion = moveDir * speed * (float)Time.deltaTime;

        // Salto
        if (Input.GetKeyDown(Key.Space) && grounded)
        {
            _verticalVelocity.Y = MathF.Sqrt(-2f * _gravity * _jumpHeight);
        }

        // Gravedad
        _verticalVelocity.Y += _gravity * (float)Time.deltaTime;

        // Desplazamiento final
        Float3 finalMotion = horizontalMotion + (_verticalVelocity * (float)Time.deltaTime);
        _controller.Move(finalMotion);
    }
}
```

---

## 🗡️ 2. Controlador en Tercera Persona (Third Person)

En un controlador de tercera persona, el personaje rota para encarar la dirección hacia la que camina con relación al punto de vista de la cámara:

```csharp
using System;
using Prowl.Runtime;
using Prowl.Vector;

[AddComponentMenu("Gameplay/Third Person Controller")]
public class ThirdPersonController : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private float _moveSpeed = 6.0f;
    [SerializeField] private float _rotationSmoothTime = 0.1f;

    private CharacterController _controller;
    private float _rotationVelocity;

    public override void Awake()
    {
        base.Awake();
        TryGetComponent(out _controller);
    }

    public override void Update()
    {
        base.Update();

        float x = (Input.GetKey(Key.D) ? 1 : 0) - (Input.GetKey(Key.A) ? 1 : 0);
        float z = (Input.GetKey(Key.W) ? 1 : 0) - (Input.GetKey(Key.S) ? 1 : 0);
        Float3 inputDir = new Float3(x, 0, z);

        if (inputDir.sqrMagnitude > 0.01f)
        {
            // Calcular ángulo objetivo basado en la dirección de la cámara
            float targetAngle = MathF.Atan2(inputDir.X, inputDir.Z) * (180f / MathF.PI) + _mainCamera.Transform.EulerAngles.Y;

            // Suavizar la rotación hacia el objetivo
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            Transform.Rotation = Quaternion.Slerp(Transform.Rotation, targetRotation, 10f * (float)Time.deltaTime);

            // Mover hacia adelante en la dirección encarada
            Float3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Float3.Forward;
            _controller.Move(moveDirection * _moveSpeed * (float)Time.deltaTime);
        }
    }
}
```

---

## 🔗 Temas Relacionados
- Componente de física: [[🚶 CharacterController]].
- Arquitectura de acciones: [[🎮 Arquitectura del Input System]].
- Mapeo de mandos: [[🗺️ Input Action Maps y Bindings]].
