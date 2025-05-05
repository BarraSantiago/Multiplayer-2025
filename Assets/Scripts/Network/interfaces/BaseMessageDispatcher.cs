using System;
using System.Collections.Generic;
using System.Net;
using Game;
using Network.ClientDir;
using Network.Messages;
using UnityEngine;

namespace Network.interfaces
{
    public abstract class BaseMessageDispatcher
    {
        protected Dictionary<MessageType, Action<byte[], IPEndPoint>> _messageHandlers;
        protected readonly NetVector3 _netVector3 = new NetVector3();
        protected readonly NetPlayers _netPlayers = new NetPlayers();
        protected readonly NetString _netString = new NetString();
        protected readonly NetPlayerInput _netPlayerInput = new NetPlayerInput();
        protected readonly NetHeartbeat _netHeartbeat = new NetHeartbeat();
        protected float _currentLatency = 0;
        public float CurrentLatency => _currentLatency;
        public static Action<string> onConsoleMessageReceived;
        protected float _lastPing;

        public MessageTracker _messageTracker = new MessageTracker();
        protected const float ResendInterval = 1.0f;
        protected float _lastResendCheckTime = 0f;

        protected UdpConnection _connection;
        protected PlayerManager _playerManager;
        protected ClientManager _clientManager;

        protected BaseMessageDispatcher(PlayerManager playerManager, UdpConnection connection,
            ClientManager clientManager)
        {
            _playerManager = playerManager;
            _connection = connection;
            _clientManager = clientManager;
            _messageHandlers = new Dictionary<MessageType, Action<byte[], IPEndPoint>>();
            InitializeMessageHandlers();
            InitializeAcknowledgmentHandler();
        }

        protected abstract void InitializeMessageHandlers();

        protected void InitializeAcknowledgmentHandler()
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
                        $"[MessageDispatcher] Dropped malformed packet from {ip}: insufficient data length ({data?.Length ?? 0} bytes)");
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

        protected void SendAcknowledgment(MessageType ackedType, int ackedNumber, IPEndPoint target)
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

        public virtual void SendMessage(byte[] data, MessageType messageType, IPEndPoint target, bool isImportant,
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

            if (target == null)
            {
                Debug.LogError("[MessageDispatcher] Target endpoint is null");
                return;
            }

            AbstractNetworkManager.Instance.SendMessage(serializedEnvelope, target);
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
                    return null;
                case MessageType.Id:
                    if (data is int idValue) return BitConverter.GetBytes(idValue);
                    throw new ArgumentException("Data must be int for Id messages");
                // TODO serialization
                case MessageType.ObjectCreate:
                case MessageType.ObjectDestroy:
                case MessageType.ObjectUpdate:
                    return null;
                case MessageType.Acknowledgment:
                    if (data is int ackedNumber)
                    {
                        byte[] ackData = new byte[4];
                        Buffer.BlockCopy(BitConverter.GetBytes(ackedNumber), 0, ackData, 0, 4);
                        return ackData;
                    }

                    throw new ArgumentException("Data must be int for Acknowledgment messages");
                case MessageType.PlayerInput:
                    if (data is PlayerInput input)
                    {
                        return _netPlayerInput.Serialize(input);
                    }
                    throw new ArgumentException("Data must be PlayerInput for PlayerInput messages");
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
            foreach (KeyValuePair<IPEndPoint, List<MessageTracker.PendingMessage>> endpointMessages in pendingMessages)
            {
                IPEndPoint target = endpointMessages.Key;
                foreach (MessageTracker.PendingMessage message in endpointMessages.Value)
                {
                    // Only resend messages that have been waiting long enough
                    if (currentTime - message.LastSentTime >= ResendInterval)
                    {
                        AbstractNetworkManager.Instance.SendMessage(message.Data, target);
                        _messageTracker.UpdateMessageSentTime(target, message.MessageType, message.MessageNumber);
                        Debug.Log(
                            $"[MessageDispatcher] Resending message: Type={message.MessageType}, Number={message.MessageNumber} to {target}");
                    }
                }
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
    }
}