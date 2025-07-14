using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Gradient healthGradient;

        public void UpdateHealthBar(int currentHealth, int maxHealth)
        {
            if (!healthSlider)
                return;

            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }
}