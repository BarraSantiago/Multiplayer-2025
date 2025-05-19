using System;
using System.Collections.Generic;
using Game;
using MultiplayerLib.Network.Factory;
using Network.Server;
using UnityEngine;

namespace Network.Factory
{
    public class NetworkFactoryManager : MonoBehaviour
    {
        private static float BulletSpeed = 5;
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
            private Action<int> OnDestroy;

            public void Initialize(NetworkFactoryManager owner)
            {
                _owner = owner;
            }

            public override void CreateGameObject(NetworkObject createMsg, bool isOwner)
            {
                if (!_owner._prefabs.TryGetValue(createMsg.PrefabType, out GameObject prefab))
                {
                    Debug.LogError(
                        $"[NetworkFactoryManager] No prefab registered for NetObjectType: {createMsg.PrefabType}");
                    return;
                }

                Vector3 position = new Vector3(createMsg.CurrentPos.X, createMsg.CurrentPos.Y, createMsg.CurrentPos.Z);
                GameObject instance = Instantiate(prefab, position, Quaternion.identity);
                UnityNetObject unityNetObj = instance.AddComponent<UnityNetObject>();
                NetworkObject netObj = createMsg;
                instance.GetComponent<MeshRenderer>().material.color = createMsg.Color switch
                {
                    0 => Color.red,
                    1 => Color.blue,
                    2 => Color.green,
                    _ => Color.red
                };

                if (isOwner && createMsg.PrefabType == NetObjectTypes.Projectile)
                {
                    instance.transform.position =
                        new Vector3(PlayerManager.IsMovingRight(createMsg.CreatorId) ? position.x + 1 : position.x - 1,
                            position.y, position.z);
                    Rigidbody rigidBody = instance.AddComponent<Rigidbody>();
                    rigidBody.isKinematic = false;
                    rigidBody.useGravity = false;
                    rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                    rigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                    rigidBody.linearVelocity =
                        new Vector3(PlayerManager.IsMovingRight(createMsg.CreatorId) ? BulletSpeed : -BulletSpeed, 0,
                            0);
                    BulletDamage bullet = instance.AddComponent<BulletDamage>();
                    bullet.NetworkObject = netObj;
                }

                unityNetObj.NetworkObject = netObj;

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

            protected override void RemoveNetworkObject(int networkId)
            {
                if (_networkObjects.TryGetValue(networkId, out NetworkObject? netObj))
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