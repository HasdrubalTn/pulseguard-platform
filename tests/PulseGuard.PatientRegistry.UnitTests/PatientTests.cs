using FluentAssertions;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.UnitTests;

public sealed class PatientTests
{
    [Fact]
    public void Register_WithValidData_NormalizesMedicalRecordNumberAndRaisesEvent()
    {
        Patient patient = Patient.Register(
            " mrn-00042 ",
            "Jane",
            "Doe",
            new DateOnly(1980, 1, 1),
            TimeProvider.System);

        patient.MedicalRecordNumber.Should().Be("MRN-00042");
        patient.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PatientRegisteredDomainEvent>();
    }

    [Fact]
    public void Register_WithFutureBirthDate_Throws()
    {
        DateOnly tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        Action act = () => Patient.Register("MRN-1", "Jane", "Doe", tomorrow, TimeProvider.System);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
