using System;
using System.Collections.Generic;
using MultiplayerLib.Game;
using MultiplayerLib.Network.Factory;
using UnityEngine;

namespace Network.Factory
{
    public class NetworkFactoryManager : MonoBehaviour
    {
        public List<GameObject> registeredPrefabs = new();

        public static PlayerManager PlayerManager;
        private Dictionary<NetObjectTypes, GameObject> _prefabs = new();
        private Dictionary<int, UnityNetObject> _unityObjects = new();
        private NetworkFactoryImplementation _factory;
        private void Awake()
        {
            _factory = new NetworkFactoryImplementation();
            NetworkObjectFactory.SetInstance(_factory);
            _factory.Initialize(this);
            RegisterPrefabs();
        }

        private void Update()
        {
            _factory.SyncPositions();
        }

        private void RegisterPrefabs()
        {
            NetObjectTypes[] netObjTypes = (NetObjectTypes[])Enum.GetValues(typeof(NetObjectTypes));
            for (int i = 0; i < registeredPrefabs.Count; i++)
            {
                GameObject prefab = registeredPrefabs[i];
                if (i + 1 < netObjTypes.Length)
                {
                    RegisterPrefab(prefab, netObjTypes[i + 1]);
                }
            }
        }

        public void RegisterPrefab(GameObject prefab, NetObjectTypes netObjType)
        {
            if (_prefabs.ContainsKey(netObjType)) return;
            _prefabs[netObjType] = prefab;
        }

        private class NetworkFactoryImplementation : NetworkObjectFactory
        {
            private NetworkFactoryManager _owner;

            public void Initialize(NetworkFactoryManager owner)
            {
                _owner = owner;
            }

            public override void CreateGameObject(NetworkObjectCreateMessage createMsg)
            {
                if (!_owner._prefabs.TryGetValue(createMsg.PrefabType, out GameObject prefab))
                {
                    Debug.LogError($"[NetworkFactoryManager] No prefab registered for NetObjectType: {createMsg.PrefabType}");
                    return;
                }

                Vector3 position = new Vector3(createMsg.Position.X, createMsg.Position.Y, createMsg.Position.Z);
                GameObject instance = Instantiate(prefab, position, Quaternion.identity);

                UnityNetObject unityNetObj = instance.AddComponent<UnityNetObject>();
                NetworkObject netObj = createMsg.PrefabType switch
                {
                    NetObjectTypes.Player => new NetPlayer(createMsg.Position, NetObjectTypes.Player),
                    NetObjectTypes.Projectile => new Bullet(createMsg.Position, NetObjectTypes.Projectile),
                    _ => throw new ArgumentOutOfRangeException()
                };

                unityNetObj.NetworkObject = netObj;
                netObj.Initialize(createMsg.NetworkId, false, createMsg.PrefabType);

                _networkObjects[createMsg.NetworkId] = netObj;
                _owner._unityObjects[createMsg.NetworkId] = unityNetObj;
                if (createMsg.PrefabType == NetObjectTypes.Player)
                {
                    PlayerManager.CreatePlayer(createMsg.NetworkId, instance);
                }
            }

            public override void UpdateObjectPosition(int id, System.Numerics.Vector3 position)
            {
                if (!_owner._unityObjects.TryGetValue(id, out UnityNetObject unityNetObj)) return;
                unityNetObj.transform.position = new Vector3(position.X, position.Y, position.Z);
                NetworkObject netObj = unityNetObj.NetworkObject;
                netObj.CurrentPos = position;
                netObj.LastUpdatedPos = position;
            }
        }
    }
}