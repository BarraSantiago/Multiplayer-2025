using UnityEngine;

namespace Game
{
    public class Controller : MonoBehaviour
    {
        public float Speed = 5f;
        public float JumpForce = 5f;
        public bool IsGrounded = true;

        private Rigidbody _rigidbody2D;

        private void Start()
        {
            _rigidbody2D = GetComponent<Rigidbody>();
        }

        private void HandleMovement(Vector2 moveDirection)
        {
            Vector2 moveVelocity = moveDirection * Speed;
            _rigidbody2D.linearVelocity = moveVelocity;
        }

        private void HandleJump(bool isJumping)
        {
            if (IsGrounded && isJumping)
            {
                _rigidbody2D.AddForce(Vector2.up * JumpForce, ForceMode.Impulse);
                IsGrounded = false;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                IsGrounded = true;
            }
        }

        public void UpdateInput(PlayerInput input)
        {
            if (input.IsShooting)
            {
            }

            if (input.IsCrouching)
            {
                gameObject.transform.localScale = new Vector3(1, 0.5f, 1);
            }
            else
            {
                gameObject.transform.localScale = new Vector3(1, 1f, 1);
                HandleMovement(input.MoveDirection);
                HandleJump(input.IsJumping);
            }
        }
    }
}