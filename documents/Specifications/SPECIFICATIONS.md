# Spécifications — Plateforme de Gestion d'Événements

**Date :** Mardi 15 avril 2026  
**Projet :** EventsAPI - Application de gestion d'événements culturels  
**Objectif :** Définir le périmètre fonctionnel et technique avant développement

---

## Vision Produit

Plateforme de gestion d'événements culturels permettant aux organisateurs de publier des événements (concerts, spectacles, expositions) et aux utilisateurs de les découvrir, réserver des places, et partager leur avis.

**Volumétrie cible :**
- 50-100 organisateurs actifs
- 1 000-5 000 utilisateurs spectateurs
- 500-1 000 événements par an
- 5 000-10 000 commentaires par an

**Contexte :** Plateforme régionale/locale (échelle : région Île-de-France ou équivalent), pas une plateforme nationale type Fnac Spectacles.

**Objectif technique :** Démonstration d'architecture scalable applicable à volumétries plus élevées, mais dimensionnée pour un usage réaliste MVP (pas de sur-engineering).

---

## Personas

### Persona 1 : Marie, 35 ans — Organisatrice d'événements

**Profil :**
- Responsable communication dans une association culturelle
- Organise 10-15 événements par an (concerts, expositions, spectacles)
- Utilise actuellement des outils disparates (Excel, emails, réseaux sociaux)

**Besoins :**
- Publier rapidement des événements avec toutes les informations nécessaires
- Gérer les réservations et suivre le taux de remplissage
- Voir les statistiques de fréquentation
- Modérer les commentaires des spectateurs
- Interface simple et rapide à utiliser

**Frustrations actuelles :**
- Perte de temps à gérer plusieurs outils différents
- Difficulté à suivre les réservations en temps réel
- Pas de retour d'expérience des spectateurs

**Citation :**
> "J'ai besoin d'un outil centralisé qui me permette de publier un événement en quelques minutes et de suivre les réservations sans avoir à jongler entre 5 applications différentes."

---

### Persona 2 : Thomas, 28 ans — Spectateur régulier

**Profil :**
- Amateur de concerts et spectacles (2-3 sorties culturelles par mois)
- Utilise son smartphone pour découvrir et réserver des événements
- Aime partager ses avis et consulter les recommandations

**Besoins :**
- Découvrir facilement les événements à venir dans sa région
- Rechercher des événements par mot-clé, catégorie, ou date
- Réserver des places en ligne rapidement
- Laisser un avis après avoir assisté à un événement
- Consulter les avis d'autres spectateurs pour l'aider à choisir

**Frustrations actuelles :**
- Difficile de trouver tous les événements au même endroit
- Pas toujours d'avis disponibles pour décider
- Processus de réservation parfois compliqué

**Citation :**
> "Je veux pouvoir trouver rapidement ce qui m'intéresse ce week-end, voir ce que les autres en ont pensé, et réserver en 2 clics."

---

## User Stories MVP (Must Have)

### Epic 1 : Gestion des événements

**US1.1 — Créer un événement**
- **En tant qu'** organisateur
- **Je veux** créer un événement avec titre, description, date, lieu, capacité, prix, et catégorie
- **Afin de** le publier sur la plateforme et permettre aux utilisateurs de le découvrir

**Critères d'acceptation :**
- Formulaire avec tous les champs obligatoires
- Validation des données (date future, capacité > 0, prix >= 0)
- Message de confirmation après création
- Événement visible immédiatement dans la liste

**Justification technique :** CRUD de base avec SQL Server + Dapper

---

**US1.4 — Consulter la liste des événements à venir**
- **En tant qu'** utilisateur
- **Je veux** consulter la liste des événements à venir
- **Afin de** planifier mes sorties culturelles

