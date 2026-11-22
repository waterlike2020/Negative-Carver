namespace NegativeCarver.Core;

public readonly struct WorldPoint
{
    public WorldPoint(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; }

    public float Y { get; }
}

public static class BoardSpace
{
    public static WorldPoint CellCenter(int x, int y)
    {
        var half = PilotRules.CellSize * 0.5f;
        return new WorldPoint(
            (x * PilotRules.CellSize) + half,
            (y * PilotRules.CellSize) + half);
    }
}
