using System.Windows.Input;
using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class DelegateCommandTests
{
    [Fact]
    public void ConstructorWithNullExecuteThrowsArgumentNullException()
    {
        var act = static () => new DelegateCommand(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExecuteInvokesTheProvidedDelegate()
    {
        var wasCalled = false;
        object? receivedParameter = null;
        var command = new DelegateCommand(param =>
        {
            wasCalled = true;
            receivedParameter = param;
        });

        command.Execute("test");

        wasCalled.Should().BeTrue();
        receivedParameter.Should().Be("test");
    }

    [Fact]
    public void ExecutePassesNullParameter()
    {
        var wasCalled = false;
        var receivedParameter = new object();
        var command = new DelegateCommand(param =>
        {
            wasCalled = true;
            receivedParameter = param;
        });

        command.Execute(null);

        wasCalled.Should().BeTrue();
        receivedParameter.Should().BeNull();
    }

    [Fact]
    public void CanExecuteWithoutPredicateReturnsTrue()
    {
        var command = new DelegateCommand(static _ => { });

        var result = command.CanExecute(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecuteWithTruePredicateReturnsTrue()
    {
        var command = new DelegateCommand(static _ => { }, static _ => true);

        var result = command.CanExecute(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecuteWithFalsePredicateReturnsFalse()
    {
        var command = new DelegateCommand(static _ => { }, static _ => false);

        var result = command.CanExecute(null);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanExecutePassesParameterToPredicate()
    {
        object? receivedParameter = null;
        var command = new DelegateCommand(static _ => { }, param =>
        {
            receivedParameter = param;
            return true;
        });

        command.CanExecute("hello");

        receivedParameter.Should().Be("hello");
    }

    [Fact]
    public void RaiseCanExecuteChangedTriggersEvent()
    {
        var command = new DelegateCommand(static _ => { });
        var wasRaised = false;
        command.CanExecuteChanged += (_, _) => { wasRaised = true; };

        command.RaiseCanExecuteChanged();

        wasRaised.Should().BeTrue();
    }

    [Fact]
    public void RaiseCanExecuteChangedWithNoSubscribersDoesNotThrow()
    {
        var command = new DelegateCommand(static _ => { });

        var act = command.RaiseCanExecuteChanged;

        act.Should().NotThrow();
    }

    [Fact]
    public void CanExecuteChangedEventPassesCorrectSender()
    {
        var command = new DelegateCommand(static _ => { });
        object? sender = null;
        command.CanExecuteChanged += (s, _) => { sender = s; };

        command.RaiseCanExecuteChanged();

        sender.Should().BeSameAs(command);
    }

    [Fact]
    public void ImplementsICommandInterface()
    {
        var command = new DelegateCommand(static _ => { });

        command.Should().BeAssignableTo<ICommand>();
    }
}
