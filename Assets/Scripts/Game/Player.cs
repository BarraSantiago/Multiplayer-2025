using System.Collections.Generic;
using MultiplayerLib.Game.Model;
using MultiplayerLib.Network.Factory;
using MultiplayerLib.Network.Synchronization;
using UnityEngine;

namespace Game
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private UnityView unityView;

        private UnityGameController gameController;

        public void SetGameManager(GameManager manager)
        {
            gameController = new UnityGameController(unityView, manager, FactionType.Red);
        }

        private void Update()
        {
            gameController?.Update();
        }
    }
}