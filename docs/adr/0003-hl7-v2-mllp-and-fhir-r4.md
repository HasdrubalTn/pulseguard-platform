# ADR-0003 — Supporter HL7 v2/MLLP et FHIR R4 derrière une ACL

- Statut : Accepted
- Date : 2026-08-16

## Contexte

Les systèmes hospitaliers existants utilisent fréquemment HL7 v2 avec l'encapsulation MLLP, tandis que les intégrations modernes utilisent FHIR. Ces modèles ne doivent pas contaminer le Domain PulseGuard.

## Décision

- accepter des messages HL7 v2.x encodés en ER7 et encadrés par MLLP ;
- commencer avec les enveloppes `ADT^A01` et `ORU^R01` synthétiques ;
- répondre avec des ACK `AA` ou `AE` ;
- exposer des ressources FHIR R4 via une projection dédiée ;
- isoler les mappings dans une Anti-Corruption Layer ;
- imposer limites de taille, timeout, cancellation et bind loopback par défaut.

Le parser du Building Block est volontairement minimal et pédagogique. Il ne prétend pas implémenter l'intégralité de HL7 v2. Chaque profil hospitalier réel exigerait des conformance profiles, une validation des terminologies, une gestion d'encodage et des tests de compatibilité dédiés.

## Conséquences

- les contrats externes peuvent évoluer sans modifier les aggregates ;
- les erreurs de framing sont rejetées tôt ;
- MLLP seul ne sécurise pas le transport ; TLS/mTLS et segmentation sont nécessaires hors laboratoire ;
- l'ajout d'une bibliothèque HL7 mature reste possible derrière le même port.
