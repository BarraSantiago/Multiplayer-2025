using System;
using System.Collections.Generic;
using System.Net;
using Network.ClientDir;
using Network.Factory;
using Network.interfaces;
using Network.Messages;
using UnityEngine;

namespace Network.Server
{
    public class ServerNetworkManager : AbstractNetworkManager
    {
        public int TimeOut = 30;
        public float HeartbeatInterval = 0.15f;

        private float _lastHeartbeatTime;
        private float _lastTimeoutCheck;
        public static Action<object, MessageType, int> OnSerializedBroadcast;
        public static Action<int, object, MessageType, bool> OnSendToClient;

        protected override void Awake()
        {
            base.Awake();
            _clientManager.OnClientConnected += OnClientConnected;
            _clientManager.OnClientDisconnected += OnClientDisconnected;
            OnSerializedBroadcast += SerializedBroadcast;
            OnSendToClient += SendToClient;
        }

        public void StartServer(int port)
        {
            Port = port;

            try
            {
                _connection = new UdpConnection(port, this);
                _messageDispatcher = new ServerMessageDispatcher(_playerManager, _connection, _clientManager);

                Debug.Log($"[ServerNetworkManager] Server started on port {port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServerNetworkManager] Failed to start server: {e.Message}");
                throw;
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
            }
        }

        private void OnClientDisconnected(int clientId)
        {
            _playerManager.RemovePlayer(clientId);
        }

        public void SendToClient(int clientId, object data, MessageType messageType, bool isImportant = false)
        {
            try
            {
                if (_clientManager.TryGetClient(clientId, out Client client))
                {
                    byte[] serializedData = SerializeMessage(data, messageType);
                    _messageDispatcher.SendMessage(serializedData, messageType, client.ipEndPoint, isImportant);
                }
                else
                {
                    Debug.LogWarning($"[ServerNetworkManager] Cannot send to client {clientId}: client not found");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServerNetworkManager] SendToClient failed: {e.Message}");
            }
        }

        public void Broadcast(byte[] data, bool isImportant = false, MessageType messageType = MessageType.None,
            int messageNumber = -1)
        {
            try
            {
                foreach (KeyValuePair<int, Client> client in _clientManager.GetAllClients())
                {
                    _connection.Send(data, client.Value.ipEndPoint);
                    if (isImportant)
                    {
                        _messageDispatcher.MessageTracker.AddPendingMessage(data, client.Value.ipEndPoint, messageType,
                            messageNumber);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServerNetworkManager] Broadcast failed: {e.Message}");
            }
        }

        public void SerializedBroadcast(object data, MessageType messageType, int id = -1)
        {
            try
            {
                byte[] serializedData = SerializeMessage(data, messageType, id);
                serializedData = _messageDispatcher.ConvertToEnvelope(serializedData, messageType, null, false);
                Broadcast(serializedData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServerNetworkManager] Serialized broadcast failed: {e.Message}");
            }
        }

        private void SendHeartbeat()
        {
            foreach (KeyValuePair<int, Client> client in _clientManager.GetAllClients())
            {
                byte[] heartbeatData = SerializeMessage(null, MessageType.Ping);
                heartbeatData = _messageDispatcher.ConvertToEnvelope(heartbeatData, MessageType.Ping, null, false);
                _messageDispatcher.SendMessage(heartbeatData, MessageType.Ping, client.Value.ipEndPoint, false);
            }
        }

        private void CheckForTimeouts()
        {
            List<IPEndPoint> clientsToRemove = _clientManager.GetTimedOutClients(TimeOut);

            foreach (IPEndPoint ip in clientsToRemove)
            {
                _clientManager.RemoveClient(ip);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (_disposed) return;

            float currentTime = Time.realtimeSinceStartup;

            if (currentTime - _lastHeartbeatTime > HeartbeatInterval)
            {
                SendHeartbeat();
                _lastHeartbeatTime = currentTime;
            }

            if (currentTime - _lastTimeoutCheck > 1f)
            {
                CheckForTimeouts();
                _lastTimeoutCheck = currentTime;
            }

            foreach (KeyValuePair<int, NetworkObject> valuePair in NetworkObjectFactory.Instance.GetAllNetworkObjects())
            {
                if (Mathf.Approximately(valuePair.Value.LastUpdatedPos.sqrMagnitude, valuePair.Value.transform.position.sqrMagnitude)) return;
                valuePair.Value.LastUpdatedPos = valuePair.Value.transform.position;
                SerializedBroadcast(valuePair.Value.LastUpdatedPos, MessageType.Position, valuePair.Key);
            }
        }

        public override void Dispose()
        {
            if (_disposed) return;

            try
            {
                SerializedBroadcast("Server shutting down", MessageType.Console);
                Debug.Log("[ServerNetworkManager] Server shutdown notification sent");

                _clientManager.OnClientConnected -= OnClientConnected;
                _clientManager.OnClientDisconnected -= OnClientDisconnected;
                OnSerializedBroadcast -= SerializedBroadcast;
                OnSendToClient -= SendToClient;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServerNetworkManager] Disposal error: {e.Message}");
            }

            base.Dispose();
        }
    }
}