using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using NSubstitute;
using PulseGuard.PatientRegistry.Application;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.UnitTests;

public sealed class RegisterPatientHandlerTests
{
    private readonly Fixture _fixture = new();

    public RegisterPatientHandlerTests()
    {
        _fixture.Customize(new AutoNSubstituteCustomization());
    }

    [Fact]
    public async Task HandleAsyncWhenMedicalRecordNumberIsAvailableReturnsRegisteredPatient()
    {
        IPatientRepository repository = _fixture.Freeze<IPatientRepository>();
        repository
            .TryAddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>())
            .Returns(true);

        RegisterPatientHandler sut = new(repository, TimeProvider.System);
        RegisterPatientCommand command = _fixture
            .Build<RegisterPatientCommand>()
            .With(candidate => candidate.BirthDate, new DateOnly(1980, 1, 1))
            .Create();

        RegisterPatientResult result = await sut.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Patient.Should().NotBeNull();
        await repository.Received(1).TryAddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenMedicalRecordNumberAlreadyExistsReturnsConflictResult()
    {
        IPatientRepository repository = _fixture.Freeze<IPatientRepository>();
        repository
            .TryAddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>())
            .Returns(false);

        RegisterPatientHandler sut = new(repository, TimeProvider.System);
        RegisterPatientCommand command = _fixture
            .Build<RegisterPatientCommand>()
            .With(candidate => candidate.BirthDate, new DateOnly(1980, 1, 1))
            .Create();

        RegisterPatientResult result = await sut.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(RegisterPatientError.DuplicateMedicalRecordNumber);
        result.Patient.Should().BeNull();
    }
}
