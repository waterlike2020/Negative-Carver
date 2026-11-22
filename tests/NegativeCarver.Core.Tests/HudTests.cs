using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class HudTests
{
    [Theory]
    [InlineData(false, false, MatchStatus.Playing)]
    [InlineData(true, false, MatchStatus.Clear)]
    [InlineData(false, true, MatchStatus.Failed)]
    [InlineData(true, true, MatchStatus.Clear)]
    public void Status_reads_freedom_before_defeat(bool freed, bool defeated, string expected)
    {
        Assert.Equal(expected, MatchStatus.Describe(freed, defeated));
        Assert.Equal("Playing", MatchStatus.Playing);
        Assert.Equal("Stage Clear", MatchStatus.Clear);
        Assert.Equal("Stage Failed", MatchStatus.Failed);
    }

    [Fact]
    public void The_hud_shows_drops_chain_and_status_from_the_match()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var hud = File.ReadAllText(Path.Combine(dir.FullName, "Assets", "Scripts", "PilotHud.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var scene = File.ReadAllText(Path.Combine(dir.FullName, "Assets", "Scenes", "Pilot.unity"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var update = Body(hud, "private void Update()");
        var drops = update.IndexOf("dropText.text = \"Drops \" + grid.Match.DropCount;", StringComparison.Ordinal);
        var chain = update.IndexOf("chainText.text = \"Chain \" + grid.Match.ChainCount;", StringComparison.Ordinal);
        var status = update.IndexOf("statusText.text = MatchStatus.Describe(grid.CheckCoreGemFreed(), grid.Match.Defeated);", StringComparison.Ordinal);

        Assert.True(drops >= 0);
        Assert.True(chain > drops);
        Assert.True(status > chain);
        Assert.Contains("guid: e55a42719fb362c2f61a8b3d4c52ce17", scene, StringComparison.Ordinal);
        Assert.Contains("Assembly-CSharp::NegativeCarver.Unity.PilotHud\n  grid: {fileID: 880001011}", scene, StringComparison.Ordinal);
        Assert.DoesNotContain("UnityEngine", File.ReadAllText(Path.Combine(dir.FullName, "src", "NegativeCarver.Core", "MatchStatus.cs")), StringComparison.Ordinal);
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
