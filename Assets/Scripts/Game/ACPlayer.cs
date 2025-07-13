using AuthClient.Game.Model;
using MultiplayerLib.Game.Model;
using UnityEngine;

namespace Game
{
    public class ACPlayer : MonoBehaviour
    {
        [SerializeField] private UnityView unityView;

        private ACGameController gameController;

        public void SetGameManager(ACGameManager manager)
        {
            gameController = new ACGameController(unityView, manager, FactionType.Red);
        }

        private void Update()
        {
            gameController?.Update();
        }
    }
}