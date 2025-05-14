using System;
using System.Collections.Generic;
using MultiplayerLib.Network.Factory;
using UnityEngine;

namespace Network.Factory
{
    public class FactoryConnection : NetworkObjectFactory
    {

        private ObjectCreator _objectCreator;
        public FactoryConnection( ObjectCreator objectCreator)
        {
            _objectCreator = objectCreator;
        }

        public override void CreateGameObject(NetworkObjectCreateMessage createMsg)
        {
            _objectCreator.CreateObject(createMsg);
        }

        public override void UpdateObjectPosition(int id, System.Numerics.Vector3 position)
        {
            if (_networkObjects.TryGetValue(id, out NetworkObject networkObject))
            {
                networkObject.UpdatePosition(position);
                OnPositionUpdate?.Invoke(id, position);
            }
            else
            {
                Debug.LogError($"[FactoryConnection] No object found with ID: {id}");
            }
        }
    }
    
}