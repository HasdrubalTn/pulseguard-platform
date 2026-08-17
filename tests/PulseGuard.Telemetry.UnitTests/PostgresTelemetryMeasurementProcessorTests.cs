using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.Telemetry.Application;
using PulseGuard.Telemetry.Domain;
using PulseGuard.Telemetry.Infrastructure;

namespace PulseGuard.Telemetry.UnitTests;

public sealed class PostgresTelemetryMeasurementProcessorTests
{
    private readonly Fixture _fixture = new();

    public PostgresTelemetryMeasurementProcessorTests()
    {
        _fixture.Customize(new AutoNSubstituteCustomization());
    }

    [Fact]
    public async Task ProcessAsyncCommitsMeasurementAndSuppressesDuplicate()
    {
        TelemetryMeasurementInput input = CreateInput();
        (PostgresTelemetryMeasurementProcessor sut, IDbContextFactory<TelemetryDbContext> factory) = CreateSut();

        TelemetryIngestionOutcome first = await sut.ProcessAsync(input, CancellationToken.None);
        TelemetryIngestionOutcome duplicate = await sut.ProcessAsync(input, CancellationToken.None);

        first.Should().Be(TelemetryIngestionOutcome.Accepted);
        duplicate.Should().Be(TelemetryIngestionOutcome.Duplicate);
        await using TelemetryDbContext verification = await factory.CreateDbContextAsync();
        TelemetryMeasurement stored = await verification.Measurements.SingleAsync();
        stored.Id.Should().Be(input.MeasurementId);
        stored.PatientId.Should().Be(input.PatientId);
        stored.Value.Should().Be(input.Value);
        (await verification.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsyncRejectsMeasurementIdReusedWithDifferentValue()
    {
        TelemetryMeasurementInput input = CreateInput();
        (PostgresTelemetryMeasurementProcessor sut, _) = CreateSut();
        await sut.ProcessAsync(input, CancellationToken.None);
        TelemetryMeasurementInput conflicting = input with { Value = input.Value + 1 };

        Func<Task> act = () => sut.ProcessAsync(conflicting, CancellationToken.None);

        await act.Should().ThrowAsync<InboxMessageConflictException>()
            .Where(exception => exception.MessageId == input.MeasurementId);
    }

    private TelemetryMeasurementInput CreateInput() => new(
        _fixture.Create<Guid>(),
        _fixture.Create<Guid>(),
        $"device-{_fixture.Create<Guid>():N}",
        VitalMeasurementType.OxygenSaturation,
        98.5,
        "%",
        _fixture.Create<DateTimeOffset>());

    private (PostgresTelemetryMeasurementProcessor, IDbContextFactory<TelemetryDbContext>) CreateSut()
    {
        DbContextOptions<TelemetryDbContext> options = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase(_fixture.Create<Guid>().ToString("N"))
            .Options;
        IDbContextFactory<TelemetryDbContext> factory = Substitute.For<IDbContextFactory<TelemetryDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new TelemetryDbContext(options)));
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_fixture.Create<DateTimeOffset>());
        ILogger<TransactionalInboxProcessor<TelemetryDbContext>> logger =
            Substitute.For<ILogger<TransactionalInboxProcessor<TelemetryDbContext>>>();
        TransactionalInboxProcessor<TelemetryDbContext> inboxProcessor = new(factory, timeProvider, logger);
        return (new PostgresTelemetryMeasurementProcessor(inboxProcessor, timeProvider), factory);
    }
}
