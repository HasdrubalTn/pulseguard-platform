using System.Reflection;
using FluentAssertions;
using PulseGuard.PatientRegistry.Application;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.ArchitectureTests;

public sealed class CleanArchitectureDependencyTests
{
    [Fact]
    public void Domain_DoesNotReferenceOuterLayers()
    {
        Assembly domainAssembly = typeof(Patient).Assembly;

        string[] forbiddenReferences = GetPulseGuardReferences(domainAssembly)
            .Where(reference => reference.Contains(".Application", StringComparison.Ordinal) ||
                                reference.Contains(".Infrastructure", StringComparison.Ordinal) ||
                                reference.EndsWith(".Api", StringComparison.Ordinal))
            .ToArray();

        forbiddenReferences.Should().BeEmpty();
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrApi()
    {
        Assembly applicationAssembly = typeof(RegisterPatientCommand).Assembly;

        string[] forbiddenReferences = GetPulseGuardReferences(applicationAssembly)
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