**Critères d'acceptation :**
- Liste paginée (20 événements par page)
- Affichage : titre, date, lieu, prix, catégorie
- Tri par date croissante (événements les plus proches en premier)
- Uniquement événements futurs (date >= aujourd'hui)
- Bouton "Charger plus" pour pagination

**Justification technique :** Lecture SQL Server avec pagination

---

**US1.5 — Consulter le détail d'un événement**
- **En tant qu'** utilisateur
- **Je veux** consulter le détail complet d'un événement
- **Afin de** décider si je souhaite réserver

**Critères d'acceptation :**
- Affichage de toutes les informations (titre, description complète, date, lieu, capacité, prix, catégorie)
- Bouton "Réserver" (pour future implémentation)
- Section commentaires visible
- Design clair et lisible

**Justification technique :** Lecture détails SQL Server

---

### Epic 2 : Recherche d'événements

**US2.1 — Rechercher des événements par mot-clé**
- **En tant qu'** utilisateur
- **Je veux** rechercher des événements en tapant un mot-clé
- **Afin de** trouver rapidement ce qui m'intéresse

**Critères d'acceptation :**
- Barre de recherche visible et accessible
- Recherche full-text sur titre, description, et catégorie
- Résultats triés par pertinence puis par date
- Affichage "Aucun résultat" si recherche infructueuse
- Boost du titre (résultats avec mot-clé dans titre mieux classés)

**Justification technique :** Elasticsearch recherche full-text

---

### Epic 4 : Avis et commentaires

**US4.1 — Laisser un commentaire et une note**
- **En tant qu'** utilisateur
- **Je veux** laisser un commentaire et une note (1-5 étoiles) après un événement
- **Afin de** partager mon expérience avec d'autres spectateurs

**Critères d'acceptation :**
- Formulaire avec : nom utilisateur, note 1-5 (obligatoire), texte commentaire (optionnel)
- Validation note entre 1 et 5
- Texte commentaire max 1000 caractères
- Affichage immédiat du commentaire après soumission
- Message de confirmation

**Justification technique :** MongoDB pour données semi-structurées (commentaires)

---

**US4.2 — Consulter les avis d'autres spectateurs**
- **En tant qu'** utilisateur
- **Je veux** consulter les avis d'autres spectateurs
- **Afin de** m'aider à choisir un événement

**Critères d'acceptation :**
- Liste des commentaires sous le détail événement
- Affichage : nom utilisateur, note (étoiles), texte, date
- Tri par date décroissante (plus récents en premier)
- Affichage "Aucun commentaire" si pas de commentaires

**Justification technique :** MongoDB lecture

---

## User Stories Hors MVP (Won't Have pour cette version)

**Volontairement exclus du périmètre pour limiter la complexité :**

- US1.2 : Modifier un événement
- US1.3 : Supprimer un événement
- US2.2 : Filtrer par catégorie
- US2.3 : Filtrer par date
- US3.1 : Réserver des places
- US3.2 : Annuler une réservation
- US3.3 : Voir liste des réservations
- US4.3 : Modérer les commentaires

**Raison :** Concentrer le développement sur les fonctionnalités démontrant les technologies clés (SQL Server, MongoDB, Redis, Elasticsearch)

---

## Modèle de Données

### SQL Server (Données structurées)

#### Table : Events

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | GUID | PRIMARY KEY | Identifiant unique |
| Title | VARCHAR(200) | NOT NULL | Titre événement |
| Description | TEXT | NOT NULL | Description complète |
| Date | DATETIME | NOT NULL | Date et heure événement |
| Location | VARCHAR(200) | NOT NULL | Lieu (ville, salle) |
| Capacity | INT | NOT NULL, > 0 | Capacité maximale |
| Price | DECIMAL(10,2) | NOT NULL, >= 0 | Prix entrée (€) |
| Category | VARCHAR(50) | NOT NULL | Catégorie événement |
| ArtistName | VARCHAR(200) | NULL | Nom artiste/troupe (optionnel) |
| CreatedAt | DATETIME | NOT NULL, DEFAULT GETUTCDATE() | Date création |
| UpdatedAt | DATETIME | NULL | Date dernière modification |

**Index :**
- `IX_Events_Date` sur colonne `Date` (requêtes fréquentes par date)
- `IX_Events_Category` sur colonne `Category` (filtres futurs)

**Note sur ArtistName :**
- Champ optionnel pour enrichir événements
- Approche simple sans table Artists séparée (évite complexité M:N)
- Indexé dans Elasticsearch pour recherche ("concert Coldplay")
- Évolution post-MVP : migration vers table Artists si besoin relations complexes

---

#### Table : Users

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | GUID | PRIMARY KEY | Identifiant unique |
| Email | VARCHAR(200) | NOT NULL, UNIQUE | Email utilisateur |
| Name | VARCHAR(100) | NOT NULL | Nom utilisateur |
| CreatedAt | DATETIME | NOT NULL, DEFAULT GETUTCDATE() | Date création |

**Note :** Table préparée pour future gestion utilisateurs (réservations, authentification)

---

### MongoDB (Données semi-structurées)

#### Collection : event_comments

```javascript
{
  _id: ObjectId,                    // Identifiant MongoDB
  eventId: GUID,                    // Référence vers Event (SQL Server)
  userId: GUID,                     // Référence vers User (SQL Server)
  userName: string,                 // Nom affiché (dénormalisé pour perf)
  text: string,                     // Texte commentaire (optionnel)
  rating: int,                      // Note 1-5 (obligatoire)
  createdAt: datetime               // Date création
}
```

**Index :**
- Index sur `eventId` (requêtes fréquentes : récupérer commentaires d'un événement)
- Index sur `createdAt` (tri par date décroissante)

**Justification MongoDB :**
- Données semi-structurées (texte libre, longueur variable)
- Pas de relations complexes nécessaires
- Évolutivité future facile (ajout champs : likes, réponses imbriquées, metadata)
- Pas besoin de transactions ACID strictes

---

### Choix Techniques : Clés Primaires

#### GUID vs INT Auto-incrémental

**Décision :** Utilisation de GUID (Uniqueidentifier) comme Primary Key pour tables Events et Users.

**Justification GUID :**

| Avantage | Description |
|----------|-------------|
| **Génération distribuée** | Génération côté application (.NET) sans requête DB → réduction round-trips |
| **Fusion données** | Facilite fusion bases de données multiples (pas de collision ID) |
| **Sécurité** | IDs non séquentiels → pas de prédiction possible (user ne peut deviner ID événement suivant) |
| **Architecture future** | Compatible microservices (chaque service génère ses IDs sans coordination) |
| **APIs publiques** | IDs opaques dans URLs : `/api/events/3fa85f64-...` vs `/api/events/1234` |

**Inconvénients GUID (acceptés) :**

| Inconvénient | Impact | Mitigation |
|--------------|--------|------------|
| Taille stockage | 16 bytes vs 4 bytes (INT) | Négligeable pour volumétrie MVP (< 10k rows) |
| Performance index | Fragmentation index (non séquentiel) | Utiliser NEWSEQUENTIALID() en SQL Server si nécessaire |
| Lisibilité | Moins lisible en logs/debug | Acceptable, outils permettent copier/coller |

**Alternative écartée : INT IDENTITY**

Cas d'usage INT préférable :
- Tables volumineuses (> 100M rows) où taille compte
- Système monolithique sans distribution prévue
- Jointures ultra-fréquentes (index INT légèrement plus rapide)

**Décision :** GUID adapté au contexte :
- Architecture évolutive (microservices possibles)
- Sécurité APIs publiques
- Pattern moderne .NET/Azure

**Implémentation :**
```csharp
// Génération côté application
var newEvent = new Event 
{ 
    Id = Guid.NewGuid(),  // Généré en .NET, pas en DB
    // ...
};
```

**Note SQL Server :** Utilisation de `UNIQUEIDENTIFIER` avec génération applicative plutôt que `NEWID()` (génération DB) pour contrôle total.

---

## Simplifications MVP

### Gestion utilisateurs simplifiée

**Décision :** Pas de création/authentification utilisateurs dans MVP.

**Justification :**

1. **Focus technique :** Démonstration architecture multi-technologies (SQL Server, MongoDB, Redis, Elasticsearch) prioritaire sur authentification complète.

2. **Complexité évitée :** Authentification = ajout :
   - Identity/JWT tokens
   - Gestion mots de passe (hash, salt)
   - Endpoints register/login/logout
   - Middleware authentification
   - Frontend : formulaires login, gestion tokens
   - Tests sécurité
   - **Durée estimée : +6-8 heures** (dépassement planning)

3. **Workaround MVP :**
   - Formulaire commentaire demande `userName` (champ texte libre)
   - Génération `userId` fictif côté frontend (Guid.NewGuid())
   - **Permet démonstration fonctionnalité sans overhead auth**

**Évolution post-MVP :**

Table Users déjà créée et prête pour :
- Ajout colonnes : PasswordHash, Salt, EmailConfirmed
- Relation 1:N → Reservations (quand implémentées)
- Relation 1:N → Comments (userId devient FK réelle)

**Pattern actuel (commentaires) :**
```json
{
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",  // Généré frontend
  "userName": "Thomas Martin",                       // Saisi utilisateur
  "text": "Excellent concert !",
  "rating": 5
}
```

**Note sécurité :** 
- ⚠️ Approche acceptable uniquement pour MVP/démo
- ❌ Production nécessiterait authentification complète
- Frontend pourrait spammer commentaires (pas de rate limiting user)

---

## Règles de Gestion

### RG1 : Création événement

**Validations obligatoires :**

| Champ | Règle | Message d'erreur |
|-------|-------|------------------|
| Title | Obligatoire, max 200 caractères | "Le titre est obligatoire et ne doit pas dépasser 200 caractères" |
| Description | Obligatoire, max 2000 caractères | "La description est obligatoire et ne doit pas dépasser 2000 caractères" |
| Date | Obligatoire, >= aujourd'hui | "La date de l'événement doit être aujourd'hui ou dans le futur" |
| Location | Obligatoire, max 200 caractères | "Le lieu est obligatoire" |
| Capacity | Obligatoire, > 0 | "La capacité doit être supérieure à 0" |
| Price | Obligatoire, >= 0 | "Le prix doit être 0 ou supérieur" |
| Category | Obligatoire, valeur parmi liste autorisée | "Catégorie invalide" |
| ArtistName | Optionnel, max 200 caractères si fourni | "Le nom de l'artiste ne doit pas dépasser 200 caractères" |

**Catégories autorisées :**
- Concert
- Théâtre
- Exposition
- Conférence
- Spectacle
- Autre

**Comportement :**
- Si validation échoue → retour HTTP 400 Bad Request avec détails erreurs
- Si validation réussie → création événement, retour HTTP 201 Created avec événement créé

---

### RG2 : Commentaires

**Validations obligatoires :**

| Champ | Règle | Message d'erreur |
|-------|-------|------------------|
| UserName | Obligatoire, max 100 caractères | "Le nom est obligatoire" |
| Rating | Obligatoire, entre 1 et 5 inclus | "La note doit être entre 1 et 5" |
| Text | Optionnel, max 1000 caractères si fourni | "Le commentaire ne doit pas dépasser 1000 caractères" |

**Comportement :**
- Commentaire enregistré avec `createdAt` = date/heure actuelle
- Génération automatique `_id` (ObjectId MongoDB)
- Affichage immédiat dans liste commentaires

---

### RG3 : Recherche

**Comportement recherche full-text :**

1. **Champs recherchés :**
   - Title (boost ×2 — priorité maximale)
   - Description (boost ×1)
   - Category (boost ×1)

2. **Tri des résultats :**
   - 1er critère : Score de pertinence (descendant)
   - 2ème critère : Date événement (ascendant — plus proches en premier)

3. **Pagination :**
   - 20 résultats par page par défaut
   - Query param `page` pour naviguer

4. **Gestion "Aucun résultat" :**
   - Retour HTTP 200 OK avec tableau vide `[]`
   - Frontend affiche message "Aucun résultat trouvé"

---

## Choix Techniques Justifiés

### SQL Server — Données structurées événements

**Pourquoi SQL Server :**
- Données fortement structurées (schéma fixe : Event, User)
- Relations futures prévisibles (Event ↔ Reservation ↔ User)
- Transactions ACID nécessaires pour réservations futures
- Requêtes complexes (jointures, agrégations statistiques)
- Intégrité référentielle garantie

**Alternative écartée :** NoSQL document-oriented (MongoDB) — trop peu structuré pour données événements

---

### MongoDB — Commentaires semi-structurés

**Pourquoi MongoDB :**
- Données semi-structurées (texte libre, longueur variable)
- Schéma flexible (évolution facile : ajout likes, réponses imbriquées, metadata)
- Pas de relations complexes (lien simple eventId → Event)
- Performance lecture (dénormalisation userName évite jointure)
- Pas besoin transactions ACID strictes (commentaire = opération atomique isolée)

**Alternative écartée :** SQL Server — rigidité du schéma, over-engineering pour commentaires simples

---

### Redis — Cache applicatif

**Pourquoi Redis :**
- Cache résultats recherche Elasticsearch (requêtes coûteuses)
- Cache liste événements (requête fréquente, données peu changeantes)
- TTL configuré (10 minutes) → fraîcheur acceptable
- Invalidation ciblée lors modifications (create/update/delete événement)
- Performance : réduction latence + charge SQL Server

**Pattern :** Cache-aside (lecture : check cache → miss → DB → store cache)

**Alternative écartée : Memcached**

| Critère | Redis | Memcached |
|---------|-------|-----------|
| Structures de données | Riches (strings, lists, sets, hashes) | Simples (key-value strings) |
| Persistance | Optionnelle (snapshot, AOF) | Non (RAM uniquement) |
| Éviction | Politiques configurables (LRU, LFU) | LRU uniquement |
| Invalidation patterns | Patterns complexes possibles (pub/sub) | Invalidation basique |
| Cas d'usage | Cache + pub/sub + queues | Cache pur |

**Décision :** Redis choisi pour :
1. Structures de données riches (utiles pour évolutions futures : compteurs, listes)
2. Patterns d'invalidation flexibles (invalidation par pattern "events:page:*")
3. Écosystème .NET mature (StackExchange.Redis)
4. Compétence transférable (Redis plus demandé que Memcached sur marché)

**Note :** Pour volumétrie MVP, les deux feraient l'affaire techniquement.

**Alternative écartée 2 : Pas de cache**

Sans cache :
- Chaque requête GET /api/events → hit SQL Server
- Latence : 50-100ms par requête
- Charge DB inutile pour données peu changeantes
- Coût infrastructure Azure plus élevé (scaling DB vs. scaling cache)

---

### Elasticsearch — Recherche full-text

**Pourquoi Elasticsearch :**
- Recherche full-text performante multi-champs (Title + Description + Category)
- Boost configuré (Title ×2) pour pertinence
- Tokenisation, lemmatisation automatique (recherche "concert" trouve "concerts")
- Scoring pertinence (résultats mieux classés)
- Performance : recherche quasi instantanée même avec volumétrie élevée

**Alternative écartée :** SQL Server LIKE '%keyword%' — performance faible, pas de scoring pertinence

---

### Varnish — Cache HTTP

**Pourquoi Varnish :**
- Cache réponses HTTP complètes (GET /api/events)
- Transparent pour API (pas de modification code)
- Réduit hits API (serveur allégé)
- Configuration via VCL (règles cache fines)
- Complémentaire à Redis (2 niveaux cache : HTTP + applicatif)

**Pattern :** Cache HTTP avec TTL 5 minutes

**Alternative écartée :** Uniquement Redis — cache HTTP offre gain supplémentaire pour endpoints lecture intensive

---

## Endpoints API MVP

### GET /api/events

**Description :** Récupérer la liste paginée des événements à venir

**Paramètres Query :**
- `page` (int, optionnel, défaut=1) : Numéro de page
- `pageSize` (int, optionnel, défaut=20, max=50) : Nombre d'événements par page

**Réponse Success (200 OK) :**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Concert Jazz au Sunset",
    "description": "Soirée jazz avec quartet exceptionnel...",
    "date": "2026-05-15T20:00:00Z",
    "location": "Sunset Jazz Club, Paris",
    "capacity": 150,
    "price": 25.00,
    "category": "Concert",
    "artistName": "Miles Quartet",
    "createdAt": "2026-04-15T10:30:00Z",
    "updatedAt": null
  }
]
```

**Headers Cache :**
- `Cache-Control: public, max-age=300` (cache 5 minutes)

---

### GET /api/events/{id}

**Description :** Récupérer le détail d'un événement

**Paramètres Path :**
- `id` (GUID, requis) : Identifiant événement

**Réponse Success (200 OK) :**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Concert Jazz au Sunset",
  "description": "Soirée jazz avec quartet exceptionnel...",
  "date": "2026-05-15T20:00:00Z",
  "location": "Sunset Jazz Club, Paris",
  "capacity": 150,
  "price": 25.00,
  "category": "Concert",
  "artistName": "Miles Quartet",
  "createdAt": "2026-04-15T10:30:00Z",
  "updatedAt": null
}
```

