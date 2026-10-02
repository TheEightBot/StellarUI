using Microsoft.Maui.Controls;

namespace Stellar.Maui.UnitTests;

public sealed class MauiCommandRebindingTests
{
    private readonly MauiCommandRebinding _rebinding = new();

    [Fact]
    public void UpdatesTheCommandOfAControlWithCommandAndCommandParameter()
    {
        var button = new Button { Command = new Command(() => { }) };
        var replacement = new Command(() => { });

        Assert.True(_rebinding.TryUpdateCommand(button, replacement));
        Assert.Same(replacement, button.Command);
    }

    [Fact]
    public void UpdatesTheCommandOfAGestureRecognizer()
    {
        var recognizer = new TapGestureRecognizer();
        var replacement = new Command(() => { });

        Assert.True(_rebinding.TryUpdateCommand(recognizer, replacement));
        Assert.Same(replacement, recognizer.Command);
    }

    [Fact]
    public void ClearsTheCommandWhenTheReplacementIsNull()
    {
        var button = new Button { Command = new Command(() => { }) };

        Assert.True(_rebinding.TryUpdateCommand(button, null));
        Assert.Null(button.Command);
    }

    [Fact]
    public void LeavesTheCommandParameterAlone()
    {
        var button = new Button { Command = new Command(() => { }), CommandParameter = "parameter" };

        _rebinding.TryUpdateCommand(button, new Command(() => { }));

        Assert.Equal("parameter", button.CommandParameter);
    }

    [Fact]
    public void DeclinesANullControl()
    {
        Assert.False(_rebinding.TryUpdateCommand<Button>(null, new Command(() => { })));
    }

    [Fact]
    public void DeclinesAControlWithoutACommandProperty()
    {
        Assert.False(_rebinding.TryUpdateCommand(new Label(), new Command(() => { })));
    }

    [Fact]
    public void DeclinesWhenTheStaticControlTypeHasNoCommandProperty()
    {
        var original = new Command(() => { });
        var button = new Button { Command = original };

        Assert.False(_rebinding.TryUpdateCommand<View>(button, new Command(() => { })));
        Assert.Same(original, button.Command);
    }
}
