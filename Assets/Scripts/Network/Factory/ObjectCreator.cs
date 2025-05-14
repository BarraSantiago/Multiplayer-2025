using System;
using System.Collections.Generic;
using MultiplayerLib.Network.Factory;
using UnityEngine;

namespace Network.Factory
{
    public class ObjectCreator : MonoBehaviour
    {
        public List<GameObject> registeredPrefabs = new();
        private readonly Dictionary<NetObjectTypes, GameObject> _prefabs = new();

        public void Awake()
        {
            NetObjectTypes[] netObjTypes = (NetObjectTypes[])Enum.GetValues(typeof(NetObjectTypes));
            for (int i = 0; i < registeredPrefabs.Count; i++)
            {
                GameObject prefab = registeredPrefabs[i];
                NetworkObject netObj = prefab.GetComponent<NetworkObject>();
                if (netObj != null) RegisterPrefab(prefab, netObjTypes[i + 1]);
            }
        }

        public void RegisterPrefab(GameObject prefab, NetObjectTypes netObjType)
        {
            if (_prefabs.ContainsKey(netObjType)) return;

            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            if (netObj == null) return;

            _prefabs[netObjType] = prefab;
        }


        public NetworkObject CreateObject(NetworkObjectCreateMessage obj)
        {
            if (!_prefabs.TryGetValue(obj.PrefabType, out GameObject prefab))
            {
                Debug.LogError($"[FactoryConnection] No prefab registered for NetObjectType: {obj.PrefabType}");
                return null;
            }

            GameObject instance = Instantiate(prefab, new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z), Quaternion.identity);
            NetworkObject netObj = instance.GetComponent<UnityNetObject>();
            netObj.Initialize(obj.NetworkId, false, obj.PrefabType);
            return netObj;
        }
    }
}