using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class PilotRulesTests
{
    [Fact]
    public void Board_matches_pilot_contract()
    {
        Assert.Equal(10, PilotRules.Width);
        Assert.Equal(18, PilotRules.Height);
        Assert.Equal(10, PilotRules.InitialRockRows);
        Assert.Equal(4, PilotRules.CoreX);
        Assert.Equal(2, PilotRules.CoreY);
        Assert.Equal(16, PilotRules.CutterSpawnY);
        Assert.Equal(15, PilotRules.DefeatHeight);
        Assert.Equal(30, PilotRules.MaxCutterDrops);
        Assert.Equal(1f, PilotRules.CellSize);
        Assert.Equal(0.4f, PilotRules.FallSecondsPerCell);
        Assert.Equal(1, PilotRules.FirstStage);
        Assert.Equal(0.22f, PilotRules.HudBottomReserve);
        Assert.Equal(1920, PilotRules.PcWidth);
        Assert.Equal(1080, PilotRules.PcHeight);
        Assert.Equal(1080, PilotRules.MobileWidth);
        Assert.Equal(1920, PilotRules.MobileHeight);
    }

    [Fact]
    public void Block_types_match_the_pilot_contract()
    {
        Assert.Equal(0, (int)BlockType.Empty);
        Assert.Equal(1, (int)BlockType.SolidRock);
        Assert.Equal(2, (int)BlockType.CoreGem);
        Assert.Equal(3, (int)BlockType.CutterPreview);
        Assert.Equal(4, (int)BlockType.HardRock);
        Assert.Equal(5, Enum.GetValues<BlockType>().Length);
        Assert.Equal(20, PilotRules.HardRockCount);
        Assert.Equal(0.16f, PilotRules.HardRockRed);
        Assert.Equal(0.16f, PilotRules.HardRockGreen);
        Assert.Equal(0.18f, PilotRules.HardRockBlue);
        Assert.Equal(0.65f, PilotRules.HardRockMetallic);
        Assert.Equal(0.72f, PilotRules.HardRockSmoothness);
        Assert.Equal(1, PilotRules.SmallCarveCells);
        Assert.Equal(2, PilotRules.BigCarveCells);
        Assert.Equal(1000, PilotRules.ClearScorePerStage);
        Assert.Equal(500, PilotRules.SpareDropScore);
    }

    [Fact]
    public void Core_sources_do_not_reference_UnityEngine()
    {
        var root = FindRepositoryRoot();
        var coreDir = Path.Combine(root, "src", "NegativeCarver.Core");
        var files = Directory.GetFiles(coreDir, "*.*", SearchOption.AllDirectories)
            .Where(static path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

        foreach (var path in files)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("UnityEngine", text, StringComparison.Ordinal);
        }
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
