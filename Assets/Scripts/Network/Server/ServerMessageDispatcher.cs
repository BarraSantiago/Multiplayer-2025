using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Network.ClientDir;
using Network.interfaces;
using Network.Messages;
using UnityEngine;

namespace Network
{
    public class ServerMessageDispatcher : BaseMessageDispatcher
    {
        private readonly ServerNetworkManager _serverNetworkManager;

        public ServerMessageDispatcher(PlayerManager playerManager, UdpConnection connection, ClientManager clientManager, ServerNetworkManager serverNetworkManager)
            : base(playerManager, connection, clientManager)
        {
            _serverNetworkManager = serverNetworkManager;
        }

        protected override void InitializeMessageHandlers()
        {
            _messageHandlers[MessageType.HandShake] = HandleHandshake;
            _messageHandlers[MessageType.Console] = HandleConsoleMessage;
            _messageHandlers[MessageType.Position] = HandlePositionUpdate;
            _messageHandlers[MessageType.Ping] = HandlePing;
            _messageHandlers[MessageType.Id] = HandleIdMessage;
        }

        private void HandleHandshake(byte[] data, IPEndPoint ip)
        {
            try
            {
                int clientId = _clientManager.AddClient(ip);
                _clientManager.UpdateClientTimestamp(clientId);

                if (!_playerManager.TryGetPlayer(clientId, out GameObject player))
                {
                    Debug.LogWarning($"[ServerMessageDispatcher] Player not found for client ID {clientId}, creating new player");
                }

                List<byte> newId = BitConverter.GetBytes((int)MessageType.Id).ToList();
                newId.AddRange(BitConverter.GetBytes(clientId));
                _connection.Send(newId.ToArray(), ip);

                _serverNetworkManager.SerializedBroadcast(player.transform.position, MessageType.HandShake, clientId);

                _netPlayers.Data = _playerManager.GetAllPlayers();
                byte[] msg = ConvertToEnvelope(_netPlayers.Serialize(), MessageType.HandShake, ip, true);
                _connection.Send(msg, ip);

                Debug.Log($"[ServerMessageDispatcher] New client {clientId} connected from {ip}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerMessageDispatcher] Error in HandleHandshake: {ex.Message}");
            }
        }

        private void HandleConsoleMessage(byte[] data, IPEndPoint ip)
        {
            try
            {
                string message = _netString.Deserialize(data);
                onConsoleMessageReceived?.Invoke(message);

                if (string.IsNullOrEmpty(message)) return;

                _serverNetworkManager.SerializedBroadcast(message, MessageType.Console);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerMessageDispatcher] Error in HandleConsoleMessage: {ex.Message}");
            }
        }

        private void HandlePositionUpdate(byte[] data, IPEndPoint ip)
        {
            try
            {
                if (data == null || data.Length < sizeof(float) * 3)
                {
                    Debug.LogError("[ServerMessageDispatcher] Invalid position data received");
                    return;
                }

                Vector3 position = _netVector3.Deserialize(data);

                if (!_clientManager.TryGetClientId(ip, out int clientId))
                {
                    Debug.LogWarning($"[ServerMessageDispatcher] Position update from unknown client {ip}");
                    return;
                }

                _playerManager.UpdatePlayerPosition(clientId, position);

                _serverNetworkManager.SerializedBroadcast(position, MessageType.Position, clientId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerMessageDispatcher] Error in HandlePositionUpdate: {ex.Message}");
            }
        }

        private void HandlePing(byte[] data, IPEndPoint ip)
        {
            try
            {
                if (!_clientManager.TryGetClientId(ip, out int clientId))
                {
                    Debug.LogWarning($"[ServerMessageDispatcher] Ping from unknown client {ip}");
                    return;
                }

                _clientManager.UpdateClientTimestamp(clientId);
                _serverNetworkManager.SendToClient(clientId, null, MessageType.Ping, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerMessageDispatcher] Error in HandlePing: {ex.Message}");
            }
        }

        private void HandleIdMessage(byte[] data, IPEndPoint ip)
        {
            Debug.Log("[ServerMessageDispatcher] Received ID message from client (unexpected)");
        }
    }
}