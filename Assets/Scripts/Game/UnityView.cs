using System;
using System.Collections.Generic;
using AuthClient.Game.Model;
using AuthClient.Network.ClientDir;
using MultiplayerLib.Game;
using MultiplayerLib.Game.Model;
using TMPro;
using UnityEngine;

namespace Game
{
    public class UnityView : MonoBehaviour
    {
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

        public Dictionary<int, GameObject> entityObjects = new Dictionary<int, GameObject>();
        private GameObject[,] gridTiles;
        public float TileSize => tileSize;
        private ACGameManager gameManager;
        private void Start()
        {
            CreateGrid();
            NetEntity.OnEntityPositionUpdate += UpdateEntityPosition;
            NetEntity.OnEntityHpUpdate += UpdateEntityHealth;
            ACClientMessageDispatcher.OnHandshakeComplete += InitializeGameManager;
        }

        private void Update()
        {
            if (gameManager == null) return;

            UpdateUI();
        }

        private void InitializeGameManager(ACGameManager arg1, FactionType arg2)
        {
            gameManager = arg1;
        }

        private void OnDestroy()
        {
            NetEntity.OnEntityPositionUpdate -= UpdateEntityPosition;
            foreach (GameObject obj in entityObjects.Values)
            {
                Destroy(obj);
            }
            entityObjects.Clear();
        }
        public void Initialize()
        {
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
            // Clear previous entities
            foreach (GameObject obj in entityObjects.Values)
            {
                Destroy(obj);
            }

            entityObjects.Clear();

            UpdateUI();
        }

        public GameObject SpawnEntity(NetEntity entity, GameObject prefab)
        {
            Vector3 position = GridToWorldPosition((int)entity.X, (int)entity.Y);
            GameObject entityObject = Instantiate(prefab, position, Quaternion.identity, gridContainer);
            entityObject.name = $"{entity.GetType().Name}_{entity.NetworkId}";

            // Add component to store entity data and update visual elements
            EntityVisual visual = entityObject.GetComponent<EntityVisual>();
            visual.SetEntity(entity);

            entityObjects[entity.NetworkId] = entityObject;
            return entityObject;
        }

        public void UpdateUI()
        {
            if (turnText)
                turnText.text = $"Turn: {gameManager.CurrentTurn}";

            if (movementsText)
                movementsText.text = $"Movements: {gameManager.RemainingMovements}";

            if (redCastleHealthText)
                redCastleHealthText.text = $"Red Castle: {gameManager.RedCastle.Hp}/{100}";

            if (blueCastleHealthText)
                blueCastleHealthText.text = $"Blue Castle: {gameManager.BlueCastle.Hp}/{100}";

            if (!gameOverPanel) return;
            gameOverPanel.SetActive(gameManager.GameOver);
            if (winnerText && gameManager.GameOver)
            {
                winnerText.text = $"{gameManager.Winner} faction wins!";
            }
        }

        public void UpdateEntityPosition(int networkId, int newX, int newY)
        {
            if (entityObjects.TryGetValue(networkId, out GameObject obj))
            {
                obj.transform.position = GridToWorldPosition(newX, newY);
            }
        }

        public void RemoveEntity(int networkId)
        {
            if (!entityObjects.TryGetValue(networkId, out GameObject obj)) return;
            
        }
        
        public void HighlightSelectedUnit(int networkId)
        {
            if (!entityObjects.TryGetValue(networkId, out GameObject obj)) return;
            UnitHighlighter highlighter = obj.GetComponent<UnitHighlighter>() ?? obj.AddComponent<UnitHighlighter>();
            highlighter.Highlight();
        }
        
        public void ClearHighlights()
        {
            foreach (GameObject obj in entityObjects.Values)
            {
                if (!obj) continue;
                UnitHighlighter highlighter = obj.GetComponent<UnitHighlighter>();
                if (highlighter)
                    highlighter.RemoveHighlight();
            }
        }

        public void UpdateEntityHealth(int networkId, int health)
        {
            if (!entityObjects.TryGetValue(networkId, out GameObject obj)) return;
            EntityVisual visual = obj.GetComponent<EntityVisual>();
            if (visual != null)
                visual.UpdateHealth(health);
        }

        public void ShowGameOver(FactionType winner)
        {
            gameOverPanel.SetActive(true);
            winnerText.text = $"{winner} faction wins!";
        }
    }
}