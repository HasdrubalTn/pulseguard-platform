# Postman scenarios

Importer la collection REST/FHIR et `environments/PulseGuard.local.postman_environment.json`, puis sélectionner **PulseGuard Local** dans le menu d'environnement de Postman. Coller le secret généré par `eng/initialize-development.ps1` dans la valeur courante de `clientSecret`.

Les URL locales sont également définies au niveau de la collection pour éviter les erreurs DNS lorsqu'aucun environnement n'est sélectionné. Les scripts pré-requête interrompent les appels et affichent une consigne explicite lorsqu'un secret, un token ou un identifiant de patient manque.

Démarrer Identity et Patient Registry, puis exécuter les requêtes dans l'ordre.

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
