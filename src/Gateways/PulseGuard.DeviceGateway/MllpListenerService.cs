using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using PulseGuard.Framework.Hl7;

namespace PulseGuard.DeviceGateway;

public sealed partial class MllpListenerService(
    IOptions<MllpListenerOptions> options,
    Hl7MessageProcessor processor,
    ILogger<MllpListenerService> logger) : BackgroundService
{
    private readonly MllpListenerOptions _options = options.Value;
    private readonly SemaphoreSlim _connectionSlots = new(options.Value.MaximumConcurrentConnections);
    private TcpListener? _listener;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IPAddress bindAddress = IPAddress.Parse(_options.BindAddress);
        _listener = new TcpListener(bindAddress, _options.Port);
        _listener.Start();

        LogListenerStarted(logger, bindAddress, _options.Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _connectionSlots.WaitAsync(stoppingToken).ConfigureAwait(false);

                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                    _ = HandleClientAndReleaseSlotAsync(client, stoppingToken);
                }
                catch
                {
                    _connectionSlots.Release();
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogListenerStopping(logger);
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleClientAndReleaseSlotAsync(TcpClient client, CancellationToken stoppingToken)
    {
        try
        {
            await HandleClientSafelyAsync(client, stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionSlots.Release();
        }
    }

    private async Task HandleClientSafelyAsync(TcpClient client, CancellationToken stoppingToken)
    {
        using (client)
        {
            try
            {
                await HandleClientAsync(client, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown path.
            }
            catch (Exception exception) when (exception is IOException or SocketException or FormatException or InvalidDataException)
            {
                LogClientProtocolOrTransportError(logger, exception);
            }
            catch (Exception exception)
            {
                // The per-connection boundary must observe every fault because clients run concurrently.
                LogUnexpectedClientFailure(logger, exception);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        client.NoDelay = true;
        await using NetworkStream stream = client.GetStream();
        MllpFrameDecoder decoder = new(_options.MaximumMessageBytes);
        byte[] buffer = new byte[16 * 1024];

        while (!stoppingToken.IsCancellationRequested)
        {
            using CancellationTokenSource readTimeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            readTimeout.CancelAfter(TimeSpan.FromSeconds(_options.ReadTimeoutSeconds));

            int bytesRead = await stream.ReadAsync(buffer, readTimeout.Token).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                return;
            }

            foreach (byte[] frame in decoder.Feed(buffer.AsSpan(0, bytesRead)))
            {
                string payload = Encoding.UTF8.GetString(frame);
                string acknowledgement = await processor.ProcessAsync(payload, stoppingToken).ConfigureAwait(false);
                byte[] response = MllpProtocol.Frame(acknowledgement);
                await stream.WriteAsync(response, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    public override void Dispose()
    {
        _listener?.Stop();
        base.Dispose();
    }

    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Information,
        Message = "HL7 MLLP listener started on {Address}:{Port}")]
    private static partial void LogListenerStarted(ILogger logger, IPAddress address, int port);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "HL7 MLLP listener is stopping")]
    private static partial void LogListenerStopping(ILogger logger);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Warning,
        Message = "HL7 MLLP client connection ended with a protocol or transport error")]
    private static partial void LogClientProtocolOrTransportError(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1103,
        Level = LogLevel.Error,
        Message = "Unexpected failure while processing an HL7 MLLP client connection")]
    private static partial void LogUnexpectedClientFailure(ILogger logger, Exception exception);
}
