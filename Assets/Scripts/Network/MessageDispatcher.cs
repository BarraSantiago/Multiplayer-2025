using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using Network.Messages;
using UnityEngine;

namespace Network
{
    public class MessageDispatcher
    {
        private Dictionary<MessageType, Action<byte[], IPEndPoint>> _messageHandlers;
        private readonly NetVector3 _netVector3 = new NetVector3();
        private readonly NetPlayers _netPlayers = new NetPlayers();
        private readonly NetString _netString = new NetString();
        private readonly NetHeartbeat _netHeartbeat = new NetHeartbeat();
        private float _currentLatency = 0;
        public float CurrentLatency => _currentLatency;
        public static Action<string> onConsoleMessageReceived;
        private float _lastPing;

        public MessageTracker _messageTracker = new MessageTracker();
        private const float ResendInterval = 1.0f;
        private float _lastResendCheckTime = 0f;

        public MessageDispatcher(PlayerManager playerManager, UdpConnection connection,
            ClientManager clientManager, bool isServer)
        {
            InitializeMessageHandlers(playerManager, connection, clientManager, isServer);
            InitializeAckHandler(connection);
        }

        private void InitializeMessageHandlers(PlayerManager playerManager, UdpConnection connection,
            ClientManager clientManager, bool isServer)
        {
            _messageHandlers = new Dictionary<MessageType, Action<byte[], IPEndPoint>>
            {
                {
                    MessageType.HandShake,
                    (data, ip) => HandleHandshake(data, ip, playerManager, connection, clientManager, isServer)
                },
                { MessageType.Console, (data, ip) => HandleConsoleMessage(data, ip, isServer) },
                {
                    MessageType.Position,
                    (data, ip) => HandlePositionUpdate(data, ip, playerManager, clientManager, isServer)
                },
                { MessageType.Ping, (data, ip) => HandlePing(ip, connection, clientManager, isServer) },
                { MessageType.Id, (data, ip) => HandleIdMessage(data, isServer) }
            };
        }

        private void HandleHandshake(byte[] data, IPEndPoint ip, PlayerManager playerManager,
            UdpConnection connection, ClientManager clientManager, bool isServer)
        {
            try
            {
                if (isServer)
                {
                    int clientId = clientManager.AddClient(ip);
                    clientManager.UpdateClientTimestamp(clientId);

                    if (!playerManager.TryGetPlayer(clientId, out var player))
                    {
                        Debug.LogWarning(
                            $"[MessageDispatcher] Player not found for client ID {clientId}, creating new player");
                    }

                    List<byte> newId = BitConverter.GetBytes((int)MessageType.Id).ToList();
                    newId.AddRange(BitConverter.GetBytes(clientId));
                    connection.Send(newId.ToArray(), ip);

                    NetworkManager.Instance.SerializedBroadcast(player.transform.position, MessageType.HandShake,
                        clientId);

                    _netPlayers.Data = playerManager.GetAllPlayers();
                    byte[] msg = ConvertToEnvelope(_netPlayers.Serialize(), MessageType.HandShake, ip, true);
                    connection.Send(msg, ip);

                    Debug.Log($"[MessageDispatcher] New client {clientId} connected from {ip}");
                }
                else
                {
                    if (data == null)
                    {
                        Debug.LogError("[MessageDispatcher] Received null handshake data from server");
                        return;
                    }


                    Dictionary<int, Vector3> newPlayersDic = _netPlayers.Deserialize(data);
                    int newPlayersAdded = 0;

                    foreach (KeyValuePair<int, Vector3> kvp in newPlayersDic)
                    {
                        if (playerManager.HasPlayer(kvp.Key)) continue;
                        playerManager.CreatePlayer(kvp.Key, kvp.Value);
                        newPlayersAdded++;
                    }

                    Debug.Log($"[MessageDispatcher] Handshake received, added {newPlayersAdded} new players");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageDispatcher] Error in HandleHandshake: {ex.Message}");
            }
        }

