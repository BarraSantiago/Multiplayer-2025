using System;
using MultiplayerLib.Game;
using Network.Factory;
using UnityEngine;

namespace Game
{
    public class BulletDamage : MonoBehaviour
    {
        public int NetworkId { get; set; }
        public static Action<int> OnDestroy;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                UnityNetObject player = other.GetComponent<UnityNetObject>();
                if (!player) return;
                NetPlayer netPlayer = player.NetworkObject as NetPlayer;
                netPlayer.Hp -= NetworkFactoryManager.PlayerManager.IsCrouching(netPlayer.NetworkId) ? 5 : 10;
            }

            OnDestroy?.Invoke(NetworkId);
            this.enabled = false;
        }
    }
}