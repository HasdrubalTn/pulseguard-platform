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
await client.ConnectAsync(host, port);
await using NetworkStream stream = client.GetStream();

byte[] request = MllpProtocol.Frame(message);
await stream.WriteAsync(request);

byte[] responseBuffer = new byte[16 * 1024];
int bytesRead = await stream.ReadAsync(responseBuffer);
MllpFrameDecoder decoder = new();
byte[] acknowledgement = decoder.Feed(responseBuffer.AsSpan(0, bytesRead)).Single();
Hl7Message parsedAcknowledgement = Hl7MessageParser.Parse(Encoding.UTF8.GetString(acknowledgement));

Console.WriteLine($"Received {parsedAcknowledgement.MessageCode} for control ID {parsedAcknowledgement.FindSegment("MSA")?.GetField(2)}");
