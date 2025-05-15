using MultiplayerLib.Network.Server;
using Network.Factory;
using UnityEngine;

namespace Network.Server
{
    public class UnityServerManager : MonoBehaviour
    {
        [SerializeField] private int serverPort = 12346;

        private ServerNetworkManager _networkManager;
        private PlayerManager _playerManager;
        private UnityServerMessageDispatcher _messageDispatcher;

        private void Awake()
        {
            _playerManager = new PlayerManager();
            NetworkFactoryManager.PlayerManager = _playerManager;
            _networkManager = new ServerNetworkManager();
            
            _networkManager.Init();
            ServerNetworkManager.SetInstance(_networkManager);
            _messageDispatcher = new UnityServerMessageDispatcher(_networkManager.ClientManager)
            {
                PlayerManager = _playerManager
            };

            _networkManager._messageDispatcher = _messageDispatcher;

            _networkManager.StartServer(serverPort);

            Debug.Log($"Server started on port {serverPort}");
        }

        private void Update()
        {
            _networkManager.Tick();
        }

        private void OnApplicationQuit()
        {
            _networkManager.Dispose();
        }

        private void OnDestroy()
        {
            _networkManager.Dispose();
        }
    }
}