using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network.Factory
{
    public class FactoryConnection : MonoBehaviour
    {
        [SerializeField] private List<GameObject> registeredPrefabs = new();
        private readonly Dictionary<NetObjectTypes, GameObject> _prefabs = new();

        private void Awake()
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

        public void CreateNetObject(NetworkObjectCreateMessage message)
        {
            if (!_prefabs.TryGetValue(message.PrefabType, out GameObject prefab)) return;

            Transform transf = new RectTransform();
            transf.position = message.Position;
            transf.localScale = Vector3.one;
            GameObject instance = Instantiate(prefab, transf);

            instance.GetComponent<MeshRenderer>().material.color = message.Color switch
            {
                0 => Color.red,
                1 => Color.blue,
                2 => Color.green,
                _ => Color.red
            };
        }
    }
}