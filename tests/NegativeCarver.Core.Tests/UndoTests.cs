namespace NegativeCarver.Core.Tests;

public sealed class UndoTests
{
    [Fact]
    public void Undo_restores_the_rock_above_a_carved_cell_and_redo_carves_it_again()
    {
        var board = new Board();
        var match = new MatchState();
        var aimed = new CutterPiece(CutterShape.S, 1, 8, 0);
        var before = PlaySnapshot.Take(board, match, aimed, 2);
        board.TryClear(1, 6);
        match.RecordDrop(false);
        var after = PlaySnapshot.Take(board, match, null, 3);

        Assert.Equal(BlockType.SolidRock, before.Cell(1, 6));
        Assert.Equal(BlockType.SolidRock, before.Cell(1, 7));
        Assert.Equal(BlockType.SolidRock, before.Cell(1, 8));
        Assert.Equal(BlockType.SolidRock, before.Cell(1, 9));
        Assert.Equal(CutterShape.S, before.Shape);
        Assert.Equal(1, before.OriginX);
        Assert.Equal(8, before.OriginY);
        Assert.Equal(0, before.Rotation);
        Assert.Equal(2, before.QueueIndex);
        Assert.True(before.HasPiece);
        Assert.Equal(0, before.DropCount);
        Assert.Equal(BlockType.Empty, after.Cell(1, 6));
        Assert.Equal(BlockType.SolidRock, after.Cell(1, 7));
        Assert.Equal(BlockType.SolidRock, after.Cell(1, 8));
        Assert.Equal(BlockType.SolidRock, after.Cell(1, 9));
        Assert.False(after.HasPiece);
        Assert.Equal(1, after.DropCount);
        Assert.Equal(3, after.QueueIndex);

        board.TryClear(0, 0);
        Assert.Equal(BlockType.SolidRock, before.Cell(0, 0));
        Assert.Equal(BlockType.SolidRock, after.Cell(0, 0));

        var ledger = new CarveLedger();
        Assert.Null(ledger.Undo());
        Assert.Null(ledger.Redo());
        Assert.False(ledger.CanUndo);
        Assert.False(ledger.CanRedo);
        ledger.Commit(null, after);
        Assert.False(ledger.CanUndo);

        ledger.Commit(before, after);
        var undone = ledger.Undo();
        Assert.NotNull(undone);
        Assert.Equal(BlockType.SolidRock, undone!.Cell(1, 6));
        Assert.Equal(BlockType.SolidRock, undone.Cell(1, 7));
        Assert.True(undone.HasPiece);
        Assert.Equal(0, undone.DropCount);
        Assert.True(ledger.CanRedo);

        Assert.True(board.RestoreCells(undone.CopyCells(), undone.CoreOverlap));
        match.Restore(undone.ChainCount, undone.DropCount, undone.Defeated);
        Assert.Equal(BlockType.SolidRock, board.Get(1, 6));
        Assert.Equal(BlockType.SolidRock, board.Get(1, 7));
        Assert.Equal(0, match.DropCount);
        Assert.False(match.Defeated);

        var redone = ledger.Redo();
        Assert.NotNull(redone);
        Assert.Equal(BlockType.Empty, redone!.Cell(1, 6));
        Assert.Equal(BlockType.SolidRock, redone.Cell(1, 7));
        Assert.Equal(BlockType.SolidRock, redone.Cell(1, 8));
        Assert.Equal(BlockType.SolidRock, redone.Cell(1, 9));
        Assert.False(redone.HasPiece);
        Assert.Equal(1, redone.DropCount);
        Assert.False(ledger.CanRedo);
        Assert.True(board.RestoreCells(redone.CopyCells(), redone.CoreOverlap));
        Assert.Equal(BlockType.Empty, board.Get(1, 6));
        Assert.Equal(BlockType.SolidRock, board.Get(1, 7));
    }

