namespace NegativeCarver.Core;

public sealed class CarveLedger
{
    private readonly System.Collections.Generic.List<PlaySnapshot> before = new System.Collections.Generic.List<PlaySnapshot>();
    private readonly System.Collections.Generic.List<PlaySnapshot> after = new System.Collections.Generic.List<PlaySnapshot>();
    private int applied;

    public bool CanUndo => applied > 0;

    public bool CanRedo => applied < before.Count;

    public void Commit(PlaySnapshot? prior, PlaySnapshot? result)
    {
        if (prior == null || result == null)
        {
            return;
        }

        DiscardRedo();
        before.Add(prior);
        after.Add(result);
        applied++;
    }

    public PlaySnapshot? Undo()
    {
        if (!CanUndo)
        {
            return null;
        }

        applied--;
        return before[applied];
    }

    public PlaySnapshot? Redo()
    {
        if (!CanRedo)
        {
            return null;
        }

        var snapshot = after[applied];
        applied++;
        return snapshot;
    }

    public void Clear()
    {
        before.Clear();
        after.Clear();
        applied = 0;
    }

    public void DiscardRedo()
    {
        if (applied >= before.Count)
        {
            return;
        }

        before.RemoveRange(applied, before.Count - applied);
        after.RemoveRange(applied, after.Count - applied);
    }
}
