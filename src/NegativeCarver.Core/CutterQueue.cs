namespace NegativeCarver.Core;

public static class CutterSpawns
{
    public static CutterPiece AtTop(CutterShape shape)
    {
        var cells = CutterShapes.Cells(shape, 0);
        var minX = cells[0].X;
        var maxX = cells[0].X;
        var minY = cells[0].Y;
        for (var i = 1; i < cells.Length; i++)
        {
            if (cells[i].X < minX)
            {
                minX = cells[i].X;
            }

            if (cells[i].X > maxX)
            {
                maxX = cells[i].X;
            }

            if (cells[i].Y < minY)
            {
                minY = cells[i].Y;
            }
        }

        var span = maxX - minX + 1;
        var originX = (PilotRules.Width - span) / 2 - minX;
        var originY = PilotRules.CutterSpawnY - minY;
        return new CutterPiece(shape, originX, originY, 0);
    }
}

public sealed class CutterQueue
{
    private static readonly CutterShape[] Shapes =
    {
        CutterShape.I,
        CutterShape.J,
        CutterShape.L,
        CutterShape.O,
        CutterShape.S,
        CutterShape.T,
        CutterShape.Z,
    };

    private readonly CutterShape[] order;
    private readonly Random random;
    private int next;

    public CutterQueue()
        : this(new Random())
    {
    }

    public CutterQueue(int seed)
        : this(new Random(seed))
    {
    }

    private CutterQueue(Random random)
    {
        this.random = random ?? new Random();
        order = new CutterShape[Shapes.Length];
        for (var i = 0; i < Shapes.Length; i++)
        {
            order[i] = Shapes[i];
        }

        Shuffle();
    }

    public int Position => next;

    public void Reset()
    {
        Shuffle();
    }

    public void MoveTo(int index)
    {
        if (index < 0 || index >= order.Length)
        {
            return;
        }

        next = index;
    }

    public CutterPiece Next()
    {
        var shape = order[next];
        next++;
        if (next >= order.Length)
        {
            next = 0;
        }

        return CutterSpawns.AtTop(shape);
    }

    private void Shuffle()
    {
        for (var i = order.Length - 1; i > 0; i--)
        {
            var swap = random.Next(i + 1);
            var held = order[i];
            order[i] = order[swap];
            order[swap] = held;
        }

        next = 0;
    }
}
