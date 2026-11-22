using System.Collections.Generic;
using NegativeCarver.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NegativeCarver.Unity
{
    [DefaultExecutionOrder(100)]
    public sealed class CutterController : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private CarveFxManager carveFx;
        [SerializeField] private BlockTile previewTilePrefab;

        private readonly List<BlockTile> previewTiles = new List<BlockTile>();
        private readonly CutterQueue queue = new CutterQueue();
        private readonly CarveLedger ledger = new CarveLedger();
        private CutterCommandBuffer commands;
        private CutterPiece piece;
        private PlaySnapshot fallStart;
        private bool falling;
        private float fallElapsed;

        public int LastChainCount { get; private set; }

        public bool CanUndo => ledger.CanUndo;

        public bool CanRedo => ledger.CanRedo;

        public void Enqueue(CutterCommand command)
        {
            commands.Add(command);
        }

        public void UsePiece(CutterPiece next)
        {
            piece = next;
            ShowPreview();
        }

        private void Start()
        {
            SpawnNext();
        }

        private void Update()
        {
            PollUndoKeys();
            if (IsDefeated() || IsCleared())
            {
                commands = default;
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                {
                    commands.Add(CutterCommand.Left);
                }

                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                {
                    commands.Add(CutterCommand.Right);
                }

                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                {
                    commands.Add(CutterCommand.Rotate);
                }

                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                {
                    commands.Add(CutterCommand.HardDrop);
                }
            }

            commands.ConsumeInOrder(MoveLeft, MoveRight, Rotate, HardDrop);
            AdvanceFall();
        }

        public void MoveLeft()
        {
            if (IsDefeated() || IsCleared() || grid == null || piece == null)
            {
                return;
            }

            if (falling)
            {
                Steer(grid.Sidestep(piece, -1));
                return;
            }

            var movedLeft = piece.TryMove(-1);
            if (movedLeft)
            {
                ledger.DiscardRedo();
            }

            ShowPreview();
        }

        public void MoveRight()
        {
            if (IsDefeated() || IsCleared() || grid == null || piece == null)
            {
                return;
            }

            if (falling)
            {
                Steer(grid.Sidestep(piece, 1));
                return;
            }

            var movedRight = piece.TryMove(1);
            if (movedRight)
            {
                ledger.DiscardRedo();
            }

            ShowPreview();
        }

        public void Rotate()
        {
            if (IsDefeated() || IsCleared() || grid == null || piece == null)
            {
                return;
            }

            if (falling)
            {
                var turnedWhileFalling = grid.TryRotateFalling(piece);
                if (turnedWhileFalling)
                {
                    ledger.DiscardRedo();
                    ShowPreview();
                }

                return;
            }

            var turned = piece.TryRotateClockwise();
            if (turned)
            {
                ledger.DiscardRedo();
            }

            ShowPreview();
        }

        public void HardDrop()
        {
            if (IsDefeated() || IsCleared() || grid == null || piece == null)
            {
                return;
            }

            if (falling)
            {
                return;
            }

            ledger.DiscardRedo();
            BeginFall();
        }

        private void BeginFall()
        {
            if (IsDefeated() || IsCleared() || grid == null || piece == null || falling)
            {
                return;
            }

            fallStart = Capture();
            falling = true;
            fallElapsed = PilotRules.FallSecondsPerCell;
        }

        private void AdvanceFall()
        {
            if (!falling || IsDefeated() || IsCleared() || grid == null || piece == null)
            {
                return;
            }

            fallElapsed += Time.deltaTime;
            if (fallElapsed < PilotRules.FallSecondsPerCell)
            {
                return;
            }

            fallElapsed = 0f;
            var descended = grid.Descend(piece);
            if (!descended.Locked)
            {
                if (descended.Moved)
                {
                    ShowPreview();
                }

                return;
            }

            FinishDrop(descended);
        }

        private void Steer(CutterTouch touch)
        {
            if (touch.Locked)
            {
                FinishDrop(touch);
                return;
            }

            if (touch.Moved)
            {
                ledger.DiscardRedo();
                ShowPreview();
            }
        }

        private void FinishDrop(CutterTouch touch)
        {
            falling = false;
            var prior = fallStart;
            var cells = touch.Carved;
            var pattern = new List<Vector2Int>(cells.Length);
            for (var i = 0; i < cells.Length; i++)
            {
                pattern.Add(new Vector2Int(cells[i].X, cells[i].Y));
            }

            grid.CarveArea(pattern, new Vector2Int(0, 0));
            LastChainCount = grid.CheckAndCollapseLines();
            grid.Match.RecordDrop(grid.HasSolidRockOnDefeatLine());
            if (IsDefeated() || IsCleared())
            {
                ClearPreview();
                piece = null;
                ledger.Commit(prior, Capture());
                return;
            }

            SpawnNext();
            ledger.Commit(prior, Capture());
        }

        public void Undo()
        {
            if (grid == null)
            {
                return;
            }

            if (falling)
            {
                falling = false;
                if (fallStart != null && grid.Apply(fallStart))
                {
                    Show(fallStart);
                }

                return;
            }

            var snapshot = ledger.Undo();
            if (snapshot == null)
            {
                return;
            }

            if (!grid.Apply(snapshot))
            {
                return;
            }

            Show(snapshot);
        }

        public void Redo()
        {
            if (grid == null)
            {
                return;
            }

            var snapshot = ledger.Redo();
            if (snapshot == null)
            {
                return;
            }

            if (!grid.Apply(snapshot))
            {
                return;
            }

            Show(snapshot);
        }

        public void Restart()
        {
            falling = false;
            LastChainCount = 0;
            ledger.Clear();
            queue.Reset();
            SpawnNext();
        }

        private void PollUndoKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.zKey.wasPressedThisFrame)
            {
                Undo();
            }

            if (keyboard.yKey.wasPressedThisFrame)
            {
                Redo();
            }
        }

        private PlaySnapshot Capture()
        {
            return grid.Capture(piece, queue.Position);
        }

        private void Show(PlaySnapshot snapshot)
        {
            falling = false;
            LastChainCount = snapshot.ChainCount;
            queue.MoveTo(snapshot.QueueIndex);
            if (!snapshot.HasPiece)
            {
                ClearPreview();
                piece = null;
                return;
            }

            UsePiece(new CutterPiece(snapshot.Shape, snapshot.OriginX, snapshot.OriginY, snapshot.Rotation));
            BeginFall();
        }

        private bool IsDefeated()
        {
            return grid != null && grid.Match.Defeated;
        }

        private bool IsCleared()
        {
            return grid != null && grid.CheckCoreGemFreed();
        }

        private void SpawnNext()
        {
            ClearPreview();
            piece = null;
            if (grid == null || previewTilePrefab == null)
            {
                if (previewTilePrefab == null)
                {
                    Debug.LogError("Cutter preview prefab is not assigned.");
                }

                return;
            }

            piece = queue.Next();
            ShowPreview();
            BeginFall();
        }

        private void ShowPreview()
        {
            ClearPreview();
            if (piece == null || grid == null || previewTilePrefab == null)
            {
                return;
            }

            var cells = piece.Cells;
            for (var i = 0; i < cells.Length; i++)
            {
                var x = piece.OriginX + cells[i].X;
                var y = piece.OriginY + cells[i].Y;
                var center = BoardSpace.CellCenter(x, y);
                var tile = Instantiate(
                    previewTilePrefab,
                    new Vector3(center.X, center.Y, 0f),
                    Quaternion.identity,
                    transform);
                tile.Bind(grid, x, y, BlockType.CutterPreview);
                previewTiles.Add(tile);
            }
        }

        private void ClearPreview()
        {
            for (var i = 0; i < previewTiles.Count; i++)
            {
                if (previewTiles[i] != null)
                {
                    Detach(previewTiles[i]);
                    Destroy(previewTiles[i].gameObject);
                }
            }

            previewTiles.Clear();
        }

        private static void Detach(BlockTile tile)
        {
            if (tile == null)
            {
                return;
            }

            tile.transform.SetParent(null, false);
        }
    }
}
