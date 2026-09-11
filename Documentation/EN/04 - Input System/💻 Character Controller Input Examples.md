---
title: Character Controller Input Examples
tags: [input, charactercontroller, fps, thirdperson, gameplay, examples, movement]
category: input
updated: 2026-09-10
---

# 💻 Character Controller Input Examples in Prowl Engine

This guide provides two complete, production-ready, and **Zero-GC** character locomotion implementations in Prowl Engine:
1. **First-Person Controller (FPS):** Camera locked to player eye height, pitch and yaw clamping, and kinematic capsule movement.
2. **Third-Person Controller:** Independent orbital camera reference with smooth orientation facing movement direction vectors.

---

## 🎮 1. First-Person Controller (FPS)

### Scene Hierarchy:
- A `GameObject` containing [`CharacterController`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Physics/CharacterController.cs).
- A child `GameObject` holding [`Camera`](file:///c:/Users/SaidR/Documents/GitHub/ProwlStar/Prowl.Runtime/Components/Camera.cs) placed at eye level (`Y = 1.6m`).

```csharp
using System;
using Prowl.Runtime;
using Prowl.Vector;

[AddComponentMenu("Gameplay/First Person Controller")]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _cameraTransform;

    [Header("Locomotion Tuning")]
    [SerializeField] private float _walkSpeed = 5.0f;
    [SerializeField] private float _sprintSpeed = 8.5f;
    [SerializeField] private float _jumpHeight = 1.6f;
    [SerializeField] private float _gravity = -22.0f;

    [Header("Mouse Sensitivity & Clamping")]
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
        // Lock and hide mouse cursor for FPS control
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

        // 1. Horizontal rotation (Yaw) turns the entire player body
        float yaw = mouseDelta.X * _mouseSensitivity;
        Transform.Rotate(new Float3(0, yaw, 0), isWorldSpace: false);

        // 2. Vertical rotation (Pitch) exclusively rotates the eye camera
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
            _verticalVelocity.Y = -2.0f; // Slope sticking force
        }

        // Digital input acquisition
        float inputX = (Input.GetKey(Key.D) ? 1 : 0) - (Input.GetKey(Key.A) ? 1 : 0);
        float inputZ = (Input.GetKey(Key.W) ? 1 : 0) - (Input.GetKey(Key.S) ? 1 : 0);
        bool sprinting = Input.GetKey(Key.LeftShift);

        Float3 moveDir = (Transform.Right * inputX) + (Transform.Forward * inputZ);
        if (moveDir.sqrMagnitude > 1.0f)
            moveDir = moveDir.normalized;

        float speed = sprinting ? _sprintSpeed : _walkSpeed;
        Float3 horizontalMotion = moveDir * speed * (float)Time.deltaTime;

        // Jump trigger
        if (Input.GetKeyDown(Key.Space) && grounded)
        {
            _verticalVelocity.Y = MathF.Sqrt(-2f * _gravity * _jumpHeight);
        }

        // Gravity integration
        _verticalVelocity.Y += _gravity * (float)Time.deltaTime;

        // Submit total kinematic displacement
        Float3 finalMotion = horizontalMotion + (_verticalVelocity * (float)Time.deltaTime);
        _controller.Move(finalMotion);
    }
}
```

---

## 🗡️ 2. Third-Person Controller

In third-person setups, characters rotate smoothly to face directional movement vectors relative to camera view space:

```csharp
using System;
using Prowl.Runtime;
using Prowl.Vector;

[AddComponentMenu("Gameplay/Third Person Controller")]
public class ThirdPersonController : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private float _moveSpeed = 6.0f;

    private CharacterController _controller;

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
            // Evaluate angle relative to orbital camera orientation
            float targetAngle = MathF.Atan2(inputDir.X, inputDir.Z) * (180f / MathF.PI) + _mainCamera.Transform.EulerAngles.Y;

            // Smoothly slerp character forward heading
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            Transform.Rotation = Quaternion.Slerp(Transform.Rotation, targetRotation, 10f * (float)Time.deltaTime);

            // Move forward along facing trajectory
            Float3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Float3.Forward;
            _controller.Move(moveDirection * _moveSpeed * (float)Time.deltaTime);
        }
    }
}
```

---

## 🔗 Related Topics
- Kinematic capsule physics: [[🚶 CharacterController]].
- Action architecture: [[🎮 Input System Architecture]].
- Composite bindings: [[🗺️ Input Action Maps & Bindings]].
