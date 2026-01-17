# Public Site Phase 3 Plan

## Goal
Establish a cohesive public UI shell (header/footer + logged-in navigation) and begin the first player-centric pages, keeping tournament creation as a focused next step.

## Scope
1) Public UI shell (header/footer + responsive styling).
2) Logged-in navigation scaffolding (tabs + routes).
3) My Tournaments list + entry points for create/edit (minimal functionality).
4) Create/Edit Tournament: first draft (form skeleton + validation, content details in Phase 4).

## Deliverables
### 1) Public UI Shell
- Finalize public header and footer layout.
- Logged-in header variant with icons (notifications/messages/calendar) placeholders.
- Public sub-navigation for player area (Dashboard, Matches, Events, Membership, My Tournaments).
- Responsive behavior for mobile/tablet/desktop.

### 2) Logged-in Player Area Routes (Scaffold)
- `/bg/dashboard` and `/en/dashboard` (basic profile summary placeholder).
- `/bg/matches` and `/en/matches` (empty state list).
- `/bg/events` and `/en/events` (empty state list).
- `/bg/membership` and `/en/membership` (empty state list).
- `/bg/my-tournaments` and `/en/my-tournaments` (real data list; see below).

### 3) My Tournaments
- List tournaments created by the logged-in user.
- Show status (published/unpublished) and links to edit.
- Provide a primary CTA to create a new tournament.

### 4) Create/Edit Tournament (Draft)
- Basic form shell for create/edit.
- Minimal required fields only (Name, Country, Start/End dates, Level placeholder if needed).
- Validation + save to existing tournament entity.
- No detailed content or complex sections (Phase 4).

## Out of Scope (Phase 3)
- Full tournament content editor.
- Organization ownership & roles.
- Payments, rankings, live scoring.

## Notes
- Keep interface-first design (public layout + navigation is the primary deliverable).
- Focus on player workflows first; tournament creators are secondary.
