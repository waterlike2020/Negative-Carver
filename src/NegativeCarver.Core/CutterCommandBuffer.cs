namespace NegativeCarver.Core;

public enum CutterCommand
{
    Left = 0,
    Right = 1,
    Rotate = 2,
    HardDrop = 3,
}

public struct CutterCommandBuffer
{
    private bool left;
    private bool right;
    private bool rotate;
    private bool hardDrop;

    public void Add(CutterCommand command)
    {
        switch (command)
        {
            case CutterCommand.Left:
                left = true;
                break;
            case CutterCommand.Right:
                right = true;
                break;
            case CutterCommand.Rotate:
                rotate = true;
                break;
            case CutterCommand.HardDrop:
                hardDrop = true;
                break;
        }
    }

    public void ConsumeInOrder(System.Action left, System.Action right, System.Action rotate, System.Action hardDrop)
    {
        if (TryConsume(CutterCommand.Left) && left != null)
        {
            left();
        }

        if (TryConsume(CutterCommand.Right) && right != null)
        {
            right();
        }

        if (TryConsume(CutterCommand.Rotate) && rotate != null)
        {
            rotate();
        }

        if (TryConsume(CutterCommand.HardDrop) && hardDrop != null)
        {
            hardDrop();
        }
    }

    public bool TryConsume(CutterCommand command)
    {
        switch (command)
        {
            case CutterCommand.Left:
                return Take(ref left);
            case CutterCommand.Right:
                return Take(ref right);
            case CutterCommand.Rotate:
                return Take(ref rotate);
            case CutterCommand.HardDrop:
                return Take(ref hardDrop);
            default:
                return false;
        }
    }

    private static bool Take(ref bool pending)
    {
        if (!pending)
        {
            return false;
        }

        pending = false;
        return true;
    }
}
