using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    public class PlayerManager
    {
        private readonly GameObject _playerPrefab;
        private readonly ConcurrentDictionary<int, GameObject> _players = new ConcurrentDictionary<int, GameObject>();

        public PlayerManager(GameObject playerPrefab)
        {
            _playerPrefab = playerPrefab;
        }

        public bool HasPlayer(int clientId) => _players.ContainsKey(clientId);
        
        public bool TryGetPlayer(int clientId, out GameObject player) => 
            _players.TryGetValue(clientId, out player);
            
        public IReadOnlyDictionary<int, GameObject> GetAllPlayers() => _players;

        public GameObject CreatePlayer(int clientId, Vector3? position = null)
        {
            GameObject player = Object.Instantiate(_playerPrefab);
            if (position.HasValue)
            {
                player.transform.position = position.Value;
            }
            
            _players[clientId] = player;
            return player;
        }
        
        public bool RemovePlayer(int clientId)
        {
            if (!_players.TryRemove(clientId, out GameObject player))
                return false;
                
            if (player != null)
                Object.Destroy(player);
                
            return true;
        }
        
        public void UpdatePlayerPosition(int clientId, Vector3 position)
        {
            if (_players.TryGetValue(clientId, out GameObject player) && player != null)
            {
                player.transform.position = position;
            }
            else
            {
                Debug.LogWarning($"[PlayerManager] Player with id {clientId} not found");
            }
        }
        
        public Dictionary<int, Vector3> GetPlayerPositions()
        {
            Dictionary<int, Vector3> positions = new Dictionary<int, Vector3>();
            
            foreach (KeyValuePair<int, GameObject> kvp in _players)
            {
                if (kvp.Value != null)
                {
                    positions[kvp.Key] = kvp.Value.transform.position;
                }
            }
            
            return positions;
        }
        
        public void Clear()
        {
            foreach (GameObject player in _players.Values)
            {
                if (player != null) Object.Destroy(player);
            }
            _players.Clear();
        }
    }
}