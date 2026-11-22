using NegativeCarver.Core;
using UnityEngine;

namespace NegativeCarver.Unity
{
    public sealed class GridManager : MonoBehaviour
    {
        [SerializeField] private CutterController cutter;
        [SerializeField] private CarveFxManager carveFx;
        [SerializeField] private BlockTile rockTilePrefab;
        [SerializeField] private BlockTile coreGemTilePrefab;

        private Board board;
        private BlockTile[,] tiles;

        public MatchState Match { get; } = new MatchState();

        private void Awake()
        {
            board = new Board(new System.Random());
            tiles = new BlockTile[board.Width, board.Height];
            SpawnInitialRocks();
        }

        public void DropToFirstContact(CutterPiece piece)
        {
            piece.DropToFirstContact(board);
        }

        public CutterTouch Descend(CutterPiece piece)
        {
            if (piece == null || board == null)
            {
                return CutterTouch.None;
            }

            return piece.TryDescend(board);
        }

        public CutterTouch Sidestep(CutterPiece piece, int deltaX)
        {
            if (piece == null || board == null)
            {
                return CutterTouch.None;
            }

            return piece.TrySidestep(board, deltaX);
        }

        public bool TryRotateFalling(CutterPiece piece)
        {
            if (piece == null || board == null)
            {
                return false;
            }

            return piece.TryRotateClockwise(board);
        }

        public PlaySnapshot Capture(CutterPiece piece, int queueIndex)
        {
            return PlaySnapshot.Take(board, Match, piece, queueIndex);
        }

        public bool Apply(PlaySnapshot snapshot)
        {
            if (snapshot == null || board == null || tiles == null)
            {
                return false;
            }

            if (!board.RestoreCells(snapshot.CopyCells(), snapshot.CoreOverlap))
            {
                return false;
            }

            Match.Restore(snapshot.ChainCount, snapshot.DropCount, snapshot.Defeated);
            ClearTiles();
            tiles = new BlockTile[board.Width, board.Height];
            SpawnInitialRocks();
            return true;
        }

        public void CarveArea(System.Collections.Generic.List<Vector2Int> cutterPattern, Vector2Int origin)
        {
            if (cutterPattern == null || board == null || tiles == null)
            {
                return;
            }

            var pattern = new CellOffset[cutterPattern.Count];
            for (var i = 0; i < cutterPattern.Count; i++)
            {
                pattern[i] = new CellOffset(cutterPattern[i].x, cutterPattern[i].y);
            }

            var carved = board.CarveSolids(origin.x, origin.y, pattern);
            for (var i = 0; i < carved.Count; i++)
            {
                var cell = carved[i];
                var center = BoardSpace.CellCenter(cell.X, cell.Y);
                if (carveFx != null)
                {
                    carveFx.PlayCarveEffect(new Vector3(center.X, center.Y, 0f));
                }

                var tile = tiles[cell.X, cell.Y];
                if (tile == null)
                {
                    continue;
                }

            tiles[cell.X, cell.Y] = null;
            Detach(tile);
            Destroy(tile.gameObject);
            }

            var softened = ShowSoftened(origin.x, origin.y, pattern);
            var impact = CarveImpacts.FromRemoved(carved.Count);

            if (carveFx != null && (carved.Count > 0 || softened > 0))
            {
                carveFx.PlayCameraShake();
                carveFx.PlayCarveSound(impact);
            }
        }

        public bool CheckPenetration(int y)
        {
            if (board == null)
            {
                return false;
            }

            return board.CheckPenetration(y);
        }

        public void ApplyGravity()
        {
            if (board == null || tiles == null)
            {
                return;
            }

            board.ClearCoreOverlap();
            var rows = board.PenetratedRowsHighToLow();
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                board.ShiftRowDown(row);
                ShiftTilesDown(row);
            }

            WarnIfCoreOverlapped();
        }

