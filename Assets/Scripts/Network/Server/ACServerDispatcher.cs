using AuthClient.Network.Server;
using MultiplayerLib.Network.ClientDir;
using MultiplayerLib.Network.Server;

namespace Network.Server
{
    public class ACServerDispatcher : ACServerMessageDispatcher
    {
        public ACServerDispatcher(ClientManager cManager, IServerBroadcaster broadcaster) : base(cManager, broadcaster)
        {
        }
    }
}