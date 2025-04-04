using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Game;
using Network.interfaces;
using Network.Messages;
using UnityEngine;
using Utils;

namespace Network
{
    public struct Client
    {
        public float lastHeartbeatTime;
        public int id;
        public IPEndPoint ipEndPoint;

        public Client(IPEndPoint ipEndPoint, int id, float timeStamp)
        {
            this.lastHeartbeatTime = timeStamp;
            this.id = id;
            this.ipEndPoint = ipEndPoint;
        }
    }

    public class NetworkManager : MonoBehaviourSingleton<NetworkManager>, IReceiveData, IDisposable
    {
        public IPAddress IPAddress { get; private set; }
        public int Port { get; private set; }
        public bool IsServer { get; private set; }
        public GameObject PlayerPrefab;
        public int TimeOut = 30;
        public float HeartbeatInterval = 5f;

        public Action<byte[], IPEndPoint> OnReceiveEvent;
        public Action<string> OnReceiveMessageEvent;
        public Action<int> OnClientDisconnected;
        public Action<int> OnClientConnected;

        private UdpConnection _connection;
        private readonly ConcurrentDictionary<int, Client> _clients = new ConcurrentDictionary<int, Client>();
        private readonly ConcurrentDictionary<int, GameObject> _players = new ConcurrentDictionary<int, GameObject>();
        private readonly ConcurrentDictionary<IPEndPoint, int> _ipToId = new ConcurrentDictionary<IPEndPoint, int>();
        private Dictionary<MessageType, Action<byte[], IPEndPoint>> _messageHandlers;

        private int _clientId = 0;
        private float _lastHeartbeatTime;
        private float _lastTimeoutCheck;
        private bool _disposed = false;

        private readonly NetVector3 _netVector3 = new NetVector3();
        private readonly NetPlayers _netPlayers = new NetPlayers();
        private readonly NetString _netString = new NetString();

        private void Awake()
        {
            InitializeMessageHandlers();
        }

        private void InitializeMessageHandlers()
        {
            _messageHandlers = new Dictionary<MessageType, Action<byte[], IPEndPoint>>
            {
                { MessageType.HandShake, HandleHandshake },
                { MessageType.Console, HandleConsoleMessage },
                { MessageType.Position, HandlePositionUpdate }
            };
        }

        public void StartServer(int port)
        {
            IsServer = true;
            Port = port;
            try
            {
                _connection = new UdpConnection(port, this);
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
                GameObject player = new GameObject();
                player.AddComponent<Player>();
                AddClient(new IPEndPoint(ip, port));
                SendToServer(null, MessageType.HandShake);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Failed to start client: {e.Message}");
                throw;
            }
        }

        private bool AddClient(IPEndPoint ip)
        {
            if (_ipToId.ContainsKey(ip)) return false;

            int id = _clientId++;
            _ipToId[ip] = id;
            Client newClient = new Client(ip, id, Time.realtimeSinceStartup);
            _clients[id] = newClient;

            _players[id] = Instantiate(PlayerPrefab);
            OnClientConnected?.Invoke(id);

            Debug.Log($"[NetworkManager] Client added: {ip.Address}, ID: {id}");
            return true;
        }

        private bool RemoveClient(IPEndPoint ip)
        {
            if (!_ipToId.TryGetValue(ip, out int clientId)) return false;

            _ipToId.TryRemove(ip, out _);
            _clients.TryRemove(clientId, out _);

            if (_players.TryRemove(clientId, out GameObject player))
            {
                if (player != null) Destroy(player);
            }

            OnClientDisconnected?.Invoke(clientId);
            return true;
        }

