# Specifications — Events Management Platform

**Date:** 2026-04-15  
**Project:** EventsAPI — Cultural Events Management Application  
**Objective:** Define the functional scope before development

---

## Product Vision

A cultural events management platform enabling organizers to publish events (concerts, shows, exhibitions) and users to discover them, book seats, and share their reviews.

**Target volume:**
- 50–100 active organizers
- 1,000–5,000 spectator users
- 500–1,000 events per year
- 5,000–10,000 comments per year

**Context:** Regional/local platform (scope: Île-de-France region or equivalent) — not a national platform like Fnac Spectacles.

**Technical objective:** Demonstrate a scalable architecture applicable to higher volumes, but sized for realistic MVP usage (no over-engineering).

---

## Personas

### Persona 1: Marie, 35 — Event Organizer

**Profile:**
- Communications manager at a cultural association
- Organizes 10–15 events per year (concerts, exhibitions, shows)
- Currently uses disparate tools (Excel, emails, social media)

**Needs:**
- Quickly publish events with all necessary information
- Manage bookings and track occupancy rate
- View attendance statistics
- Moderate spectator comments
- Simple and fast interface

**Current frustrations:**
- Time lost managing multiple different tools
- Difficulty tracking bookings in real time
- No feedback from spectators

**Quote:**
> "I need a centralized tool that lets me publish an event in a few minutes and track bookings without juggling 5 different applications."

---

### Persona 2: Thomas, 28 — Regular Spectator

**Profile:**
- Enthusiast of concerts and shows (2–3 cultural outings per month)
- Uses smartphone to discover and book events
- Likes sharing reviews and consulting recommendations

**Needs:**
- Easily discover upcoming events in his area
- Search events by keyword, category, or date
- Book seats online quickly
- Leave a review after attending an event
- Read other spectators' reviews to help decide

**Current frustrations:**
- Hard to find all events in one place
- Reviews not always available to help decide
- Booking process sometimes complicated

**Quote:**
> "I want to quickly find what interests me this weekend, see what others thought, and book in 2 clicks."

---

## MVP User Stories (Must Have)

### Epic 1: Event Management

**US1.1 — Create an event**
- **As an** organizer
- **I want to** create an event with title, description, date, location, capacity, price, and category
- **So that** I can publish it on the platform and let users discover it

**Acceptance criteria:**
- Form with all required fields
- Data validation (future date, capacity > 0, price >= 0)
- Confirmation message after creation
- Event immediately visible in the list

---

**US1.4 — View list of upcoming events**
- **As a** user
- **I want to** view the list of upcoming events
- **So that** I can plan my cultural outings

**Acceptance criteria:**
- Paginated list (20 events per page)
- Display: title, date, location, price, category
- Sorted by ascending date (closest events first)
- Only future events (date >= today)
- "Load more" button for pagination

---

**US1.5 — View event details**
- **As a** user
- **I want to** view the complete details of an event
- **So that** I can decide whether to book

**Acceptance criteria:**
- Display all information (title, full description, date, location, capacity, price, category)
- "Book" button (for future implementation)
- Comments section visible
- Clear and readable design

---

### Epic 2: Event Search

**US2.1 — Search events by keyword**
- **As a** user
- **I want to** search for events by typing a keyword
- **So that** I can quickly find what interests me

**Acceptance criteria:**
- Search bar visible and accessible
- Full-text search on title, description, and category
- Results sorted by relevance then by date
- "No results" message if search returns nothing
- Title boost (results with keyword in title ranked higher)

---

### Epic 4: Reviews and Comments

**US4.1 — Leave a comment and rating**
- **As a** user
- **I want to** leave a comment and a rating (1–5 stars) after an event
- **So that** I can share my experience with other spectators

**Acceptance criteria:**
- Form with: username, rating 1–5 (required), comment text (optional)
- Rating validation between 1 and 5
- Comment text max 1000 characters
- Comment displayed immediately after submission
- Confirmation message

---

**US4.2 — View other spectators' reviews**
- **As a** user
- **I want to** view other spectators' reviews
- **So that** I can help decide which event to attend

**Acceptance criteria:**
- List of comments under event detail
- Display: username, rating (stars), text, date
- Sorted by descending date (most recent first)
- "No comments" message if no comments exist

---

## Out of Scope (Won't Have for this version)

Deliberately excluded to limit complexity:

- US1.2: Edit an event
- US1.3: Delete an event
- US2.2: Filter by category
- US2.3: Filter by date
- US3.1: Book seats
- US3.2: Cancel a booking
- US3.3: View booking list
- US4.3: Moderate comments

**Reason:** Focus development on features demonstrating the key technologies (SQL Server, MongoDB, Redis, Elasticsearch)

---

## Data Model

See [DATA_MODEL.md](../technical/DATA_MODEL.md)

---

## MVP Simplifications

### Simplified user management

**Decision:** No user creation or authentication in the MVP.

**Justification:**
- Technical focus: demonstrating multi-technology architecture is the priority
- Authentication adds an estimated +6–8 hours of work

**MVP workaround:**
- Comment form asks for `userName` (free text field)
- Fictitious `userId` generated client-side (`Guid.NewGuid()`)

**Post-MVP evolution:** Users table already created and ready for authentication columns and FK relations.

**Security note:**
- Acceptable only for MVP/demo
- Production would require full authentication

---

## Business Rules

### BR1: Event creation

| Field | Rule | Error message |
|-------|------|---------------|
| Title | Required, max 200 characters | "Title is required and must not exceed 200 characters" |
| Description | Required, max 2000 characters | "Description is required and must not exceed 2000 characters" |
| Date | Required, >= today | "Event date must be today or in the future" |
| Location | Required, max 200 characters | "Location is required" |
| Capacity | Required, > 0 | "Capacity must be greater than 0" |
| Price | Required, >= 0 | "Price must be 0 or greater" |
| Category | Required, value from allowed list | "Invalid category" |
| ArtistName | Optional, max 200 characters if provided | "Artist name must not exceed 200 characters" |

**Allowed categories:** Concert, Théâtre, Exposition, Conférence, Spectacle, Autre

**Behaviour:**
- Validation failure → HTTP 400 Bad Request with error details
- Validation success → event created, HTTP 201 Created

---

### BR2: Comments

| Field | Rule | Error message |
|-------|------|---------------|
| UserName | Required, max 100 characters | "Name is required" |
| Rating | Required, between 1 and 5 inclusive | "Rating must be between 1 and 5" |
| Text | Optional, max 1000 characters if provided | "Comment must not exceed 1000 characters" |

---

### BR3: Search

1. **Searched fields:** Title (boost x2), Description (boost x1), Category (boost x1), ArtistName (boost x1)
2. **Result sorting:** relevance score descending, then event date ascending
3. **Pagination:** 20 results per page, `page` query param to navigate
4. **No results:** HTTP 200 OK with empty array `[]`

---

## Bonus User Stories (Nice to Have)

### US6.1 — View event statistics

**As an** organizer or user  
**I want to** see a breakdown chart of events by category  
**So that** I can quickly visualize trends

**Acceptance criteria:**
- Pie chart (Chart.js)
- Data: number of events per category
- API endpoint: GET /api/events/stats/by-category
- Displayed on dedicated page /statistics
