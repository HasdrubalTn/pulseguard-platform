# Postman scenarios

Importer la collection REST/FHIR et l'environnement local, démarrer Identity et Patient Registry, puis exécuter les requêtes dans l'ordre.

Pour gRPC, importer directement `contracts/protobuf/telemetry.proto` dans Postman et appeler :

```text
grpcs://localhost:5201/pulseguard.telemetry.v1.TelemetryIngestor/Ingest
```

Ajouter le metadata :

```text
Authorization: Bearer <access token with pulseguard.telemetry.write>
```

Le protocole MLLP n'est pas HTTP et n'est donc pas simulé par Postman. Démarrer le Device Gateway, puis utiliser :

```powershell
dotnet run --project src/Simulators/PulseGuard.DeviceSimulator -- 127.0.0.1 2575
```