**Réponse Erreur (404 Not Found) :**
```json
{
  "error": "Event not found"
}
```

---

### POST /api/events

**Description :** Créer un nouvel événement

**Corps Requête :**
```json
{
  "title": "Concert Jazz au Sunset",
  "description": "Soirée jazz avec quartet exceptionnel...",
  "date": "2026-05-15T20:00:00Z",
  "location": "Sunset Jazz Club, Paris",
  "capacity": 150,
  "price": 25.00,
  "category": "Concert",
  "artistName": "Miles Quartet"
}
```

**Réponse Success (201 Created) :**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Concert Jazz au Sunset",
  "description": "Soirée jazz avec quartet exceptionnel...",
  "date": "2026-05-15T20:00:00Z",
  "location": "Sunset Jazz Club, Paris",
  "capacity": 150,
  "price": 25.00,
  "category": "Concert",
  "artistName": "Miles Quartet",
  "createdAt": "2026-04-15T10:30:00Z",
  "updatedAt": null
}
```

**Header :**
- `Location: /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6`

**Réponse Erreur Validation (400 Bad Request) :**
```json
{
  "errors": {
    "Date": ["La date de l'événement doit être aujourd'hui ou dans le futur"],
    "Capacity": ["La capacité doit être supérieure à 0"]
  }
}
```

---

### GET /api/events/search?q={query}

**Description :** Rechercher événements par mot-clé

**Paramètres Query :**
- `q` (string, requis) : Mot-clé recherche
- `page` (int, optionnel, défaut=1) : Numéro de page
- `pageSize` (int, optionnel, défaut=20, max=50) : Résultats par page

**Note :** La recherche s'effectue sur Title, Description, Category, **et ArtistName**

**Réponse Success (200 OK) :**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Concert Jazz au Sunset",
    "description": "Soirée jazz avec quartet exceptionnel...",
    "date": "2026-05-15T20:00:00Z",
    "location": "Sunset Jazz Club, Paris",
    "capacity": 150,
    "price": 25.00,
    "category": "Concert",
    "artistName": "Miles Quartet",
    "createdAt": "2026-04-15T10:30:00Z",
    "updatedAt": null
  }
]
```

