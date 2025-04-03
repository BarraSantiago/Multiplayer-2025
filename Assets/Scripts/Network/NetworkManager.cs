using System;
using System.Collections.Generic;
using System.Net;
using Game;
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
        public GameObject PlayerPrefab;
        public int TimeOut = 30;

        public Action<byte[], IPEndPoint> OnReceiveEvent;

        private UdpConnection _connection;

        private readonly Dictionary<int, Client> _clients = new Dictionary<int, Client>();
        private readonly Dictionary<int, GameObject> _players = new Dictionary<int, GameObject>();
        private readonly Dictionary<IPEndPoint, int> _ipToId = new Dictionary<IPEndPoint, int>();

        private int _clientId = 0; // This id should be generated during first handshake
        private NetVector3 _netVector3 = new NetVector3();
        private NetPlayers _netPlayers = new NetPlayers();
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
            GameObject player = Instantiate(PlayerPrefab);
            player.AddComponent<Player>();
            AddClient(new IPEndPoint(ip, port));
            SendToServer(null, MessageType.HandShake);
        }

        private void AddClient(IPEndPoint ip)
        {
            if (_ipToId.ContainsKey(ip)) return;
            Debug.Log("Adding client: " + ip.Address);

            int id = _clientId;
            _ipToId[ip] = _clientId;

            _clients.Add(_clientId, new Client(ip, id, Time.realtimeSinceStartup));
            _players.Add(id, Instantiate(PlayerPrefab));
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

            try
            {
                MessageType mType = DeserializeMessageType(data);
                if (IsServer)
                {
                    Broadcast(data);
                    ServerUseData(data, ip, mType);
                }
                else
                {
                    ClientUseData(data, _ipToId[ip], mType);
                }
            }
            catch
            {
                Debug.LogError($"Failed to deserialize data from {ip.Address}");
            }
        }
        private void ServerUseData(byte[] data, IPEndPoint ip, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.HandShake:
                    _netPlayers.Data = _players;
                    Broadcast(_netPlayers.Serialize());
                    break;
                case MessageType.Console:
                    OnReceiveEvent?.Invoke(data, ip);
                    break;
                case MessageType.Position:
                    Vector3 pos = _netVector3.Deserialize(data);
                    if (_players.TryGetValue(_ipToId[ip], out GameObject player))
                    {
                        player.transform.position = pos;
                    }
                    else
                    {
                        Debug.LogError($"Player with id {_ipToId[ip]} not found.");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }
        private void ClientUseData(byte[] data, int id, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.HandShake:
                    Dictionary<int, Vector3> newPlayersDic = _netPlayers.Deserialize(data);
                    foreach (KeyValuePair<int, Vector3> kvp in newPlayersDic)
                    {
                        if (_players.ContainsKey(kvp.Key)) continue;
                        GameObject newplayer = Instantiate(PlayerPrefab);
                        newplayer.transform.position = kvp.Value;
                        _players.Add(kvp.Key, newplayer);
                    }
                    break;
                case MessageType.Console:
                    break;
                case MessageType.Position:
                    Vector3 pos = _netVector3.Deserialize(data);
                    if (_players.TryGetValue(id, out GameObject player))
                    {
                        player.transform.position = pos;
                    }
                    else
                    {
                        Debug.LogError($"Player with id {id} not found.");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }

        public MessageType DeserializeMessageType(byte[] data)
        {
            if (data == null || data.Length < 4)
            {
                throw new ArgumentException("Invalid byte array for deserialization");
            }

            int messageTypeInt = BitConverter.ToInt32(data, 0);
            return (MessageType)messageTypeInt;
        }

        public void SendToServer(byte[] data)
        {
            _connection.Send(data);
        }

        public void SendToServer(object data, MessageType messageType)
        {
            byte[] serializedData = new byte[] { };
            switch (messageType)
            {
                case MessageType.HandShake:
                    break;
                case MessageType.Console:
                    break;
                case MessageType.Position:
                    if (data is Vector3 vec3)
                    {
                        serializedData = _netVector3.Serialize(vec3);
                    }
                    else
                    {
                        throw new ArgumentException("Data must be of type Vec3 for Position message type.");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }

            _connection.Send(serializedData);
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