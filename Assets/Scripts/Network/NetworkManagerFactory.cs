using System.Net;
using UnityEngine;

namespace Network
{
    public class NetworkManagerFactory : MonoBehaviour
    {
        [SerializeField] private ServerNetworkManager serverManagerPrefab;
        [SerializeField] private ClientNetworkManager clientManagerPrefab;

        public ServerNetworkManager CreateServerManager(int port)
        {
            ServerNetworkManager manager = Instantiate(serverManagerPrefab);
            manager.StartServer(port);
            return manager;
        }

        public ClientNetworkManager CreateClientManager(IPAddress ip, int port)
        {
            ClientNetworkManager manager = Instantiate(clientManagerPrefab);
            manager.StartClient(ip, port);
            return manager;
        }
    }
}