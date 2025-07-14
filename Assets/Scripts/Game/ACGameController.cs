using MultiplayerLib.Game.Controller;
using MultiplayerLib.Game.Model;
using System;
using AuthClient.Game.Model;
using MultiplayerLib.Game;
using MultiplayerLib.Network.ClientDir;
using UnityEngine;

namespace Game
{
    public class ACGameController : AuthClient.Game.Controller.ACGameController
    {
        private UnityView _view;
        private Camera _mainCamera;
        private LayerMask _entityLayerMask;
        private LayerMask _tileLayerMask;
        private const int GridSize = 30;

        public ACGameController(UnityView view, ACGameManager manager, FactionType faction) : base(faction, manager)
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

            if (Input.GetMouseButtonDown(1) && SelectedUnit != null)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit entityHit, 100f, _entityLayerMask))
                {
                    EntityVisual entityVisual = entityHit.collider.GetComponent<EntityVisual>();
                    if (entityVisual && entityVisual.GetEntity() is NetEntity entity
                                     && (FactionType)entity.FactionId != LocalPlayerFaction)
                    {
                        AttackTarget(entity);
                    }
                }
                else if (Physics.Raycast(ray, out RaycastHit tileHit, 100f, _tileLayerMask))
                {
                    Vector3 worldPos = tileHit.point;
                    Vector2Int gridPos = WorldToGridPosition(worldPos);
                    MoveSelectedUnit(gridPos.x, gridPos.y);
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                EndTurn();
                _view.ClearHighlights();
                SelectedUnit = null;
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

        private void HandleUnitSelected(InfantryUnit unit)
        {
            _view.ClearHighlights();
            _view.HighlightSelectedUnit(unit.NetworkId);
        }

        private void HandleEntityDamaged(NetEntity entity)
        {
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
                    break;

                case GameActionType.EndTurn:
                    break;
            }
        }
    }
}