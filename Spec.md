# Spec.md — “RankedIn-like” Monolith (ASP.NET Core Razor + SignalR)

## 1. Purpose
Build a **single, monolithic web application** that reproduces the **core functionality** of Rankedin-style platform:
- player accounts & profiles
- organizations (clubs/federations/groups)
- rankings (with point levels + approvals)
- events: tournaments, club leagues, team leagues, americano
- live scoring (E-Referee) + live scoreboards
- online payments (Stripe)
- messaging/notifications and event discovery

This spec focuses on **functional parity** (not UI/branding/text copying).

Primary reference behaviors are from Rankedin public site + their User Manual/FAQ. :contentReference[oaicite:0]{index=0}

---

## 2. Product scope

### 2.1 Roles
- **Visitor**: browse public events/rankings/organizations.
- **Player**: profile, join events/classes, pay fees, message, referee matches if eligible.
- **Organizer/Admin**: create/manage organizations, events, scheduling, draws, payments, publishing.
- **Ranking Owner**: manage ranking policies, point levels, approve/reject events when “with permission”.
- **System Admin** (internal): moderation, support, configuration.

Ranking privacy policy options include **Open / Closed / With permission**. :contentReference[oaicite:1]{index=1}

### 2.2 Event types (parity targets)
1) **Tournament**
- creation wizard: name, dates, location, organizer, ranking link, courts, regulations, consent, payment options :contentReference[oaicite:2]{index=2}
- classes/divisions (gender, age group, match type, limits, playing dates, age validation) :contentReference[oaicite:3]{index=3}
- sign-in close (manual), then generate draws, schedule matches, publish draws & publish schedule :contentReference[oaicite:4]{index=4}
- draw types & stages (support “four types of draws and combination…”) :contentReference[oaicite:5]{index=5}
2) **Club League**
- pool-based rounds with promote/demote; skipped-round position drop; manual close sign-in; optional ranking integration :contentReference[oaicite:6]{index=6}
- players schedule matches themselves (admin does not schedule) :contentReference[oaicite:7]{index=7}
3) **Team League**
- league competition with draws, publish/unpublish, ranking integration :contentReference[oaicite:8]{index=8}
4) **Americano**
- feature exists in manual index; implement as event type with rounds + rotations (details to be refined from UI parity) :contentReference[oaicite:9]{index=9}

### 2.3 Live features
- **E-Referee**: users update points live; updates propagate to scoreboards and (optionally) live video integration :contentReference[oaicite:10]{index=10}
- supported sports and scoring rules include Tennis/Squash/Badminton/Racquetball etc. :contentReference[oaicite:11]{index=11}
- **Scoreboards**: match scoreboard + court/table scoreboard (tournaments); match scoreboard for club/team leagues :contentReference[oaicite:12]{index=12}

### 2.4 Payments
- Tournament fees via **Stripe**, with org-level setup; class fee per player; player checkout; admin view financial summary; refunds in Stripe dashboard :contentReference[oaicite:13]{index=13}

### 2.5 Rankings
- Create ranking requires organization; has sport; built-in gender/age splits; privacy policy options :contentReference[oaicite:14]{index=14}
- Rankings have **point levels**; points depend on selected level + final standing; editable but no bulk import :contentReference[oaicite:15]{index=15}
- Connect event with ranking: Tournament / Club League / Team League flows :contentReference[oaicite:16]{index=16}
- Approve/Reject tournaments toward ranking (for “with permission”) :contentReference[oaicite:17]{index=17}

---

## 3. Non-functional requirements
- **Monolith**: single ASP.NET Core web app (Razor + SignalR).
- **RDBMS**: SQL Server (default) with EF Core; support PostgreSQL as optional.
- **Auth**: ASP.NET Core Identity (email/password), optional external providers later.
- **Performance**: server-side caching for public pages; CDN for static/media.
- **Auditability**: log admin actions; immutable event/match history.
- **Security**: OWASP basics, CSRF for forms, rate limiting on auth, file upload validation.
- **GDPR**: consent handling (player consent request) aligns with event “Ask for Consent” behavior :contentReference[oaicite:18]{index=18}

---

## 4. High-level architecture

### 4.1 Technology
- ASP.NET Core (.NET 8 LTS recommended)
- Razor Pages (or MVC + Razor Views) with Areas:
  - `/` public
  - `/app` authenticated user area
  - `/admin` organizers/ranking owners
- SignalR hubs:
  - `LiveScoringHub` (match updates)
  - `ScoreboardHub` (fan displays)
  - `NotificationsHub` (optional; can also be polling)
- Background jobs:
  - outbox sender (emails/webhooks)
  - payment reconciliation (Stripe webhooks)
  - scheduled notifications, reminders

### 4.2 Storage
- Relational DB for core domain.
- Blob storage for logos/media (S3-compatible or Azure Blob).
- Optional Redis (SignalR backplane + caching) if scaled beyond single node.

