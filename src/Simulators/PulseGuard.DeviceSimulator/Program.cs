using System.Net.Sockets;
using System.Text;
using PulseGuard.Framework.Hl7;

string host = args.ElementAtOrDefault(0) ?? "127.0.0.1";
int port = args.Length > 1 && int.TryParse(args[1], out int configuredPort) ? configuredPort : 2575;
string controlId = Guid.NewGuid().ToString("N");

string message = string.Join(
    '\r',
    $"MSH|^~\\&|SYNTHETIC-MONITOR|WARD-A|PULSEGUARD|LAB|{DateTimeOffset.UtcNow:yyyyMMddHHmmss}||ORU^R01|{controlId}|P|2.5.1",
    "PID|1||synthetic-patient-0001^^^PULSEGUARD^MR||DOE^JANE||19800101",
    "OBR|1|||VITALS",
    "OBX|1|NM|59408-5^SpO2^LN||98|%|95-100|N|||F",
    string.Empty);

using TcpClient client = new();
using CancellationTokenSource operationTimeout = new(TimeSpan.FromSeconds(10));
await client.ConnectAsync(host, port, operationTimeout.Token);
await using NetworkStream stream = client.GetStream();

byte[] request = MllpProtocol.Frame(message);
await stream.WriteAsync(request, operationTimeout.Token);

MllpFrameDecoder decoder = new();
byte[] acknowledgement = await ReadFrameAsync(stream, decoder, operationTimeout.Token);
Hl7Message parsedAcknowledgement = Hl7MessageParser.Parse(Encoding.UTF8.GetString(acknowledgement));

Console.WriteLine($"Received {parsedAcknowledgement.MessageCode} for control ID {parsedAcknowledgement.FindSegment("MSA")?.GetField(2)}");

static async Task<byte[]> ReadFrameAsync(
    NetworkStream stream,
    MllpFrameDecoder decoder,
    CancellationToken cancellationToken)
{
    byte[] buffer = new byte[16 * 1024];

    while (true)
    {
        int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
        if (bytesRead == 0)
        {
            throw new EndOfStreamException("The MLLP connection closed before a complete acknowledgement was received.");
        }

        IReadOnlyList<byte[]> frames = decoder.Feed(buffer.AsSpan(0, bytesRead));
        if (frames.Count > 0)
        {
            return frames[0];
        }
    }
}
