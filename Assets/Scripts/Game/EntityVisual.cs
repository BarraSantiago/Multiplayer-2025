using System;
using MultiplayerLib.Game;
using MultiplayerLib.Game.Model;
using UnityEngine;

namespace Game
{
    public class EntityVisual : MonoBehaviour
    {
        [SerializeField] private HealthBar healthBar;
        private NetEntity entity;
        private GameObject healthBarInstance;
        

        public NetEntity GetEntity() => entity;

        public void SetEntity(NetEntity entity)
        {
            this.entity = entity;
            UpdateVisuals();
        }

        private void Update()
        {
            UpdateHealth(entity?.Hp ?? 0);
        }

        public void UpdateVisuals()
        {
            UpdateHealth(entity.Hp);
        }

        public void UpdateHealth(int currentHealth)
        {
            if (!healthBar) return;
            int maxHealth = entity is Castle ? 100 : 50;
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }
}