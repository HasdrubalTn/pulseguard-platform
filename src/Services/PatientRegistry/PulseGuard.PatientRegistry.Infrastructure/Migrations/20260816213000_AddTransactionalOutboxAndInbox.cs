using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PulseGuard.PatientRegistry.Infrastructure.Migrations;

[DbContext(typeof(PatientRegistryDbContext))]
[Migration("20260816213000_AddTransactionalOutboxAndInbox")]
public sealed partial class AddTransactionalOutboxAndInbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "inbox_messages",
            schema: "patient_registry",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                content_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                received_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inbox_messages", candidate => candidate.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "patient_registry",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                occurred_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                schema_version = table.Column<int>(type: "integer", nullable: false),
                event_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                published_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_messages", candidate => candidate.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_inbox_messages_pending",
            schema: "patient_registry",
            table: "inbox_messages",
            columns: new[] { "processed_on_utc", "received_on_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_pending",
            schema: "patient_registry",
            table: "outbox_messages",
            columns: new[] { "published_on_utc", "occurred_on_utc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "inbox_messages", schema: "patient_registry");
        migrationBuilder.DropTable(name: "outbox_messages", schema: "patient_registry");
    }
}
