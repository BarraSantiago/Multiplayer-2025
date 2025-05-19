using System.Collections.Concurrent;
using System.Collections.Generic;
using Game;
using MultiplayerLib.Game;
using UnityEngine;

namespace Network.Server
{
    public class PlayerManager
    {
        private readonly ConcurrentDictionary<int, GameObject> _players = new();

        private readonly ConcurrentDictionary<int, Controller> _playerControllers = new();

        private readonly ConcurrentDictionary<int, PlayerInput> _lastInput = new();

        private readonly ConcurrentDictionary<int, int> _playerColor = new();

        public bool HasPlayer(int clientId) => _players.ContainsKey(clientId);

        public bool TryGetPlayer(int clientId, out GameObject player) => _players.TryGetValue(clientId, out player);

        public IReadOnlyDictionary<int, GameObject> GetAllPlayers() => _players;

        public GameObject CreatePlayer(int clientId, GameObject player)
        {
            Controller controller = player.AddComponent<Controller>();
            _players[clientId] = player;
            _playerControllers[clientId] = controller;

            return player;
        }

        public void RemovePlayer(int clientId)
        {
            if (!_players.TryRemove(clientId, out GameObject player)) return;

            if (player) Object.Destroy(player);
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
                if (kvp.Value)
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
                if (player) Object.Destroy(player);
            }

            _players.Clear();
        }

        public void UpdatePlayerInput(int clientId, PlayerInput input)
        {
            if (!_players.TryGetValue(clientId, out GameObject player) || !player) return;
            Controller controller = _playerControllers[clientId];
            _lastInput[clientId] = input;
            if (controller)
            {
                controller.UpdateInput(input);
            }
        }
        
        public bool IsMovingRight(int clientId)
        {
            if (_playerControllers.TryGetValue(clientId, out Controller controller))
            {
                return controller.movingRight;
            }
            else
            {
                Debug.LogWarning($"[PlayerManager] Player with id {clientId} not found");
                return false;
            }
        }

        public bool IsCrouching(int clientId)
        {
            if (_playerControllers.TryGetValue(clientId, out Controller controller))
            {
                return controller.isCrouching;
            }
            else
            {
                Debug.LogWarning($"[PlayerManager] Player with id {clientId} not found");
                return false;
            }
        }
        
        public int GetPlayerColor(int clientId)
        {
            if (_playerColor.TryGetValue(clientId, out int color))
            {
                return color;
            }
            else
            {
                Debug.LogWarning($"[PlayerManager] Player with id {clientId} not found");
                return 1;
            }
        }
    }
}