    [Fact]
    public void A_new_drop_discards_the_redo_and_a_nudge_after_undo_does_too()
    {
        var board = new Board();
        var match = new MatchState();
        var before = PlaySnapshot.Take(board, match, null, 0);
        board.TryClear(1, 6);
        var after = PlaySnapshot.Take(board, match, null, 1);
        board.TryClear(2, 6);
        var later = PlaySnapshot.Take(board, match, null, 2);
        board.TryClear(3, 6);
        var other = PlaySnapshot.Take(board, match, null, 3);

        var ledger = new CarveLedger();
        ledger.Commit(before, after);
        ledger.Commit(after, later);
        var step = ledger.Undo();
        Assert.NotNull(step);
        Assert.Equal(BlockType.SolidRock, step!.Cell(2, 6));
        Assert.Equal(BlockType.Empty, step.Cell(1, 6));

        ledger.Commit(after, other);
        Assert.False(ledger.CanRedo);
        var back = ledger.Undo();
        Assert.NotNull(back);
        Assert.Equal(BlockType.Empty, back!.Cell(1, 6));
        Assert.Equal(BlockType.SolidRock, back.Cell(2, 6));
        Assert.Equal(BlockType.SolidRock, back.Cell(3, 6));
        var forward = ledger.Redo();
        Assert.NotNull(forward);
        Assert.Equal(BlockType.Empty, forward!.Cell(3, 6));

        ledger.Undo();
        ledger.DiscardRedo();
        Assert.False(ledger.CanRedo);
        Assert.True(ledger.CanUndo);
        var only = new CarveLedger();
        only.Commit(before, after);
        only.Undo();
        only.DiscardRedo();
        Assert.False(only.CanRedo);
        Assert.False(only.CanUndo);
        ledger.Clear();
        Assert.False(ledger.CanUndo);
    }

    [Fact]
    public void Restore_rejects_a_short_copy_and_the_queue_can_return_to_an_earlier_shape()
    {
        var board = new Board();
        Assert.False(board.RestoreCells(null, true));
        Assert.False(board.RestoreCells(new BlockType[3], true));
        Assert.Equal(BlockType.SolidRock, board.Get(0, 0));
        Assert.False(board.CoreOverlapDetected);
        var cells = board.CopyCells();
        Assert.True(board.RestoreCells(cells, true));
        Assert.True(board.CoreOverlapDetected);
        Assert.True(board.RestoreCells(cells, false));
        Assert.False(board.CoreOverlapDetected);

        var match = new MatchState();
        match.RecordDrop(true);
        Assert.True(match.Defeated);
        match.Restore(4, 2, false);
        Assert.Equal(4, match.ChainCount);
        Assert.Equal(2, match.DropCount);
        Assert.False(match.Defeated);

        var queue = new CutterQueue(1);
        var first = queue.Next().Shape;
        var second = queue.Next().Shape;
        queue.Next();
        Assert.Equal(3, queue.Position);
        queue.MoveTo(-1);
        queue.MoveTo(99);
        Assert.Equal(3, queue.Position);
        queue.MoveTo(1);
        Assert.Equal(1, queue.Position);
        Assert.Equal(second, queue.Next().Shape);
    }

    [Fact]
    public void The_hud_can_step_back_to_the_board_saved_before_the_drop()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var root = dir!.FullName;
        var cutter = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "CutterController.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var hud = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "PilotHud.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        var hardDrop = Body(cutter, "public void HardDrop()");
        var prior = hardDrop.IndexOf("ledger.DiscardRedo();", StringComparison.Ordinal);
        var arm = hardDrop.IndexOf("BeginFall();", StringComparison.Ordinal);
        Assert.True(prior >= 0);
        Assert.True(prior < arm);
        Assert.DoesNotContain("ledger.Commit", hardDrop, StringComparison.Ordinal);
        var begin = Body(cutter, "private void BeginFall()");
        Assert.True(begin.IndexOf("fallStart = Capture();", StringComparison.Ordinal) < begin.IndexOf("falling = true;", StringComparison.Ordinal));
        Assert.DoesNotContain("DiscardRedo", begin, StringComparison.Ordinal);
        var finish = Body(cutter, "private void FinishDrop(CutterTouch touch)");
        var drop = finish.IndexOf("grid.CarveArea(pattern, new Vector2Int(0, 0));", StringComparison.Ordinal);
        var firstCommit = finish.IndexOf("ledger.Commit(prior, Capture());", StringComparison.Ordinal);
        var spawn = finish.IndexOf("SpawnNext();", StringComparison.Ordinal);
        var secondCommit = finish.IndexOf("ledger.Commit(prior, Capture());", firstCommit + 1, StringComparison.Ordinal);
        Assert.True(drop >= 0);
        Assert.True(firstCommit > drop);
        Assert.True(spawn > firstCommit);
        Assert.True(secondCommit > spawn);

