using Network.Messages;
using UnityEngine;

namespace Network.Factory
{
    public abstract class NetworkObject : MonoBehaviour
    {
        public int NetworkId { get; private set; } = -1;
        public bool IsOwner { get; private set; } = false;
        
        public virtual void Initialize(int networkId, bool isOwner)
        {
            NetworkId = networkId;
            IsOwner = isOwner;
            
            NetworkObjectFactory.Instance.RegisterObject(this);
        }
        
        public virtual void OnNetworkDestroy()
        {
            NetworkObjectFactory.Instance.UnregisterObject(NetworkId);
        }
        
        public virtual void SyncState() 
        {
        }
        
        public virtual void OnNetworkMessage(object data, MessageType messageType) { }
        
        protected virtual void OnDestroy()
        {
            OnNetworkDestroy();
        }
    }
}