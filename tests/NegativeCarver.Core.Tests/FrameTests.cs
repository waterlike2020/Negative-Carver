using System.Globalization;
using System.Text.RegularExpressions;
using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class FrameTests
{
    [Theory]
    [InlineData(16f, 9f)]
    [InlineData(9f, 16f)]
    public void The_board_sits_above_the_buttons(float width, float height)
    {
        var aspect = width / height;
        var view = BoardFrame.Fit(aspect, PilotRules.HudBottomReserve);
        var halfHeight = view.OrthographicSize;
        var halfWidth = halfHeight * aspect;
        var left = view.CameraX - halfWidth;
        var right = view.CameraX + halfWidth;
        var bottom = view.CameraY - halfHeight;
        var top = view.CameraY + halfHeight;
        var usableBottom = bottom + ((top - bottom) * PilotRules.HudBottomReserve);
        var boardRight = PilotRules.Width * PilotRules.CellSize;
        var boardTop = PilotRules.Height * PilotRules.CellSize;

        Assert.True(left < 0f);
        Assert.True(right > boardRight);
        Assert.True(usableBottom < 0f);
        Assert.True(top > boardTop);
    }

    [Theory]
    [InlineData(PilotRules.PcWidth, PilotRules.PcHeight)]
    [InlineData(PilotRules.MobileWidth, PilotRules.MobileHeight)]
    public void Command_buttons_stay_on_the_screen(int screenWidth, int screenHeight)
    {
        var hud = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Assets", "Scripts", "PilotHud.cs"));
        Assert.Contains("scaler.matchWidthOrHeight = 0.5f;", hud, StringComparison.Ordinal);
        AssertButtonChrome(hud, "private Button CreateHistoryButton(");
        AssertButtonChrome(hud, "private void CreateCommandButton(");
        var centers = new List<float>();
        foreach (Match match in Regex.Matches(hud, "Create(?:Command|History)Button\\(parent, \"[^\"]+\", (-?\\d+)f"))
        {
            centers.Add(float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
        }

        var places = new List<(float Y, float Width, float Height)>();
        foreach (Match match in Regex.Matches(
            hud,
            "anchoredPosition = new Vector2\\(x, (\\d+)f\\);\\s*rect.sizeDelta = new Vector2\\((\\d+)f, (\\d+)f\\);"))
        {
            places.Add((
                float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture)));
        }

        Assert.Equal(6, centers.Count);
        Assert.Equal(2, places.Count);
        var buttonWidth = places[0].Width;
        var buttonHeight = places[0].Height;
        Assert.Equal(buttonWidth, places[1].Width);
        Assert.Equal(buttonHeight, places[1].Height);
        foreach (var center in centers)
        {
            var left = (screenWidth * 0.5f) + center - (buttonWidth * 0.5f);
            var right = left + buttonWidth;
            Assert.True(left >= 0f);
            Assert.True(right <= screenWidth);
        }

        var lower = Math.Min(places[0].Y, places[1].Y);
        var upper = Math.Max(places[0].Y, places[1].Y);
        Assert.True(lower >= 0f);
        Assert.True(lower + buttonHeight <= upper);
        Assert.True(upper + buttonHeight <= screenHeight);
    }

    [Fact]
    public void The_saved_windows_match_the_pilot_screens()
    {
        var root = FindRepositoryRoot();
        var settings = File.ReadAllText(Path.Combine(root, "ProjectSettings", "ProjectSettings.asset"));
        var hud = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "PilotHud.cs"));

        Assert.Contains("defaultScreenWidth: " + PilotRules.PcWidth, settings, StringComparison.Ordinal);
        Assert.Contains("defaultScreenHeight: " + PilotRules.PcHeight, settings, StringComparison.Ordinal);
        Assert.Contains("androidDefaultWindowWidth: " + PilotRules.MobileWidth, settings, StringComparison.Ordinal);
        Assert.Contains("androidDefaultWindowHeight: " + PilotRules.MobileHeight, settings, StringComparison.Ordinal);
        Assert.Contains("scaler.referenceResolution = new Vector2(PilotRules.MobileWidth, PilotRules.MobileHeight);", hud, StringComparison.Ordinal);
        Assert.Contains("CanvasScaler.ScaleMode.ScaleWithScreenSize", hud, StringComparison.Ordinal);
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

    private static void AssertButtonChrome(string hud, string signature)
    {
        var start = hud.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = hud.IndexOf("private ", start + signature.Length, StringComparison.Ordinal);
        Assert.True(end > start);
        var body = hud.Substring(start, end - start);
        Assert.Contains("rect.anchorMin = new Vector2(0.5f, 0f);", body, StringComparison.Ordinal);
        Assert.Contains("rect.anchorMax = new Vector2(0.5f, 0f);", body, StringComparison.Ordinal);
        Assert.Contains("rect.pivot = new Vector2(0.5f, 0f);", body, StringComparison.Ordinal);
    }
}
