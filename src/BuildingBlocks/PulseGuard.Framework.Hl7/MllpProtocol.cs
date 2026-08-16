using System.Text;

namespace PulseGuard.Framework.Hl7;

public static class MllpProtocol
{
    public const byte StartBlock = 0x0B;
    public const byte EndBlock = 0x1C;
    public const byte CarriageReturn = 0x0D;

    public static byte[] Frame(string message, Encoding? encoding = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        encoding ??= Encoding.UTF8;

        byte[] payload = encoding.GetBytes(message);
        byte[] frame = new byte[payload.Length + 3];
        frame[0] = StartBlock;
        payload.CopyTo(frame, 1);
        frame[^2] = EndBlock;
        frame[^1] = CarriageReturn;
        return frame;
    }
}
