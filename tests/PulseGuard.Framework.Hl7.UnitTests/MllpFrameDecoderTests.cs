using System.Text;
using AutoFixture;
using FluentAssertions;
using PulseGuard.Framework.Hl7;

namespace PulseGuard.Framework.Hl7.UnitTests;

public sealed class MllpFrameDecoderTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Feed_WithFragmentedFrame_ReturnsFrameOnlyAfterTerminator()
    {
        string message = $"MSH|^~\\&|APP|FAC|PG|PG|20260816120000||ADT^A01|{_fixture.Create<Guid>():N}|P|2.5.1\r";
        byte[] frame = MllpProtocol.Frame(message);
        MllpFrameDecoder sut = new();

        IReadOnlyList<byte[]> firstResult = sut.Feed(frame.AsSpan(0, frame.Length / 2));
        IReadOnlyList<byte[]> secondResult = sut.Feed(frame.AsSpan(frame.Length / 2));

        firstResult.Should().BeEmpty();
        secondResult.Should().ContainSingle();
        Encoding.UTF8.GetString(secondResult[0]).Should().Be(message);
    }

    [Fact]
    public void Feed_WhenPayloadExceedsLimit_ThrowsAndResetsDecoder()
    {
        MllpFrameDecoder sut = new(maximumPayloadBytes: 4);
        byte[] oversized = [MllpProtocol.StartBlock, 1, 2, 3, 4, 5];

        Action act = () => sut.Feed(oversized);

        act.Should().Throw<InvalidDataException>();
        sut.Feed(MllpProtocol.Frame("OK")).Should().ContainSingle();
    }
}
