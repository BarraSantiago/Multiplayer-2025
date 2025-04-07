using System;
using System.Collections.Generic;
using System.Net;
using Game;
using Network.interfaces;
using Network.Messages;
using TMPro;
using UnityEngine;
using Utils;

namespace Network
{
    public class NetworkManager : MonoBehaviourSingleton<NetworkManager>, IReceiveData, IDisposable
    {
        [SerializeField] private TMP_Text heartbeatText;
        public IPAddress IPAddress { get; private set; }
        public int Port { get; private set; }
        public bool IsServer { get; private set; }
        public GameObject PlayerPrefab;
        public int TimeOut = 30;
        public float HeartbeatInterval = 0.15f;

        public Action<byte[], IPEndPoint> OnReceiveEvent;
        public Action<byte[], string> OnReceiveMessageEvent;
        
        private UdpConnection _connection;
        private ClientManager _clientManager;
        private PlayerManager _playerManager;
        private MessageDispatcher _messageDispatcher;
        
        private float _lastHeartbeatTime;
        private float _lastTimeoutCheck;
        private bool _disposed = false;

        private void Awake()
        {
            _clientManager = new ClientManager();
            _playerManager = new PlayerManager(PlayerPrefab);
            
            _clientManager.OnClientConnected += OnClientConnected;
            _clientManager.OnClientDisconnected += OnClientDisconnected;
        }

        public void StartServer(int port)
        {
            IsServer = true;
            Port = port;
            
            try
            {
                _connection = new UdpConnection(port, this);
                _messageDispatcher = new MessageDispatcher(_playerManager, _connection, _clientManager, true);
                
                Debug.Log($"[NetworkManager] Server started on port {port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Failed to start server: {e.Message}");
                throw;
            }
        }

        public void StartClient(IPAddress ip, int port)
        {
            IsServer = false;
            Port = port;
            IPAddress = ip;

            try
            {
                _connection = new UdpConnection(ip, port, this);
                _messageDispatcher = new MessageDispatcher(_playerManager, _connection, _clientManager, false);
                
                GameObject player = new GameObject();
                player.AddComponent<Player>();
                
                IPEndPoint serverEndpoint = new IPEndPoint(ip, port);
                _clientManager.AddClient(serverEndpoint);
                
                SendToServer(null, MessageType.HandShake);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Failed to start client: {e.Message}");
                throw;
            }
        }

        public void OnReceiveData(byte[] data, IPEndPoint ip)
        {
            try
            {
                int clientId = _clientManager.AddClient(ip);
                _clientManager.UpdateClientTimestamp(clientId);

                _messageDispatcher.TryDispatchMessage(data, ip);
                
                if (IsServer)
                {
                    Broadcast(data);
                }
                
                OnReceiveEvent?.Invoke(data, ip);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Error processing data from {ip}: {ex.Message}");
            }
        }
        
        public int GetClientId(IPEndPoint ip)
        {
            if (_clientManager.TryGetClientId(ip, out int clientId))
            {
                return clientId;
            }
            return -1;
        }

        private void OnClientConnected(int clientId)
        {
            if (!_playerManager.HasPlayer(clientId))
            {
                _playerManager.CreatePlayer(clientId);
            }
        }
        
        private void OnClientDisconnected(int clientId)
        {
            _playerManager.RemovePlayer(clientId);
        }
        
        private void OnConsoleMessageReceived(string message)
        {
            Debug.Log($"[NetworkManager] Console message: {message}");
        }

        public void SendToServer(object data, MessageType messageType)
        {
            try
            {
                byte[] serializedData = _messageDispatcher.SerializeMessage(data, messageType);
                SendToServer(serializedData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] SendToServer failed: {e.Message}");
            }
        }

        public void SendToServer(byte[] data)
        {
            try
            {
                _connection?.Send(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Send failed: {e.Message}");
            }
        }

        public void Broadcast(byte[] data)
        {
            try
            {
                foreach (KeyValuePair<int, Client> client in _clientManager.GetAllClients())
                {
                    _connection.Send(data, client.Value.ipEndPoint);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Broadcast failed: {e.Message}");
            }
        }

        private void SendHeartbeat()
        {
            byte[] heartbeatData = _messageDispatcher.SerializeMessage(null, MessageType.Heartbeat);
            Broadcast(heartbeatData);
        }

        private void CheckForTimeouts()
        {
            if (!IsServer) return;
            
            List<IPEndPoint> clientsToRemove = _clientManager.GetTimedOutClients(TimeOut);
            
            foreach (IPEndPoint ip in clientsToRemove)
            {
                _clientManager.RemoveClient(ip);
            }
        }

        private void Update()
        {
            if (_disposed) return;

            _connection?.FlushReceiveData();

            float currentTime = Time.realtimeSinceStartup;

            if (IsServer && currentTime - _lastHeartbeatTime > HeartbeatInterval)
            {
                SendHeartbeat();
                _lastHeartbeatTime = currentTime;
            }

            if (IsServer && currentTime - _lastTimeoutCheck > 1f)
            {
                CheckForTimeouts();
                _lastTimeoutCheck = currentTime;
            }
            if (heartbeatText && _messageDispatcher != null && !IsServer)
            {
                heartbeatText.text = $"Ping: {_messageDispatcher.CurrentLatency:F0} ms";
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _connection?.Close();
                _playerManager.Clear();
                _clientManager.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Disposal error: {e.Message}");
            }

            _disposed = true;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}