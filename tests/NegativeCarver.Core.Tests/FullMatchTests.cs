using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class FullMatchTests
{
    [Fact]
    public void Legal_drops_free_the_core_and_chain_once()
    {
        var board = new Board();
        var match = new MatchState();
        AssertOpening(board, match);

        for (var layer = PilotRules.InitialRockRows - 1; layer >= PilotRules.CoreY + 1; layer--)
        {
            var touch = DescendLock(new CutterPiece(CutterShape.I, PilotRules.CoreX - 2, PilotRules.CutterSpawnY), board);
            Assert.Equal(4, touch.Carved.Length);
            Commit(board, match, touch);
        }

        var againstCore = DescendLock(new CutterPiece(CutterShape.I, PilotRules.CoreX - 2, PilotRules.CutterSpawnY), board);
        Assert.DoesNotContain(new CellOffset(PilotRules.CoreX, PilotRules.CoreY), againstCore.Carved);
        Assert.Contains(new CellOffset(PilotRules.CoreX - 1, PilotRules.CoreY), againstCore.Carved);
        Assert.Contains(new CellOffset(PilotRules.CoreX + 1, PilotRules.CoreY), againstCore.Carved);
        Commit(board, match, againstCore);
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
        Assert.Equal(BlockType.Empty, board.Get(PilotRules.CoreX, PilotRules.CoreY + 1));
        Assert.Equal(BlockType.Empty, board.Get(PilotRules.CoreX - 1, PilotRules.CoreY));
        Assert.Equal(BlockType.Empty, board.Get(PilotRules.CoreX + 1, PilotRules.CoreY));

        for (var layer = PilotRules.InitialRockRows - 1; layer >= PilotRules.CoreY; layer--)
        {
            Commit(board, match, DescendLock(new CutterPiece(CutterShape.I, PilotRules.CoreX + 1, PilotRules.CutterSpawnY), board));
        }

        Commit(board, match, DescendLock(new CutterPiece(CutterShape.I, PilotRules.CoreX + 1, PilotRules.CutterSpawnY), board));
        Assert.Equal(BlockType.Empty, board.Get(PilotRules.CoreX + 1, PilotRules.CoreY - 1));

        var approach = new CutterPiece(CutterShape.I, PilotRules.CoreX + 1, PilotRules.CutterSpawnY);
        FallTo(approach, board, PilotRules.CoreY - 1);
        var side = approach.TrySidestep(board, -1);
        Assert.True(side.Locked);
        Assert.Equal(new[] { new CellOffset(PilotRules.CoreX, PilotRules.CoreY - 1) }, side.Carved);
        Commit(board, match, side);

        Assert.True(board.IsCoreFreed());
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
        Assert.Equal(MatchStatus.Clear, MatchStatus.Describe(board.IsCoreFreed(), match.Defeated));
        Assert.False(match.Defeated);
        Assert.True(match.DropCount < PilotRules.MaxCutterDrops);

        Commit(board, match, DescendLock(new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY), board));
        Commit(board, match, DescendLock(new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY), board));
        var finisher = new CutterPiece(CutterShape.I, PilotRules.CoreX + 1, PilotRules.CutterSpawnY);
        FallTo(finisher, board, PilotRules.InitialRockRows - 2);
        var opened = finisher.TrySidestep(board, 1);
        Assert.Equal(new[] { new CellOffset(PilotRules.Width - 1, PilotRules.InitialRockRows - 2) }, opened.Carved);
        var chains = Commit(board, match, opened);

        Assert.Equal(new[] { PilotRules.InitialRockRows - 2 }, chains);
        Assert.Equal(1, match.ChainCount);
        Assert.True(board.IsCoreFreed());
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
        Assert.Equal(MatchStatus.Clear, MatchStatus.Describe(board.IsCoreFreed(), match.Defeated));
        Assert.False(match.Defeated);
    }

    [Fact]
    public void Thirty_drops_fail_and_another_drop_does_not_count()
    {
        var board = new Board();
        var match = new MatchState();
        for (var drop = 0; drop < PilotRules.MaxCutterDrops; drop++)
        {
            Commit(board, match, DescendLock(new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY), board));
        }

        Assert.Equal(PilotRules.MaxCutterDrops, match.DropCount);
        Assert.True(match.Defeated);
        Assert.False(board.IsCoreFreed());
        Assert.Equal(MatchStatus.Failed, MatchStatus.Describe(board.IsCoreFreed(), match.Defeated));

        var frozen = match.DropCount;
        match.RecordDrop(board.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));
        Assert.Equal(frozen, match.DropCount);
        Assert.True(match.Defeated);

        var high = new Board();
        Assert.True(high.TrySet(0, PilotRules.DefeatHeight, BlockType.SolidRock));
        var height = new MatchState();
        height.RecordDrop(high.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));
        Assert.Equal(1, height.DropCount);
        Assert.True(height.Defeated);
        height.RecordDrop(false);
        Assert.Equal(1, height.DropCount);
        Assert.Equal(MatchStatus.Failed, MatchStatus.Describe(high.IsCoreFreed(), height.Defeated));
    }

    [Fact]
    public void Restart_restores_the_opening_board()
    {
        var board = new Board();
        var match = new MatchState();
        Commit(board, match, DescendLock(new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY), board));
        Assert.True(match.DropCount > 0);

        board = new Board();
        match.Reset();

        AssertOpening(board, match);
        Assert.Equal(MatchStatus.Playing, MatchStatus.Describe(board.IsCoreFreed(), match.Defeated));
    }

    private static void AssertOpening(Board board, MatchState match)
    {
        Assert.Equal(0, match.ChainCount);
        Assert.Equal(0, match.DropCount);
        Assert.False(match.Defeated);
        Assert.False(board.IsCoreFreed());
        var rocks = 0;
        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                var type = board.Get(x, y);
                if (x == PilotRules.CoreX && y == PilotRules.CoreY)
                {
                    Assert.Equal(BlockType.CoreGem, type);
                    continue;
                }

                if (y < PilotRules.InitialRockRows)
                {
                    Assert.Equal(BlockType.SolidRock, type);
                    rocks++;
                    continue;
                }

                Assert.Equal(BlockType.Empty, type);
            }
        }

        Assert.Equal((PilotRules.Width * PilotRules.InitialRockRows) - 1, rocks);
    }

    private static void FallTo(CutterPiece piece, Board board, int originY)
    {
        while (piece.OriginY > originY)
        {
            var before = piece.OriginY;
            var touch = piece.TryDescend(board);
            Assert.False(touch.Locked);
            Assert.Empty(touch.Carved);
            Assert.Equal(before - 1, piece.OriginY);
        }

        Assert.Equal(originY, piece.OriginY);
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

    private static int[] Commit(Board board, MatchState match, CutterTouch touch)
    {
        Assert.True(touch.Locked);
        foreach (var cell in touch.Carved)
        {
            Assert.Equal(BlockType.SolidRock, board.Get(cell.X, cell.Y));
            Assert.False(cell.X == PilotRules.CoreX && cell.Y == PilotRules.CoreY);
        }

        var carved = board.CarveSolids(0, 0, touch.Carved);
        Assert.Equal(touch.Carved.Length, carved.Count);
        var rows = board.CollapseChains();
        match.RecordChain(rows.Length);
        match.RecordDrop(board.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));
        return rows;
    }
}
