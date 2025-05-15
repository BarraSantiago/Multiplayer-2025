using MultiplayerLib.Game;
using MultiplayerLib.Network;
using MultiplayerLib.Network.ClientDir;
using MultiplayerLib.Network.Server;
using Vector3 = System.Numerics.Vector3;

namespace Network.Server
{
    public class UnityServerMessageDispatcher : ServerMessageDispatcher
    {
        public PlayerManager PlayerManager;
        public UnityServerMessageDispatcher(ClientManager clientManager) : base(clientManager)
        {
        }

        protected override void UpdatePlayerPosition(int clientId, Vector3 position)
        {
            throw new System.NotImplementedException();
        }

        protected override void UpdatePlayerInput(int clientId, PlayerInput input)
        {
            PlayerManager.UpdatePlayerInput(clientId, input);
        }
    }
}