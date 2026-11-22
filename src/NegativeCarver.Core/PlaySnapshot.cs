namespace NegativeCarver.Core;

public sealed class PlaySnapshot
{
    private readonly BlockType[] cells;

    private PlaySnapshot(
        BlockType[] cells,
        bool coreOverlap,
        int chainCount,
        int dropCount,
        bool defeated,
        bool hasPiece,
        CutterShape shape,
        int originX,
        int originY,
        int rotation,
        int queueIndex)
    {
        this.cells = cells;
        CoreOverlap = coreOverlap;
        ChainCount = chainCount;
        DropCount = dropCount;
        Defeated = defeated;
        HasPiece = hasPiece;
        Shape = shape;
        OriginX = originX;
        OriginY = originY;
        Rotation = rotation;
        QueueIndex = queueIndex;
    }

    public bool CoreOverlap { get; }

    public int ChainCount { get; }

    public int DropCount { get; }

    public bool Defeated { get; }

    public bool HasPiece { get; }

    public CutterShape Shape { get; }

    public int OriginX { get; }

    public int OriginY { get; }

    public int Rotation { get; }

    public int QueueIndex { get; }

    public static PlaySnapshot Take(Board board, MatchState match, CutterPiece? piece, int queueIndex)
    {
        var hasPiece = piece != null;
        return new PlaySnapshot(
            board.CopyCells(),
            board.CoreOverlapDetected,
            match.ChainCount,
            match.DropCount,
            match.Defeated,
            hasPiece,
            hasPiece ? piece!.Shape : CutterShape.I,
            hasPiece ? piece!.OriginX : 0,
            hasPiece ? piece!.OriginY : 0,
            hasPiece ? piece!.Rotation : 0,
            queueIndex);
    }

    public BlockType Cell(int x, int y)
    {
        return cells[(y * PilotRules.Width) + x];
    }

    public BlockType[] CopyCells()
    {
        var copy = new BlockType[cells.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            copy[i] = cells[i];
        }

        return copy;
    }
}
