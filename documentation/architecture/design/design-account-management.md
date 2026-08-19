# Design — Account Management

**Reference:** design-account-management
**Status:** Validated
**Domain:** Cross-Cutting
**Version introduced:** V1
**Last updated:** 2026-08-18

**Architectural references:**
- [ADR-016 — Authorisation model](../adr/security/adr-016-authorisation-model.md)
- [ADR-017 — First super admin provisioning](../adr/security/adr-017-first-super-admin-provisioning.md)

**Related design documents:**
- [design-authentication.md](design-authentication.md) — session lifecycle, force reset flow

---

## 1. Use Case Diagram

Actors and account management use cases for V1.

```mermaid
graph TD
    subgraph Actors
        SA[Super Admin]
        AD[Admin]
        ORG[Organizer]
        SYS[System]
    end

    subgraph SA_Scope["Account Management: Super Admin"]
        UC01[Create admin account]
        UC02[Modify admin account]
        UC03[Deactivate admin account]
        UC04[Reactivate admin account]
        UC05[Force password reset - admin]
        UC06[Promote admin to super admin]
    end

    subgraph AD_Scope["Account Management: Admin"]
        UC07[Create organizer account]
        UC08[Modify organizer account]
        UC09[Deactivate organizer account]
        UC10[Reactivate organizer account]
        UC11[Force password reset - organizer]
    end

    subgraph Access_Control["Access Control"]
        UC12[View account list - own scope]
        UC13[Access administration section]
        UC14[Access event management - blocked]
    end

    subgraph Provisioning
        UC15[Provision first super admin at startup]
    end

    SA --> UC01
    SA --> UC02
    SA --> UC03
    SA --> UC04
    SA --> UC05
    SA --> UC06
    SA --> UC07
    SA --> UC08
    SA --> UC09
    SA --> UC10
    SA --> UC11
    SA --> UC12
    SA --> UC13
    AD --> UC07
    AD --> UC08
    AD --> UC09
    AD --> UC10
    AD --> UC11
    AD --> UC12
    AD --> UC13
    ORG --> UC14
    SYS --> UC15
```

---

## 2. Component Diagram

How components interact for account management in V1.

```mermaid
graph TD
    Browser["Client Browser"]
    SPA["Vue.js 3 SPA\n- Administration section\n- Account list view\n- Create / modify forms\n- Deactivate / reactivate action\n- Promote action\n- Force reset action"]
    Varnish["Varnish\n- All account management routes\npass through (no caching)"]
    API["ASP.NET Core API\n- Account management endpoints\n- RBAC policy enforcement\n- Rank hierarchy enforcement\n- Refresh token revocation"]
    Identity["Identity Store\n(SQL Server — EventManager_Identity)\n- ApplicationUser\n- Roles\n- Refresh tokens\n- Password history"]

    Browser -->|HTTP| Varnish
    Varnish -->|"pass-through:\nall /admin/* routes"| API
    SPA -->|"API calls via HTTP\nhttpOnly cookies\nmanaged by browser"| API
    API -->|"Identity read/write\n(EF Core)"| Identity
```

---

## 3. State Diagrams

### 3.1 Account status lifecycle

```mermaid
stateDiagram-v2
    [*] --> Active : Account created\n(by higher-rank user)\nmust_reset_password = true

    Active --> Active : Modified\n(first name, last name, email)

    Active --> PasswordResetPending : Force reset triggered\nby higher-rank user\nRefresh token revoked immediately

    PasswordResetPending --> Active : Password reset completed\nFlag cleared

    Active --> Inactive : Deactivated\nby higher-rank user\nRefresh token revoked immediately

    Inactive --> Active : Reactivated\nby higher-rank user

    Active --> SuperAdmin : Promoted to super admin\n(irreversible — admin only)

    SuperAdmin --> Inactive : Deactivated\nby another super admin
```

### 3.2 Rank hierarchy and permitted actions

```mermaid
graph LR
    SA["Super Admin\n---\nCan act on: Admin, Organizer\nCannot act on: other Super Admins"]
    AD["Admin\n---\nCan act on: Organizer\nCannot act on: Admin, Super Admin"]
    ORG["Organizer\n---\nNo account management capability"]

    ORG -->|"Promoted by Super Admin (irreversible)"| AD
    AD -->|"Promoted by Super Admin (irreversible)"| SA
```

