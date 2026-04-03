using FluentAssertions;
using WindowsFileManager.Helpers;

namespace WindowsFileManager.Tests.Helpers;

public class RelayCommandTests
{
    [Fact]
    public void Execute_ShouldCallAction()
    {
        var executed = false;
        var command = new RelayCommand(_ => executed = true);

        command.Execute(null);

        executed.Should().BeTrue();
    }

    [Fact]
    public void Execute_ShouldPassParameter()
    {
        object? receivedParam = null;
        var command = new RelayCommand(p => receivedParam = p);

        command.Execute("test");

        receivedParam.Should().Be("test");
    }

    [Fact]
    public void CanExecute_WithoutPredicate_ShouldReturnTrue()
    {
        var command = new RelayCommand(_ => { });

        command.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithPredicate_ShouldEvaluate()
    {
        var command = new RelayCommand(_ => { }, p => p is string s && s == "yes");

        command.CanExecute("yes").Should().BeTrue();
        command.CanExecute("no").Should().BeFalse();
        command.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Constructor_NullExecute_ShouldThrow()
    {
        var act = () => new RelayCommand(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CanExecuteChanged_ShouldSubscribeAndUnsubscribe()
    {
        var command = new RelayCommand(_ => { });
        var handler = new EventHandler((_, _) => { });

        // Should not throw
        command.CanExecuteChanged += handler;
        command.CanExecuteChanged -= handler;
    }

    [Fact]
    public void RaiseCanExecuteChanged_ShouldNotThrow()
    {
        var command = new RelayCommand(_ => { });

        var act = () => command.RaiseCanExecuteChanged();

        act.Should().NotThrow();
    }
}
