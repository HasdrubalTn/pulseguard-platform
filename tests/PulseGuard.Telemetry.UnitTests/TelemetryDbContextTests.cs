using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.Telemetry.Domain;
using PulseGuard.Telemetry.Infrastructure;

namespace PulseGuard.Telemetry.UnitTests;

public sealed class TelemetryDbContextTests
{
    [Fact]
    public void ModelKeepsMeasurementsAndInboxInsideTelemetrySchema()
    {
        using TelemetryDbContext sut = CreateContext();

        IEntityType measurement = sut.Model.FindEntityType(typeof(TelemetryMeasurement))!;
        IEntityType inbox = sut.Model.FindEntityType(typeof(InboxMessage))!;

        measurement.GetSchema().Should().Be("telemetry");
        measurement.GetTableName().Should().Be("measurements");
        measurement.FindPrimaryKey()!.Properties.Should().ContainSingle()
            .Which.Name.Should().Be(nameof(TelemetryMeasurement.Id));
        inbox.GetSchema().Should().Be("telemetry");
        inbox.GetTableName().Should().Be("inbox_messages");
        sut.Model.FindEntityType(typeof(OutboxMessage)).Should().BeNull();
    }

    [Fact]
    public void ContextDiscoversTelemetryMigration()
    {
        using TelemetryDbContext sut = CreateContext();

        sut.Database.GetMigrations().Should().Equal(
            "20260817010000_AddTelemetryInboxAndMeasurements");
    }

    private static TelemetryDbContext CreateContext()
    {
        DbContextOptions<TelemetryDbContext> options = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseNpgsql("Host=localhost;Database=pulseguard;Username=pulseguard")
            .Options;
        return new TelemetryDbContext(options);
    }
}
