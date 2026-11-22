namespace NegativeCarver.Core;

public enum CarveImpact
{
    None = 0,
    Small = 1,
    Big = 2,
}

public static class CarveImpacts
{
    public static CarveImpact FromRemoved(int removed)
    {
        if (removed >= PilotRules.BigCarveCells)
        {
            return CarveImpact.Big;
        }

        if (removed == PilotRules.SmallCarveCells)
        {
            return CarveImpact.Small;
        }

        return CarveImpact.None;
    }
}
