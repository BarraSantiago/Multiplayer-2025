using System;
using System.Collections.Generic;
using Game;
using MultiplayerLib.Game;
using MultiplayerLib.Network.Factory;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace Network.Factory
{
    public class NetworkFactoryManager : MonoBehaviour
    {
        [SerializeField] private UnityView unityView;
        
        private static float BulletSpeed = 5;
        public List<GameObject> registeredPrefabs = new();

        private Dictionary<NetObjectTypes, GameObject> _prefabs = new();
        private Dictionary<int, UnityNetObject> _unityObjects = new();
        private NetworkFactoryImplementation _factory;

        private void Awake()
        {
            _factory = new NetworkFactoryImplementation();
            NetworkObjectFactory.SetInstance(_factory);
            _factory.Initialize(this, unityView);
            RegisterPrefabs();
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
            private Action<int> OnDestroy;
            private UnityView _unityView;
            public void Initialize(NetworkFactoryManager owner, UnityView unityView)
            {
                _owner = owner;
                _unityView = unityView;
            }

            public override void CreateGameObject(INetworkObject netObject, bool isOwner)
            {
                if (!_owner._prefabs.TryGetValue(netObject.PrefabType, out GameObject prefab))
                {
                    Debug.LogError(
                        $"[NetworkFactoryManager] No prefab registered for NetObjectType: {netObject.PrefabType}");
                    return;
                }

                if (!_unityView) return;
                GameObject instance = _unityView.SpawnEntity(netObject as NetEntity, prefab);
                UnityNetObject unityNetObj = instance.AddComponent<UnityNetObject>();
                
                unityNetObj.NetworkObject = netObject;
                _networkObjects[netObject.NetworkId] = netObject;
                _owner._unityObjects[netObject.NetworkId] = unityNetObj;
            }

            public override void UpdateObjectPosition(int id, Vector3 position)
            {
                if (!_owner._unityObjects.TryGetValue(id, out UnityNetObject unityNetObj)) return;
                unityNetObj.transform.position = new UnityEngine.Vector3(position.X, position.Y, position.Z);
                INetworkObject netObj = unityNetObj.NetworkObject;
                netObj.X = (int)position.X;
                netObj.Y = (int)position.Y;
            }

            protected override void RemoveNetworkObject(int networkId)
            {
                if (_networkObjects.TryGetValue(networkId, out INetworkObject? netObj))
                {
                    netObj.OnNetworkDestroy();
                }

                if (!_owner._unityObjects.TryGetValue(networkId, out UnityNetObject unityNetObj)) return;
                Destroy(unityNetObj.gameObject);
                _owner._unityObjects.Remove(networkId);
            }
        }
    }
}