**Si aucun résultat :**
```json
[]
```

---

### GET /api/events/stats/by-category

**Description :** Récupérer statistiques événements par catégorie (pour graphique)

**Réponse Success (200 OK) :**
```json
[
  {
    "category": "Concert",
    "count": 15
  },
  {
    "category": "Théâtre",
    "count": 8
  },
  {
    "category": "Exposition",
    "count": 12
  },
  {
    "category": "Conférence",
    "count": 5
  },
  {
    "category": "Spectacle",
    "count": 6
  },
  {
    "category": "Autre",
    "count": 3
  }
]
```

**Utilisation :** Affichage graphique camembert dans frontend (Chart.js)

---

### POST /api/events/{eventId}/comments

**Description :** Ajouter un commentaire à un événement

**Paramètres Path :**
- `eventId` (GUID, requis) : Identifiant événement

**Corps Requête :**
```json
{
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "userName": "Thomas Martin",
  "text": "Excellente soirée, ambiance chaleureuse !",
  "rating": 5
}
```

**Réponse Success (201 Created) :**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "userName": "Thomas Martin",
  "text": "Excellente soirée, ambiance chaleureuse !",
  "rating": 5,
  "createdAt": "2026-05-16T22:30:00Z"
}
```

**Réponse Erreur Validation (400 Bad Request) :**
```json
{
  "errors": {
    "Rating": ["La note doit être entre 1 et 5"],
    "Text": ["Le commentaire ne doit pas dépasser 1000 caractères"]
  }
}
```

---

### GET /api/events/{eventId}/comments

**Description :** Récupérer les commentaires d'un événement

**Paramètres Path :**
- `eventId` (GUID, requis) : Identifiant événement

**Réponse Success (200 OK) :**
```json
[
  {
    "id": "507f1f77bcf86cd799439011",
    "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "userName": "Thomas Martin",
    "text": "Excellente soirée, ambiance chaleureuse !",
    "rating": 5,
    "createdAt": "2026-05-16T22:30:00Z"
  },
  {
    "id": "507f1f77bcf86cd799439012",
    "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "8d0f7680-8536-51ef-c15g-f18gd2g01bf8",
    "userName": "Sophie Dubois",
    "text": "Bon concert mais salle trop petite",
    "rating": 4,
    "createdAt": "2026-05-16T23:00:00Z"
  }
]
```

**Si aucun commentaire :**
```json
[]
```

---

## Démonstration Invalidation Cache

### User Stories démontrant le cache

**US1.1 — Créer un événement**
- Action : POST /api/events
- Impact cache :
  1. Insertion SQL Server
  2. Indexation Elasticsearch (avec ArtistName)
  3. **Invalidation cache liste événements (pattern "events:page:*")**
  4. Varnish cache HTTP invalidé (TTL expiré naturellement)
- **Démo :** 
  - GET /api/events (cache MISS, résultat sans nouvel événement)
  - POST /api/events (création)
  - GET /api/events (cache MISS, résultat AVEC nouvel événement)

**US1.4 — Consulter liste événements**
- Action : GET /api/events
- Impact cache :
  1. Check Redis clé "events:page:1:size:20"
  2. Si MISS → Query SQL Server → Store Redis (TTL 10min)
  3. Si HIT → Return cached data (pas de SQL)
- **Démo :**
  - GET /api/events (1ère fois → logs "Cache MISS")
  - GET /api/events (2ème fois → logs "Cache HIT")
  - Vérifier latence réduite (MISS: 50ms, HIT: 5ms)

**US1.5 — Consulter détail événement**
- Action : GET /api/events/{id}
- Impact cache :
  1. Check Redis clé "event:{id}"
  2. Si MISS → Query SQL Server → Store Redis (TTL 10min)
  3. Si HIT → Return cached data
- **Démo :** Identique à US1.4

---

### Scénario de démonstration complet (pour entretiens)

**Scénario A : Cache applicatif (Redis)**

```
1. GET /api/events 
   → MISS (logs: "Querying SQL Server")
   → Latence: 50ms