        var update = Body(cutter, "private void Update()");
        var poll = update.IndexOf("PollUndoKeys();", StringComparison.Ordinal);
        var gate = update.IndexOf("commands = default;", StringComparison.Ordinal);
        Assert.True(poll >= 0);
        Assert.True(poll < gate);
        var keys = Body(cutter, "private void PollUndoKeys()");
        var undoKey = keys.IndexOf("keyboard.zKey.wasPressedThisFrame", StringComparison.Ordinal);
        var redoKey = keys.IndexOf("keyboard.yKey.wasPressedThisFrame", StringComparison.Ordinal);
        Assert.True(undoKey >= 0);
        Assert.True(keys.IndexOf("Undo();", undoKey, StringComparison.Ordinal) > undoKey);
        Assert.True(redoKey > undoKey);
        Assert.True(keys.IndexOf("Redo();", redoKey, StringComparison.Ordinal) > redoKey);

        var restart = Body(cutter, "public void Restart()");
        Assert.True(restart.IndexOf("ledger.Clear();", StringComparison.Ordinal) < restart.IndexOf("SpawnNext();", StringComparison.Ordinal));
        Assert.Contains("ledger.DiscardRedo();", Body(cutter, "public void MoveLeft()"), StringComparison.Ordinal);
        Assert.Contains("ledger.DiscardRedo();", Body(cutter, "public void MoveRight()"), StringComparison.Ordinal);
        Assert.Contains("ledger.DiscardRedo();", Body(cutter, "public void Rotate()"), StringComparison.Ordinal);

        var apply = Body(grid, "public bool Apply(PlaySnapshot snapshot)");
        var restore = apply.IndexOf("board.RestoreCells(snapshot.CopyCells(), snapshot.CoreOverlap)", StringComparison.Ordinal);
        var match = apply.IndexOf("Match.Restore(snapshot.ChainCount, snapshot.DropCount, snapshot.Defeated);", StringComparison.Ordinal);
        var clear = apply.IndexOf("ClearTiles();", StringComparison.Ordinal);
        var spawnRocks = apply.IndexOf("SpawnInitialRocks();", StringComparison.Ordinal);
        Assert.True(restore >= 0);
        Assert.True(restore < match);
        Assert.True(match < clear);
        Assert.True(clear < spawnRocks);

        Assert.Contains("undoButton = CreateHistoryButton(parent, \"Undo\", -300f, UndoTurn);", hud, StringComparison.Ordinal);
        Assert.Contains("redoButton = CreateHistoryButton(parent, \"Redo\", -100f, RedoTurn);", hud, StringComparison.Ordinal);
        Assert.Contains("rect.anchoredPosition = new Vector2(x, 96f);", hud, StringComparison.Ordinal);
        Assert.Contains("cutter.Undo();", Body(hud, "private void UndoTurn()"), StringComparison.Ordinal);
        Assert.Contains("cutter.Redo();", Body(hud, "private void RedoTurn()"), StringComparison.Ordinal);
        Assert.Contains("undoButton.interactable = cutter.CanUndo;", hud, StringComparison.Ordinal);
        Assert.Contains("redoButton.interactable = cutter.CanRedo;", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("UnityEngine", File.ReadAllText(Path.Combine(root, "src", "NegativeCarver.Core", "CarveLedger.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("UnityEngine", File.ReadAllText(Path.Combine(root, "src", "NegativeCarver.Core", "PlaySnapshot.cs")), StringComparison.Ordinal);
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

        throw new InvalidOperationException("Block was not closed: " + signature);
    }
}
