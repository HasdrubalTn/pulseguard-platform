using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PulseGuard.Telemetry.Infrastructure.Migrations;

[DbContext(typeof(TelemetryDbContext))]
[Migration("20260817010000_AddTelemetryInboxAndMeasurements")]
public sealed partial class AddTelemetryInboxAndMeasurements : Migration
{
    private static readonly string[] PatientTimelineColumns = ["patient_id", "measured_at_utc"];
    private static readonly string[] PendingInboxColumns = ["processed_on_utc", "received_on_utc"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "telemetry");

        migrationBuilder.CreateTable(
            name: "inbox_messages",
            schema: "telemetry",
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
            name: "measurements",
            schema: "telemetry",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                device_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                vital_type = table.Column<int>(type: "integer", nullable: false),
                value = table.Column<double>(type: "double precision", nullable: false),
                unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                measured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                received_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_measurements", candidate => candidate.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_inbox_messages_pending",
            schema: "telemetry",
            table: "inbox_messages",
            columns: PendingInboxColumns);

        migrationBuilder.CreateIndex(
            name: "ix_measurements_patient_measured_at",
            schema: "telemetry",
            table: "measurements",
            columns: PatientTimelineColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "inbox_messages", schema: "telemetry");
        migrationBuilder.DropTable(name: "measurements", schema: "telemetry");
    }
}
