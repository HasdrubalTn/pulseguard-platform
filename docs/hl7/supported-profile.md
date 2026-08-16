# Profil HL7 v2 initial

Le gateway accepte des messages ER7 HL7 `2.x` dans un frame MLLP :

```text
0x0B + HL7 payload + 0x1C + 0x0D
```

## Messages du laboratoire

| Message | Usage synthétique | Traitement initial |
|---|---|---|
| `ADT^A01` | Admission/enregistrement d'un patient | Validation de l'enveloppe et Integration Event |
| `ORU^R01` | Observation ou constante vitale | Validation de l'enveloppe et Integration Event |

Champs MSH requis :

- `MSH-9` : message type ;
- `MSH-10` : message control ID unique ;
- `MSH-12` : version commençant par `2.`.

Un ACK `AA` confirme l'acceptation applicative. Un ACK `AE` indique une erreur applicative. La sémantique de persistance fiable sera ajoutée avec Outbox/Inbox : un ACK ne doit pas être envoyé avant que le niveau de durabilité promis soit réellement atteint.

## Limites explicites

- aucune donnée clinique réelle ;
- UTF-8 utilisé pour ce laboratoire, alors qu'un profil réel doit négocier ou imposer l'encodage ;
- pas encore de répétitions, escaping complet, custom Z-segments ou validation de vocabulary ;
- pas de TLS natif sur le socket ; utiliser une terminaison mTLS ou un réseau protégé ;
- taille maximale par défaut : 1 MiB ; timeout de lecture : 30 secondes.
