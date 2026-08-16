namespace PulseGuard.Framework.Hl7;

public sealed class MllpFrameDecoder
{
    private readonly int _maximumPayloadBytes;
    private readonly List<byte> _payload = [];
    private DecoderState _state;

    public MllpFrameDecoder(int maximumPayloadBytes = 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumPayloadBytes, 1);
        _maximumPayloadBytes = maximumPayloadBytes;
    }

    public IReadOnlyList<byte[]> Feed(ReadOnlySpan<byte> bytes)
    {
        List<byte[]> frames = [];

        foreach (byte current in bytes)
        {
            switch (_state)
            {
                case DecoderState.WaitingForStart when current == MllpProtocol.StartBlock:
                    _payload.Clear();
                    _state = DecoderState.ReadingPayload;
                    break;

                case DecoderState.ReadingPayload when current == MllpProtocol.EndBlock:
                    _state = DecoderState.WaitingForCarriageReturn;
                    break;

                case DecoderState.ReadingPayload:
                    Append(current);
                    break;

                case DecoderState.WaitingForCarriageReturn when current == MllpProtocol.CarriageReturn:
                    frames.Add(_payload.ToArray());
                    Reset();
                    break;

                case DecoderState.WaitingForCarriageReturn:
                    Reset();
                    throw new FormatException("An MLLP end block must be followed by a carriage return.");
            }
        }

        return frames;
    }

    private void Append(byte value)
    {
        if (_payload.Count >= _maximumPayloadBytes)
        {
            Reset();
            throw new InvalidDataException($"The MLLP payload exceeds {_maximumPayloadBytes} bytes.");
        }

        _payload.Add(value);
    }

    private void Reset()
    {
        _payload.Clear();
        _state = DecoderState.WaitingForStart;
    }

    private enum DecoderState
    {
        WaitingForStart,
        ReadingPayload,
        WaitingForCarriageReturn,
    }
}
