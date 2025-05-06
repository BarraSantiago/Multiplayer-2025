using System;
using System.Collections.Generic;
using System.Net;
using Network.Factory;
using Network.interfaces;
using Network.Messages;
using UnityEngine;

namespace Network.ClientDir
{
    public class ClientMessageDispatcher : BaseMessageDispatcher
    {
        private readonly ClientNetworkManager _clientNetworkManager;

        public ClientMessageDispatcher(PlayerManager playerManager, UdpConnection connection,
            ClientManager clientManager, ClientNetworkManager clientNetworkManager)
            : base(playerManager, connection, clientManager)
        {
            _clientNetworkManager = clientNetworkManager;
        }

        protected override void InitializeMessageHandlers()
        {
            _messageHandlers[MessageType.HandShake] = HandleHandshake;
            _messageHandlers[MessageType.Console] = HandleConsoleMessage;
            _messageHandlers[MessageType.Position] = HandlePositionUpdate;
            _messageHandlers[MessageType.Ping] = HandlePing;
            _messageHandlers[MessageType.Id] = HandleIdMessage;
            _messageHandlers[MessageType.ObjectCreate] = HandleObjectCreate;
            _messageHandlers[MessageType.ObjectDestroy] = HandleObjectDestroy;
            _messageHandlers[MessageType.ObjectUpdate] = HandleObjectUpdate;
        }

        private void HandleHandshake(byte[] data, IPEndPoint ip)
        {
            try
            {
                if (data == null)
                {
                    Debug.LogError("[ClientMessageDispatcher] Received null handshake data from server");
                    return;
                }

                
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientMessageDispatcher] Error in HandleHandshake: {ex.Message}");
            }
        }

        private void HandleConsoleMessage(byte[] data, IPEndPoint ip)
        {
            try
            {
                string message = _netString.Deserialize(data);
                onConsoleMessageReceived?.Invoke(message);
                Debug.Log($"[ClientMessageDispatcher] Console message received: {message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientMessageDispatcher] Error in HandleConsoleMessage: {ex.Message}");
            }
        }

        private void HandlePositionUpdate(byte[] data, IPEndPoint ip)
        {
            try
            {
                if (data == null || data.Length < sizeof(float) * 3)
                {
                    Debug.LogError("[ClientMessageDispatcher] Invalid position data received");
                    return;
                }

                Vector3 position = _netVector3.Deserialize(data);
                int clientId = _netVector3.GetId(data);
                _playerManager.UpdatePlayerPosition(clientId, position);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientMessageDispatcher] Error in HandlePositionUpdate: {ex.Message}");
            }
        }

        private void HandlePing(byte[] data, IPEndPoint ip)
        {
            try
            {
                _currentLatency = (Time.realtimeSinceStartup - _lastPing) * 1000;
                _lastPing = Time.realtimeSinceStartup;

                _clientNetworkManager.SendToServer(null, MessageType.Ping, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientMessageDispatcher] Error in HandlePing: {ex.Message}");
            }
        }

        private void HandleIdMessage(byte[] data, IPEndPoint ip)
        {
            try
            {
                if (data == null || data.Length < 4)
                {
                    Debug.LogError("[ClientMessageDispatcher] Invalid ID message data");
                    return;
                }

                int clientId = BitConverter.ToInt32(data, 0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientMessageDispatcher] Error in HandleIdMessage: {ex.Message}");
            }
        }

        private void HandleObjectCreate(byte[] data, IPEndPoint ip)
        {
            try
            {
                NetworkObjectCreateMessage createMsg = _netCreateObject.Deserialize(data);

                NetworkObjectFactory.Instance.HandleCreateObjectMessage(createMsg);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientMessageDispatcher] Error in HandleObjectCreate: {ex.Message}");
            }
        }

        private void HandleObjectDestroy(byte[] data, IPEndPoint ip)
        {
            try
            {
                int networkId = BitConverter.ToInt32(data, 0);
                NetworkObjectFactory.Instance.DestroyNetworkObject(networkId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientMessageDispatcher] Error in HandleObjectDestroy: {ex.Message}");
            }
        }

        private void HandleObjectUpdate(byte[] data, IPEndPoint ip)
        {
            try
            {
                int networkId = BitConverter.ToInt32(data, 0);
                MessageType objectMessageType = (MessageType)BitConverter.ToInt32(data, 4);

                // Get the payload (skip first 8 bytes)
                byte[] payload = new byte[data.Length - 8];
                Array.Copy(data, 8, payload, 0, payload.Length);

                NetworkObject obj = NetworkObjectFactory.Instance.GetNetworkObject(networkId);
                if (obj != null)
                {
                    obj.OnNetworkMessage(payload, objectMessageType);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientMessageDispatcher] Error in HandleObjectUpdate: {ex.Message}");
            }
        }
    }
}