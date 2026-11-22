namespace NegativeCarver.Core.Tests;

public sealed class PilotPrefabTests
{
    [Fact]
    public void Tile_prefabs_use_distinct_materials_and_block_tile()
    {
        var root = FindRepositoryRoot();
        var blockTile = "guid: c33e205f7d9140a0c4f86e1b2d30ac95";
        var pairs = new (string Prefab, string Material)[]
        {
            ("RockTile.prefab", "guid: e1a0c3d45b6f4788a9d2e7c1b4f08316"),
            ("CoreGemTile.prefab", "guid: f2b1d4e56c704899b0e3f8d2c5a19427"),
            ("CutterPreviewTile.prefab", "guid: a3c2e5f67d8159aac1f409e3d6b2a538"),
        };

        var seenColors = new HashSet<string>();
        foreach (var pair in pairs)
        {
            var prefab = File.ReadAllText(Path.Combine(root, "Assets", "Prefabs", pair.Prefab));
            Assert.Contains(blockTile, prefab, StringComparison.Ordinal);
            Assert.Contains(pair.Material, prefab, StringComparison.Ordinal);

            var materialPath = pair.Prefab switch
            {
                "RockTile.prefab" => "Rock.mat",
                "CoreGemTile.prefab" => "CoreGem.mat",
                _ => "CutterPreview.mat",
            };
            var material = File.ReadAllText(Path.Combine(root, "Assets", "Materials", materialPath));
            var colorLine = material.Split('\n').First(static line => line.Contains("_BaseColor:", StringComparison.Ordinal));
            Assert.True(seenColors.Add(colorLine));
        }

        Assert.Equal(3, seenColors.Count);
    }

    [Fact]
    public void Initial_spawn_uses_the_core_prefab_only_for_the_core_gem()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Pilot.unity"));
        var body = MethodBody(grid, "private void SpawnInitialRocks()");
        var rock = body.IndexOf("prefab = rockTilePrefab;", StringComparison.Ordinal);
        var gem = body.IndexOf("prefab = coreGemTilePrefab;", StringComparison.Ordinal);
        var type = body.IndexOf("var type = board.Get(x, y);", StringComparison.Ordinal);

        Assert.Contains("[SerializeField] private BlockTile coreGemTilePrefab;", grid, StringComparison.Ordinal);
        Assert.True(type >= 0);
        Assert.True(rock > type);
        Assert.True(gem > type);
        Assert.Contains("if (type == BlockType.SolidRock)", body, StringComparison.Ordinal);
        Assert.Contains("else if (type == BlockType.CoreGem)", body, StringComparison.Ordinal);
        Assert.Contains("Instantiate(\n                        prefab,", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Instantiate(\n                        rockTilePrefab,", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Instantiate(\n                        coreGemTilePrefab,", body, StringComparison.Ordinal);
        Assert.Contains("tile.Bind(this, x, y, type);", body, StringComparison.Ordinal);
        Assert.Contains(
            "coreGemTilePrefab: {fileID: 810000000000000005, guid: c5e407b89fa371cce3162b05f8d4c750, type: 3}",
            scene,
            StringComparison.Ordinal);
        Assert.Contains(
            "rockTilePrefab: {fileID: 810000000000000005, guid: b4d3f6a78e9260bbd2051af4e7c3b649, type: 3}",
            scene,
            StringComparison.Ordinal);
    }

    private static string MethodBody(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var open = source.IndexOf('{', start);
        Assert.True(open >= 0);
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
                    return source.Substring(open + 1, i - open - 1);
                }
            }
        }

        throw new InvalidOperationException("Block was not closed: " + signature);
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
