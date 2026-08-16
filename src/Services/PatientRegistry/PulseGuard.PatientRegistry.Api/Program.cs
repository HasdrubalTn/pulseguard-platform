using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PulseGuard.Framework.Cqrs;
using PulseGuard.Framework.Security;
using PulseGuard.PatientRegistry.Application;
using PulseGuard.PatientRegistry.Contracts;
using PulseGuard.PatientRegistry.Domain;
using PulseGuard.PatientRegistry.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddPulseGuardOidc(builder.Configuration, "pulseguard.patient.read", "pulseguard.patient.write");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
builder.Services.AddScoped<ICommandHandler<RegisterPatientCommand, RegisterPatientResult>, RegisterPatientHandler>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health/live").AllowAnonymous();

RouteGroupBuilder api = app.MapGroup("/api/patients").WithTags("Patients");

api.MapPost("/", RegisterPatientAsync)
    .RequireAuthorization("pulseguard.patient.write")
    .Produces<PatientResponse>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status409Conflict)
    .ProducesValidationProblem();

api.MapGet("/{patientId:guid}", GetPatientAsync)
    .RequireAuthorization("pulseguard.patient.read")
    .Produces<PatientResponse>()
    .Produces(StatusCodes.Status404NotFound);

app.MapGet("/fhir/Patient/{patientId:guid}", GetFhirPatientAsync)
    .WithTags("FHIR R4")
    .RequireAuthorization("pulseguard.patient.read")
    .Produces<FhirPatientResource>(StatusCodes.Status200OK, "application/fhir+json")
    .Produces(StatusCodes.Status404NotFound);

app.Run();

static async Task<Results<Created<PatientResponse>, Conflict<ProblemDetails>, ValidationProblem>> RegisterPatientAsync(
    RegisterPatientRequest request,
    ICommandHandler<RegisterPatientCommand, RegisterPatientResult> handler,
    CancellationToken cancellationToken)
{
    try
    {
        RegisterPatientResult result = await handler.HandleAsync(
            new RegisterPatientCommand(
                request.MedicalRecordNumber,
                request.GivenName,
                request.FamilyName,
                request.BirthDate),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Patient already exists",
                Detail = "A patient with this medical record number is already registered.",
                Status = StatusCodes.Status409Conflict,
            });
        }

        PatientResponse response = ToResponse(result.Patient!);
        return TypedResults.Created($"/api/patients/{response.Id:D}", response);
    }
    catch (ArgumentException exception)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["patient"] = [exception.Message],
        });
    }
}

static async Task<Results<Ok<PatientResponse>, NotFound>> GetPatientAsync(
    Guid patientId,
    IPatientRepository repository,
    CancellationToken cancellationToken)
{
    Patient? patient = await repository.GetAsync(new PatientId(patientId), cancellationToken);
    return patient is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(PatientView.From(patient)));
}

static async Task<Results<JsonHttpResult<FhirPatientResource>, NotFound>> GetFhirPatientAsync(
    Guid patientId,
    IPatientRepository repository,
    CancellationToken cancellationToken)
{
    Patient? patient = await repository.GetAsync(new PatientId(patientId), cancellationToken);
    if (patient is null)
    {
        return TypedResults.NotFound();
    }

    FhirPatientResource resource = new(
        "Patient",
        patient.Id.ToString(),
        [new FhirIdentifier("urn:pulseguard:medical-record-number", patient.MedicalRecordNumber)],
        [new FhirHumanName("official", patient.FamilyName, [patient.GivenName])],
        patient.BirthDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        true);

    return TypedResults.Json(resource, contentType: "application/fhir+json");
}

static PatientResponse ToResponse(PatientView patient) => new(
    patient.Id.Value,
    patient.MedicalRecordNumber,
    patient.GivenName,
    patient.FamilyName,
    patient.BirthDate,
    patient.RegisteredOnUtc);

public partial class Program;
