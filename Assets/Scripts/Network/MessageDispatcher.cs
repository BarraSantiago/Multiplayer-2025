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
        
        private MessageTracker _messageTracker = new MessageTracker();
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
                    MessageType.HandShake, (data, ip) =>
                    {
                        if (isServer)
                        {
                            int clientId = clientManager.AddClient(ip);
                            clientManager.UpdateClientTimestamp(clientId);
                            playerManager.TryGetPlayer(clientId, out var player);
                            NetworkManager.Instance.Broadcast(_netPlayers.Serialize(player.transform.position, clientId));
                            List<byte> newId = BitConverter.GetBytes((int)MessageType.Id).ToList();
                            newId.AddRange(BitConverter.GetBytes(NetworkManager.Instance.GetClientId(ip)));
                            connection.Send(newId.ToArray(), ip);
                            _netPlayers.Data = playerManager.GetAllPlayers();
                            connection.Send(_netPlayers.Serialize(), ip);
                        }
                        else
                        {
                            Dictionary<int, Vector3> newPlayersDic = _netPlayers.Deserialize(data);
                            foreach (KeyValuePair<int, Vector3> kvp in newPlayersDic)
                            {
                                if (playerManager.HasPlayer(kvp.Key)) continue;
                                playerManager.CreatePlayer(kvp.Key, kvp.Value);
                            }
                        }
                    }
                },
                {
                    MessageType.Console, (data, _) =>
                    {
                        string message = _netString.Deserialize(data);
                        onConsoleMessageReceived?.Invoke(message);
                        if (isServer) NetworkManager.Instance.Broadcast(data);
                    }
                },
                {
                    MessageType.Position, (data, ip) =>
                    {
                        Vector3 position = _netVector3.Deserialize(data);
                        if (isServer)
                        {
                            if (clientManager.TryGetClientId(ip, out int clientId))
                            {
                                playerManager.UpdatePlayerPosition(clientId, position);
                            }

                            data = _netVector3.Serialize(position, clientId);
                            NetworkManager.Instance.Broadcast(data);
                        }
                        else
                        {
                            playerManager.UpdatePlayerPosition(_netVector3.GetId(data), position);
                        }
                    }
                },
                {
                    MessageType.Ping, (_, ip) =>
                    {


                        if (!isServer)
                        {
                            _currentLatency = (Time.realtimeSinceStartup - _lastPing) * 1000;
                            _lastPing = Time.realtimeSinceStartup;

                            connection.Send(_netHeartbeat.Serialize());
                        }
                        else
                        {
                            if (!clientManager.TryGetClientId(ip, out int clientId)) return;
                            clientManager.UpdateClientTimestamp(clientId);

                            connection.Send(_netHeartbeat.Serialize(), ip);
                        }
                    }
                },
                {
                    MessageType.Id, (data, _) =>
                    {
                        if (isServer) return;
                        int clientId = BitConverter.ToInt32(data, 4);
                    }
                }
            };
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

        public void SendMessage(byte[] data, MessageType messageType, IPEndPoint target, bool isImportant, bool isCritical = false)
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
    
            Dictionary<IPEndPoint, List<MessageTracker.PendingMessage>> pendingMessages = _messageTracker.GetPendingMessages();
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
                        Debug.Log($"[MessageDispatcher] Resending message: Type={message.MessageType}, Number={message.MessageNumber} to {target}");
                    }
                }
            }
        }
    }
}