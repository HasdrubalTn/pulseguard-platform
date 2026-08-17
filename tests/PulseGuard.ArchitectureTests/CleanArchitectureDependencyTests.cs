using System.Reflection;
using FluentAssertions;
using PulseGuard.PatientRegistry.Application;
using PulseGuard.PatientRegistry.Domain;
using PulseGuard.Telemetry.Application;
using PulseGuard.Telemetry.Domain;

namespace PulseGuard.ArchitectureTests;

public sealed class CleanArchitectureDependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterLayers()
    {
        Assembly[] domainAssemblies = [typeof(Patient).Assembly, typeof(TelemetryMeasurement).Assembly];

        string[] forbiddenReferences = domainAssemblies
            .SelectMany(GetPulseGuardReferences)
            .Where(reference => reference.Contains(".Application", StringComparison.Ordinal) ||
                                reference.Contains(".Infrastructure", StringComparison.Ordinal) ||
                                reference.EndsWith(".Api", StringComparison.Ordinal))
            .ToArray();

        forbiddenReferences.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        Assembly[] applicationAssemblies =
        [
            typeof(RegisterPatientCommand).Assembly,
            typeof(TelemetryMeasurementInput).Assembly,
        ];

        string[] forbiddenReferences = applicationAssemblies
            .SelectMany(GetPulseGuardReferences)
            .Where(reference => reference.Contains(".Infrastructure", StringComparison.Ordinal) ||
                                reference.EndsWith(".Api", StringComparison.Ordinal))
            .ToArray();

        forbiddenReferences.Should().BeEmpty();
    }

    private static IEnumerable<string> GetPulseGuardReferences(Assembly assembly) => assembly
        .GetReferencedAssemblies()
        .Select(reference => reference.Name)
        .OfType<string>()
        .Where(reference => reference.StartsWith("PulseGuard.", StringComparison.Ordinal));
}
