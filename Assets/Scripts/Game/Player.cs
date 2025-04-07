using Network;
using Network.Messages;
using UnityEngine;

namespace Game
{
    public class Player : MonoBehaviour
    {
        public float moveSpeed = 5f;
        private Vector3 _position = Vector3.zero;
        private void Update()
        {
            Move();
        }

        private void Move()
        {
            Vector3 move = new Vector3();

            if (Input.GetKey(KeyCode.W))
            {
                move += Vector3.up;
            }

            if (Input.GetKey(KeyCode.S))
            {
                move += Vector3.down;
            }

            if (Input.GetKey(KeyCode.A))
            {
                move += Vector3.left;
            }

            if (Input.GetKey(KeyCode.D))
            {
                move += Vector3.right;
            }

            _position += move * (moveSpeed * Time.deltaTime);
            if (Mathf.Approximately(move.magnitude, 0f)) return;
            NetworkManager.Instance.SendToServer(_position, MessageType.Position);
        }
    }
}