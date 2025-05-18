using System.Net;
using MultiplayerLib.Network.Server;
using Network.Factory;
using UnityEngine;

namespace Network.Server
{
    public class UnityServerManager : MonoBehaviour
    {
        private ServerNetworkManager _networkManager;
        private PlayerManager _playerManager;
        private UnityServerMessageDispatcher _messageDispatcher;

        public int ServerPort { get; set; } = 7777;
        public int ServerId { get; set; } = 0;
        public IPEndPoint MatchmakerEndpoint { get; set; }

        private void Awake()
        {
            UnityConsoleMessages.Initialize();
            _playerManager = new PlayerManager();
            NetworkFactoryManager.PlayerManager = _playerManager;
            _networkManager = new ServerNetworkManager();

            ServerNetworkManager.SetInstance(_networkManager);
            _messageDispatcher = new UnityServerMessageDispatcher(_networkManager.ClientManager)
            {
                PlayerManager = _playerManager
            };
            _networkManager._messageDispatcher = _messageDispatcher;
            _networkManager.Init(ref _messageDispatcher.OnNewClient);
            _messageDispatcher.OnClientDisconnect += _playerManager.RemovePlayer;

            // Uncomment and use proper values
            if (MatchmakerEndpoint != null)
            {
                _networkManager.SetMatchmakerInfo(MatchmakerEndpoint.Address, MatchmakerEndpoint.Port, ServerId);
            }
            _networkManager.TimeOut = 30;
        }

        public PlayerManager GetPlayerManager()
        {
            return _playerManager;
        }

        private void Update()
        {
            _networkManager.Tick();
        }

        private void OnApplicationQuit()
        {
            _networkManager.Dispose();
            _playerManager.Clear();
        }

        private void OnDestroy()
        {
            _networkManager.Dispose();
            _playerManager.Clear();
        }

        public void StartServer()
        {
            _networkManager.StartServer(ServerPort);
            Debug.Log($"Server started on port {ServerPort}");
        }
    }
}