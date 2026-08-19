# User Stories — Epic 4: Access Control

**Version:** V1 — User Management & Containerisation
**Status:** Ready

---

## US-022 — Organizer access to event management

**As an organizer, I can access all existing event management features so that my working scope is preserved without regression.**

- All existing event management routes accessible with organizer role
- No regression on existing POC features

---

## US-023 — Admin and super admin access boundaries

**As an admin or super admin, I cannot access event management features, and I can access all administration and system maintenance endpoints, so that role boundaries are strictly enforced.**

- Any request to event management routes with admin or super admin role → 403
- Technical endpoints (including POST /admin/search/reindex) accessible to admin and super admin only → 403 for any other role

---

## US-024 — Organizer access boundaries

**As an organizer, I cannot access the administration interface or any system maintenance endpoint so that account management and technical operations are restricted to authorized roles.**

- Any request to /admin/* routes with organizer role → 403
