using UnityEngine;

namespace Game
{
    public class UnitHighlighter : MonoBehaviour
    {
        [SerializeField] private Color highlightColor = Color.yellow;
        private Material[] originalMaterials;
        private Renderer[] renderers;
        private bool isHighlighted = false;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            StoreOriginalMaterials();
        }

        private void StoreOriginalMaterials()
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }

        public void Highlight()
        {
            if (isHighlighted)
                return;

            isHighlighted = true;

            foreach (Renderer renderer in renderers)
            {
                Material highlightMaterial = new Material(renderer.material);
                highlightMaterial.color = highlightColor;
                renderer.material = highlightMaterial;
            }
        }

        public void RemoveHighlight()
        {
            if (!isHighlighted)
                return;

            isHighlighted = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = originalMaterials[i];
            }
        }
    }
}