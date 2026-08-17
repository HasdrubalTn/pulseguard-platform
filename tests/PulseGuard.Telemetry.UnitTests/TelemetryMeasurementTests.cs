using AutoFixture;
using FluentAssertions;
using PulseGuard.Telemetry.Domain;

namespace PulseGuard.Telemetry.UnitTests;

public sealed class TelemetryMeasurementTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void RecordRejectsNonFiniteClinicalValue()
    {
        Action act = () => TelemetryMeasurement.Record(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            VitalMeasurementType.HeartRate,
            double.NaN,
            "bpm",
            _fixture.Create<DateTimeOffset>(),
            _fixture.Create<DateTimeOffset>());

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("value");
    }
}
