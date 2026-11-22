using System.Text.RegularExpressions;

namespace NegativeCarver.Core.Tests;

public sealed class PilotSceneTests
{
    [Fact]
    public void Pilot_scene_frames_the_full_board()
    {
        var root = FindRepositoryRoot();
        var scenePath = Path.Combine(root, "Assets", "Scenes", "Pilot.unity");
        var scene = File.ReadAllText(scenePath);
        var meta = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "BoardFramer.cs.meta"));
        var guid = Regex.Match(meta, "guid: ([0-9a-f]{32})").Groups[1].Value;
        Assert.Equal(32, guid.Length);

        var camera = Slice(scene, "--- !u!1 &330585543", "--- !u!");
        Assert.Contains("m_Name: Main Camera", camera, StringComparison.Ordinal);
        Assert.Contains("- component: {fileID: 330585548}", camera, StringComparison.Ordinal);

        var framerObject = Slice(scene, "--- !u!114 &330585548", "--- !u!");
        Assert.Contains("m_GameObject: {fileID: 330585543}", framerObject, StringComparison.Ordinal);
        Assert.Contains("m_Enabled: 1", framerObject, StringComparison.Ordinal);
        Assert.Contains("guid: " + guid, framerObject, StringComparison.Ordinal);
        Assert.Contains("m_Name: Board", scene, StringComparison.Ordinal);
        Assert.Contains("m_Name: Directional Light", scene, StringComparison.Ordinal);
        Assert.Contains("orthographic: 1", scene, StringComparison.Ordinal);
        var framer = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "BoardFramer.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var enable = Body(framer, "private void OnEnable()");
        var update = Body(framer, "private void Update()");
        var apply = Body(framer, "private void Apply()");
        Assert.Contains("Apply();", enable, StringComparison.Ordinal);
        Assert.Contains("Apply();", update, StringComparison.Ordinal);
        Assert.Contains("var view = BoardFrame.Fit(framed.aspect, PilotRules.HudBottomReserve);", apply, StringComparison.Ordinal);
        Assert.Contains("framed.orthographicSize = view.OrthographicSize;", apply, StringComparison.Ordinal);
        Assert.Contains("framed.transform.position = new Vector3(view.CameraX, view.CameraY, position.z);", apply, StringComparison.Ordinal);
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

    private static string Slice(string source, string startMarker, string nextMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var next = source.IndexOf(nextMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(next > start);
        return source.Substring(start, next - start);
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