2. GET /api/events 
   → HIT (logs: "Returning from Redis cache")
   → Latence: 5ms

3. POST /api/events (nouvel événement "Concert Coldplay")
   → Invalidation cache liste

4. GET /api/events 
   → MISS (logs: "Cache invalidated, querying SQL Server")
   → Résultat: "Concert Coldplay" présent
   → Latence: 50ms

5. GET /api/events 
   → HIT (nouvel événement en cache)
   → Latence: 5ms
```

**Scénario B : Cache HTTP (Varnish)**

```
1. GET http://localhost:8080/api/events 
   → Header: X-Cache: MISS
   → Latence: 50ms

2. GET http://localhost:8080/api/events 
   → Header: X-Cache: HIT
   → Latence: 2ms (Varnish direct, API pas appelée)

3. Attendre 5 minutes (TTL Varnish expiré)

4. GET http://localhost:8080/api/events 
   → Header: X-Cache: MISS
   → Latence: 50ms (si Redis HIT) ou 50ms (si Redis MISS aussi)
```

**Scénario C : Double niveau cache (démonstration architecture complète)**

```
Request 1: GET /api/events (via Varnish)
→ Varnish MISS → API appelée
→ Redis MISS → SQL Server query
→ Flux: SQL → Redis (store) → Varnish (store) → Client
→ Latence: 50ms

