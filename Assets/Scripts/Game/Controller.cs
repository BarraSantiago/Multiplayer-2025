using Network.interfaces;
using Network.Messages;
using Network.Server;
using UnityEngine;

namespace Game
{
    public class Controller : MonoBehaviour
    {
        public float Speed = 5f;
        public float JumpForce = 5f;
        public bool IsGrounded = false;

        private Rigidbody _rigidbody;

        private void Start()
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rigidbody.useGravity = true;
        }
        private Vector3 _position;
        private void Update()
        {
            if(Mathf.Approximately(_position.sqrMagnitude, transform.position.sqrMagnitude)) return;
            _position = transform.position;
            ServerNetworkManager.OnSerializedBroadcast.Invoke(_position, MessageType.Position, -1);
        }
        private void HandleMovement(Vector2 moveDirection)
        {
            Vector2 moveVelocity = moveDirection * Speed;
            _rigidbody.linearVelocity = moveVelocity;
        }

        private void HandleJump(bool isJumping)
        {
            if (IsGrounded && isJumping)
            {
                _rigidbody.AddForce(Vector2.up * JumpForce, ForceMode.Impulse);
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
                _position = transform.position;
            }
        }
    }
}