---

## 5. Core domain model (entities)

### 5.1 Identity & profiles
- `User` (Identity)
- `UserProfile` (name, avatar, sport preferences, country/city, contact fields)
- `ConsentRecord` (scope, timestamp, eventId, granted)

### 5.2 Organizations
- `Organization` (name, type: Club/Federation/Group/Company)
- `OrganizationMembership` (role: Owner/Admin/Member)
- `OrgStripeAccount` (stripeAccountId, status, currency)

### 5.3 Rankings
- `Ranking` (organizationId, sport, name, privacyPolicy: Open/Closed/WithPermission)
- `RankingPointLevel` (name, eventType, pointMatrix by standing, win/loss rules for team league)
- `RankingApproval` (eventId, rankingId, status, decidedBy, decidedAt)

### 5.4 Events (base)
- `Event` (id, type, name, sport, organizerOrgId?, location, dates, regulations, status)
- `EventParticipant` (userId, status, payments, eligibility)
- `EventSettings` (askConsent, streamingInfo, etc.) :contentReference[oaicite:19]{index=19}

#### Tournament specifics
- `TournamentClass` (name, matchType, ageGroup, validateAge, limit, playingDates, feePerPlayer) :contentReference[oaicite:20]{index=20}
- `Court` (name, location fields)
- `TournamentSignIn` (open/closed timestamps)
- `Draw` (classId, type, stage, config, publishedFlag)
- `Match` (players/teams, round, scheduled time, courtId, status)
- `SchedulePublication` (publishedFlag, publishedAt) :contentReference[oaicite:21]{index=21}

#### Club League specifics
- `ClubLeagueRound`
- `Pool` (roundId, index)
- `PoolStanding` (userId, position, W/L, points)
- `PromotionRule` (promoteCount, demoteCount, skippedDropCount) :contentReference[oaicite:22]{index=22}

#### Team League specifics
- `Team`
- `TeamMembership`
- `TeamMatch` (ties, individual matches)
- `Lineup` (position/lining-up for points) :contentReference[oaicite:23]{index=23}

### 5.5 Messaging & notifications
- `Conversation` (direct messages)
- `Message` (senderId, body, timestamp)
- `Notification` (type, payload, readAt)

---

## 6. Functional requirements (by module)

## 6.1 Accounts & onboarding
- Register/login, profile setup.
- Select default sport preference (used as default when creating events) :contentReference[oaicite:24]{index=24}
- Privacy & contact preferences.

## 6.2 Organization management
- Create organization; manage roles (owner/admin).
- Organization manager dashboard.
- Stripe setup entry points (link to Stripe onboarding) :contentReference[oaicite:25]{index=25}

## 6.3 Rankings
### Create ranking
- Requires organization selection. :contentReference[oaicite:26]{index=26}
- Configure privacy policy: Open/Closed/With permission. :contentReference[oaicite:27]{index=27}
- Configure sport type.

### Point levels
- Manage point levels per event type.
- UI to edit points one-by-one; save version. :contentReference[oaicite:28]{index=28}

### Event-to-ranking integration
- Events may connect to a ranking at creation time (tournament/club/team league). :contentReference[oaicite:29]{index=29}
- Selecting “ranking level” occurs during close-sign-in steps (admin). :contentReference[oaicite:30]{index=30}
- If ranking is “with permission”, ranking owner must approve/reject. :contentReference[oaicite:31]{index=31}

## 6.4 Tournament management
### Step-based admin panel (wizard parity)
- Step 1: Create/Edit details (name, ranking, dates, regulations, organizer, location, courts, advanced: streaming/consent/payment). :contentReference[oaicite:32]{index=32}
- Step 2: Sponsor logos (upload + placement) (parity target; details from UI)
- Step 3: Classes: create/edit/delete with validations; class settings and limits; age validation. :contentReference[oaicite:33]{index=33}
- Step 4: Players: add/search players; payment marking; exports when consent collected (parity target). :contentReference[oaicite:34]{index=34}
- Step 5: Close sign-in (tournament-wide or per-class). :contentReference[oaicite:35]{index=35}
- Step 6: Draws: select draw type, generate, preview, publish/unpublish draws. :contentReference[oaicite:36]{index=36}
- Step 7: Times: configure day time windows, courts/day, match duration, min break, round/day. :contentReference[oaicite:37]{index=37}
- Step 8: Matches & Video: schedule matches, publish schedule, streaming court integration (parity target). :contentReference[oaicite:38]{index=38}

### Draw types
- Implement 4 draw types + stages (as in Rankedin). :contentReference[oaicite:39]{index=39}
- Each draw generation creates matches, rounds, bracket metadata.
- Allow admin manual adjustments before event start.

