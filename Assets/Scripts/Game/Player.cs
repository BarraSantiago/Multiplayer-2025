using System;
using Network.ClientDir;
using Network.Messages;
using UnityEngine;

namespace Game
{
    [Serializable]
    public struct PlayerInput
    {
        public Vector2 MoveDirection;
        public bool IsShooting;
        public bool IsJumping;
        public bool IsCrouching;
        public float Timestamp;
    }
    public class Player : MonoBehaviour
    {
        private ClientNetworkManager _clientNetworkManager;
        private float _inputSendInterval = 0.05f;
        private float _timeSinceLastSend = 0f;

        private PlayerInput _lastSentInput;

        private void Awake()
        {
            _clientNetworkManager ??= FindAnyObjectByType<ClientNetworkManager>();
        }

        private void Update()
        {
            _timeSinceLastSend += Time.deltaTime;
            if (_timeSinceLastSend < _inputSendInterval) return;

            SendInput();
            _timeSinceLastSend = 0f;
        }

        private void SendInput()
        {
            // Movement input
            Vector2 moveDirection = Vector2.zero;
            //if (Input.GetKey(KeyCode.W)) moveDirection.y += 1;
            //if (Input.GetKey(KeyCode.S)) moveDirection.y -= 1;
            if (Input.GetKey(KeyCode.A)) moveDirection.x -= 1;
            if (Input.GetKey(KeyCode.D)) moveDirection.x += 1;

            if (moveDirection.sqrMagnitude > 1f)
                moveDirection.Normalize();
                
            bool isShooting = Input.GetKey(KeyCode.Mouse0);
            bool isJumping = Input.GetKey(KeyCode.Space);
            bool isCrouching = Input.GetKey(KeyCode.LeftShift);

            PlayerInput inputData = new PlayerInput
            {
                MoveDirection = moveDirection,
                IsShooting = isShooting,
                IsJumping = isJumping,
                IsCrouching = isCrouching,
                Timestamp = Time.realtimeSinceStartup
            };
            
            bool hasMovement = !Mathf.Approximately(moveDirection.sqrMagnitude, 0f);
            bool hasAction = isShooting || isJumping || isCrouching;
            bool inputChanged = !InputEquals(_lastSentInput, inputData);

            if (!hasMovement && !hasAction && !inputChanged) return;
            _clientNetworkManager.SendToServer(inputData, MessageType.PlayerInput);
            _lastSentInput = inputData;
        }
        
        private bool InputEquals(PlayerInput a, PlayerInput b)
        {
            return a.MoveDirection == b.MoveDirection &&
                   a.IsShooting == b.IsShooting &&
                   a.IsJumping == b.IsJumping &&
                   a.IsCrouching == b.IsCrouching;
        }
    }
}