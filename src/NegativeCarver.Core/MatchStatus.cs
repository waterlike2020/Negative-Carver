namespace NegativeCarver.Core;

public static class MatchStatus
{
    public const string Playing = "Playing";
    public const string Clear = "Stage Clear";
    public const string Failed = "Stage Failed";

    public static string Describe(bool freed, bool defeated)
    {
        if (freed)
        {
            return Clear;
        }

        if (defeated)
        {
            return Failed;
        }

        return Playing;
    }
}
