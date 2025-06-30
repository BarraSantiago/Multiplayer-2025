using MultiplayerLib.Network.Factory;
using UnityEngine;

namespace Network.Factory
{
    public class UnityNetObject : MonoBehaviour
    {
        public INetworkObject NetworkObject { get; set; }
        private readonly float _positionThreshold = 0.001f;

        private void Update()
        {
            NetworkObject.X = transform.position.x;
            NetworkObject.Y = transform.position.y;
        }

        private System.Numerics.Vector3 ConvertToSystemVector3(Vector3 unityVec)
        {
            return new System.Numerics.Vector3(unityVec.x, unityVec.y, unityVec.z);
        }

        private Vector3 ConvertToUnityVector3(System.Numerics.Vector3 sysVec)
        {
            return new Vector3(sysVec.X, sysVec.Y, sysVec.Z);
        }
    }
}