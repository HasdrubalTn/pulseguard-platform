using PulseGuard.Framework.Cqrs;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.Application;

public sealed record RegisterPatientCommand(
    string MedicalRecordNumber,
    string GivenName,
    string FamilyName,
    DateOnly BirthDate) : ICommand<RegisterPatientResult>;

public sealed record RegisterPatientResult(PatientView? Patient, RegisterPatientError Error)
{
    public bool IsSuccess => Error == RegisterPatientError.None;

    public static RegisterPatientResult Success(Patient patient) => new(PatientView.From(patient), RegisterPatientError.None);

    public static RegisterPatientResult Duplicate() => new(null, RegisterPatientError.DuplicateMedicalRecordNumber);
}

public enum RegisterPatientError
{
    None = 0,
    DuplicateMedicalRecordNumber = 1,
}

public sealed record PatientView(
    PatientId Id,
    string MedicalRecordNumber,
    string GivenName,
    string FamilyName,
    DateOnly BirthDate,
    DateTimeOffset RegisteredOnUtc)
{
    public static PatientView From(Patient patient) => new(
        patient.Id,
        patient.MedicalRecordNumber,
        patient.GivenName,
        patient.FamilyName,
        patient.BirthDate,
        patient.RegisteredOnUtc);
}

public sealed class RegisterPatientHandler(IPatientRepository repository, TimeProvider timeProvider)
    : ICommandHandler<RegisterPatientCommand, RegisterPatientResult>
{
    public async ValueTask<RegisterPatientResult> HandleAsync(
        RegisterPatientCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Patient patient = Patient.Register(
            command.MedicalRecordNumber,
            command.GivenName,
            command.FamilyName,
            command.BirthDate,
            timeProvider);

        bool added = await repository.TryAddAsync(patient, cancellationToken).ConfigureAwait(false);
        return added ? RegisterPatientResult.Success(patient) : RegisterPatientResult.Duplicate();
    }
}
