using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class RestartCountTests
{
    [Fact]
    public void Five_restarts_keep_the_opening_block_count_without_throwing()
    {
        var opening = Occupied(new Board());
        var board = new Board();
        var match = new MatchState();
        var queue = new CutterQueue();

        for (var restart = 0; restart < 5; restart++)
        {
            var touch = DescendLock(queue.Next(), board);
            Commit(board, match, touch);
            Assert.True(Occupied(board) < opening);

            board = new Board();
            match.Reset();
            queue.Reset();

            Assert.Equal(opening, Occupied(board));
            Assert.Equal(0, match.DropCount);
            Assert.Equal(0, match.ChainCount);
            Assert.False(match.Defeated);
            Assert.Equal(MatchStatus.Playing, MatchStatus.Describe(board.IsCoreFreed(), match.Defeated));
        }
    }

    [Fact]
    public void Restart_releases_tiles_before_spawning_the_opening_set()
    {
        var root = FindRepositoryRoot();
        var scripts = Path.Combine(root, "Assets", "Scripts");
        var grid = File.ReadAllText(Path.Combine(scripts, "GridManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var cutter = File.ReadAllText(Path.Combine(scripts, "CutterController.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);

        var restart = Body(grid, "public void Restart()");
        var clear = restart.IndexOf("ClearTiles();", StringComparison.Ordinal);
        var board = restart.IndexOf("board = new Board(new System.Random());", StringComparison.Ordinal);
        var spawn = restart.IndexOf("SpawnInitialRocks();", StringComparison.Ordinal);
        var cutterRestart = restart.IndexOf("cutter.Restart();", StringComparison.Ordinal);
        Assert.True(clear >= 0);
        Assert.True(board > clear);
        Assert.True(spawn > board);
        Assert.True(cutterRestart > spawn);
        Assert.DoesNotContain("throw", restart, StringComparison.Ordinal);

        AssertReleased(Body(grid, "private void ClearTiles()"));
        AssertReleased(Body(grid, "public void RemoveTile(BlockTile tile)"));
        AssertReleased(Body(grid, "public void CarveArea(System.Collections.Generic.List<Vector2Int> cutterPattern, Vector2Int origin)"));
        AssertReleased(Body(cutter, "private void ClearPreview()"));

        var detach = Body(grid, "private static void Detach(BlockTile tile)");
        Assert.Contains("tile.transform.SetParent(null, false);", detach, StringComparison.Ordinal);
        Assert.Contains("tile.transform.SetParent(null, false);", Body(cutter, "private static void Detach(BlockTile tile)"), StringComparison.Ordinal);

        var count = Body(grid, "public int BlockCount");
        Assert.Contains("tiles[x, y] != null", count, StringComparison.Ordinal);
        Assert.DoesNotContain("childCount", count, StringComparison.Ordinal);

        var rocks = Body(grid, "private void SpawnInitialRocks()");
        var instantiate = rocks.IndexOf("Instantiate(", StringComparison.Ordinal);
        Assert.True(rocks.IndexOf("if (rockTilePrefab == null)", StringComparison.Ordinal) < instantiate);
        Assert.True(rocks.IndexOf("if (coreGemTilePrefab == null)", StringComparison.Ordinal) < instantiate);
        var deal = Body(cutter, "private void SpawnNext()");
        Assert.True(deal.IndexOf("if (previewTilePrefab == null)", StringComparison.Ordinal) < deal.IndexOf("ShowPreview();", StringComparison.Ordinal));
    }

    private static void AssertReleased(string body)
    {
        var detach = body.IndexOf("Detach(", StringComparison.Ordinal);
        var destroy = body.IndexOf("Destroy(", StringComparison.Ordinal);
        Assert.True(detach >= 0);
        Assert.True(destroy > detach);
    }

    private static int Occupied(Board board)
    {
        var count = 0;
        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                if (board.Get(x, y) != BlockType.Empty)
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

    private static void Commit(Board board, MatchState match, CutterTouch touch)
    {
        Assert.True(touch.Locked);
        var carved = board.CarveSolids(0, 0, touch.Carved);
        Assert.Equal(touch.Carved.Length, carved.Count);
        var rows = board.CollapseChains();
        match.RecordChain(rows.Length);
        match.RecordDrop(board.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));
    }

    private static string Body(string source, string signature)
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

        throw new InvalidOperationException(signature);
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
