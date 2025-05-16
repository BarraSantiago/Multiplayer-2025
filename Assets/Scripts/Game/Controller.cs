using System;
using MultiplayerLib.Game;
using UnityEngine;

namespace Game
{
    public class Controller : MonoBehaviour
    {
        public float Speed = 5f;
        public float JumpForce = 5f;
        public bool IsGrounded = false;

        private Rigidbody _rigidbody;
        private PlayerInput _input;

        private void Start()
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
            _rigidbody.constraints = (RigidbodyConstraints)80;
            _rigidbody.useGravity = true;
        }

        private void Update()
        {
            ExecuteInput();
        }

        private void HandleMovement(System.Numerics.Vector2 moveDirection)
        {
            moveDirection.X *= Speed;
            moveDirection.Y *= Speed;
            Vector2 moveVelocity = new Vector2(){x = moveDirection.X, y = moveDirection.Y};
            _rigidbody.linearVelocity = moveVelocity;
        }

        private void HandleJump(bool isJumping)
        {
            if (!IsGrounded || !isJumping) return;
            _rigidbody.AddForce(Vector2.up * JumpForce, ForceMode.Impulse);
            IsGrounded = false;
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("Ground"))
            {
                IsGrounded = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Ground"))
            {
                IsGrounded = true;
            }
        }

        public void UpdateInput(PlayerInput input)
        {
            _input = input;
        }

        private void ExecuteInput()
        {
            if (_input.IsShooting)
            {
            }

            if (_input.IsCrouching)
            {
                gameObject.transform.localScale = new Vector3(1, 0.5f, 1);
            }
            else
            {
                gameObject.transform.localScale = new Vector3(1, 1f, 1);
                
                HandleMovement(_input.MoveDirection);
                HandleJump(_input.IsJumping);
            }
        }
    }
}