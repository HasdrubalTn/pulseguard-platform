using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PulseGuard.PatientRegistry.Infrastructure.Migrations;

[DbContext(typeof(PatientRegistryDbContext))]
[Migration("20260816190000_InitialPatientRegistry")]
public sealed partial class InitialPatientRegistry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "patient_registry");

        migrationBuilder.CreateTable(
            name: "patients",
            schema: "patient_registry",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                medical_record_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                given_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                family_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                registered_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_patients", candidate => candidate.id);
            });

        migrationBuilder.CreateIndex(
            name: "ux_patients_medical_record_number",
            schema: "patient_registry",
            table: "patients",
            column: "medical_record_number",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "patients", schema: "patient_registry");
    }
}