Request 2: GET /api/events (via Varnish, même page)
→ Varnish HIT → Résultat direct
→ Flux: Varnish → Client (API pas appelée)
→ Latence: 2ms

Request 3: POST /api/events (création "Concert Miles Davis", bypass Varnish)
→ API direct port 5000
→ SQL insert + Elasticsearch index + Redis invalidation
→ Cache Varnish reste valide (TTL pas expiré)

Request 4: GET /api/events (via Varnish, après 5min)
→ Varnish MISS (TTL expiré)
→ Redis MISS (invalidé par POST) → SQL Server query
→ Résultat: "Concert Miles Davis" présent
→ Flux: SQL → Redis (store) → Varnish (store) → Client
→ Latence: 50ms

Request 5: GET /api/events (via Varnish)
→ Varnish HIT → nouveau résultat en cache
→ Latence: 2ms
```

---

## Architecture Technique (Vue d'ensemble)

```
┌─────────────────┐
│   Client Web    │
│   (Vue.js 3)    │
└────────┬────────┘
         │ HTTP
         ▼
┌─────────────────┐
│    Varnish      │  ← Cache HTTP (5 min TTL)
│  (Cache HTTP)   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   API .NET 8    │
│  (ASP.NET Core) │
└────────┬────────┘
         │
    ┌────┴────┬──────────┬────────────┐
    ▼         ▼          ▼            ▼
