﻿using System;
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
        private ReliableMessageManager _reliableManager;

        private float _currentLatency = 0;
        public float CurrentLatency => _currentLatency;
        public static Action<string> onConsoleMessageReceived;
        private float _lastPing;

        public MessageDispatcher(PlayerManager playerManager, UdpConnection connection,
            ClientManager clientManager, bool isServer)
        {
            _reliableManager = new ReliableMessageManager(connection);

            InitializeMessageHandlers(playerManager, connection, clientManager, isServer);
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
                            NetworkManager.Instance.Broadcast(
                                _netPlayers.Serialize(player.transform.position, clientId));
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
                },
                {
                    MessageType.Acknowledgment, (data, _) =>
                    {
                        if (data.Length >= 8)
                        {
                            uint sequenceNumber = BitConverter.ToUInt32(data, 4);
                            _reliableManager.ProcessAcknowledgment(sequenceNumber);
                        }
                    }
                }
            };
        }

        public bool TryDispatchMessage(byte[] data, IPEndPoint ip)
        {
            try
            {
                if (data.Length < MessageHeader.HEADER_SIZE)
                    return false;

                MessageType messageType = (MessageType)BitConverter.ToInt32(data, 0);

                if (messageType == MessageType.Acknowledgment)
                {
                    if (!_messageHandlers.TryGetValue(messageType, out var handler)) return false;
                    handler(data, ip);
                    return true;

                }

                MessageHeader header;
                try
                {
                    header = MessageHeader.Deserialize(data);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MessageDispatcher] Header deserialization failed: {ex.Message}");
                    return false;
                }

                byte[] body = new byte[data.Length - MessageHeader.HEADER_SIZE];
                Array.Copy(data, MessageHeader.HEADER_SIZE, body, 0, body.Length);

                uint calculatedBodyChecksum = MessageHeader.CalculateBodyChecksum(body);
                if (calculatedBodyChecksum != header.BodyChecksum)
                {
                    Debug.LogError("[MessageDispatcher] Body checksum verification failed");
                    return false;
                }

                if (header.IsImportant)
                {
                    _reliableManager.SendAcknowledgment(header.SequenceNumber, ip);
                }

                if (!_reliableManager.IsNewMessage(header.SequenceNumber, ip))
                {
                    return true;
                }

                if (!_messageHandlers.TryGetValue(header.MessageType, out var msgHandler)) return false;
                msgHandler(body, ip);
                return true;

            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageDispatcher] Error dispatching message: {ex.Message}");
                return false;
            }
        }

        public void SendMessage(byte[] body, MessageType messageType, IPEndPoint target, bool isImportant = false)
        {
            byte[] fullMessage = _reliableManager.PrepareMessage(body, messageType, isImportant);
            _reliableManager.SendMessage(fullMessage, target, isImportant);
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

        public byte[] SerializeMessage(object data, MessageType messageType, int id = -1)
        {
            switch (messageType)
            {
                case MessageType.HandShake:
                    return BitConverter.GetBytes((int)MessageType.HandShake);
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
    }
}