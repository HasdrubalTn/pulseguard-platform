# ADR-0001 — Utiliser une Clean Architecture évolutive

- Statut : Accepted
- Date : 2026-08-16

## Contexte

La plateforme doit enseigner DDD, CQRS, Events et distributed systems sans démarrer comme un microservice zoo. Un découpage prématuré imposerait des transactions distribuées, de l'observabilité réseau et une exploitation complexe avant que les boundaries soient comprises.

## Décision

Séparer les `Bounded Contexts` logiquement avec Clean Architecture, tout en gardant peu de processus déployables au début. Extraire un contexte uniquement lorsqu'une force mesurable le justifie : autonomie d'équipe, profil de charge, isolation de panne, sécurité ou rythme de livraison.

Les Building Blocks sont de petits packages NuGet techniques. Ils ne contiennent aucun concept métier médical.

## Conséquences

- les boundaries peuvent être testées avant d'être distribuées ;
- les transactions restent locales tant que possible ;
- l'extraction ultérieure demande des contrats explicites ;
- la discipline modulaire reste nécessaire, même dans un même dépôt.
