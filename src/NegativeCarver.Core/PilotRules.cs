namespace NegativeCarver.Core;

public enum BlockType
{
    Empty = 0,
    SolidRock = 1,
    CoreGem = 2,
    CutterPreview = 3,
    HardRock = 4,
}

public static class PilotRules
{
    public const int Width = 10;
    public const int Height = 18;
    public const int InitialRockRows = 10;
    public const int CoreX = 4;
    public const int CoreY = 2;
    public const int CutterSpawnY = 16;
    public const int DefeatHeight = 15;
    public const int MaxCutterDrops = 30;
    public const float CellSize = 1f;
    public const float FallSecondsPerCell = 0.4f;
    public const int FirstStage = 1;
    public const float HudBottomReserve = 0.22f;
    public const int PcWidth = 1920;
    public const int PcHeight = 1080;
    public const int MobileWidth = 1080;
    public const int MobileHeight = 1920;
    public const int HardRockCount = 20;
    public const float HardRockRed = 0.16f;
    public const float HardRockGreen = 0.16f;
    public const float HardRockBlue = 0.18f;
    public const float HardRockMetallic = 0.65f;
    public const float HardRockSmoothness = 0.72f;
    public const int SmallCarveCells = 1;
    public const int BigCarveCells = 2;
    public const int ClearScorePerStage = 1000;
    public const int SpareDropScore = 500;
}
