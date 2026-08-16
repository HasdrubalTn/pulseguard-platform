using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PulseGuard.Framework.Messaging.RabbitMq;

internal sealed class RabbitMqConnectionManager : IRabbitMqConnectionManager, IAsyncDisposable
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnectionManager(IOptions<RabbitMqOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        RabbitMqOptions configuredOptions = options.Value;
        _connectionFactory = new ConnectionFactory
        {
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = configuredOptions.ClientProvidedName,
            HostName = configuredOptions.HostName,
            Password = configuredOptions.Password,
            Port = configuredOptions.Port,
            RequestedConnectionTimeout = configuredOptions.ConnectionTimeout,
            RequestedHeartbeat = configuredOptions.RequestedHeartbeat,
            TopologyRecoveryEnabled = true,
            UserName = configuredOptions.UserName,
            VirtualHost = configuredOptions.VirtualHost,
        };
    }

    public async ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        IConnection? currentConnection = Volatile.Read(ref _connection);
        if (currentConnection is { IsOpen: true })
        {
            return currentConnection;
        }

        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            currentConnection = _connection;
            if (currentConnection is { IsOpen: true })
            {
                return currentConnection;
            }

            if (currentConnection is not null)
            {
                await currentConnection.DisposeAsync();
            }

            currentConnection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            Volatile.Write(ref _connection, currentConnection);
            return currentConnection;
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connectionGate.WaitAsync();
        try
        {
            IConnection? currentConnection = _connection;
            _connection = null;

            if (currentConnection is not null)
            {
                await currentConnection.DisposeAsync();
            }
        }
        finally
        {
            _connectionGate.Release();
            _connectionGate.Dispose();
        }
    }
}
