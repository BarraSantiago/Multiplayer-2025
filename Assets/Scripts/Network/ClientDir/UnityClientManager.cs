using System.Net;
using Game;
using MultiplayerLib.Game.Model;
using MultiplayerLib.Network.ClientDir;
using MultiplayerLib.Network.interfaces;
using TMPro;
using UnityEngine;

namespace Network.ClientDir
{
    public class UnityClientManager : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int serverPort = 12346;
        [SerializeField] private bool ConnectToMatchmaker = false;
        [SerializeField] private int Timeout = 120;

        [Header("Player Settings")] 
        [SerializeField] private string playerName = "Player";
        [SerializeField] private int playerColor = 0;
        [SerializeField] private TMP_Text heartbeatText;
        [SerializeField] private GameResult gameResult;
        [SerializeField] private TMP_Text pingBroadcastText;
        [SerializeField] private Player player;

        public ClientNetworkManager _networkManager;
        public ClientNetworkManager NetworkManager => _networkManager;
        public bool IsConnected { get; private set; }

        private ClientMessageDispatcher _messageDispatcher;

        private void Awake()
        {
            UnityConsoleMessages.Initialize();
            _networkManager = new ClientNetworkManager();
            ClientNetworkManager.SetInstance(_networkManager);

            _messageDispatcher = new ClientMessageDispatcher();
            AbstractMessageDispatcher.OnPingBroadcast += (ping) => { pingBroadcastText.text = ping; };
            _networkManager._messageDispatcher = _messageDispatcher;
            _networkManager.ServerTimeout = Timeout;
            _networkManager.Init();
            ClientMessageDispatcher.OnGameEnd += gameResult.OnGameResult;
            player.SetGameManager(_messageDispatcher.GameManager);
        }


        public void ConnectToServer(IPAddress ip, int port, string pName, int color)
        {
            IsConnected = true;
            if (ConnectToMatchmaker)
            {
                _networkManager.ConnectToMatchmaker(ip, port, pName, color);
            }
            else
            {
                _networkManager.StartClient(ip, port, pName, color);
            }
        }

        private void Update()
        {
            if (!IsConnected) return;
            heartbeatText.text = "Ping: " + _networkManager._messageDispatcher.CurrentLatency.ToString("F0");
            _networkManager.Tick();
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
            if (!IsConnected) return;
            _networkManager.Dispose();
            IsConnected = false;
            Debug.Log("Disconnected from server");
            Application.Quit();
        }
    }
}