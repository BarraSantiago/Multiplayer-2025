using System;
using MultiplayerLib.Game;
using MultiplayerLib.Network.Factory;
using Network.Factory;
using UnityEngine;

namespace Game
{
    public class BulletDamage : MonoBehaviour
    {
        public NetworkObject NetworkObject { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                UnityNetObject player = other.GetComponent<UnityNetObject>();
                if (!player) return;
                NetPlayer netPlayer = player.NetworkObject as NetPlayer;
                netPlayer.Hp -= NetworkFactoryManager.PlayerManager.IsCrouching(netPlayer.NetworkId) ? 5 : 10;
            }

            NetworkObject.DestroySelf();
            this.enabled = false;
        }
    }
}