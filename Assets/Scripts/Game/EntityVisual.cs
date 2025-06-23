using MultiplayerLib.Game;
using MultiplayerLib.Game.Model;
using UnityEngine;

namespace Game
{
    public class EntityVisual : MonoBehaviour
    {
        private NetEntity entity;
        [SerializeField] private GameObject healthBarPrefab;
        private GameObject healthBarInstance;
        private HealthBar healthBar;

        public NetEntity GetEntity() => entity;

        public void SetEntity(NetEntity entity)
        {
            this.entity = entity;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            // Create health bar if needed
            if (healthBarInstance == null && healthBarPrefab != null)
            {
                healthBarInstance = Instantiate(healthBarPrefab, transform);
                healthBarInstance.transform.localPosition = new Vector3(0, 1, 0);
                healthBar = healthBarInstance.GetComponent<HealthBar>();
            }

            UpdateHealth(entity.Hp);
        }

        public void UpdateHealth(int currentHealth)
        {
            // Update health bar
            if (healthBar == null) return;
            int maxHealth = entity is Castle ? GameManager.CastleStartingHP : GameManager.InfantryStartingHP;
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }
}