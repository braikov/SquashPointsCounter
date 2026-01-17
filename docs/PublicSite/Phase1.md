# Public Site Phase 1 Plan

## Goal
Minimal public site in separate area with language prefixes (`/bg`, `/en`) that supports:
- Registration/login
- Public tournament listing and details
- Create tournament access (content details deferred to Phase 4)

Focus: extensible design with interface-first approach for replaceable implementations.

## Decisions (locked for Phase 1)
- Public routing uses language prefixes from day one: `/bg/...`, `/en/...`.
- Localization via `IStringLocalizer` + `.resx` resources.
- Root `/` redirects to last known culture (cookie), defaulting to `/en`.
- SEO: readable URLs with slug (see below), canonical/meta tags.
- Sitemap: combined approach (DB table + dynamic providers) behind interfaces.
- Responsive: public layout uses existing CSS framework; validated for mobile/tablet/desktop.

## Concepts
### Slug (SEO URL)
Short, URL-friendly text derived from name (e.g. "National Open 2026" -> `national-open-2026`).
Used for readable URLs: `/bg/tournament/123/national-open-2026`.
Slug is not the primary identifier; it improves SEO and UX.

## Architecture (interfaces first)
Define small contracts for replaceable implementations:
- `ILanguageRouteResolver` (gets culture from URL prefix)
- `ISlugGenerator` (name -> slug)
- `IPublicUrlBuilder` (builds canonical URLs)
- `ISitemapEntryProvider` (returns sitemap entries)
- `ISitemapEntryStore` (CRUD for DB entries)
- `ISitemapBuilder` (combines providers)

## Phase 1 Deliverables
### 1) Routing and Area
- New public area (e.g. `Areas/Public`).
- Route pattern with `{culture}` prefix for `/bg` and `/en`.
- Culture middleware configured to respect URL prefix.

### 2) Localization
- Base resource files for public UI strings.
- Default cultures: `bg-BG`, `en-GB`.
- Language switch (simple link toggle).

### 3) SEO Baseline
- Add `Slug` and `IsPublished` to tournaments.
- Public detail uses canonical URL with slug.
- Meta title/description per listing/detail.

### 4) Sitemap (combined)
- DB table for manual entries (e.g. `SitemapEntries`).
- Dynamic providers for public tournaments.
- Sitemap endpoint builds combined list via interfaces.

### 5) Public Pages
- `/bg/tournaments` and `/en/tournaments` listing.
- `/bg/tournament/{id}/{slug}` and `/en/tournament/{id}/{slug}` details.
- Public layout with header/footer and language switch.

### 6) Access and Ownership
- Use existing `Tournament.UserId` as owner.
- Create tournament access requires authentication.
- Public listing shows only `IsPublished = true`.

## Data Changes
Tournament:
- `Slug` (string, indexed)
- `IsPublished` (bool, indexed)
- Reuse `UserId` as owner

Sitemap:
- New table `SitemapEntries` (url, title, changefreq, priority, culture, isEnabled)

## Out of Scope (Phase 1)
- Complex create tournament UI (Phase 4).
- Organizations and organization ownership.
- Payments, rankings, live scoring.

## Risks / Notes
- URL culture from day one reduces later migration cost.
- Slug collisions handled by appending suffix (implementation detail).
- Sitemap entries should support per-culture URLs.
