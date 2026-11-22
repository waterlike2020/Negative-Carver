using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class HardRockTests
{
    [Fact]
    public void A_plain_board_has_no_hard_rock()
    {
        var board = new Board();
        Assert.Equal(0, Count(board, BlockType.HardRock));
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
    }

    [Fact]
    public void A_dealt_board_keeps_about_twenty_percent_hard_and_the_same_seed()
    {
        var first = new Board(new System.Random(1));
        var again = new Board(new System.Random(1));
        var other = new Board(new System.Random(2));

        Assert.Equal(PilotRules.HardRockCount, Count(first, BlockType.HardRock));
        Assert.Equal(BlockType.CoreGem, first.Get(PilotRules.CoreX, PilotRules.CoreY));
        Assert.Equal(99 - PilotRules.HardRockCount, Count(first, BlockType.SolidRock));
        Assert.True(PilotRules.HardRockCount * 100 >= 99 * 18);
        Assert.True(PilotRules.HardRockCount * 100 <= 99 * 22);

        for (var y = 0; y < first.Height; y++)
        {
            for (var x = 0; x < first.Width; x++)
            {
                Assert.Equal(first.Get(x, y), again.Get(x, y));
                if (first.Get(x, y) == BlockType.HardRock)
                {
                    Assert.True(y < PilotRules.InitialRockRows);
                    Assert.False(x == PilotRules.CoreX && y == PilotRules.CoreY);
                }
            }
        }

        Assert.False(SameCells(first, other));
    }

    [Fact]
    public void Hard_rock_softens_on_the_first_carve_and_clears_on_the_second()
    {
        var board = new Board();
        Assert.True(board.TrySet(1, 9, BlockType.HardRock));
        Assert.True(board.TrySet(2, 9, BlockType.SolidRock));

        Assert.False(board.TryCarveSolid(1, 9));
        Assert.Equal(BlockType.SolidRock, board.Get(1, 9));
        Assert.True(board.TryCarveSolid(1, 9));
        Assert.Equal(BlockType.Empty, board.Get(1, 9));

        Assert.True(board.TryCarveSolid(2, 9));
        Assert.Equal(BlockType.Empty, board.Get(2, 9));
        Assert.False(board.TryCarveSolid(PilotRules.CoreX, PilotRules.CoreY));
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));

        Assert.True(board.TrySet(3, 9, BlockType.HardRock));
        var carved = board.CarveSolids(3, 9, new[] { new CellOffset(0, 0) });
        Assert.Empty(carved);
        Assert.Equal(BlockType.SolidRock, board.Get(3, 9));
    }

    [Fact]
    public void A_cutter_locks_on_hard_rock_and_gravity_carries_it()
    {
        var board = new Board();
        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                if (board.Get(x, y) != BlockType.CoreGem)
                {
                    board.TryClear(x, y);
                }
            }
        }

        Assert.True(board.TrySet(0, 10, BlockType.HardRock));
        var piece = new CutterPiece(CutterShape.O, 0, 11);
        var touch = piece.TryDescend(board);
        Assert.True(touch.Locked);
        Assert.Contains(new CellOffset(0, 10), touch.Carved);

        var falling = new Board();
        Assert.True(falling.TrySet(0, 5, BlockType.HardRock));
        for (var x = 0; x < falling.Width; x++)
        {
            falling.TryClear(x, 4);
        }

        var rows = falling.CollapseChains();
        Assert.Contains(4, rows);
        Assert.Equal(BlockType.HardRock, falling.Get(0, 4));
        Assert.True(new Board().TrySet(0, PilotRules.DefeatHeight, BlockType.HardRock));
        var lined = new Board();
        lined.TrySet(0, PilotRules.DefeatHeight, BlockType.HardRock);
        Assert.True(lined.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));
    }

    [Fact]
    public void Play_spawns_hard_rock_darker_and_softens_it_without_destroying_the_tile()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var tile = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "BlockTile.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        var awake = MethodBody(grid, "private void Awake()");
        var restart = MethodBody(grid, "public void Restart()");
        Assert.Contains("board = new Board(new System.Random());", awake, StringComparison.Ordinal);
        Assert.DoesNotContain("new Board()", awake, StringComparison.Ordinal);
        Assert.Contains("board = new Board(new System.Random());", restart, StringComparison.Ordinal);
        Assert.Contains("else if (type == BlockType.HardRock)", MethodBody(grid, "private void SpawnInitialRocks()"), StringComparison.Ordinal);
        Assert.Contains("prefab = rockTilePrefab;", MethodBody(grid, "private void SpawnInitialRocks()"), StringComparison.Ordinal);
        var carve = MethodBody(grid, "public void CarveArea(System.Collections.Generic.List<Vector2Int> cutterPattern, Vector2Int origin)");
        var carvedAt = carve.IndexOf("board.CarveSolids(origin.x, origin.y, pattern);", StringComparison.Ordinal);
        var softenAt = carve.IndexOf("var softened = ShowSoftened(origin.x, origin.y, pattern);", StringComparison.Ordinal);
        Assert.True(carvedAt >= 0);
        Assert.True(softenAt > carvedAt);
        var soften = MethodBody(grid, "private int ShowSoftened(int originX, int originY, CellOffset[] pattern)");
        Assert.Contains("tiles[x, y].Bind(this, x, y, BlockType.SolidRock);", soften, StringComparison.Ordinal);
        Assert.DoesNotContain("Destroy", soften, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(carve, "carveFx.PlayCarveEffect("));

        var bind = MethodBody(tile, "public void Bind(GridManager owner, int x, int y, BlockType type)");
        var hard = bind.IndexOf("if (type != BlockType.HardRock)", StringComparison.Ordinal);
        var clear = bind.IndexOf("renderer.SetPropertyBlock(null);", StringComparison.Ordinal);
        var leave = bind.IndexOf("return;", clear, StringComparison.Ordinal);
        var dark = bind.IndexOf("PilotRules.HardRockRed", StringComparison.Ordinal);
        Assert.True(hard >= 0);
        Assert.True(clear > hard);
        Assert.True(leave > clear);
        Assert.True(dark > leave);
        Assert.Contains("PilotRules.HardRockMetallic", bind, StringComparison.Ordinal);
        Assert.Contains("PilotRules.HardRockSmoothness", bind, StringComparison.Ordinal);
        Assert.Contains("block.SetColor(\"_BaseColor\", color);", bind, StringComparison.Ordinal);
    }

    private static int Count(Board board, BlockType type)
    {
        var count = 0;
        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                if (board.Get(x, y) == type)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool SameCells(Board left, Board right)
    {
        for (var y = 0; y < left.Height; y++)
        {
            for (var x = 0; x < left.Width; x++)
            {
                if (left.Get(x, y) != right.Get(x, y))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static int CountOf(string source, string value)
    {
        var count = 0;
        var start = 0;
        while (start < source.Length)
        {
            var found = source.IndexOf(value, start, StringComparison.Ordinal);
            if (found < 0)
            {
                return count;
            }

            count++;
            start = found + value.Length;
        }

        return count;
    }

    private static string MethodBody(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(open, i - open + 1);
                }
            }
        }

        return string.Empty;
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("NegativeCarver.sln");
    }
}
