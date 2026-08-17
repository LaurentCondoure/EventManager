# Scoping Note — V1 : Gestion des utilisateurs & containerisation

**Status:** Draft
**Depends on:** POC EventManager fonctionnel (non mis en production)

## Objectif
Le POC EventManager permet déjà de gérer des événements, mais n'a pas
de notion d'utilisateur : n'importe qui peut accéder à l'application.
Cette version pose les fondations indispensables à la mise en production :
contrôler qui accède à l'application, gérer les comptes de manière
sécurisée, et faire tourner le tout dans un environnement maîtrisé
et reproductible.

## In scope

### Gestion des utilisateurs
- Connexion / déconnexion pour tous les rôles
- Protection de toutes les fonctionnalités derrière l'authentification

### Rôle Super Admin
- Plusieurs super admins possibles
- Premier super admin provisionné au démarrage de l'application
- Créer, modifier, désactiver un compte admin
- Forcer la redéfinition du mot de passe d'un admin
- Promouvoir un admin au rang de super admin

### Rôle Admin
- Créer, modifier, désactiver un compte organisateur
- Forcer la redéfinition du mot de passe d'un organisateur

### Rôle Organisateur
- Accès aux fonctionnalités existantes de gestion d'événements
- Aucun accès à l'interface d'administration

### Interface d'administration
- Section dédiée dans l'application existante
- Accessible aux admins et super admins uniquement
- Les admins et super admins n'ont pas accès aux fonctionnalités
  de gestion d'événements

## Out of scope
- L'auto-inscription (aucun utilisateur ne peut créer son propre compte)
- La double authentification (2FA)
- La réinitialisation de mot de passe en autonomie par l'utilisateur
- Les autres rôles futurs (spectateur, producteur, venue manager)
- Tout ce qui concerne les autres applications du futur écosystème

## Open decisions
- Aucune

## Acceptance criteria
- Un visiteur non connecté ne peut accéder à aucune fonctionnalité
- Un super admin peut créer, modifier et désactiver un compte admin
- Un super admin peut forcer la redéfinition du mot de passe d'un admin
- Un super admin peut promouvoir un admin en super admin
- Un admin peut créer, modifier et désactiver un compte organisateur
- Un admin peut forcer la redéfinition du mot de passe d'un organisateur
- Un organisateur authentifié accède aux fonctionnalités de gestion
  d'événements sans régression
- Un admin ou super admin authentifié n'a pas accès aux fonctionnalités
  de gestion d'événements
- L'application et sa base de données se lancent avec une commande
  unique en local via des containers

## Impact sur les versions existantes
- Le POC existant est modifié : toutes ses routes sont désormais
  protégées derrière l'authentification

---

## Definition of Ready — V1 : Gestion des utilisateurs & containerisation

> Format léger appliqué : périmètre clair, aucune décision ouverte, critères d'acceptance tous testables.

### Cadrage fonctionnel

| Critère | ✓ |
|---|---|
| L'objectif de la version est formulé en une phrase claire | ✅ |
| Le périmètre "In scope" est explicitement listé | ✅ |
| Le périmètre "Out of scope" est explicitement listé | ✅ |
| Les critères d'acceptance sont définis et testables | ✅ |
| Les décisions ouvertes bloquantes sont toutes résolues | ✅ |
| La dépendance sur la version précédente est explicite | ✅ |

> Toutes les cases sont cochées. Le POC est déclaré comme base de travail. Aucune ambiguïté fonctionnelle identifiée.

### Architecture

| Critère | ✓ |
|---|---|
| Le DAT est mis à jour pour cette version | ☐ |
| Les nouveaux choix techniques sont couverts par un ADR | ☐ |
| Les implications ISO dev/prod ont été vérifiées | ☐ |

> À compléter lors de la session CTO. Cette version introduit deux sujets qui nécessitent des ADR : le mécanisme d'authentification et la stratégie de containerisation.

### Environnement

| Critère | ✓ |
|---|---|
| L'environnement de développement est opérationnel | ☐ |
| Les dépendances techniques nécessaires sont disponibles | ☐ |
| La Definition of Done de la version est rédigée | ☐ |

> À compléter lors de la session Tech Lead, après validation de l'architecture.

### Décision de démarrage

```
☐ Go — toutes les cases sont cochées, le développement peut commencer
☐ No-go — case(s) non cochée(s) : Architecture (3 critères) / Environnement (3 critères)
```

> **Statut actuel : No-go.**
> Le cadrage fonctionnel est validé. 
> La note de cadrage peut être marquée "Validated" dès que les deux blocs
> restants sont complétés.