┌───────┐ ┌──────┐  ┌─────────┐  ┌──────────────┐
│ Redis │ │ SQL  │  │ MongoDB │  │ Elasticsearch│
│(Cache)│ │Server│  │(Comments)  │  (Search)    │
└───────┘ └──────┘  └─────────┘  └──────────────┘
```

**Flux de données principaux :**

1. **GET /api/events**
   - Client → Varnish (check cache) → API → Redis (check cache) → SQL Server
   - Retour : SQL Server → Redis (store) → API → Varnish (store) → Client

2. **POST /api/events**
   - Client → API → SQL Server (insert) → Elasticsearch (index) → Invalidation cache Redis
   - Retour : API → Client

3. **GET /api/events/search?q=jazz**
   - Client → API → Redis (check cache) → Elasticsearch (search) → Redis (store)
   - Retour : API → Client

4. **POST /api/events/{id}/comments**
   - Client → API → MongoDB (insert)
   - Retour : API → Client

---

## Critères de Succès MVP

**L'application MVP sera considérée comme réussie si :**

1. ✅ Les 6 User Stories MVP sont implémentées et fonctionnelles
2. ✅ Toutes les règles de gestion sont respectées (validations)
3. ✅ Les 7 endpoints API répondent correctement (codes HTTP appropriés)
4. ✅ L'architecture multi-technologies fonctionne (SQL Server + MongoDB + Redis + Elasticsearch + Varnish)
5. ✅ Les tests couvrent au minimum 80% du code
6. ✅ L'application peut être déployée localement via `docker-compose up`
7. ✅ La documentation est complète (ARCHITECTURE.md, README.md)

**Bonus (si temps disponible) :**
- ✅ Graphique statistiques par catégorie (Chart.js)
- ✅ Champ ArtistName recherchable dans Elasticsearch

---

## User Stories Bonus (Nice to Have)

### US6.1 — Visualiser statistiques événements

**En tant qu'** organisateur ou utilisateur  
**Je veux** voir un graphique de répartition des événements par catégorie  
**Afin de** visualiser rapidement les tendances

**Critères d'acceptation :**
- Graphique camembert (Chart.js)
- Données : nombre d'événements par catégorie
- Endpoint API : GET /api/events/stats/by-category
- Affichage dans page dédiée /statistics

**Impact technique :**

Jour 2 (Mercredi après-midi) :
- Endpoint backend : +20 minutes
- SQL : `SELECT Category, COUNT(*) as Count FROM Events WHERE Date >= GETUTCDATE() GROUP BY Category`

Jour 4 (Vendredi après-midi) :
- npm install chart.js vue-chartjs
- Créer StatisticsView.vue
- Route /statistics
- Durée : +45 minutes

**TOTAL : +1h05**

---

## Prochaines Étapes (Après Spécification)

**Mardi après-midi (13h-18h) :**
1. Setup projet .NET 8 (Clean Architecture)
2. Création entités Domain
3. Configuration SQL Server + Docker
4. Implémentation premier repository

**Planning complet :** Voir `plan_formation_claude_code.md`

---

**Document validé le :** Mardi 15 avril 2026, 12h00  
**Prochaine révision :** Après implémentation MVP (samedi 19 avril 2026)
