namespace NegativeCarver.Core;

public static class BlockKinds
{
    public static bool IsRock(BlockType type)
    {
        return type == BlockType.SolidRock || type == BlockType.HardRock;
    }
}