---

## 4. Activity Diagrams

### 4.1 Create account flow (admin or organizer)

```mermaid
flowchart TD
    A([Caller opens creation form]) --> B[Fill: first name, last name,\nemail, temporary password]
    B --> C{Client-side\nvalidation passes?}
    C -- No --> D[Display inline errors]
    D --> B
    C -- Yes --> E{Password policy\ncompliant?}
    E -- No --> F[Display policy error]
    F --> B
    E -- Yes --> G[POST /admin/accounts/admins\nor /admin/accounts/organizers]
    G --> H{Email unique?}
    H -- No --> I[Return 400 — duplicate email]
    I --> J[Display duplicate email error]
    J --> B
    H -- Yes --> K[Create account\nIsActive = true\nmust_reset_password = true]
    K --> L[Return 201]
    L --> M[Display confirmation\nRefresh account list]
    M --> N([Done])
```

### 4.2 Modify account flow

```mermaid
flowchart TD
    A([Caller opens edit form]) --> B[Form pre-filled\nwith current values]
    B --> C[Edit: first name, last name,\nand/or email]
    C --> D{Client-side\nvalidation passes?}
    D -- No --> E[Display inline errors]
    E --> C
    D -- Yes --> F[PUT /admin/accounts/admins/id\nor /admin/accounts/organizers/id]
    F --> G{Email unique?}
    G -- No --> H[Return 400 — duplicate email]
    H --> I[Display duplicate email error]
    I --> C
    G -- Yes --> J[Save changes]
    J --> K[Return 200]
    K --> L[Display confirmation]
    L --> M([Done])
```

### 4.3 Deactivate / reactivate account flow

```mermaid
flowchart TD
    A([Caller triggers action]) --> B[PATCH /admin/accounts/id/status]
    B --> C{Caller rank\n> target rank?}
    C -- No --> D[Return 403]
    D --> E[Display access error]
    C -- Yes --> F{Action type?}
    F -- Deactivate --> G[Revoke target refresh token immediately]
    G --> H[Set IsActive = false]
    H --> I[Return 200]
    I --> J[Account visually marked inactive\nin account list]
    J --> K([Target loses access within 10 min\naccess token TTL])
    F -- Reactivate --> L[Set IsActive = true]
    L --> M[Return 200]
    M --> N[Account visually marked active\nin account list]
    N --> O([Target can log in again])
```

### 4.4 Promote to super admin flow

```mermaid
flowchart TD
    A([Super admin triggers promotion]) --> B[POST /admin/accounts/admins/id/promote]
    B --> C{Caller is\nsuper admin?}
    C -- No --> D[Return 403]
    C -- Yes --> E{Target is\nactive admin?}
    E -- No --> F[Return 400]
    E -- Yes --> G[Update role to super_admin]
    G --> H[Return 200]
    H --> I[Role updated in account list\nimmediately]
    I --> J([Promotion irreversible\nDeactivation is the only removal path])
```

### 4.5 Super admin provisioning at startup

```mermaid
flowchart TD
    A([API starts]) --> B[EF Core migrations complete]
    B --> C{Super admin\nalready exists?}
    C -- Yes --> D[Skip seed silently]
    D --> E([API serves requests])
    C -- No --> F[Read SEED_ADMIN_EMAIL\nand SEED_ADMIN_PASSWORD\nfrom environment]
    F --> G[Create super admin account\nIsActive = true\nmust_reset_password = true]
    G --> H([API serves requests])
```

---

## 5. Sequence Diagrams

### 5.1 Create account

```mermaid
sequenceDiagram
    actor Caller as Caller\n(Super Admin or Admin)
    participant SPA
    participant API
    participant Identity as Identity Store\n(SQL Server)

    Caller->>SPA: Fill creation form
    SPA->>SPA: Client-side validation
    SPA->>API: POST /admin/accounts/{role}s\n{firstName, lastName, email, password}
    API->>API: Enforce role policy
    alt Insufficient role
        API-->>SPA: 403
    else Sufficient role
        API->>Identity: Check email uniqueness
        alt Duplicate email
            API-->>SPA: 400 — duplicate email
            SPA-->>Caller: Display duplicate email error
        else Email unique
            API->>Identity: Create account\nIsActive=true\nmust_reset_password=true
            API-->>SPA: 201
            SPA-->>Caller: Confirmation + refreshed account list
        end
    end
```