        private void HandleConsoleMessage(byte[] data, IPEndPoint ip, bool isServer)
        {
            try
            {
                string message = _netString.Deserialize(data);
                onConsoleMessageReceived?.Invoke(message);

                if (isServer && !string.IsNullOrEmpty(message))
                {
                    NetworkManager.Instance.Broadcast(data);
                    Debug.Log($"[MessageDispatcher] Broadcasting console message from {ip}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageDispatcher] Error in HandleConsoleMessage: {ex.Message}");
            }
        }

        private void HandlePositionUpdate(byte[] data, IPEndPoint ip, PlayerManager playerManager,
            ClientManager clientManager, bool isServer)
        {
            try
            {
                if (data == null || data.Length < sizeof(float) * 3)
                {
                    Debug.LogError("[MessageDispatcher] Invalid position data received");
                    return;
                }

                Vector3 position = _netVector3.Deserialize(data);
                int clientId = -1;

                if (isServer)
                {
                    if (!clientManager.TryGetClientId(ip, out clientId))
                    {
                        Debug.LogWarning($"[MessageDispatcher] Position update from unknown client {ip}");
                        return;
                    }

                    playerManager.UpdatePlayerPosition(clientId, position);

                    NetworkManager.Instance.SerializedBroadcast(position, MessageType.Position, clientId);
                }
                else
                {
                    clientId = _netVector3.GetId(data);
                    playerManager.UpdatePlayerPosition(clientId, position);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageDispatcher] Error in HandlePositionUpdate: {ex.Message}");
            }
        }

        private void HandlePing(IPEndPoint ip, UdpConnection connection, ClientManager clientManager, bool isServer)
        {
            try
            {
                if (!isServer)
                {
                    _currentLatency = (Time.realtimeSinceStartup - _lastPing) * 1000;
                    _lastPing = Time.realtimeSinceStartup;

                    connection.Send(_netHeartbeat.Serialize());
                }
                else
                {
                    if (!clientManager.TryGetClientId(ip, out int clientId))
                    {
                        Debug.LogWarning($"[MessageDispatcher] Ping from unknown client {ip}");
                        return;
                    }

                    clientManager.UpdateClientTimestamp(clientId);
                    connection.Send(_netHeartbeat.Serialize(), ip);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageDispatcher] Error in HandlePing: {ex.Message}");
            }
        }

        private void HandleIdMessage(byte[] data, bool isServer)
        {
            if (isServer) return;

            try
            {
                if (data == null || data.Length < 8)
                {
                    Debug.LogError("[MessageDispatcher] Invalid ID message data");
                    return;
                }

                int clientId = BitConverter.ToInt32(data, 4);
                Debug.Log($"[MessageDispatcher] Received client ID: {clientId}");

                // Store client ID in appropriate place
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageDispatcher] Error in HandleIdMessage: {ex.Message}");
            }
        }

        public MessageType DeserializeMessageType(byte[] data)
        {
            if (data == null || data.Length < 4)
            {
                throw new ArgumentException("[MessageDispatcher] Invalid byte array for deserialization");
            }

            int messageTypeInt = BitConverter.ToInt32(data, 0);
            return (MessageType)messageTypeInt;
        }

        private void InitializeAckHandler(UdpConnection connection)
        {
            _messageHandlers[MessageType.Acknowledgment] = (data, ip) =>
            {
                int offset = 0;
                MessageType ackedType = (MessageType)BitConverter.ToInt32(data, offset);
                offset += 4;
                int ackedNumber = BitConverter.ToInt32(data, offset);

                _messageTracker.ConfirmMessage(ip, ackedType, ackedNumber);
            };
        }

