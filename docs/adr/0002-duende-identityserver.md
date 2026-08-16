# ADR-0002 — Utiliser Duende IdentityServer comme Authorization Server

- Statut : Accepted
- Date : 2026-08-16

## Contexte

PulseGuard a besoin d'OIDC/OAuth 2.0, de scopes par API et, plus tard, de DPoP et mTLS. L'objectif est de rester dans l'écosystème .NET et de rendre les mécanismes de sécurité inspectables.

## Décision

Utiliser Duende IdentityServer 8 sur .NET 10. Le premier slice expose `client_credentials` pour Postman et les communications machine-to-machine. Les API valident l'issuer, l'audience et les scopes.

Le client de développement reçoit un secret aléatoire stocké avec `.NET User Secrets`. `AddDeveloperSigningCredential` est strictement limité au développement. Une évolution ajoutera ASP.NET Core Identity pour les utilisateurs interactifs et un stockage persistant des clients, grants et clés.

## Conséquences

- aucune dépendance à Keycloak ;
- intégration native avec ASP.NET Core et OpenTelemetry ;
- une stratégie de licence Duende doit être validée avant un usage commercial ;
- la production doit fournir une clé de licence, des signing keys persistantes protégées et une rotation contrôlée.
