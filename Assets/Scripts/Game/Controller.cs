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

        private void HandleMovement(float xMovement)
        {
            Vector2 moveDirection = new Vector2(xMovement, 0);
            moveDirection.Normalize();
            moveDirection *= Speed;
            moveDirection.y = _rigidbody.linearVelocity.y;
            _rigidbody.linearVelocity = moveDirection;
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
                
                HandleMovement(_input.xMovement);
                HandleJump(_input.IsJumping);
            }
        }
    }
}