namespace NegativeCarver.Core.Tests;

public sealed class MobileButtonTests
{
    [Fact]
    public void Buttons_join_the_same_frame_buffer_as_the_keyboard()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var root = dir.FullName;
        var cutter = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "CutterController.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var hud = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "PilotHud.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Pilot.unity"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        var enqueue = Body(cutter, "public void Enqueue(CutterCommand command)");
        Assert.Contains("commands.Add(command);", enqueue, StringComparison.Ordinal);
        Assert.DoesNotContain("MoveLeft", enqueue, StringComparison.Ordinal);
        Assert.DoesNotContain("MoveRight", enqueue, StringComparison.Ordinal);
        Assert.DoesNotContain("Rotate(", enqueue, StringComparison.Ordinal);
        Assert.DoesNotContain("HardDrop", enqueue, StringComparison.Ordinal);

        var update = Body(cutter, "private void Update()");
        var over = update.IndexOf("commands = default;", StringComparison.Ordinal);
        var leave = update.IndexOf("return;", over, StringComparison.Ordinal);
        var keys = update.IndexOf("Keyboard.current", StringComparison.Ordinal);
        var consume = update.IndexOf("commands.ConsumeInOrder(MoveLeft, MoveRight, Rotate, HardDrop);", StringComparison.Ordinal);
        Assert.Contains("[DefaultExecutionOrder(100)]", cutter, StringComparison.Ordinal);
        Assert.True(over >= 0);
        Assert.True(leave > over);
        Assert.True(keys > leave);
        Assert.True(consume > keys);
        Assert.Contains("commands.Add(CutterCommand.Left);", update, StringComparison.Ordinal);
        Assert.Contains("commands.Add(CutterCommand.Right);", update, StringComparison.Ordinal);
        Assert.Contains("commands.Add(CutterCommand.Rotate);", update, StringComparison.Ordinal);
        Assert.Contains("commands.Add(CutterCommand.HardDrop);", update, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(update, "commands.ConsumeInOrder(MoveLeft, MoveRight, Rotate, HardDrop);"));
        Assert.DoesNotContain("new CutterCommandBuffer", update, StringComparison.Ordinal);

        Assert.Contains("CreateCommandButton(parent, \"Left\", -300f, CutterCommand.Left);", hud, StringComparison.Ordinal);
        Assert.Contains("CreateCommandButton(parent, \"Right\", -100f, CutterCommand.Right);", hud, StringComparison.Ordinal);
        Assert.Contains("CreateCommandButton(parent, \"Rotate\", 100f, CutterCommand.Rotate);", hud, StringComparison.Ordinal);
        Assert.Contains("CreateCommandButton(parent, \"Drop\", 300f, CutterCommand.HardDrop);", hud, StringComparison.Ordinal);
        Assert.Contains("button.onClick.AddListener(() => Enqueue(command));", hud, StringComparison.Ordinal);
        Assert.Contains("cutter.Enqueue(command);", Body(hud, "private void Enqueue(CutterCommand command)"), StringComparison.Ordinal);
        Assert.Contains("text.text = label;", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("cutter.MoveLeft", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("cutter.MoveRight", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("cutter.Rotate", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("cutter.HardDrop", hud, StringComparison.Ordinal);
        Assert.Contains("rect.anchorMin = new Vector2(0.5f, 0f);", hud, StringComparison.Ordinal);
        Assert.Contains("rect.anchorMax = new Vector2(0.5f, 0f);", hud, StringComparison.Ordinal);
        Assert.Contains("Assembly-CSharp::NegativeCarver.Unity.PilotHud\n  grid: {fileID: 880001011}\n  cutter: {fileID: 880001012}", scene, StringComparison.Ordinal);
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

    private static int CountOf(string source, string text)
    {
        var count = 0;
        var start = 0;
        while (true)
        {
            var found = source.IndexOf(text, start, StringComparison.Ordinal);
            if (found < 0)
            {
                return count;
            }

            count++;
            start = found + text.Length;
        }
    }
}
