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
        [SerializeField] private FactionType playerFaction = FactionType.Red;

        public GameManager gameManager;
        private UnityGameController gameController;

        private void Start()
        {
            //gameManager = new GameManager(NetworkObjectFactory.Instance);
            //unityView.Initialize();

            //_mapper.RegisterObject(gameManager, 0);
        }

        public void SetGameManager(GameManager manager)
        {
            gameManager = manager;
            gameController = new UnityGameController(unityView, manager, FactionType.Red);
        }

        private void Update()
        {
            gameController?.Update();
        }
    }
}