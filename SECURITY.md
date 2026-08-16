# Security Policy

PulseGuard is an educational platform that must only process synthetic data.

Do not open a public issue for a suspected vulnerability. Report it privately to the repository owner through GitHub Security Advisories when the remote repository is available.

Never commit:

- real patient or practitioner data;
- Duende license keys;
- client secrets, signing keys, certificates, or connection strings containing credentials;
- captured HL7, FHIR, HTTP, gRPC, or socket traffic from a real environment.

The development client secret is generated locally and stored with .NET User Secrets. The developer signing credential is a local-only bootstrap mechanism. Production deployments must use managed secrets, persistent protected signing keys, TLS, network segmentation, audit logging, and an approved data-hosting and regulatory model.
