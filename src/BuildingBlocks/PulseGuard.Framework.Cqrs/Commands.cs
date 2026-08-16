namespace PulseGuard.Framework.Cqrs;

public interface ICommand;

public interface ICommand<out TResult> : ICommand;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    ValueTask<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
