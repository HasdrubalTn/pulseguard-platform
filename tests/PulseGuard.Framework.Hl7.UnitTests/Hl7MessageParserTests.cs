using AutoFixture;
using FluentAssertions;
using PulseGuard.Framework.Hl7;

namespace PulseGuard.Framework.Hl7.UnitTests;

public sealed class Hl7MessageParserTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ParseWithValidOruMessageExtractsEnvelopeMetadata()
    {
        string controlId = _fixture.Create<Guid>().ToString("N");
        string message = CreateOruMessage(controlId);

        Hl7Message result = Hl7MessageParser.Parse(message);

        result.MessageCode.Should().Be("ORU");
        result.TriggerEvent.Should().Be("R01");
        result.ControlId.Should().Be(controlId);
        result.Version.Should().Be("2.5.1");
        result.SendingApplication.Should().Be("MONITOR");
        result.FindSegment("OBX")!.GetField(5).Should().Be("98");
    }

    [Fact]
    public void ParseWithEmptyPayloadThrowsArgumentException()
    {
        Action act = () => Hl7MessageParser.Parse(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("PID|1||patient")]
    [InlineData("MSH|^~\\&|APP|FAC|PG|PG|20260816120000||ORU^R01||P|2.5.1")]
    public void ParseWithInvalidEnvelopeThrowsFormatException(string message)
    {
        Action act = () => Hl7MessageParser.Parse(message);

        act.Should().Throw<FormatException>();
    }

    private static string CreateOruMessage(string controlId) => string.Join(
        '\r',
        $"MSH|^~\\&|MONITOR|WARD-A|PULSEGUARD|HOSPITAL|20260816120000||ORU^R01|{controlId}|P|2.5.1",
        "PID|1||synthetic-patient^^^PULSEGUARD^MR||DOE^JANE||19800101",
        "OBR|1|||VITALS",
        "OBX|1|NM|59408-5^SpO2^LN||98|%|95-100|N|||F",
        string.Empty);
}
