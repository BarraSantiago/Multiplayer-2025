using System;
using MultiplayerLib.Game;
using MultiplayerLib.Network.ClientDir;
using MultiplayerLib.Network.Messages;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace Game
{
    public class Player : MonoBehaviour
    {
        private float _inputSendInterval = 0.05f;
        private float _timeSinceLastSend = 0f;

        private PlayerInput _lastSentInput;


        private void Update()
        {
            _timeSinceLastSend += Time.deltaTime;
            if (_timeSinceLastSend < _inputSendInterval) return;

            SendInput();
            _timeSinceLastSend = 0f;
        }

        private void SendInput()
        {
            Vector2 moveDirection = Vector2.Zero;
            if (Input.GetKey(KeyCode.A)) moveDirection.X -= 1;
            if (Input.GetKey(KeyCode.D)) moveDirection.X += 1;

            if (moveDirection.LengthSquared() > 1f)
                moveDirection = Vector2.Normalize(moveDirection);
            
                
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
            
            bool hasMovement = !Mathf.Approximately(moveDirection.LengthSquared(), 0f);
            bool hasAction = isShooting || isJumping || isCrouching;
            bool inputChanged = !InputEquals(_lastSentInput, inputData);

            if (!hasMovement && !hasAction && !inputChanged) return;
            ClientNetworkManager.OnSendToServer?.Invoke(inputData, MessageType.PlayerInput, false);
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