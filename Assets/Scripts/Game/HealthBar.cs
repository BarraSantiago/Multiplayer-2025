using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private Gradient healthGradient;

        public void UpdateHealthBar(int currentHealth, int maxHealth)
        {
            if (healthFill == null)
                return;

            float healthPercentage = Mathf.Clamp01((float)currentHealth / maxHealth);
            healthFill.fillAmount = healthPercentage;
            
            if (healthGradient != null)
                healthFill.color = healthGradient.Evaluate(healthPercentage);
        }
    }
}