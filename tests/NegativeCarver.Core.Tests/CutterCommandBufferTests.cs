using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class CutterCommandBufferTests
{
    [Fact]
    public void A_repeated_command_is_consumed_once()
    {
        var buffer = new CutterCommandBuffer();
        buffer.Add(CutterCommand.Left);
        buffer.Add(CutterCommand.Left);

        Assert.True(buffer.TryConsume(CutterCommand.Left));
        Assert.False(buffer.TryConsume(CutterCommand.Left));
        Assert.False(buffer.TryConsume(CutterCommand.Right));
        Assert.False(buffer.TryConsume(CutterCommand.Rotate));
        Assert.False(buffer.TryConsume(CutterCommand.HardDrop));
    }

    [Fact]
    public void Distinct_commands_each_run_once_in_one_frame()
    {
        var buffer = new CutterCommandBuffer();
        buffer.Add(CutterCommand.HardDrop);
        buffer.Add(CutterCommand.Rotate);
        buffer.Add(CutterCommand.Right);
        buffer.Add(CutterCommand.Left);
        buffer.Add(CutterCommand.Left);
        buffer.Add(CutterCommand.HardDrop);

        Assert.True(buffer.TryConsume(CutterCommand.Left));
        Assert.True(buffer.TryConsume(CutterCommand.Right));
        Assert.True(buffer.TryConsume(CutterCommand.Rotate));
        Assert.True(buffer.TryConsume(CutterCommand.HardDrop));
        Assert.False(buffer.TryConsume(CutterCommand.Left));
        Assert.False(buffer.TryConsume(CutterCommand.Right));
        Assert.False(buffer.TryConsume(CutterCommand.Rotate));
        Assert.False(buffer.TryConsume(CutterCommand.HardDrop));
    }

    [Fact]
    public void An_empty_buffer_consumes_nothing()
    {
        var buffer = new CutterCommandBuffer();

        Assert.False(buffer.TryConsume(CutterCommand.Left));
        Assert.False(buffer.TryConsume(CutterCommand.Right));
        Assert.False(buffer.TryConsume(CutterCommand.Rotate));
        Assert.False(buffer.TryConsume(CutterCommand.HardDrop));
    }

    [Fact]
    public void Consume_runs_only_added_commands_in_left_right_rotate_drop_order()
    {
        var buffer = new CutterCommandBuffer();
        buffer.Add(CutterCommand.HardDrop);
        buffer.Add(CutterCommand.Rotate);
        buffer.Add(CutterCommand.Right);
        buffer.Add(CutterCommand.Left);
        buffer.Add(CutterCommand.Right);
        var seen = new List<CutterCommand>();

        buffer.ConsumeInOrder(
            () => seen.Add(CutterCommand.Left),
            () => seen.Add(CutterCommand.Right),
            () => seen.Add(CutterCommand.Rotate),
            () => seen.Add(CutterCommand.HardDrop));
        buffer.ConsumeInOrder(
            () => seen.Add(CutterCommand.Left),
            () => seen.Add(CutterCommand.Right),
            () => seen.Add(CutterCommand.Rotate),
            () => seen.Add(CutterCommand.HardDrop));

        Assert.Equal(
            new[] { CutterCommand.Left, CutterCommand.Right, CutterCommand.Rotate, CutterCommand.HardDrop },
            seen);
    }

    [Theory]
    [InlineData(CutterCommand.Left)]
    [InlineData(CutterCommand.Right)]
    [InlineData(CutterCommand.Rotate)]
    [InlineData(CutterCommand.HardDrop)]
    public void Consume_runs_only_the_command_that_was_added(CutterCommand command)
    {
        var buffer = new CutterCommandBuffer();
        buffer.Add(command);
        var seen = new List<CutterCommand>();

        buffer.ConsumeInOrder(
            () => seen.Add(CutterCommand.Left),
            () => seen.Add(CutterCommand.Right),
            () => seen.Add(CutterCommand.Rotate),
            () => seen.Add(CutterCommand.HardDrop));

        Assert.Equal(new[] { command }, seen);
    }
}
