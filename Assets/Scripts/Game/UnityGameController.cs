using MultiplayerLib.Game.Controller;
using MultiplayerLib.Game.Model;
using System;
using MultiplayerLib.Game;
using MultiplayerLib.Network.ClientDir;
using UnityEngine;

namespace Game
{
    public class UnityGameController : GameController
    {
        private UnityView _view;
        private Camera _mainCamera;
        private LayerMask _entityLayerMask;
        private LayerMask _tileLayerMask;
        private const int GridSize = 30;

        public UnityGameController(UnityView view, GameManager manager, FactionType faction) : base(faction, manager)
        {
            _view = view;
            _mainCamera = Camera.main;
            _entityLayerMask = LayerMask.GetMask("Entity");
            _tileLayerMask = LayerMask.GetMask("Tile");

            OnUnitSelected += HandleUnitSelected;
            OnEntityDamaged += HandleEntityDamaged;
            OnTurnChanged += HandleTurnChanged;
            OnGameOver += HandleGameOver;
        }

        public void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // Handle unit selection with left click
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, _entityLayerMask))
                {
                    EntityVisual entityVisual = hit.collider.GetComponent<EntityVisual>();
                    if (entityVisual && entityVisual.GetEntity() is InfantryUnit unit
                                     && (FactionType)unit.FactionId == LocalPlayerFaction)
                    {
                        SelectUnit(unit);
                    }
                }
            }

            // Handle movement or attack with right click
            if (Input.GetMouseButtonDown(1) && SelectedUnit != null)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

                // Check if clicked on an entity (potential attack)
                if (Physics.Raycast(ray, out RaycastHit entityHit, 100f, _entityLayerMask))
                {
                    EntityVisual entityVisual = entityHit.collider.GetComponent<EntityVisual>();
                    if (entityVisual && entityVisual.GetEntity() is NetEntity entity
                                     && (FactionType)entity.FactionId != LocalPlayerFaction)
                    {
                        AttackTarget(entity);
                    }
                }
                // Check if clicked on a tile (movement)
                else if (Physics.Raycast(ray, out RaycastHit tileHit, 100f, _tileLayerMask))
                {
                    Vector3 worldPos = tileHit.point;
                    Vector2Int gridPos = WorldToGridPosition(worldPos);
                    MoveSelectedUnit(gridPos.x, gridPos.y);
                }
            }

            // End turn with spacebar
            if (Input.GetKeyDown(KeyCode.Space))
            {
                EndTurn();
            }
        }

        private Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            float gridSize = GridSize;
            float tileSize = _view.TileSize;

            float startX = -(gridSize * tileSize) / 2;
            float startZ = -(gridSize * tileSize) / 2;

            int x = Mathf.RoundToInt((worldPosition.x - startX) / tileSize);
            int y = Mathf.RoundToInt((worldPosition.z - startZ) / tileSize);

            return new Vector2Int(x, y);
        }

        // Event handlers
        private void HandleUnitSelected(InfantryUnit unit)
        {
            // Visual feedback for selected unit
            _view.HighlightSelectedUnit(unit.NetworkId);
        }

        private void HandleEntityDamaged(NetEntity entity)
        {
            // Visual update for the damaged entity
            if (entity.Hp <= 0)
            {
                _view.RemoveEntity(entity.NetworkId);
            }
            else
            {
                _view.UpdateEntityHealth(entity.NetworkId, entity.Hp);
            }
        }

        private void HandleTurnChanged(FactionType newTurn)
        {
            // Update UI for turn change
            _view.UpdateUI();
            SelectedUnit = null;
            _view.ClearHighlights();
        }

        private void HandleGameOver(FactionType winner)
        {
            _view.ShowGameOver(winner);
        }

        public void HandleRemoteAction(PlayerInput action)
        {
            base.HandleRemoteAction(action);

            switch (action.ActionType)
            {
                case GameActionType.UnitMove:
                    _view.UpdateEntityPosition(action.SourceEntityId, action.TargetX, action.TargetY);
                    break;

                case GameActionType.UnitAttack:
                    // The view will be updated via the OnEntityDamaged event
                    break;

                case GameActionType.EndTurn:
                    // The view will be updated via the OnTurnChanged event
                    break;
            }
        }
    }
}