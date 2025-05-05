using System;
using System.Collections.Generic;
using Network.interfaces;
using Network.Messages;
using UnityEngine;
using Utils;

namespace Network.Factory
{
    public enum NetObjectTypes
    {
        None,
        Player,
        Projectile
    }

    public class NetworkObjectFactory : MonoBehaviourSingleton<NetworkObjectFactory>
    {
        private readonly Dictionary<int, NetworkObject> _networkObjects = new Dictionary<int, NetworkObject>();
        private readonly Dictionary<NetObjectTypes, GameObject> _prefabs = new Dictionary<NetObjectTypes, GameObject>();
        private int _networkIdCounter = 0;

        [SerializeField] private List<GameObject> registeredPrefabs = new List<GameObject>();

        private void Awake()
        {
            NetObjectTypes[] netObjTypes = (NetObjectTypes[])Enum.GetValues(typeof(NetObjectTypes));
            for (int i = 0; i < registeredPrefabs.Count; i++)
            {
                GameObject prefab = registeredPrefabs[i];
                NetworkObject netObj = prefab.GetComponent<NetworkObject>();
                if (netObj)
                {
                    RegisterPrefab(prefab, netObjTypes[i + 1]);
                }
            }
        }

        public void RegisterPrefab(GameObject prefab, NetObjectTypes netObjType)
        {
            if (_prefabs.ContainsKey(netObjType)) return;

            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            if (!netObj) return;

            _prefabs[netObjType] = prefab;
        }

        public NetworkObject CreateNetworkObject(Vector3 position, Vector3 rotation, NetObjectTypes netObj,
            bool isOwner = false)
        {
            if (!_prefabs.TryGetValue(netObj, out GameObject prefab)) return null;


            int netId = GetNextNetworkId();
            Quaternion rot = Quaternion.Euler(rotation);
            GameObject instance = Instantiate(prefab, position, rot);
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();
            networkObject.Initialize(netId, isOwner);

            if (AbstractNetworkManager.Instance is ServerNetworkManager serverManager)
            {
                NetworkObjectCreateMessage createMsg = new NetworkObjectCreateMessage
                {
                    NetworkId = netId,
                    PrefabType = netObj,
                    Position = position,
                    Rotation = rotation
                };

                serverManager.SerializedBroadcast(createMsg, MessageType.ObjectCreate);
            }

            return networkObject;
        }

        public void RegisterObject(NetworkObject obj)
        {
            if (obj.NetworkId == -1)
            {
                obj.Initialize(GetNextNetworkId(), true);
            }

            _networkObjects[obj.NetworkId] = obj;
        }

        public void UnregisterObject(int networkId)
        {
            if (_networkObjects.ContainsKey(networkId))
            {
                _networkObjects.Remove(networkId);

                if (AbstractNetworkManager.Instance is ServerNetworkManager serverManager)
                {
                    serverManager.SerializedBroadcast(networkId, MessageType.ObjectDestroy);
                }
            }
        }

        public NetworkObject GetNetworkObject(int networkId)
        {
            return _networkObjects.TryGetValue(networkId, out NetworkObject obj) ? obj : null;
        }

        public void DestroyNetworkObject(int networkId)
        {
            if (_networkObjects.TryGetValue(networkId, out NetworkObject obj))
            {
                Destroy(obj.gameObject);
                _networkObjects.Remove(networkId);
            }
        }

        private int GetNextNetworkId()
        {
            return _networkIdCounter++;
        }

        public void HandleCreateObjectMessage(NetworkObjectCreateMessage createMsg)
        {
            NetObjectTypes objectTypeType = createMsg.PrefabType;
            
            if (objectTypeType == null) return;
            if (!_prefabs.TryGetValue(objectTypeType, out GameObject prefab)) return;

            GameObject instance = Instantiate(prefab, createMsg.Position, Quaternion.Euler(createMsg.Rotation));
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();
            networkObject.Initialize(createMsg.NetworkId, false);
        }
    }

    [Serializable]
    public class NetworkObjectCreateMessage
    {
        public int NetworkId;
        public NetObjectTypes PrefabType;
        public Vector3 Position;
        public Vector3 Rotation;
    }
}