using Microsoft.EntityFrameworkCore;
using Npgsql;
using PulseGuard.PatientRegistry.Application;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.Infrastructure;

public sealed class PostgresPatientRepository(PatientRegistryDbContext dbContext) : IPatientRepository
{
    public async ValueTask<bool> TryAddAsync(Patient patient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patient);

        await dbContext.Patients.AddAsync(patient, cancellationToken).ConfigureAwait(false);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            dbContext.Entry(patient).State = EntityState.Detached;
            return false;
        }
    }

    public async ValueTask<Patient?> GetAsync(PatientId patientId, CancellationToken cancellationToken) =>
        await dbContext.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(patient => patient.Id == patientId, cancellationToken)
            .ConfigureAwait(false);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_patients_medical_record_number",
        };
}