        public bool TryDispatchMessage(byte[] data, IPEndPoint ip)
        {
            try
            {
                if (data == null)
                {
                    Debug.LogWarning(
                        $"[MessageDispatcher] Dropped malformed packet from {ip}: insufficient data length" +
                        $" ({data?.Length ?? 0} bytes)");
                    return false;
                }

                MessageEnvelope envelope = MessageEnvelope.Deserialize(data);

                if (envelope.IsImportant)
                {
                    SendAcknowledgment(envelope.MessageType, envelope.MessageNumber, ip);
                }

                if (_messageHandlers.TryGetValue(envelope.MessageType, out Action<byte[], IPEndPoint> handler))
                {
                    handler(envelope.Data, ip);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageDispatcher] Error dispatching message: {ex.Message}");
                return false;
            }
        }

        private void SendAcknowledgment(MessageType ackedType, int ackedNumber, IPEndPoint target)
        {
            List<byte> ackData = new List<byte>();
            ackData.AddRange(BitConverter.GetBytes((int)ackedType));
            ackData.AddRange(BitConverter.GetBytes(ackedNumber));

            SendMessage(ackData.ToArray(), MessageType.Acknowledgment, target, false, false);
        }

        public byte[] ConvertToEnvelope(byte[] data, MessageType messageType, IPEndPoint target, bool isImportant,
            bool isCritical = false)
        {
            int messageNumber = _messageTracker.GetNextMessageNumber(messageType);

            MessageEnvelope envelope = new MessageEnvelope
            {
                IsCritical = isCritical,
                MessageType = messageType,
                MessageNumber = messageNumber,
                IsImportant = isImportant,
                Data = data
            };

            byte[] serializedEnvelope = envelope.Serialize();

            if (isImportant && target != null)
            {
                _messageTracker.AddPendingMessage(serializedEnvelope, target, messageType, messageNumber);
            }

            return serializedEnvelope;
        }

        public void SendMessage(byte[] data, MessageType messageType, IPEndPoint target, bool isImportant,
            bool isCritical = false)
        {
            int messageNumber = _messageTracker.GetNextMessageNumber(messageType);

            MessageEnvelope envelope = new MessageEnvelope
            {
                IsCritical = isCritical,
                MessageType = messageType,
                MessageNumber = messageNumber,
                IsImportant = isImportant,
                Data = data
            };

            byte[] serializedEnvelope = envelope.Serialize();

            if (isImportant)
            {
                _messageTracker.AddPendingMessage(serializedEnvelope, target, messageType, messageNumber);
            }

            NetworkManager.Instance.SendMessage(serializedEnvelope, target);
        }

        public byte[] SerializeMessage(object data, MessageType messageType, int id = -1)
        {
            switch (messageType)
            {
                case MessageType.HandShake:
                    return null;
                case MessageType.Console:
                    if (data is string str) return _netString.Serialize(str);
                    throw new ArgumentException("Data must be string for Console messages");

                case MessageType.Position:
                    if (data is Vector3 vec3) return _netVector3.Serialize(vec3, id);
                    throw new ArgumentException("Data must be Vector3 for Position messages");

                case MessageType.Ping:
                    return _netHeartbeat.Serialize();
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType));
            }
        }

        public void CheckAndResendMessages()
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastResendCheckTime < ResendInterval)
                return;

            _lastResendCheckTime = currentTime;

            Dictionary<IPEndPoint, List<MessageTracker.PendingMessage>> pendingMessages =
                _messageTracker.GetPendingMessages();
            foreach (var endpointMessages in pendingMessages)
            {
                IPEndPoint target = endpointMessages.Key;
                foreach (MessageTracker.PendingMessage message in endpointMessages.Value)
                {
                    // Only resend messages that have been waiting long enough
                    if (currentTime - message.LastSentTime >= ResendInterval)
                    {
                        NetworkManager.Instance.SendMessage(message.Data, target);
                        _messageTracker.UpdateMessageSentTime(target, message.MessageType, message.MessageNumber);
                        Debug.Log(
                            $"[MessageDispatcher] Resending message: Type={message.MessageType}, Number={message.MessageNumber} to {target}");
                    }
                }
            }
        }
    }
}