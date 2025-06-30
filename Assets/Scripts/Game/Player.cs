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

        private GameManager gameManager;
        private UnityGameController gameController;
        private NetworkObjectTracker _mapper = new NetworkObjectTracker();
        private void Start()
        {
            //gameManager = new GameManager(NetworkObjectFactory.Instance);
            //unityView.Initialize();
            //gameController = new UnityGameController(gameManager, playerFaction, unityView);
            //_mapper.RegisterObject(gameManager, 0);
        }

        private void Update()
        {
            gameController?.Update();

            List<byte[]> changes = _mapper.CheckForChanges();
            if (changes.Count > 0)
            {
                Debug.Log(changes.Count);
            }
        }
    }
}