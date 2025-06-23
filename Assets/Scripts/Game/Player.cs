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
            float xMovement = 0;
            if (Input.GetKey(KeyCode.A)) xMovement -= 1;
            if (Input.GetKey(KeyCode.D)) xMovement += 1;

            if (xMovement > 1f || xMovement < -1f) xMovement = Mathf.Clamp(xMovement, -1f, 1f);
            
                
            bool isShooting = Input.GetKey(KeyCode.Mouse0);
            bool isJumping = Input.GetKey(KeyCode.Space);
            bool isCrouching = Input.GetKey(KeyCode.LeftShift);

            PlayerInput inputData = new PlayerInput
            {
                xMovement = xMovement,
                IsShooting = isShooting,
                IsJumping = isJumping,
                IsCrouching = isCrouching,
            };
            
            bool hasMovement = !Mathf.Approximately(xMovement, 0f);
            bool hasAction = isShooting || isJumping || isCrouching;
            bool inputChanged = !InputEquals(_lastSentInput, inputData);

            if (!hasMovement && !hasAction && !inputChanged) return;
            ClientNetworkManager.OnSendToServer?.Invoke(inputData, MessageType.PlayerInput, true,false);
            _lastSentInput = inputData;
        }
        
        private bool InputEquals(PlayerInput a, PlayerInput b)
        {
            return Mathf.Approximately(a.xMovement, b.xMovement) &&
                   a.IsShooting == b.IsShooting &&
                   a.IsJumping == b.IsJumping &&
                   a.IsCrouching == b.IsCrouching;
        }
    }
}