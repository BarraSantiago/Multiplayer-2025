using System;
using MultiplayerLib.Game;
using MultiplayerLib.Network.ClientDir;
using UnityEngine;

namespace Game
{
    public class Controller : MonoBehaviour
    {
        public float Speed = 5f;
        public float JumpForce = 5f;
        public bool IsGrounded = false;

        private PlayerInput _input;
        public bool movingRight = true;
        public bool isCrouching = false;
      
        private void Update()
        {
            ExecuteInput();
        }
        
        public void UpdateInput(PlayerInput input)
        {
            _input = input;
            if (!Mathf.Approximately(input.xMovement, 0)) movingRight = input.xMovement > 0;
        }

        private void ExecuteInput()
        {
        }
    }
}