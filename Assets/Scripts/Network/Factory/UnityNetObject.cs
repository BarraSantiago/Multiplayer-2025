using System;
using MultiplayerLib.Network.Factory;
using UnityEngine;

namespace Network.Factory
{
    public class UnityNetObject : MonoBehaviour
    {
        public NetworkObject NetworkObject { get; private set; }
        private readonly float _positionThreshold = 0.001f;

        private void Awake()
        {
            NetworkObject = new NetworkObject();
        }

        private void Update()
        {
            if (!IsPositionApproximatelyEqual(NetworkObject.LastUpdatedPos, transform.position, _positionThreshold))
            {
                
            }
        }
        
        private bool IsPositionApproximatelyEqual(System.Numerics.Vector3 sysVec, UnityEngine.Vector3 unityVec, float threshold)
        {
            float dx = sysVec.X - unityVec.x;
            float dy = sysVec.Y - unityVec.y;
            float dz = sysVec.Z - unityVec.z;
            
            return (dx * dx + dy * dy + dz * dz) < (threshold * threshold);
        }
    }
}