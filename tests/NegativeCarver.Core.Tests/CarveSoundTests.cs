using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class CarveSoundTests
{
    [Fact]
    public void One_removed_cell_is_small_and_two_or_more_are_big()
    {
        Assert.Equal(CarveImpact.None, CarveImpacts.FromRemoved(0));
        Assert.Equal(CarveImpact.None, CarveImpacts.FromRemoved(-1));
        Assert.Equal(CarveImpact.Small, CarveImpacts.FromRemoved(PilotRules.SmallCarveCells));
        Assert.Equal(CarveImpact.Big, CarveImpacts.FromRemoved(PilotRules.BigCarveCells));
        Assert.Equal(CarveImpact.Big, CarveImpacts.FromRemoved(4));
    }

    [Fact]
    public void A_hard_rock_that_only_softens_does_not_count_as_removed()
    {
        var onlyHard = new Board();
        Assert.True(onlyHard.TrySet(0, 9, BlockType.HardRock));
        var softened = onlyHard.CarveSolids(0, 9, new[] { new CellOffset(0, 0) });
        Assert.Empty(softened);
        Assert.Equal(BlockType.SolidRock, onlyHard.Get(0, 9));
        Assert.Equal(CarveImpact.None, CarveImpacts.FromRemoved(softened.Count));

        var mixed = new Board();
        Assert.True(mixed.TrySet(0, 9, BlockType.HardRock));
        var one = mixed.CarveSolids(0, 9, new[] { new CellOffset(0, 0), new CellOffset(1, 0) });
        Assert.Equal(new[] { new CellOffset(1, 9) }, one);
        Assert.Equal(CarveImpact.Small, CarveImpacts.FromRemoved(one.Count));

        var many = new Board();
        var two = many.CarveSolids(0, 9, new[] { new CellOffset(0, 0), new CellOffset(1, 0) });
        Assert.Equal(2, two.Count);
        Assert.Equal(CarveImpact.Big, CarveImpacts.FromRemoved(two.Count));
    }

    [Fact]
    public void The_play_sound_follows_removed_cells_and_the_download_clips()
    {
        var root = FindRepositoryRoot();
        var fx = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "CarveFxManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Contains("using NegativeCarver.Core;", fx, StringComparison.Ordinal);
        var sound = MethodBody(fx, "public void PlayCarveSound(CarveImpact impact)");
        Assert.Contains("PlayClip(smallClip);", sound, StringComparison.Ordinal);
        Assert.Contains("PlayClip(bigClip);", sound, StringComparison.Ordinal);
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Pilot.unity"));
        var carve = MethodBody(grid, "public void CarveArea(System.Collections.Generic.List<Vector2Int> cutterPattern, Vector2Int origin)");
        var counted = carve.IndexOf("var impact = CarveImpacts.FromRemoved(carved.Count);", StringComparison.Ordinal);
        var call = carve.IndexOf("carveFx.PlayCarveSound(impact);", StringComparison.Ordinal);
        Assert.True(counted >= 0);
        Assert.True(call > counted);
        Assert.DoesNotContain("softened.Count", carve.Substring(counted), StringComparison.Ordinal);
        Assert.DoesNotContain("carved.Count + softened", carve, StringComparison.Ordinal);

        Assert.Equal(129567, new FileInfo(Path.Combine(root, "Assets", "Resources", "Carve", "small.mp3")).Length);
        Assert.Equal(109505, new FileInfo(Path.Combine(root, "Assets", "Resources", "Carve", "big.mp3")).Length);
        var smallMeta = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Carve", "small.mp3.meta")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var bigMeta = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Carve", "big.mp3.meta")).Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Contains("guid: a7c3e1b94d2f4a6e8b0c5d7f1a3e9b21", smallMeta, StringComparison.Ordinal);
        Assert.Contains("guid: b8d4f2c05e3a4b7f9c1d6e8a2b4f0c32", bigMeta, StringComparison.Ordinal);
        Assert.Contains("\n  preloadAudioData: 1\n  loadInBackground: 0\n", smallMeta, StringComparison.Ordinal);
        Assert.Contains("\n  3D: 0\n", smallMeta, StringComparison.Ordinal);
        Assert.Contains("\n  preloadAudioData: 1\n  loadInBackground: 0\n", bigMeta, StringComparison.Ordinal);
        Assert.Contains("\n  3D: 0\n", bigMeta, StringComparison.Ordinal);
        Assert.Contains("smallClip = Ready(smallClip, \"Carve/small\");", fx, StringComparison.Ordinal);
        Assert.Contains("bigClip = Ready(bigClip, \"Carve/big\");", fx, StringComparison.Ordinal);
        Assert.Contains("Resources.Load<AudioClip>(resource);", fx, StringComparison.Ordinal);
        Assert.Contains("voice.spatialBlend = 0f;", fx, StringComparison.Ordinal);
        Assert.Contains("voice.volume = 1f;", fx, StringComparison.Ordinal);
        Assert.Contains("clip.LoadAudioData();", fx, StringComparison.Ordinal);
        var manager = scene.IndexOf("guid: d44f31608ea251b1d5097f2c3e41bd06", StringComparison.Ordinal);
        var wired = scene.Substring(manager, 500);
        Assert.Contains("smallClip: {fileID: 8300000, guid: a7c3e1b94d2f4a6e8b0c5d7f1a3e9b21, type: 3}", wired, StringComparison.Ordinal);
        Assert.Contains("bigClip: {fileID: 8300000, guid: b8d4f2c05e3a4b7f9c1d6e8a2b4f0c32, type: 3}", wired, StringComparison.Ordinal);
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
