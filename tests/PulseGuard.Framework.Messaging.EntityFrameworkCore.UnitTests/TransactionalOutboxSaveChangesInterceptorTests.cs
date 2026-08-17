using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PulseGuard.Framework.Domain;
using PulseGuard.Framework.Messaging;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore.UnitTests;

public sealed class TransactionalOutboxSaveChangesInterceptorTests
{
    private readonly Fixture _fixture = new();

    public TransactionalOutboxSaveChangesInterceptorTests()
    {
        _fixture.Customize(new AutoNSubstituteCustomization());
    }

    [Fact]
    public async Task SaveChangesAsyncWritesMappedEventToOutboxAndClearsCommittedDomainEvent()
    {
        TestDomainEvent domainEvent = _fixture.Create<TestDomainEvent>();
        TestIntegrationEvent integrationEvent = _fixture.Create<TestIntegrationEvent>();
        TestAggregate aggregate = new(_fixture.Create<Guid>(), domainEvent);
        IDomainEventMapper mapper = Substitute.For<IDomainEventMapper>();
        IIntegrationEventSerializer serializer = Substitute.For<IIntegrationEventSerializer>();
        mapper.Map(domainEvent).Returns(integrationEvent);
        serializer.Serialize(integrationEvent).Returns("{\"source\":\"unit-test\"}");

        TransactionalOutboxSaveChangesInterceptor interceptor = new([mapper], serializer);
        await using TestDbContext sut = CreateContext(interceptor);
        sut.Aggregates.Add(aggregate);

        await sut.SaveChangesAsync();

        OutboxMessage storedMessage = await sut.Set<OutboxMessage>().AsNoTracking().SingleAsync();
        storedMessage.Id.Should().Be(integrationEvent.EventId);
        storedMessage.EventType.Should().Be(nameof(TestIntegrationEvent));
        storedMessage.SchemaVersion.Should().Be(integrationEvent.SchemaVersion);
        storedMessage.Payload.Should().Be("{\"source\":\"unit-test\"}");
        storedMessage.PublishedOnUtc.Should().BeNull();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ModelUsesMessageIdAsInboxIdempotencyKey()
    {
        TransactionalOutboxSaveChangesInterceptor interceptor = new(
            [],
            Substitute.For<IIntegrationEventSerializer>());
        using TestDbContext sut = CreateContext(interceptor);

        string[] keyProperties = sut.Model.FindEntityType(typeof(InboxMessage))!
            .FindPrimaryKey()!
            .Properties
            .Select(property => property.Name)
            .ToArray();

        keyProperties.Should().Equal(nameof(InboxMessage.Id));
    }

    [Fact]
    public async Task FailedSaveDetachesUncommittedOutboxMessageAndRetainsDomainEvent()
    {
        string databaseName = _fixture.Create<Guid>().ToString("N");
        Guid aggregateId = _fixture.Create<Guid>();
        await using (TestDbContext seedContext = CreateContext(null, databaseName))
        {
            seedContext.Aggregates.Add(new TestAggregate(aggregateId, _fixture.Create<TestDomainEvent>()));
            await seedContext.SaveChangesAsync();
        }

        TestDomainEvent domainEvent = _fixture.Create<TestDomainEvent>();
        TestIntegrationEvent integrationEvent = _fixture.Create<TestIntegrationEvent>();
        TestAggregate duplicateAggregate = new(aggregateId, domainEvent);
        IDomainEventMapper mapper = Substitute.For<IDomainEventMapper>();
        IIntegrationEventSerializer serializer = Substitute.For<IIntegrationEventSerializer>();
        mapper.Map(domainEvent).Returns(integrationEvent);
        serializer.Serialize(integrationEvent).Returns("{}");
        TransactionalOutboxSaveChangesInterceptor interceptor = new([mapper], serializer);
        await using TestDbContext sut = CreateContext(interceptor, databaseName);
        sut.Aggregates.Add(duplicateAggregate);

        Func<Task> act = () => sut.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>();
        duplicateAggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(domainEvent);
        sut.Set<OutboxMessage>().Local.Should().BeEmpty();
    }

    private TestDbContext CreateContext(
        TransactionalOutboxSaveChangesInterceptor? interceptor,
        string? databaseName = null)
    {
        DbContextOptionsBuilder<TestDbContext> builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName ?? _fixture.Create<Guid>().ToString("N"));
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new TestDbContext(builder.Options);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>(builder =>
            {
                builder.HasKey(aggregate => aggregate.Id);
                builder.Ignore(aggregate => aggregate.DomainEvents);
            });
            modelBuilder.AddTransactionalMessaging("test");
        }
    }

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        private TestAggregate()
            : base(Guid.Empty)
        {
        }

        public TestAggregate(Guid id, IDomainEvent domainEvent)
            : base(id)
        {
            Raise(domainEvent);
        }
    }

    private sealed record TestDomainEvent(Guid EventId, DateTimeOffset OccurredOnUtc) : IDomainEvent;

    private sealed record TestIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredOnUtc,
        int SchemaVersion,
        string Source) : IntegrationEvent(EventId, OccurredOnUtc, SchemaVersion);
}
