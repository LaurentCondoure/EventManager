# TECH-001 — Cross-Cutting / Rate Limiting / back

**Type:** Technical task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Rate Limiting
**Layer:** `back`
**Parent story:** none — standalone cross-cutting technical initiative
**Priority:** `medium`
**Status:** `to do`

---

> **Placement rule:** Raised by task-002-auth-ratelimit-back (epic-1-authentication) — No overall rate limiting strategy have been made yet. It appears that the same rate limiting policy is shared across multiple API endpoint.
The concern is those endpoint, while they can have the same limit, don't have the same usage (admin, business features, security atc atc).

---

## Purpose

Rate limiting today exists as a one single "fixed" policy.
Regarding the needs of [task-002-auth-ratelimit-back](../authentication/v1-user-management/epic-1-authentication/task/task-002-auth-ratelimit-back.md) and architectural decision of [ADR-019](../../../architecture/adr/security/adr-019-rate-limiting-auth-endpoints.md), some constraints have to be adressed :
- each API endpoint can have their own rate limiting configuration
- one rate limiting configuration can contain one or many rules.
- each rule have their own limitation (quantity / duration)
- rules can access the content of the request

This task makes that mechanism possible — a centralized, framework-based rate limiting capability
satisfying the four constraints above. Deciding which endpoints need rate limiting, at what
threshold, and with which partition key is a separate work item, once the mechanism exists to
express it.

**Architectural reference:** [ADR-008](../../../architecture/adr/api/adr-008-rate-limiter-algorithm.md), [ADR-019](../../../architecture/adr/security/adr-019-rate-limiting-auth-endpoints.md)
---

## Description

- Provide a centralized rate limiting mechanism built on ASP.NET Core's native rate limiting
  primitives (`System.Threading.RateLimiting`) — no reimplemented counting logic, no third-party
  package, in-memory counter storage for V1.
- Each API endpoint must be able to declare its own rate limiting configuration, independent of
  every other endpoint's.
- A configuration must support one or many rules, all enforced simultaneously — a request must
  satisfy every rule declared for that endpoint to proceed.
- Each rule defines its own quantity and duration (fixed window), independent of the other rules
  declared in the same configuration.
- A rule must be able to partition its counter by a value read from the request — at minimum the
  client IP, and optionally a named field read from the request body — without breaking the
  endpoint's own model binding of that body afterward.
- Configuration is declarative, not hardcoded per endpoint — adding a new rate-limited endpoint
  must not require new C# code.

---

## Acceptance Criteria

- [ ] An endpoint can be rate-limited independently of any other endpoint, via its own
      configuration
- [ ] A configuration can declare more than one rule, and all declared rules are enforced
      simultaneously on the same request
- [ ] Two rules in the same configuration can carry different quantity/duration values without
      affecting each other
- [ ] A rule can partition its counter by client IP
- [ ] A rule can partition its counter by a named field extracted from the JSON request body, and
      the endpoint's own model binding of that body still succeeds afterward
- [ ] Adding a new rate-limited endpoint requires only a configuration change, no new code
- [ ] Exceeding any rule returns `429 Too Many Requests` with a `Retry-After` header

---

## Implementation Notes

- No functional behavior change is required by this task unless the policy review surfaces a gap
  (e.g. an unprotected endpoint, or a threshold mismatch) — this is primarily a policy and
  documentation task; the generalized mechanism already exists in code.
