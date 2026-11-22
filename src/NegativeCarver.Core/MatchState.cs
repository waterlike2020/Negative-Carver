namespace NegativeCarver.Core;

public sealed class MatchState
{
    public int ChainCount { get; private set; }

    public int DropCount { get; private set; }

    public int Stage { get; private set; } = PilotRules.FirstStage;

    public int Score { get; private set; }

    public bool Defeated { get; private set; }

    private bool stageClearBanked;

    public void RecordChain(int chainCount)
    {
        ChainCount = chainCount;
    }

    public void Reset()
    {
        ChainCount = 0;
        DropCount = 0;
        Defeated = false;
        stageClearBanked = false;
    }

    public static int StageClearScore(int stage, int dropsUsed)
    {
        var spare = PilotRules.MaxCutterDrops - dropsUsed;
        if (spare < 0)
        {
            spare = 0;
        }

        return (PilotRules.ClearScorePerStage * stage) + (spare * PilotRules.SpareDropScore);
    }

    public int ShownScore(bool cleared)
    {
        if (!cleared || stageClearBanked)
        {
            return Score;
        }

        return Score + StageClearScore(Stage, DropCount);
    }

    public void BankStageClear()
    {
        if (stageClearBanked)
        {
            return;
        }

        stageClearBanked = true;
        Score += StageClearScore(Stage, DropCount);
    }

    public void AdvanceStage()
    {
        Stage++;
        Reset();
    }

    public void Restore(int chainCount, int dropCount, bool defeated)
    {
        ChainCount = chainCount;
        DropCount = dropCount;
        Defeated = defeated;
    }

    public void RecordDrop(bool rockOnDefeatLine)
    {
        if (Defeated)
        {
            return;
        }

        DropCount++;
        if (rockOnDefeatLine || DropCount >= PilotRules.MaxCutterDrops)
        {
            Defeated = true;
        }
    }
}
