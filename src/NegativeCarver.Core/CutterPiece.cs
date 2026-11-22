namespace NegativeCarver.Core;

public enum CutterShape
{
    I = 0,
    J = 1,
    L = 2,
    O = 3,
    S = 4,
    T = 5,
    Z = 6,
}

public readonly struct CellOffset : System.IEquatable<CellOffset>
{
    public CellOffset(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public bool Equals(CellOffset other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is CellOffset other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (X * 397) ^ Y;
    }
}

public static class CutterShapes
{
    private static readonly CellOffset[][] Bases =
    {
        new[] { new CellOffset(0, 0), new CellOffset(1, 0), new CellOffset(2, 0), new CellOffset(3, 0) },
        new[] { new CellOffset(0, 0), new CellOffset(0, 1), new CellOffset(1, 0), new CellOffset(2, 0) },
        new[] { new CellOffset(0, 0), new CellOffset(1, 0), new CellOffset(2, 0), new CellOffset(2, 1) },
        new[] { new CellOffset(0, 0), new CellOffset(1, 0), new CellOffset(0, 1), new CellOffset(1, 1) },
        new[] { new CellOffset(0, 0), new CellOffset(1, 0), new CellOffset(1, 1), new CellOffset(2, 1) },
        new[] { new CellOffset(0, 0), new CellOffset(1, 0), new CellOffset(2, 0), new CellOffset(1, 1) },
        new[] { new CellOffset(1, 0), new CellOffset(2, 0), new CellOffset(0, 1), new CellOffset(1, 1) },
    };

    public static CellOffset[] Cells(CutterShape shape, int rotation)
    {
        var turns = ((rotation % 4) + 4) % 4;
        var source = Bases[(int)shape];
        if (shape == CutterShape.O || turns == 0)
        {
            return Copy(source);
        }

        var cells = Copy(source);
        for (var turn = 0; turn < turns; turn++)
        {
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = new CellOffset(cells[i].Y, -cells[i].X);
            }
        }

        return cells;
    }

    public static bool Fits(int originX, int originY, CutterShape shape, int rotation)
    {
        foreach (var cell in Cells(shape, rotation))
        {
            var x = originX + cell.X;
            var y = originY + cell.Y;
            if (x < 0 || y < 0 || x >= PilotRules.Width || y >= PilotRules.Height)
            {
                return false;
            }
        }

        return true;
    }

    private static CellOffset[] Copy(CellOffset[] source)
    {
        var copy = new CellOffset[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            copy[i] = source[i];
        }

        return copy;
    }
}

public readonly struct CutterTouch
{
    public CutterTouch(bool moved, bool locked, CellOffset[] carved)
    {
        Moved = moved;
        Locked = locked;
        Carved = carved ?? System.Array.Empty<CellOffset>();
    }

    public bool Moved { get; }

    public bool Locked { get; }

    public CellOffset[] Carved { get; }

    public static CutterTouch None => new CutterTouch(false, false, System.Array.Empty<CellOffset>());
}

public sealed class CutterPiece
{
    public CutterPiece(CutterShape shape, int originX, int originY, int rotation = 0)
    {
        Shape = shape;
        OriginX = originX;
        OriginY = originY;
        Rotation = ((rotation % 4) + 4) % 4;
    }

    public CutterShape Shape { get; }

    public int OriginX { get; private set; }

    public int OriginY { get; private set; }

    public int Rotation { get; private set; }

    public CellOffset[] Cells => CutterShapes.Cells(Shape, Rotation);

    public bool TryRotateClockwise()
    {
        var next = (Rotation + 1) % 4;
        for (var i = 0; i < RotationKicks.Length; i++)
        {
            var kick = RotationKicks[i];
            var x = OriginX + kick.X;
            var y = OriginY + kick.Y;
            if (!CutterShapes.Fits(x, y, Shape, next))
            {
                continue;
            }

            OriginX = x;
            OriginY = y;
            Rotation = next;
            return true;
        }

        return false;
    }

    public bool TryRotateClockwise(Board board)
    {
        if (board == null)
        {
            return false;
        }

        var next = (Rotation + 1) % 4;
        for (var i = 0; i < RotationKicks.Length; i++)
        {
            var kick = RotationKicks[i];
            var x = OriginX + kick.X;
            var y = OriginY + kick.Y;
            if (!CutterShapes.Fits(x, y, Shape, next) || OverlapsRockOrCore(board, x, y, next))
            {
                continue;
            }

            OriginX = x;
            OriginY = y;
            Rotation = next;
            return true;
        }

        return false;
    }

