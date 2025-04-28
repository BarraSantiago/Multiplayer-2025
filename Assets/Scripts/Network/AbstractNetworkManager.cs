using System;
using System.Net;
using Network.interfaces;
using Network.Messages;
using UnityEngine;
using Utils;

namespace Network
{
    public abstract class AbstractNetworkManager : MonoBehaviourSingleton<AbstractNetworkManager>, IReceiveData, IDisposable
    {
        [SerializeField] protected GameObject PlayerPrefab;

        protected UdpConnection _connection;
        protected ClientManager _clientManager;
        protected PlayerManager _playerManager;
        protected BaseMessageDispatcher _messageDispatcher;
        protected bool _disposed = false;

        public int Port { get; protected set; }

        protected virtual void Awake()
        {
            _clientManager = new ClientManager();
            _playerManager = new PlayerManager(PlayerPrefab);
        }

        public virtual void OnReceiveData(byte[] data, IPEndPoint ip)
        {
            try
            {
                _messageDispatcher.TryDispatchMessage(data, ip);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Error processing data from {ip}: {ex.Message}");
            }
        }

        public virtual void SendMessage(byte[] data, IPEndPoint ipEndPoint)
        {
            try
            {
                _connection?.Send(data, ipEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Send failed: {e.Message}");
            }
        }

        public virtual byte[] SerializeMessage(object data, MessageType messageType, int id = -1)
        {
            return _messageDispatcher.SerializeMessage(data, messageType, id);
        }

        protected virtual void Update()
        {
            if (_disposed) return;

            _connection?.FlushReceiveData();
            _messageDispatcher?.CheckAndResendMessages();
        }

        public virtual void Dispose()
        {
            if (_disposed) return;

            try
            {
                _connection?.FlushReceiveData();
                System.Threading.Thread.Sleep(100);

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

        protected virtual void OnDestroy()
        {
            Dispose();
        }
    }
}