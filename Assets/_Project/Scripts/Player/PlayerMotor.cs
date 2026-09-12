using System;
using UnityEngine;
using UnityEngine.InputSystem;
using BurgerShop.UI;

namespace BurgerShop.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = PlayerBoost.BaseMoveSpeed;
        [SerializeField] float rotationSharpness = 16f;
        [SerializeField] float moveDeadzone = 0.12f;

        CharacterController _controller;
        InputAction _moveAction;
        Transform _cameraTransform;
        int boostLevel;

        public int BoostLevel => boostLevel;
        public float MoveSpeed => PlayerBoost.MoveSpeed(boostLevel);

        public void ApplyBoostLevel(int level)
        {
            if (level < 0 || level > PlayerBoost.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            boostLevel = level;
            moveSpeed = MoveSpeed;
        }

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _cameraTransform = Camera.main != null ? Camera.main.transform : null;

            if (InputSystem.actions != null)
            {
                _moveAction = InputSystem.actions.FindAction("Player/Move");
                _moveAction?.Enable();
            }
        }

        void Update()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            Vector2 input = ReadMoveInput();
            Vector3 world = ToCameraRelativeXZ(input, _cameraTransform);

            if (world.sqrMagnitude > moveDeadzone * moveDeadzone)
            {
                _controller.SimpleMove(world * MoveSpeed);
                Quaternion target = Quaternion.LookRotation(world, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    target,
                    1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
            }
            else
            {
                _controller.SimpleMove(Vector3.zero);
            }
        }

        Vector2 ReadMoveInput()
        {
            Vector2 joystick = VirtualJoystick.Value;
            if (joystick.sqrMagnitude > moveDeadzone * moveDeadzone)
                return Vector2.ClampMagnitude(joystick, 1f);

            if (_moveAction != null)
                return Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);

            return ReadKeyboardFallback();
        }

        static Vector2 ReadKeyboardFallback()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;

            Vector2 value = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) value.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) value.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value.x -= 1f;
            return Vector2.ClampMagnitude(value, 1f);
        }

        internal static Vector3 ToCameraRelativeXZ(Vector2 input, Transform cameraTransform)
        {
            if (input.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = cameraTransform.forward;
                right = cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.forward;
                else
                    forward.Normalize();
                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.right;
                else
                    right.Normalize();
            }

            Vector3 world = forward * input.y + right * input.x;
            world.y = 0f;
            return world.sqrMagnitude > 1f ? world.normalized : world;
        }
    }
}
