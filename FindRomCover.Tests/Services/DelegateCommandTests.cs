using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class DelegateCommandTests
{
    [Fact]
    public void ConstructorWithNullExecuteShouldThrowArgumentNullException()
    {
        var act = static () => new DelegateCommand(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("execute");
    }

    [Fact]
    public void CanExecuteWithoutCanExecuteFuncShouldReturnTrue()
    {
        var command = new DelegateCommand(static _ => { });

        var result = command.CanExecute(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecuteWithCanExecuteFuncShouldReturnFuncResult()
    {
        var command = new DelegateCommand(static _ => { }, static _ => false);

        var result = command.CanExecute(null);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanExecuteWithCanExecuteFuncReturningTrueShouldReturnTrue()
    {
        var command = new DelegateCommand(static _ => { }, static _ => true);

        var result = command.CanExecute(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void ExecuteShouldInvokeAction()
    {
        var executed = false;
        var command = new DelegateCommand(_ => { executed = true; });

        command.Execute(null);

        executed.Should().BeTrue();
    }

    [Fact]
    public void ExecuteShouldPassParameter()
    {
        object? receivedParam = null;
        var command = new DelegateCommand(param => { receivedParam = param; });

        command.Execute("test-param");

        receivedParam.Should().Be("test-param");
    }

    [Fact]
    public void ExecuteWithNullParameterShouldWork()
    {
        object? receivedParam = "not null";
        var command = new DelegateCommand(param => { receivedParam = param; });

        command.Execute(null);

        receivedParam.Should().BeNull();
    }

    [Fact]
    public void CanExecuteShouldPassParameter()
    {
        object? receivedParam = null;
        var command = new DelegateCommand(static _ => { }, param =>
        {
            receivedParam = param;
            return true;
        });

        command.CanExecute("test-param");

        receivedParam.Should().Be("test-param");
    }

    [Fact]
    public void CanExecuteChangedShouldBeSubscribable()
    {
        var command = new DelegateCommand(static _ => { });
        var eventRaised = false;
        EventHandler handler = (_, _) => { eventRaised = true; };
        command.CanExecuteChanged += handler;

        command.CanExecuteChanged -= handler;

        // Verify subscription/unsubscription works without error
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void CanExecuteWithFuncReturningFalseShouldReturnFalse()
    {
        var command = new DelegateCommand(static _ => { }, static _ => false);

        command.CanExecute("anything").Should().BeFalse();
        command.CanExecute(null).Should().BeFalse();
    }
}
