using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class CoreFreedomTests
{
    [Fact]
    public void A_fresh_board_keeps_the_core_buried()
    {
        var board = new Board();

        Assert.False(board.IsCoreFreed());
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX - 1, PilotRules.CoreY));
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX + 1, PilotRules.CoreY));
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX, PilotRules.CoreY - 1));
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX, PilotRules.CoreY + 1));
    }

    [Fact]
    public void Four_empty_sides_free_the_core_even_when_diagonals_stay_rock()
    {
        var board = OpenCore(PilotRules.CoreX, PilotRules.CoreY);
        Assert.True(board.TrySet(PilotRules.CoreX - 1, PilotRules.CoreY - 1, BlockType.SolidRock));
        Assert.True(board.TrySet(PilotRules.CoreX + 1, PilotRules.CoreY + 1, BlockType.SolidRock));

        Assert.True(board.IsCoreFreed());
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 1)]
    public void One_filled_side_keeps_the_core_buried(int dx, int dy)
    {
        var board = OpenCore(PilotRules.CoreX, PilotRules.CoreY);
        Assert.True(board.TrySet(PilotRules.CoreX + dx, PilotRules.CoreY + dy, BlockType.SolidRock));

        Assert.False(board.IsCoreFreed());
    }

    [Fact]
    public void A_cutter_preview_beside_the_core_is_not_an_open_side()
    {
        var board = OpenCore(PilotRules.CoreX, PilotRules.CoreY);
        Assert.True(board.TrySet(PilotRules.CoreX + 1, PilotRules.CoreY, BlockType.CutterPreview));

        Assert.False(board.IsCoreFreed());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(PilotRules.Width - 1, 0)]
    [InlineData(0, PilotRules.Height - 1)]
    [InlineData(PilotRules.Width - 1, PilotRules.Height - 1)]
    public void A_side_past_the_edge_counts_as_open(int x, int y)
    {
        var board = OpenCore(x, y);

        Assert.True(board.IsCoreFreed());
        Assert.False(board.Contains(x - 1, y) && board.Contains(x + 1, y) && board.Contains(x, y - 1) && board.Contains(x, y + 1));
    }

    [Fact]
    public void An_edge_core_stays_buried_when_the_inward_side_is_rock()
    {
        var board = OpenCore(0, PilotRules.CoreY);
        Assert.True(board.TrySet(1, PilotRules.CoreY, BlockType.SolidRock));

        Assert.False(board.IsCoreFreed());
    }

    [Fact]
    public void A_board_without_a_core_is_not_freed()
    {
        var board = OpenCore(PilotRules.CoreX, PilotRules.CoreY);
        Assert.True(board.TryClear(PilotRules.CoreX, PilotRules.CoreY));

        Assert.False(board.IsCoreFreed());
    }

    [Fact]
    public void A_neighboring_core_keeps_both_buried()
    {
        var board = OpenCore(PilotRules.CoreX, PilotRules.CoreY);
        Assert.True(board.TrySet(PilotRules.CoreX + 1, PilotRules.CoreY, BlockType.CoreGem));

        Assert.False(board.IsCoreFreed());
    }

    [Fact]
    public void A_later_buried_core_keeps_the_board_buried()
    {
        var board = OpenCore(0, 0);
        Assert.True(board.TrySet(PilotRules.CoreX, PilotRules.CoreY, BlockType.CoreGem));
        Assert.True(board.TrySet(PilotRules.CoreX + 1, PilotRules.CoreY, BlockType.SolidRock));

        Assert.False(board.IsCoreFreed());
        Assert.Equal(BlockType.CoreGem, board.Get(0, 0));
        Assert.Equal(BlockType.Empty, board.Get(1, 0));
    }

    [Fact]
    public void Every_core_must_be_open()
    {
        var board = OpenCore(PilotRules.CoreX, PilotRules.CoreY);
        Assert.True(board.TrySet(0, 0, BlockType.CoreGem));
        Assert.True(board.TrySet(1, 0, BlockType.SolidRock));

        Assert.False(board.IsCoreFreed());

        Assert.True(board.TryClear(1, 0));
        Assert.True(board.IsCoreFreed());
    }

    [Fact]
    public void The_unity_check_reads_the_board_and_ignores_effects()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var source = File.ReadAllText(Path.Combine(dir.FullName, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var start = source.IndexOf("public bool CheckCoreGemFreed()", StringComparison.Ordinal);
        var open = source.IndexOf('{', start);
        var depth = 0;
        var body = "";
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
                    body = source.Substring(open + 1, i - open - 1).Trim();
                    break;
                }
            }
        }

        Assert.Equal("if (board == null)\n            {\n                return false;\n            }\n\n            return board.IsCoreFreed();", body);
        Assert.DoesNotContain("carveFx", body, StringComparison.Ordinal);
        Assert.DoesNotContain("cutter", body, StringComparison.Ordinal);
    }

    private static Board OpenCore(int x, int y)
    {
        var board = new Board();
        for (var row = 0; row < board.Height; row++)
        {
            for (var column = 0; column < board.Width; column++)
            {
                if (board.Get(column, row) != BlockType.Empty)
                {
                    Assert.True(board.TryClear(column, row));
                }
            }
        }

        Assert.True(board.TrySet(x, y, BlockType.CoreGem));
        return board;
    }
}
