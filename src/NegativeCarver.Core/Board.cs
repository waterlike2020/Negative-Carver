namespace NegativeCarver.Core;

public sealed class Board
{
    private readonly BlockType[,] cells;

    public Board()
    {
        cells = new BlockType[PilotRules.Width, PilotRules.Height];
        for (var y = 0; y < PilotRules.Height; y++)
        {
            var type = y < PilotRules.InitialRockRows ? BlockType.SolidRock : BlockType.Empty;
            for (var x = 0; x < PilotRules.Width; x++)
            {
                cells[x, y] = type;
            }
        }

        cells[PilotRules.CoreX, PilotRules.CoreY] = BlockType.CoreGem;
    }

    public Board(System.Random random)
        : this()
    {
        if (random == null)
        {
            return;
        }

        PlaceHardRocks(random);
    }

    public int Width => cells.GetLength(0);

    public int Height => cells.GetLength(1);

    public BlockType Get(int x, int y)
    {
        return cells[x, y];
    }

    public bool Contains(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Width && y < Height;
    }

    public bool HasSolidRockAtOrAbove(int y)
    {
        var start = y < 0 ? 0 : y;
        for (var row = start; row < Height; row++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (BlockKinds.IsRock(cells[x, row]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsCoreFreed()
    {
        var found = false;
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (cells[x, y] != BlockType.CoreGem)
                {
                    continue;
                }

                found = true;
                if (!OrthogonalNeighborsAreOpen(x, y))
                {
                    return false;
                }
            }
        }

        return found;
    }

    private bool OrthogonalNeighborsAreOpen(int x, int y)
    {
        return IsOpen(x - 1, y)
            && IsOpen(x + 1, y)
            && IsOpen(x, y - 1)
            && IsOpen(x, y + 1);
    }

    private bool IsOpen(int x, int y)
    {
        if (!Contains(x, y))
        {
            return true;
        }

        return cells[x, y] == BlockType.Empty;
    }

    public bool TryClear(int x, int y)
    {
        if (!Contains(x, y) || cells[x, y] == BlockType.Empty)
        {
            return false;
        }

        cells[x, y] = BlockType.Empty;
        return true;
    }

    public bool TrySet(int x, int y, BlockType type)
    {
        if (!Contains(x, y))
        {
            return false;
        }

        cells[x, y] = type;
        return true;
    }

    public BlockType[] CopyCells()
    {
        var copy = new BlockType[Width * Height];
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                copy[(y * Width) + x] = cells[x, y];
            }
        }

        return copy;
    }

    public bool RestoreCells(BlockType[]? source, bool coreOverlap)
    {
        if (source == null || source.Length != Width * Height)
        {
            return false;
        }

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                cells[x, y] = source[(y * Width) + x];
            }
        }

        CoreOverlapDetected = coreOverlap;
        return true;
    }

    public bool CheckPenetration(int y)
    {
        if (!Contains(0, y))
        {
            return false;
        }

        for (var x = 0; x < Width; x++)
        {
            if (cells[x, y] != BlockType.Empty)
            {
                return false;
            }
        }

        return true;
    }

    public int[] PenetratedRowsHighToLow()
    {
        var count = 0;
        for (var y = 0; y < Height; y++)
        {
            if (CheckPenetration(y))
            {
                count++;
            }
        }

        var rows = new int[count];
        var index = 0;
        for (var y = Height - 1; y >= 0; y--)
        {
            if (CheckPenetration(y))
            {
                rows[index] = y;
                index++;
            }
        }

        return rows;
    }

    public bool CoreOverlapDetected { get; private set; }

    public void ClearCoreOverlap()
    {
        CoreOverlapDetected = false;
    }

    public bool ShiftRowDown(int row)
    {
        if (!Contains(0, row))
        {
            return false;
        }

        var overlapped = false;
        for (var y = row; y < Height - 1; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var incoming = cells[x, y + 1];
                var occupying = cells[x, y];
                if (y == row
                    && incoming == BlockType.CoreGem
                    && (BlockKinds.IsRock(occupying) || occupying == BlockType.CoreGem))
                {
                    overlapped = true;
                }

                cells[x, y] = incoming;
            }
        }

        for (var x = 0; x < Width; x++)
        {
            cells[x, Height - 1] = BlockType.Empty;
        }

        if (overlapped)
        {
            CoreOverlapDetected = true;
        }

        return overlapped;
    }

    public void ApplyGravity()
    {
        ClearCoreOverlap();
        var rows = PenetratedRowsHighToLow();
        for (var i = 0; i < rows.Length; i++)
        {
            ShiftRowDown(rows[i]);
        }
    }

    public int NextChainRow()
    {
        var rows = PenetratedRowsHighToLow();
        for (var i = 0; i < rows.Length; i++)
        {
            if (HasSolidAbove(rows[i]))
            {
                return rows[i];
            }
        }

        return -1;
    }

    public int[] CollapseChains()
    {
        return CollapseChains(Height);
    }

    public int[] CollapseChains(int limit)
    {
        ClearCoreOverlap();
        var shifted = new System.Collections.Generic.List<int>();
        for (var step = 0; step < limit; step++)
        {
            var row = NextChainRow();
            if (row < 0)
            {
                break;
            }

            ShiftRowDown(row);
            shifted.Add(row);
        }

        var rows = new int[shifted.Count];
        for (var i = 0; i < shifted.Count; i++)
        {
            rows[i] = shifted[i];
        }

        return rows;
    }

    private bool HasSolidAbove(int row)
    {
        for (var y = row + 1; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var type = cells[x, y];
                if (BlockKinds.IsRock(type) || type == BlockType.CoreGem)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryCarveSolid(int x, int y)
    {
        if (!Contains(x, y))
        {
            return false;
        }

        if (cells[x, y] == BlockType.HardRock)
        {
            cells[x, y] = BlockType.SolidRock;
            return false;
        }

        if (cells[x, y] != BlockType.SolidRock)
        {
            return false;
        }

        cells[x, y] = BlockType.Empty;
        return true;
    }

    public System.Collections.Generic.List<CellOffset> CarveSolids(int originX, int originY, CellOffset[]? pattern)
    {
        var carved = new System.Collections.Generic.List<CellOffset>();
        if (pattern == null)
        {
            return carved;
        }

        for (var i = 0; i < pattern.Length; i++)
        {
            var x = originX + pattern[i].X;
            var y = originY + pattern[i].Y;
            if (!TryCarveSolid(x, y))
            {
                continue;
            }

            carved.Add(new CellOffset(x, y));
        }

        return carved;
    }

    private void PlaceHardRocks(System.Random random)
    {
        var spots = new System.Collections.Generic.List<CellOffset>();
        for (var y = 0; y < PilotRules.InitialRockRows; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (cells[x, y] == BlockType.SolidRock)
                {
                    spots.Add(new CellOffset(x, y));
                }
            }
        }

        for (var i = spots.Count - 1; i > 0; i--)
        {
            var swap = random.Next(i + 1);
            var held = spots[i];
            spots[i] = spots[swap];
            spots[swap] = held;
        }

        var quota = PilotRules.HardRockCount;
        if (quota > spots.Count)
        {
            quota = spots.Count;
        }

        for (var i = 0; i < quota; i++)
        {
            cells[spots[i].X, spots[i].Y] = BlockType.HardRock;
        }
    }
}
