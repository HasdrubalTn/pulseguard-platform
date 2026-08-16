using System.Collections.Concurrent;
using PulseGuard.PatientRegistry.Application;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.Infrastructure;

public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly ConcurrentDictionary<PatientId, Patient> _patients = new();
    private readonly ConcurrentDictionary<string, PatientId> _patientIdsByMedicalRecordNumber = new(StringComparer.Ordinal);

    public ValueTask<bool> TryAddAsync(Patient patient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patient);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_patientIdsByMedicalRecordNumber.TryAdd(patient.MedicalRecordNumber, patient.Id))
        {
            return ValueTask.FromResult(false);
        }

        if (_patients.TryAdd(patient.Id, patient))
        {
            return ValueTask.FromResult(true);
        }

        // Roll back the secondary index if the primary insertion unexpectedly fails.
        _patientIdsByMedicalRecordNumber.TryRemove(patient.MedicalRecordNumber, out _);
        return ValueTask.FromResult(false);
    }

    public ValueTask<Patient?> GetAsync(PatientId patientId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _patients.TryGetValue(patientId, out Patient? patient);
        return ValueTask.FromResult(patient);
    }
}
