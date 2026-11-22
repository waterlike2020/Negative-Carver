namespace NegativeCarver.Core.Tests;

public sealed class AutoFallTests
{
    [Fact]
    public void A_new_cutter_starts_falling_and_can_still_steer()
    {
        var root = FindRepositoryRoot();
        var cutter = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "CutterController.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        var spawn = Body(cutter, "private void SpawnNext()");
        var deal = spawn.IndexOf("piece = queue.Next();", StringComparison.Ordinal);
        var show = spawn.IndexOf("ShowPreview();", StringComparison.Ordinal);
        var drop = spawn.IndexOf("BeginFall();", StringComparison.Ordinal);
        Assert.True(deal >= 0);
        Assert.True(show > deal);
        Assert.True(drop > show);

        var restore = Body(cutter, "private void Show(PlaySnapshot snapshot)");
        var missing = restore.IndexOf("piece = null;", StringComparison.Ordinal);
        var leave = restore.IndexOf("return;", missing, StringComparison.Ordinal);
        var use = restore.IndexOf("UsePiece(", StringComparison.Ordinal);
        var resume = restore.IndexOf("BeginFall();", StringComparison.Ordinal);
        Assert.True(missing >= 0);
        Assert.True(leave > missing);
        Assert.True(use > leave);
        Assert.True(resume > use);
        Assert.DoesNotContain("HardDrop(", restore, StringComparison.Ordinal);
        Assert.DoesNotContain("HardDrop(", spawn, StringComparison.Ordinal);
        Assert.DoesNotContain("DiscardRedo", restore, StringComparison.Ordinal);
        Assert.DoesNotContain("DiscardRedo", Body(cutter, "private void BeginFall()"), StringComparison.Ordinal);

        var left = BodyOf(Body(cutter, "public void MoveLeft()"), "if (falling)");
        var right = BodyOf(Body(cutter, "public void MoveRight()"), "if (falling)");
        var rotate = BodyOf(Body(cutter, "public void Rotate()"), "if (falling)");
        Assert.Contains("Steer(grid.Sidestep(piece, -1));", left, StringComparison.Ordinal);
        Assert.Contains("return;", left, StringComparison.Ordinal);
        Assert.DoesNotContain("TryMove", left, StringComparison.Ordinal);
        Assert.Contains("Steer(grid.Sidestep(piece, 1));", right, StringComparison.Ordinal);
        Assert.Contains("return;", right, StringComparison.Ordinal);
        Assert.DoesNotContain("TryMove", right, StringComparison.Ordinal);
        Assert.Contains("grid.TryRotateFalling(piece)", rotate, StringComparison.Ordinal);
        Assert.Contains("return;", rotate, StringComparison.Ordinal);
        Assert.DoesNotContain("TryRotateClockwise", rotate, StringComparison.Ordinal);

        var hardDrop = Body(cutter, "public void HardDrop()");
        var falling = BodyOf(hardDrop, "if (falling)");
        var guard = hardDrop.IndexOf("if (falling)", StringComparison.Ordinal);
        var elapsed = hardDrop.IndexOf("BeginFall();", StringComparison.Ordinal);
        Assert.True(guard >= 0);
        Assert.True(elapsed > guard);
        Assert.Contains("return;", falling, StringComparison.Ordinal);
        Assert.DoesNotContain("fallElapsed", falling, StringComparison.Ordinal);

        var update = Body(cutter, "private void Update()");
        Assert.True(update.IndexOf("commands.ConsumeInOrder(MoveLeft, MoveRight, Rotate, HardDrop);", StringComparison.Ordinal)
            < update.IndexOf("AdvanceFall();", StringComparison.Ordinal));
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

    private static string BodyOf(string source, string marker)
    {
        var start = source.IndexOf(marker, StringComparison.Ordinal);
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

        throw new InvalidOperationException(marker);
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
