using MultiplayerLib.Network.ClientDir;
using MultiplayerLib.Network.Server;

namespace Network.Server
{
    public class UnityServerMessageDispatcher : ServerMessageDispatcher
    {
        public UnityServerMessageDispatcher(ClientManager clientManager) : base(clientManager)
        {
        }
    }
}