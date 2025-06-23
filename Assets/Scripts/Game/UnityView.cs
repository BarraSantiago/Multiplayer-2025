using System.Collections.Generic;
using MultiplayerLib.Game;
using MultiplayerLib.Game.Model;
using TMPro;
using UnityEngine;

namespace Game
{
    public class UnityView : MonoBehaviour
    {
        [Header("Game References")]
        [SerializeField] private GameManager gameManager;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject redCastlePrefab;
        [SerializeField] private GameObject blueCastlePrefab;
        [SerializeField] private GameObject redInfantryPrefab;
        [SerializeField] private GameObject blueInfantryPrefab;
        [SerializeField] private GameObject emptyTilePrefab;
        
        [Header("UI References")]
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text movementsText;
        [SerializeField] private TMP_Text redCastleHealthText;
        [SerializeField] private TMP_Text blueCastleHealthText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text winnerText;
        
        [Header("Grid Settings")]
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Transform gridContainer;

        private Dictionary<int, GameObject> entityObjects = new Dictionary<int, GameObject>();
        private GameObject[,] gridTiles;

        private void Start()
        {
            GameManager manager = new GameManager();
            Initialize(manager);
        }

        public void Initialize(GameManager gameManager)
        {
            this.gameManager = gameManager;
            CreateGrid();
            UpdateGameView();
        }

        private void CreateGrid()
        {
            gridTiles = new GameObject[GameManager.GridSize, GameManager.GridSize];
            
            for (int y = 0; y < GameManager.GridSize; y++)
            {
                for (int x = 0; x < GameManager.GridSize; x++)
                {
                    Vector3 position = GridToWorldPosition(x, y);
                    GameObject tile = Instantiate(emptyTilePrefab, position, Quaternion.identity, gridContainer);
                    tile.name = $"Tile_{x}_{y}";
                    gridTiles[x, y] = tile;
                }
            }
        }

        private Vector3 GridToWorldPosition(int x, int y)
        {
            float startX = -(GameManager.GridSize * tileSize) / 2;
            float startZ = -(GameManager.GridSize * tileSize) / 2;
            return new Vector3(startX + x * tileSize, 0, startZ + y * tileSize);
        }

        public void UpdateGameView()
        {
            if (gameManager == null) return;
            
            // Clear previous entities
            foreach (var obj in entityObjects.Values)
            {
                Destroy(obj);
            }
            entityObjects.Clear();
            
            // Create castles
            SpawnEntity(gameManager.RedCastle, redCastlePrefab);
            SpawnEntity(gameManager.BlueCastle, blueCastlePrefab);
            
            // Create infantry units
            foreach (var unit in gameManager.RedUnits)
            {
                SpawnEntity(unit, redInfantryPrefab);
            }
            
            foreach (var unit in gameManager.BlueUnits)
            {
                SpawnEntity(unit, blueInfantryPrefab);
            }
            
            // Update UI
            UpdateUI();
        }
        
        private void SpawnEntity(NetEntity entity, GameObject prefab)
        {
            Vector3 position = GridToWorldPosition((int)entity.X, (int)entity.Y);
            GameObject entityObject = Instantiate(prefab, position, Quaternion.identity, gridContainer);
            entityObject.name = $"{entity.GetType().Name}_{entity.NetworkId}";
            
            // Add component to store entity data and update visual elements
            EntityVisual visual = entityObject.AddComponent<EntityVisual>();
            visual.SetEntity(entity);
            
            entityObjects[entity.NetworkId] = entityObject;
        }
        
        private void UpdateUI()
        {
            if (turnText != null)
                turnText.text = $"Turn: {gameManager.CurrentTurn}";
                
            if (movementsText != null)
                movementsText.text = $"Movements: {gameManager.RemainingMovements}";
                
            if (redCastleHealthText != null)
                redCastleHealthText.text = $"Red Castle: {gameManager.RedCastle.Hp}/{GameManager.CastleStartingHP}";
                
            if (blueCastleHealthText != null)
                blueCastleHealthText.text = $"Blue Castle: {gameManager.BlueCastle.Hp}/{GameManager.CastleStartingHP}";
                
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(gameManager.GameOver);
                if (winnerText != null && gameManager.GameOver)
                {
                    winnerText.text = $"{gameManager.Winner} faction wins!";
                }
            }
        }
        
        // Call this when an entity moves
        public void UpdateEntityPosition(int networkId, int newX, int newY)
        {
            if (entityObjects.TryGetValue(networkId, out GameObject obj))
            {
                obj.transform.position = GridToWorldPosition(newX, newY);
            }
        }
        
        // Call this when an entity is removed
        public void RemoveEntity(int networkId)
        {
            if (entityObjects.TryGetValue(networkId, out GameObject obj))
            {
                Destroy(obj);
                entityObjects.Remove(networkId);
            }
        }
    }
    
    // Helper class to manage entity visuals
    public class EntityVisual : MonoBehaviour
    {
        private NetEntity entity;
        [SerializeField] private GameObject healthBarPrefab;
        private GameObject healthBarInstance;
        
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
            }
            
            // Update health bar
            if (healthBarInstance != null)
            {
                // Update health bar visual based on entity.Hp and max health
            }
        }
    }
}