using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace Network
{
    public class ClientManager
    {
        private readonly ConcurrentDictionary<int, Client> _clients = new ConcurrentDictionary<int, Client>();
        private readonly ConcurrentDictionary<IPEndPoint, int> _ipToId = new ConcurrentDictionary<IPEndPoint, int>();
        private int _clientIdCounter = 0;

        public Action<int> OnClientConnected;
        public Action<int> OnClientDisconnected;

        public bool HasClient(IPEndPoint endpoint) => _ipToId.ContainsKey(endpoint);

        public bool TryGetClientId(IPEndPoint endpoint, out int clientId) =>
            _ipToId.TryGetValue(endpoint, out clientId);

        public bool TryGetClient(int clientId, out Client client) =>
            _clients.TryGetValue(clientId, out client);

        public IReadOnlyDictionary<int, Client> GetAllClients() => _clients;

        public int AddClient(IPEndPoint endpoint)
        {
            if (_ipToId.ContainsKey(endpoint))
                return _ipToId[endpoint];

            int id = _clientIdCounter++;
            Client newClient = new Client(endpoint, id, Time.realtimeSinceStartup);

            _ipToId[endpoint] = id;
            _clients[id] = newClient;

            OnClientConnected?.Invoke(id);
            Debug.Log($"[ClientManager] Client added: {endpoint.Address}, ID: {id}");

            return id;
        }

        public bool RemoveClient(IPEndPoint endpoint)
        {
            if (!_ipToId.TryGetValue(endpoint, out int clientId))
                return false;

            _ipToId.TryRemove(endpoint, out _);
            _clients.TryRemove(clientId, out _);

            OnClientDisconnected?.Invoke(clientId);
            return true;
        }

        public void UpdateClientTimestamp(int clientId)
        {
            if (_clients.TryGetValue(clientId, out Client client))
            {
                client.lastHeartbeatTime = Time.realtimeSinceStartup;
                _clients[clientId] = client;
            }
        }

        public List<IPEndPoint> GetTimedOutClients(float timeout)
        {
            float currentTime = Time.realtimeSinceStartup;
            List<IPEndPoint> timedOut = new List<IPEndPoint>();

            foreach (KeyValuePair<int, Client> client in _clients)
            {
                if (currentTime - client.Value.lastHeartbeatTime > timeout)
                {
                    timedOut.Add(client.Value.ipEndPoint);
                    Debug.Log($"[ClientManager] Client {client.Key} timed out");
                }
            }

            return timedOut;
        }

        public void Clear()
        {
            _clients.Clear();
            _ipToId.Clear();
        }
    }
}