### 5.2 Modify account

```mermaid
sequenceDiagram
    actor Caller as Caller\n(Super Admin or Admin)
    participant SPA
    participant API
    participant Identity as Identity Store\n(SQL Server)

    Caller->>SPA: Edit form — submit changes
    SPA->>API: PUT /admin/accounts/{role}s/{id}\n{firstName, lastName, email}
    API->>API: Enforce role policy
    alt Insufficient role
        API-->>SPA: 403
    else Sufficient role
        API->>Identity: Check email uniqueness
        alt Duplicate email
            API-->>SPA: 400 — duplicate email
            SPA-->>Caller: Display error
        else Email unique
            API->>Identity: Save changes
            API-->>SPA: 200
            SPA-->>Caller: Confirmation
        end
    end
```

### 5.3 Deactivate account

```mermaid
sequenceDiagram
    actor Caller as Caller\n(higher-rank user)
    participant SPA
    participant API
    participant Identity as Identity Store\n(SQL Server)

    Caller->>SPA: Trigger deactivation
    SPA->>API: PATCH /admin/accounts/{id}/status\n{active: false}
    API->>API: Verify caller rank > target rank
    alt Insufficient rank
        API-->>SPA: 403
    else Sufficient rank
        API->>Identity: Revoke refresh token immediately
        API->>Identity: Set IsActive = false
        API-->>SPA: 200
        SPA-->>Caller: Account marked inactive in list
    end
    Note over API,Identity: Target retains API access\nfor up to 10 min (access token TTL)\nAccepted limitation — ADR-014
```

### 5.4 Promote to super admin

```mermaid
sequenceDiagram
    actor SA as Super Admin
    participant SPA
    participant API
    participant Identity as Identity Store\n(SQL Server)

    SA->>SPA: Trigger promotion
    SPA->>API: POST /admin/accounts/admins/{id}/promote
    API->>API: Verify caller is super admin
    alt Not super admin
        API-->>SPA: 403
    else Super admin
        API->>Identity: Verify target is active admin
        alt Target not eligible
            API-->>SPA: 400
        else Target eligible
            API->>Identity: Update role to super_admin
            API-->>SPA: 200
            SPA-->>SA: Role updated in account list
        end
    end
```

### 5.5 Super admin provisioning at startup

```mermaid
sequenceDiagram
    participant API
    participant Identity as Identity Store\n(SQL Server)

    API->>Identity: Migrate() — EventManager
    API->>Identity: Migrate() — EventManager_Identity
    API->>Identity: Count super admin accounts
    alt No super admin exists
        API->>Identity: Create super admin\n(SEED_ADMIN_EMAIL + SEED_ADMIN_PASSWORD)\nIsActive=true, must_reset_password=true
        API-->>API: Seed complete
    else Super admin already exists
        API-->>API: Seed skipped silently
    end
    API-->>API: Serve requests
```

---

## 6. Interaction Overview Diagram

Full account management lifecycle — how the individual flows connect.

```mermaid
flowchart TD
    subgraph Startup
        A([System starts])
        B[Provisioning seed]
    end

    subgraph Account Creation
        C[Create account\nadmin or organizer]
        D[Account active\nmust_reset_password = true]
    end

    subgraph Account Lifecycle
        E{Account active?}
        F[Modify account]
        G[Force password reset]
        H[Deactivate account]
        I[Reactivate account]
        J[Promote to super admin]
    end

    subgraph Authentication Link
        K([Login — see design-authentication])
    end

    A --> B
    B --> D
    C --> D
    D --> E
    E -- Yes --> F
    E -- Yes --> G
    E -- Yes --> H
    E -- Yes --> J
    E -- No --> I
    F --> E
    G --> K
    H --> E
    I --> E
    J --> E
    D --> K
```

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-08-18 | Document created — V1 account management flows |
