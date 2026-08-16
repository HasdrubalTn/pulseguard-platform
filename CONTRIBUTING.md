# Contribuer à PulseGuard

## Workflow Git

1. Synchroniser `develop`.
2. Créer une branche courte : `feature/<topic>`, `fix/<topic>` ou `docs/<topic>`.
3. Écrire des commits atomiques au format Conventional Commits.
4. Ouvrir une Pull Request vers `develop`.
5. Exiger une CI verte et une revue avant le merge.
6. Utiliser `squash merge` avec un titre sémantique.

`main` représente les releases stables. Les hotfixes partent de `main`, reviennent dans `main`, puis sont réintégrés dans `develop`.

## Format des commits

```text
type(scope)!: lowercase imperative subject
```

Types autorisés : `feat`, `fix`, `docs`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.

Un changement cassant ajoute `!` et un footer :

```text
feat(contracts)!: version telemetry ingestion contract

BREAKING CHANGE: clients must populate measurement_id.
```

## Definition of Done

- le comportement est couvert au bon niveau de test ;
- les boundaries Clean Architecture restent respectées ;
- les contrats publics sont versionnés et documentés ;
- les erreurs, timeouts, retries et cancellations sont traités explicitement ;
- aucun secret ni aucune donnée patient réelle n'est commité ;
- logs, metrics et traces ne contiennent pas de données cliniques brutes ;
- un ADR accompagne toute décision structurante ou difficile à inverser ;
- `npm run verify` réussit.

## Code review

La revue cherche en priorité les invariants violés, les erreurs de concurrence, les effets de bord, les dépendances orientées dans le mauvais sens, les contrats ambigus et les risques opérationnels. Elle ne se limite pas au style, déjà automatisé.
