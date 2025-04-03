using System;
using System.Collections.Generic;
using System.Net;
using Network.interfaces;
using UnityEngine;
using Utils;

namespace Network
{
    public struct Client
    {
        public float timeStamp;
        public int id;
        public IPEndPoint ipEndPoint;

        public Client(IPEndPoint ipEndPoint, int id, float timeStamp)
        {
            this.timeStamp = timeStamp;
            this.id = id;
            this.ipEndPoint = ipEndPoint;
        }
    }

    public class NetworkManager : MonoBehaviourSingleton<NetworkManager>, IReceiveData
    {
        public IPAddress IPAddress { get; private set; }

        public int Port { get; private set; }

        public bool IsServer { get; private set; }

        public int TimeOut = 30;

        public Action<byte[], IPEndPoint> OnReceiveEvent;

        private UdpConnection _connection;

        private readonly Dictionary<int, Client> _clients = new Dictionary<int, Client>();
        private readonly Dictionary<IPEndPoint, int> _ipToId = new Dictionary<IPEndPoint, int>();

        private int _clientId = 0; // This id should be generated during first handshake

        public void StartServer(int port)
        {
            IsServer = true;
            this.Port = port;
            _connection = new UdpConnection(port, this);
        }

        public void StartClient(IPAddress ip, int port)
        {
            IsServer = false;

            this.Port = port;
            this.IPAddress = ip;

            _connection = new UdpConnection(ip, port, this);

            AddClient(new IPEndPoint(ip, port));
        }

        private void AddClient(IPEndPoint ip)
        {
            if (_ipToId.ContainsKey(ip)) return;
            Debug.Log("Adding client: " + ip.Address);

            int id = _clientId;
            _ipToId[ip] = _clientId;

            _clients.Add(_clientId, new Client(ip, id, Time.realtimeSinceStartup));

            _clientId++;
        }

        private void RemoveClient(IPEndPoint ip)
        {
            if (!_ipToId.ContainsKey(ip)) return;
            Debug.Log("Removing client: " + ip.Address);
            _clients.Remove(_ipToId[ip]);
        }

        public void OnReceiveData(byte[] data, IPEndPoint ip)
        {
            AddClient(ip);

            OnReceiveEvent?.Invoke(data, ip);
        }

        public void SendToServer(byte[] data)
        {
            _connection.Send(data);
        }

        public void Broadcast(byte[] data)
        {
            using Dictionary<int, Client>.Enumerator iterator = _clients.GetEnumerator();
            while (iterator.MoveNext())
            {
                _connection.Send(data, iterator.Current.Value.ipEndPoint);
            }
        }

        private void Update()
        {
            // Flush the data in main thread
            _connection?.FlushReceiveData();
        }
    }
}