    private static readonly CellOffset[] RotationKicks =
    {
        new CellOffset(0, 0),
        new CellOffset(0, -1),
        new CellOffset(0, 1),
        new CellOffset(-1, 0),
        new CellOffset(1, 0),
        new CellOffset(0, -2),
        new CellOffset(0, 2),
        new CellOffset(-2, 0),
        new CellOffset(2, 0),
        new CellOffset(0, -3),
        new CellOffset(0, 3),
        new CellOffset(-3, 0),
        new CellOffset(3, 0),
    };

    public bool TryMove(int deltaX)
    {
        var nextX = OriginX + deltaX;
        if (!CutterShapes.Fits(nextX, OriginY, Shape, Rotation))
        {
            return false;
        }

        OriginX = nextX;
        return true;
    }

    public void DropToFirstContact(Board board)
    {
        var cells = Cells;
        var landedY = OriginY;
        var found = false;
        for (var i = 0; i < cells.Length; i++)
        {
            if (!IsColumnBottom(cells, i))
            {
                continue;
            }

            var columnContact = 0;
            var x = OriginX + cells[i].X;
            for (var y = OriginY + cells[i].Y; y >= 0; y--)
            {
                if (!board.Contains(x, y))
                {
                    continue;
                }

                var type = board.Get(x, y);
                if (BlockKinds.IsRock(type) || type == BlockType.CoreGem)
                {
                    columnContact = y;
                    break;
                }
            }

            var originForColumn = columnContact - cells[i].Y;
            if (!found || originForColumn > landedY)
            {
                landedY = originForColumn;
                found = true;
            }
        }

        if (found && landedY < OriginY)
        {
            OriginY = landedY;
        }
    }

    public CutterTouch TryDescend(Board board)
    {
        return Step(board, 0, -1, true);
    }

    public CutterTouch TrySidestep(Board board, int deltaX)
    {
        if (board == null || deltaX == 0)
        {
            return CutterTouch.None;
        }

        return Step(board, deltaX, 0, false);
    }

    private CutterTouch Step(Board board, int deltaX, int deltaY, bool lockWhenBlocked)
    {
        if (board == null)
        {
            return CutterTouch.None;
        }

        var nextX = OriginX + deltaX;
        var nextY = OriginY + deltaY;
        var cells = Cells;
        var rocks = new System.Collections.Generic.List<CellOffset>();
        var blocked = false;
        for (var i = 0; i < cells.Length; i++)
        {
            var x = nextX + cells[i].X;
            var y = nextY + cells[i].Y;
            if (!board.Contains(x, y))
            {
                blocked = true;
                continue;
            }

            var type = board.Get(x, y);
            if (type == BlockType.CoreGem)
            {
                blocked = true;
                continue;
            }

            if (BlockKinds.IsRock(type))
            {
                rocks.Add(new CellOffset(x, y));
            }
        }

        if (blocked)
        {
            if (!lockWhenBlocked)
            {
                return CutterTouch.None;
            }

            return new CutterTouch(false, true, rocks.ToArray());
        }

        OriginX = nextX;
        OriginY = nextY;
        if (rocks.Count > 0)
        {
            return new CutterTouch(true, true, rocks.ToArray());
        }

        return new CutterTouch(true, false, System.Array.Empty<CellOffset>());
    }

    private bool OverlapsRockOrCore(Board board, int originX, int originY, int rotation)
    {
        var cells = CutterShapes.Cells(Shape, rotation);
        for (var i = 0; i < cells.Length; i++)
        {
            var x = originX + cells[i].X;
            var y = originY + cells[i].Y;
            if (!board.Contains(x, y))
            {
                return true;
            }

            var type = board.Get(x, y);
            if (BlockKinds.IsRock(type) || type == BlockType.CoreGem)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsColumnBottom(CellOffset[] cells, int index)
    {
        var column = cells[index].X;
        var y = cells[index].Y;
        for (var i = 0; i < cells.Length; i++)
        {
            if (cells[i].X == column && cells[i].Y < y)
            {
                return false;
            }
        }

        return true;
    }
}
