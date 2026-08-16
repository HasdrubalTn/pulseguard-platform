using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.Application;

public interface IPatientRepository
{
    ValueTask<bool> TryAddAsync(Patient patient, CancellationToken cancellationToken);

    ValueTask<Patient?> GetAsync(PatientId patientId, CancellationToken cancellationToken);
}
