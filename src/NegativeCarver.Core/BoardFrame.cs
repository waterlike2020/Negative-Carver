namespace NegativeCarver.Core;

public readonly struct BoardView
{
    public BoardView(float cameraX, float cameraY, float orthographicSize)
    {
        CameraX = cameraX;
        CameraY = cameraY;
        OrthographicSize = orthographicSize;
    }

    public float CameraX { get; }

    public float CameraY { get; }

    public float OrthographicSize { get; }
}

public static class BoardFrame
{
    public static BoardView Fit(float aspect, float bottomReserve)
    {
        var width = PilotRules.Width * PilotRules.CellSize;
        var height = PilotRules.Height * PilotRules.CellSize;
        var margin = PilotRules.CellSize;
        var contentWidth = width + (margin * 2f);
        var contentHeight = height + (margin * 2f);
        var usable = 1f - bottomReserve;
        var sizeForHeight = contentHeight / (2f * usable);
        var sizeForWidth = contentWidth / (2f * aspect);
        var size = sizeForHeight > sizeForWidth ? sizeForHeight : sizeForWidth;
        var cameraX = width * 0.5f;
        var cameraY = (height * 0.5f) - (size * bottomReserve);
        return new BoardView(cameraX, cameraY, size);
    }
}
