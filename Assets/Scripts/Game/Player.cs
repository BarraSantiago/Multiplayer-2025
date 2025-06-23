using MultiplayerLib.Game.Model;
using UnityEngine;

namespace Game
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private UnityView unityView;
        [SerializeField] private FactionType playerFaction = FactionType.Red;

        private GameManager gameManager;
        private UnityGameController gameController;

        private void Start()
        {
            gameManager = new GameManager();
            unityView.Initialize(gameManager);
            gameController = new UnityGameController(gameManager, playerFaction, unityView);
        }

        private void Update()
        {
            if (gameController != null)
                gameController.Update();
        }
    }
}