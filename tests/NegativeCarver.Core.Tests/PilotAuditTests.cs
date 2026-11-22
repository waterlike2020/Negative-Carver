using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class PilotAuditTests
{
    [Fact]
    public void Checked_steps_still_match_the_fixed_rules()
    {
        var board = new Board();
        var opening = Occupied(board);
        Assert.Equal(PilotRules.Width * PilotRules.InitialRockRows, opening);
        Assert.Equal(1, Count(board, BlockType.CoreGem));
        Assert.Equal(opening - 1, Count(board, BlockType.SolidRock));

        var piece = CutterSpawns.AtTop(CutterShape.I);
        var touch = DescendLock(piece, board);
        var carved = board.CarveSolids(0, 0, touch.Carved);
        Assert.Equal(4, carved.Count);
        var top = PilotRules.InitialRockRows - 1;
        for (var i = 0; i < carved.Count; i++)
        {
            Assert.Equal(top, carved[i].Y);
            Assert.Equal(BlockType.Empty, board.Get(carved[i].X, carved[i].Y));
            Assert.Equal(BlockType.SolidRock, board.Get(carved[i].X, top - 1));
        }

        var collapse = new Board();
        var row = PilotRules.InitialRockRows - 2;
        for (var x = 0; x < collapse.Width; x++)
        {
            Assert.True(collapse.TryClear(x, row));
        }

        var chains = collapse.CollapseChains();
        Assert.Equal(new[] { row }, chains);
        Assert.Equal(BlockType.SolidRock, collapse.Get(0, row));
        Assert.Equal(BlockType.Empty, collapse.Get(0, row + 1));

        var freed = new Board();
        Assert.True(freed.TryClear(PilotRules.CoreX, PilotRules.CoreY + 1));
        Assert.True(freed.TryClear(PilotRules.CoreX, PilotRules.CoreY - 1));
        Assert.True(freed.TryClear(PilotRules.CoreX - 1, PilotRules.CoreY));
        Assert.True(freed.TryClear(PilotRules.CoreX + 1, PilotRules.CoreY));
        Assert.True(freed.IsCoreFreed());

        var match = new MatchState();
        for (var drop = 0; drop < PilotRules.MaxCutterDrops - 1; drop++)
        {
            match.RecordDrop(false);
        }

        Assert.False(match.Defeated);
        match.RecordDrop(false);
        Assert.True(match.Defeated);
        Assert.Equal(PilotRules.MaxCutterDrops, match.DropCount);

        for (var restart = 0; restart < 5; restart++)
        {
            Assert.True(Occupied(new Board()) == opening);
        }

        AssertFits(16f / 9f);
        AssertFits(9f / 16f);

        var list = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "to-do-list.md"));
        Assert.DoesNotContain("그 열의 암석 4칸", list, StringComparison.Ordinal);
        Assert.DoesNotContain("암석 100개", list, StringComparison.Ordinal);
        Assert.Contains("가로 I형 커터가 암석 윗면에 닿으면 맞닿은 네 칸만 비고, 그 아래 암석은 남는다.", list, StringComparison.Ordinal);
        Assert.Contains("코어 칸을 빼면 암석은 99개다.", list, StringComparison.Ordinal);
        Assert.Contains("재생 중에는 버튼 띠 위로 다시 맞춘다.", list, StringComparison.Ordinal);
        Assert.Contains("아래로 암석에 닿으면 겹친 윗면을 깎고", list, StringComparison.Ordinal);

        foreach (var line in list.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("- [", StringComparison.Ordinal) || !trimmed.Contains("**P", StringComparison.Ordinal))
            {
                continue;
            }

            var id = trimmed.Substring(trimmed.IndexOf("**P", StringComparison.Ordinal) + 3, 1);
            if (id[0] >= '0' && id[0] <= '4')
            {
                Assert.StartsWith("- [x] **P" + id, trimmed, StringComparison.Ordinal);
            }
        }
    }

    private static void AssertFits(float aspect)
    {
        var view = BoardFrame.Fit(aspect, PilotRules.HudBottomReserve);
        var halfHeight = view.OrthographicSize;
        var halfWidth = halfHeight * aspect;
        var bottom = view.CameraY - halfHeight;
        var top = view.CameraY + halfHeight;
        var usableBottom = bottom + ((top - bottom) * PilotRules.HudBottomReserve);
        Assert.True(view.CameraX - halfWidth < 0f);
        Assert.True(view.CameraX + halfWidth > PilotRules.Width * PilotRules.CellSize);
        Assert.True(usableBottom < 0f);
        Assert.True(top > PilotRules.Height * PilotRules.CellSize);
    }

    private static int Occupied(Board board)
    {
        return Count(board, BlockType.SolidRock) + Count(board, BlockType.CoreGem);
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

    private static CutterTouch DescendLock(CutterPiece piece, Board board)
    {
        CutterTouch touch;
        var steps = 0;
        do
        {
            touch = piece.TryDescend(board);
            steps++;
            Assert.True(steps <= PilotRules.Height);
        }
        while (!touch.Locked);

        return touch;
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

        throw new InvalidOperationException("NegativeCarver.sln was not found above the test output directory.");
    }
}
