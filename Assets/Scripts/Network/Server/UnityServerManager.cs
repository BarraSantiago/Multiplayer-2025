using System;
using System.Net;
using System.Threading;
using MultiplayerLib.Network.Server;
using MultiplayerLib.Utils;
using UnityEngine;

namespace Network.Server
{
    public class UnityServerManager : MonoBehaviour
    {
        private ServerNetworkManager _networkManager;
        private UnityServerMessageDispatcher _messageDispatcher;

        public int ServerPort { get; set; } = 7777;
        public int ServerId { get; set; } = 0;
        public IPEndPoint MatchmakerEndpoint { get; set; }
        private Thread _serverThread;
        private bool _isRunning;
        private bool testDone = false;
        private string testMessage = " Mod";
        private void Awake()
        {
            UnityConsoleMessages.Initialize();
            _networkManager = new ServerNetworkManager();

            ServerNetworkManager.SetInstance(_networkManager);
            _messageDispatcher = new UnityServerMessageDispatcher(_networkManager.ClientManager);
            _networkManager._messageDispatcher = _messageDispatcher;
            _networkManager.Init(ref _messageDispatcher.OnNewClient);

            if (MatchmakerEndpoint != null)
            {
                _networkManager.SetMatchmakerInfo(MatchmakerEndpoint.Address, MatchmakerEndpoint.Port, ServerId);
            }

            _networkManager.TimeOut = 30;
        }


        private void Update()
        {
            _networkManager.Tick();
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (testDone) return;
                testDone = true;
                _networkManager.test.ImportantArray[3] += 1;
                ConsoleMessages.Log("Test fields modified successfully.");
            }
            else
            {
                testDone = false;
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (testDone) return;
                testDone = true;
                _networkManager.test.ImportantArray = new int[_networkManager.test.ImportantArray.Length+1];
                testMessage = " Mod" + testMessage;
                ConsoleMessages.Log("Test fields modified successfully.");
            }
            else
            {
                testDone = false;
            }
        }

        private void OnApplicationQuit()
        {
            _networkManager.Dispose();
            StopServer();
        }

        private void OnDestroy()
        {
            _networkManager.Dispose();
            StopServer();
        }

        public void StartServer(int timeout, int afkTimeout)
        {
            _isRunning = true;
            _networkManager.StartServer(ServerPort);
            _networkManager.TimeOut = timeout;
            _networkManager.InactivityTimeout = afkTimeout;
            _networkManager.OnDispose += StopServer;
            ConsoleMessages.Log($"Server started on port {ServerPort}");
        }

        public void StopServer()
        {
            if (!_isRunning) return;

            _isRunning = false;

            if (_serverThread is not { IsAlive: true }) return;
            if (_serverThread.Join(1000)) return;
            Debug.LogWarning("Server thread did not terminate gracefully, aborting");
            try
            {
                _serverThread.Interrupt();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error stopping server thread: {ex.Message}");
            }
        }
    }
}