        public void OnReceiveData(byte[] data, IPEndPoint ip)
        {
            AddClient(ip);

            try
            {
                // Update client timestamp (for timeout detection)
                if (_ipToId.TryGetValue(ip, out int clientId) && _clients.TryGetValue(clientId, out Client client))
                {
                    client.lastHeartbeatTime = Time.realtimeSinceStartup;
                    _clients[clientId] = client;
                }

                MessageType messageType = DeserializeMessageType(data);

                if (IsServer)
                {
                    Broadcast(data);
                    if (_messageHandlers.TryGetValue(messageType, out var handler))
                    {
                        handler(data, ip);
                    }
                }
                else
                {
                    if (_messageHandlers.TryGetValue(messageType, out var handler))
                    {
                        handler(data, ip);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Error processing data from {ip}: {ex.Message}");
            }
        }

        #region Message Handlers

        private void HandleHandshake(byte[] data, IPEndPoint ip)
        {
            if (IsServer)
            {
                _netPlayers.Data = _players.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                _connection.Send(_netPlayers.Serialize(), ip);
            }
            else
            {
                Dictionary<int, Vector3> newPlayersDic = _netPlayers.Deserialize(data);
                foreach (var kvp in newPlayersDic)
                {
                    if (_players.ContainsKey(kvp.Key)) continue;
                    GameObject newplayer = Instantiate(PlayerPrefab);
                    newplayer.transform.position = kvp.Value;
                    _players[kvp.Key] = newplayer;
                }
            }
        }

        private void HandleConsoleMessage(byte[] data, IPEndPoint ip)
        {
            string message = _netString.Deserialize(data);
            OnReceiveMessageEvent?.Invoke(message);
            OnReceiveEvent?.Invoke(data, ip);
        }

        private void HandlePositionUpdate(byte[] data, IPEndPoint ip)
        {
            Vector3 position = _netVector3.Deserialize(data);
            int id = _ipToId.TryGetValue(ip, out int clientId) ? clientId : 0;

            if (_players.TryGetValue(id, out GameObject player))
            {
                player.transform.position = position;
            }
            else
            {
                Debug.LogWarning($"[NetworkManager] Player with id {id} not found");
            }
        }

        #endregion

        public MessageType DeserializeMessageType(byte[] data)
        {
            if (data == null || data.Length < 4)
            {
                throw new ArgumentException("[NetworkManager] Invalid byte array for deserialization");
            }

            int messageTypeInt = BitConverter.ToInt32(data, 0);
            return (MessageType)messageTypeInt;
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

        public void SendToServer(object data, MessageType messageType)
        {
            try
            {
                byte[] serializedData;
                switch (messageType)
                {
                    case MessageType.HandShake:
                        serializedData = BitConverter.GetBytes((int)MessageType.HandShake);
                        break;
                    case MessageType.Console:
                        serializedData = data is string str
                            ? _netString.Serialize(str)
                            : throw new ArgumentException("Data must be string for Console messages");
                        break;
                    case MessageType.Position:
                        serializedData = data is Vector3 vec3
                            ? _netVector3.Serialize(vec3)
                            : throw new ArgumentException("Data must be Vector3 for Position messages");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(messageType));
                }

                SendToServer(serializedData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] SendToServer failed: {e.Message}");
            }
        }

        public void Broadcast(byte[] data)
        {
            try
            {
                foreach (var client in _clients)
                {
                    _connection.Send(data, client.Value.ipEndPoint);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Broadcast failed: {e.Message}");
            }
        }

        private void CheckForTimeouts()
        {
            if (!IsServer) return;

            float currentTime = Time.realtimeSinceStartup;
            List<IPEndPoint> clientsToRemove = new List<IPEndPoint>();

            foreach (var client in _clients)
            {
                if (currentTime - client.Value.lastHeartbeatTime > TimeOut)
                {
                    clientsToRemove.Add(client.Value.ipEndPoint);
                    Debug.Log($"[NetworkManager] Client {client.Key} timed out");
                }
            }

            foreach (var ip in clientsToRemove)
            {
                RemoveClient(ip);
            }
        }

        private void SendHeartbeat()
        {
            if (!IsServer) return;
            byte[] heartbeatData = BitConverter.GetBytes((int)MessageType.Heartbeat);
            Broadcast(heartbeatData);
        }

        private void Update()
        {
            if (_disposed) return;

            _connection?.FlushReceiveData();

            float currentTime = Time.realtimeSinceStartup;

            // Send heartbeats periodically if server
            if (IsServer && currentTime - _lastHeartbeatTime > HeartbeatInterval)
            {
                SendHeartbeat();
                _lastHeartbeatTime = currentTime;
            }

            // Check for client timeouts
            if (IsServer && currentTime - _lastTimeoutCheck > 1f)
            {
                CheckForTimeouts();
                _lastTimeoutCheck = currentTime;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _connection?.Close();

                foreach (var player in _players.Values)
                {
                    if (player != null) Destroy(player);
                }

                _players.Clear();
                _clients.Clear();
                _ipToId.Clear();
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