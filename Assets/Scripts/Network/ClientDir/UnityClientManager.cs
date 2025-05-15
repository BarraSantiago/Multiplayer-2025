using System.Net;
using MultiplayerLib.Network.ClientDir;
using MultiplayerLib.Network.Messages;
using UnityEngine;

namespace Network.ClientDir
{
    public class UnityClientManager : MonoBehaviour
    {
        [Header("Connection Settings")] 
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int serverPort = 12346;

        [Header("Player Settings")] 
        [SerializeField] private string playerName = "Player";
        [SerializeField] private int playerColor = 0;

        public ClientNetworkManager _networkManager;
        private ClientMessageDispatcher _messageDispatcher;

        public ClientNetworkManager NetworkManager => _networkManager;
        public bool IsConnected { get; private set; }

        private void Awake()
        {
            _networkManager = new ClientNetworkManager();
            ClientNetworkManager.SetInstance(_networkManager);

            _messageDispatcher = new ClientMessageDispatcher();
            _networkManager._messageDispatcher = _messageDispatcher;
        }

        public void ConnectToServer(IPAddress ip, int port, string pName, int color)
        {
            IsConnected = true;
            _networkManager.StartClient(ip, port, pName, color);
        }

        private void Update()
        {
            if (IsConnected)
            {
                _networkManager.Tick();
            }
        }

        private void OnApplicationQuit()
        {
            DisconnectFromServer();
        }

        private void OnDestroy()
        {
            DisconnectFromServer();
        }

        public void DisconnectFromServer()
        {
            if (IsConnected)
            {
                _networkManager.Dispose();
                IsConnected = false;
                Debug.Log("Disconnected from server");
            }
        }
    }
}