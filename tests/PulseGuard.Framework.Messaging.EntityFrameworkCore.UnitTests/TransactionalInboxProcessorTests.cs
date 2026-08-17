using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore.UnitTests;

public sealed class TransactionalInboxProcessorTests
{
    private readonly Fixture _fixture = new();

    public TransactionalInboxProcessorTests()
    {
        _fixture.Customize(new AutoNSubstituteCustomization());
    }

    [Fact]
    public async Task ProcessAsyncCommitsSideEffectOnceAndSuppressesIdenticalDuplicate()
    {
        Guid messageId = _fixture.Create<Guid>();
        byte[] content = _fixture.CreateMany<byte>(32).ToArray();
        (TransactionalInboxProcessor<TestDbContext> sut, IDbContextFactory<TestDbContext> factory) = CreateSut();
        int handlerCalls = 0;

        Task Handler(TestDbContext dbContext, CancellationToken _)
        {
            handlerCalls++;
            dbContext.SideEffects.Add(new TestSideEffect(messageId));
            return Task.CompletedTask;
        }

        InboxProcessingResult first = await sut.ProcessAsync(
            messageId,
            "TestEvent.v1",
            content,
            Handler,
            CancellationToken.None);
        InboxProcessingResult duplicate = await sut.ProcessAsync(
            messageId,
            "TestEvent.v1",
            content,
            Handler,
            CancellationToken.None);

        first.Should().Be(InboxProcessingResult.Processed);
        duplicate.Should().Be(InboxProcessingResult.Duplicate);
        handlerCalls.Should().Be(1);
        await using TestDbContext verification = await factory.CreateDbContextAsync();
        (await verification.SideEffects.CountAsync()).Should().Be(1);
        InboxMessage inbox = await verification.Set<InboxMessage>().SingleAsync();
        inbox.ProcessedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessAsyncRejectsMessageIdReusedWithDifferentContent()
    {
        Guid messageId = _fixture.Create<Guid>();
        (TransactionalInboxProcessor<TestDbContext> sut, _) = CreateSut();
        byte[] originalContent = [1, 2, 3];
        byte[] conflictingContent = [3, 2, 1];
        static Task NoSideEffect(TestDbContext _, CancellationToken __) => Task.CompletedTask;
        await sut.ProcessAsync(
            messageId,
            "TestEvent.v1",
            originalContent,
            NoSideEffect,
            CancellationToken.None);

        Func<Task> act = () => sut.ProcessAsync(
            messageId,
            "TestEvent.v1",
            conflictingContent,
            NoSideEffect,
            CancellationToken.None);

        await act.Should().ThrowAsync<InboxMessageConflictException>()
            .Where(exception => exception.MessageId == messageId);
    }

    [Fact]
    public async Task ProcessAsyncDoesNotPersistInboxWhenHandlerFails()
    {
        Guid messageId = _fixture.Create<Guid>();
        byte[] content = _fixture.CreateMany<byte>(16).ToArray();
        (TransactionalInboxProcessor<TestDbContext> sut, IDbContextFactory<TestDbContext> factory) = CreateSut();

        Func<Task> act = () => sut.ProcessAsync(
            messageId,
            "TestEvent.v1",
            content,
            (dbContext, _) =>
            {
                dbContext.SideEffects.Add(new TestSideEffect(messageId));
                throw new InvalidOperationException("Synthetic handler failure.");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await using TestDbContext verification = await factory.CreateDbContextAsync();
        (await verification.Set<InboxMessage>().CountAsync()).Should().Be(0);
        (await verification.SideEffects.CountAsync()).Should().Be(0);
    }

    private (TransactionalInboxProcessor<TestDbContext>, IDbContextFactory<TestDbContext>) CreateSut()
    {
        string databaseName = _fixture.Create<Guid>().ToString("N");
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        IDbContextFactory<TestDbContext> factory = Substitute.For<IDbContextFactory<TestDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(new TestDbContext(options)));
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_fixture.Create<DateTimeOffset>());
        ILogger<TransactionalInboxProcessor<TestDbContext>> logger =
            Substitute.For<ILogger<TransactionalInboxProcessor<TestDbContext>>>();
        return (new TransactionalInboxProcessor<TestDbContext>(factory, timeProvider, logger), factory);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestSideEffect> SideEffects => Set<TestSideEffect>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestSideEffect>().HasKey(sideEffect => sideEffect.Id);
            modelBuilder.AddTransactionalInbox("test");
        }
    }

    private sealed class TestSideEffect
    {
        private TestSideEffect()
        {
        }

        public TestSideEffect(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
    }
}