        public int CheckAndCollapseLines()
        {
            if (board == null || tiles == null)
            {
                return 0;
            }

            var rows = board.CollapseChains();
            for (var i = 0; i < rows.Length; i++)
            {
                ShiftTilesDown(rows[i]);
            }

            WarnIfCoreOverlapped();
            Match.RecordChain(rows.Length);
            if (carveFx != null)
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    carveFx.PlayCollapseEffect(rows[i]);
                }
            }

            return rows.Length;
        }

        private void WarnIfCoreOverlapped()
        {
            if (board != null && board.CoreOverlapDetected)
            {
                Debug.LogError("Core gem overlaps another block.");
            }
        }

        private void ShiftTilesDown(int row)
        {
            for (var y = row; y < board.Height - 1; y++)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    tiles[x, y] = tiles[x, y + 1];
                    var tile = tiles[x, y];
                    if (tile == null)
                    {
                        continue;
                    }

                    var center = BoardSpace.CellCenter(x, y);
                    tile.transform.position = new Vector3(center.X, center.Y, 0f);
                    tile.Bind(this, x, y, board.Get(x, y));
                }
            }

            for (var x = 0; x < board.Width; x++)
            {
                tiles[x, board.Height - 1] = null;
            }
        }

        public bool HasSolidRockOnDefeatLine()
        {
            if (board == null)
            {
                return false;
            }

            return board.HasSolidRockAtOrAbove(PilotRules.DefeatHeight);
        }

        public bool CheckCoreGemFreed()
        {
            if (board == null)
            {
                return false;
            }

            return board.IsCoreFreed();
        }

        public int BlockCount
        {
            get
            {
                if (tiles == null)
                {
                    return 0;
                }

                var count = 0;
                for (var y = 0; y < tiles.GetLength(1); y++)
                {
                    for (var x = 0; x < tiles.GetLength(0); x++)
                    {
                        if (tiles[x, y] != null)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        public void Restart()
        {
            ClearTiles();
            board = new Board(new System.Random());
            tiles = new BlockTile[board.Width, board.Height];
            Match.Reset();
            SpawnInitialRocks();
            if (cutter != null)
            {
                cutter.Restart();
            }
        }

        public void AdvanceStage()
        {
            if (CheckCoreGemFreed())
            {
                Match.BankStageClear();
            }

            Match.AdvanceStage();
            Restart();
        }

        public void RemoveTile(BlockTile tile)
        {
            if (tile == null || board == null || tiles == null)
            {
                return;
            }

            if (!board.Contains(tile.X, tile.Y))
            {
                return;
            }

            board.TryClear(tile.X, tile.Y);
            if (tiles[tile.X, tile.Y] == tile)
            {
                tiles[tile.X, tile.Y] = null;
            }

            Detach(tile);
            Destroy(tile.gameObject);
        }

        private void ClearTiles()
        {
            if (tiles == null)
            {
                return;
            }

            for (var y = 0; y < tiles.GetLength(1); y++)
            {
                for (var x = 0; x < tiles.GetLength(0); x++)
                {
                    var tile = tiles[x, y];
                    if (tile != null)
                    {
                        Detach(tile);
                        Destroy(tile.gameObject);
                    }
                }
            }
        }

        private void SpawnInitialRocks()
        {
            if (rockTilePrefab == null)
            {
                Debug.LogError("Rock tile prefab is not assigned.");
                return;
            }

            if (coreGemTilePrefab == null)
            {
                Debug.LogError("Core gem tile prefab is not assigned.");
                return;
            }

            for (var y = 0; y < board.Height; y++)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    var type = board.Get(x, y);
                    BlockTile prefab;
                    if (type == BlockType.SolidRock)
                    {
                        prefab = rockTilePrefab;
                    }
                    else if (type == BlockType.HardRock)
                    {
                        prefab = rockTilePrefab;
                    }
                    else if (type == BlockType.CoreGem)
                    {
                        prefab = coreGemTilePrefab;
                    }
                    else
                    {
                        continue;
                    }

                    var center = BoardSpace.CellCenter(x, y);
                    var tile = Instantiate(
                        prefab,
                        new Vector3(center.X, center.Y, 0f),
                        Quaternion.identity,
                        transform);
                    tile.Bind(this, x, y, type);
                    tiles[x, y] = tile;
                }
            }
        }

        private static void Detach(BlockTile tile)
        {
            if (tile == null)
            {
                return;
            }

            tile.transform.SetParent(null, false);
        }

        private int ShowSoftened(int originX, int originY, CellOffset[] pattern)
        {
            var count = 0;
            for (var i = 0; i < pattern.Length; i++)
            {
                var x = originX + pattern[i].X;
                var y = originY + pattern[i].Y;
                if (!IsSoftened(x, y))
                {
                    continue;
                }

                count++;
                var center = BoardSpace.CellCenter(x, y);
                if (carveFx != null)
                {
                    carveFx.PlayCarveEffect(new Vector3(center.X, center.Y, 0f));
                }

                tiles[x, y].Bind(this, x, y, BlockType.SolidRock);
            }

            return count;
        }

        private bool IsSoftened(int x, int y)
        {
            if (board == null || tiles == null || !board.Contains(x, y) || tiles[x, y] == null)
            {
                return false;
            }

            return tiles[x, y].Type == BlockType.HardRock && board.Get(x, y) == BlockType.SolidRock;
        }
    }
}
