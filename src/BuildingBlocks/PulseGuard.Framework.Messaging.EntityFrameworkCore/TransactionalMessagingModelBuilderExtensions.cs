using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public static class TransactionalMessagingModelBuilderExtensions
{
    public static ModelBuilder AddTransactionalMessaging(
        this ModelBuilder modelBuilder,
        string schema,
        string? outboxPayloadColumnType = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        ConfigureOutbox(modelBuilder.Entity<OutboxMessage>(), schema, outboxPayloadColumnType);
        ConfigureInbox(modelBuilder.Entity<InboxMessage>(), schema);
        return modelBuilder;
    }

    private static void ConfigureOutbox(
        EntityTypeBuilder<OutboxMessage> builder,
        string schema,
        string? payloadColumnType)
    {
        builder.ToTable("outbox_messages", schema);
        builder.HasKey(message => message.Id).HasName("pk_outbox_messages");

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(message => message.OccurredOnUtc)
            .HasColumnName("occurred_on_utc")
            .IsRequired();
        builder.Property(message => message.SchemaVersion)
            .HasColumnName("schema_version")
            .IsRequired();
        builder.Property(message => message.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(256)
            .IsRequired();

        PropertyBuilder<string> payload = builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .IsRequired();
        if (!string.IsNullOrWhiteSpace(payloadColumnType))
        {
            payload.HasColumnType(payloadColumnType);
        }

        builder.Property(message => message.PublishedOnUtc)
            .HasColumnName("published_on_utc");
        builder.HasIndex(message => new { message.PublishedOnUtc, message.OccurredOnUtc })
            .HasDatabaseName("ix_outbox_messages_pending");
    }

    private static void ConfigureInbox(EntityTypeBuilder<InboxMessage> builder, string schema)
    {
        builder.ToTable("inbox_messages", schema);
        builder.HasKey(message => message.Id).HasName("pk_inbox_messages");

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(message => message.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(message => message.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();
        builder.Property(message => message.ReceivedOnUtc)
            .HasColumnName("received_on_utc")
            .IsRequired();
        builder.Property(message => message.ProcessedOnUtc)
            .HasColumnName("processed_on_utc");
        builder.HasIndex(message => new { message.ProcessedOnUtc, message.ReceivedOnUtc })
            .HasDatabaseName("ix_inbox_messages_pending");
    }
}