## 6.5 Club League
- Create club league with settings: ranking link optional; organizer; location; dates; pools settings; promote/demote rules; skipped-round drop. :contentReference[oaicite:40]{index=40}
- Rounds: generate next round standings based on rules; admin can edit results/standings. :contentReference[oaicite:41]{index=41}
- Match scheduling: players set match times themselves; admin cannot schedule. :contentReference[oaicite:42]{index=42}

## 6.6 Team League
- Create league, teams, sign-in close.
- Draw generation and publish/unpublish. :contentReference[oaicite:43]{index=43}
- Ranking points can depend on win/loss + individual matches + lineup positions. :contentReference[oaicite:44]{index=44}

## 6.7 E-Referee (Live scoring)
- Eligibility: user must be event participant or admin to find/referee match. :contentReference[oaicite:45]{index=45}
- UI: locate match, increment score/points by sport rules, undo last point, end game/match.
- Supported sport scoring presets (at minimum): Tennis, Squash, Badminton, Racquetball. :contentReference[oaicite:46]{index=46}
- SignalR: broadcast every point update to:
  - match viewers
  - scoreboard displays
  - match page / “Matches” list

## 6.8 Scoreboards
- Scoreboards accessible via browser links; require an updating device that runs E-Referee. :contentReference[oaicite:47]{index=47}
- Tournament: Match scoreboard + Court/Table scoreboard.
- Club/Team league: Match scoreboard only. :contentReference[oaicite:48]{index=48}

## 6.9 Online payments (Stripe)
- Organization-level Stripe connection + status check. :contentReference[oaicite:49]{index=49}
- Tournament payment configuration at creation (Stripe/Cash/Bank transfer) and currency selection constraints. :contentReference[oaicite:50]{index=50}
- Class fee per player; doubles join flow supports “partner/no partner” and pays accordingly. :contentReference[oaicite:51]{index=51}
- Admin financial tools: payment list, player financial summary, refunds (via Stripe). :contentReference[oaicite:52]{index=52}
- Stripe webhooks:
  - `checkout.session.completed`
  - `payment_intent.succeeded`
  - `charge.refunded`
  - account status updates (Connect)

## 6.10 Discovery (search/calendar)
- Global search: events, organizations, rankings.
- Calendar page with filters by country etc. (parity target; referenced by manual). :contentReference[oaicite:53]{index=53}

---

## 7. Realtime design (SignalR)

### 7.1 Hubs & channels
- `LiveScoringHub`
  - group: `match:{matchId}`
  - messages: `ScoreUpdated`, `MatchStateChanged`
- `ScoreboardHub`
  - group: `scoreboard:{scoreboardId}` (court or match)
- `NotificationsHub` (optional)
  - group: `user:{userId}`

### 7.2 Consistency
- Authoritative state persisted in DB (append-only score events recommended).
- SignalR broadcasts are derived from persisted events (or transactional outbox).

---

## 8. Public pages (SEO-friendly)
- `/organisation/{id}/{slug}`
- `/ranking/{id}/{slug}`
- `/tournament/{id}/{slug}`
- `/clubleague/{id}/{slug}`
- `/teamleague/{id}/{slug}`
- `/live/scoreboard/{type}/{id}` (public display)
- Embed endpoints (ranking list embed exists as a concept in manual). :contentReference[oaicite:54]{index=54}

---

## 9. Admin UX (Razor)
- Step-based admin panels matching event types.
- “Publish / Not published” toggles:
  - draws published for matches visibility :contentReference[oaicite:55]{index=55}
  - schedule published for times/courts visibility :contentReference[oaicite:56]{index=56}

---

## 10. Acceptance criteria (selected)

### Tournament: close sign-in
- When closed, join button disappears; admin can still add players in admin panel. :contentReference[oaicite:57]{index=57}

### Tournament: schedule publish
- Before publishing: times/courts visible to admin only.
- After publishing: visible on draws + matches section. :contentReference[oaicite:58]{index=58}

### E-Referee eligibility
- Non-participant/non-admin cannot referee (cannot find match). :contentReference[oaicite:59]{index=59}

### Club League scheduling
- Admin cannot schedule; players can set schedules for their matches. :contentReference[oaicite:60]{index=60}

---

## 11. Phasing (you will choose what/when)
To support “full clone but pick later”, implement in this order for lowest risk:

1) Identity + profiles + organizations
2) Rankings (create + point levels)
3) Tournament core (create + classes + players + close sign-in)
4) Draw generation + publish draws
5) E-Referee + scoreboards (SignalR)
6) Schedule matches + publish schedule
7) Stripe payments
8) Club League
9) Team League
10) Americano + embeds + advanced federation features

---

## 12. Open items (need UI parity confirmation)
These exist in Rankedin ecosystem but require closer UI inspection / deeper manual pages to spec precisely:
- exact set of “four draw types” names + all options & stage combinations (we will mirror the manual pages per type)
- sponsor logo placement rules
- federation membership/license management (mentioned in changelog)
- video streaming “SportCam” integration flows

References: User Manual index + changelog. :contentReference[oaicite:61]{index=61}
