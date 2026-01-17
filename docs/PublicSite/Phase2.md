# Public Site Phase 2 Plan

## Goal
Enable logged-in user flows by adding a global public header with login/register links
and wiring basic authentication entry points. Prepare for owner-based create flow.

## Scope
1) Public header visible on all public pages.
2) Login/Register links using existing Identity pages (Login) and new Register page.
3) Registration flow (custom page, no Identity Register page exists).
4) Email delivery interface for verification codes (implementation later).
5) Localization for header labels (bg/en).
6) Basic return flow (return to home or current page as configured).

## Registration Requirements
- Fields: Email, Password, First Name, Last Name, Birth Date, Gender, Country, Sport (Squash only for now).
- Generate verification code (uses ShortCodeToToken table).
- Send email with code + link to Player Home (email sending via interface only, no real email yet).
- Player Home page must exist (minimal placeholder).

## Deliverables
- Update Public layout to include a shared header.
- Add localized resources for header labels.
- Add returnUrl handling for login/register links.
- Keep layout minimal and consistent with existing public UI.

## Out of Scope
- Create tournament UI (Phase 3).
- Owner dashboard / My tournaments.
- Advanced profile management.

## Notes
- Uses Identity pages in `Areas/Identity`.
- ReturnUrl uses current culture prefix.
