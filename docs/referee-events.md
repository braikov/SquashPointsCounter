# Referee Event Logic

This document defines how referee UI actions map to match events, log text, and side effects.

## Event pipeline

For each UI action:
1. Send the event to the server (`/api/refferee/game-log`) using the enum name.
2. Add a human-readable entry to the referee log.
3. Apply local side effects (score changes, timers, navigation).

## Local state

- `GameScoreFirst`, `GameScoreSecond`: games in match for player A and B.
- `CurrentGameScoreFirst`, `CurrentGameScoreSecond`: points in current game for player A and B.
- `LastPointWinner`: `A` or `B` (used to detect handout).
- `ServeSide`: `left` or `right` for the current server.
- `EventHistory[]`: ordered list of enum events for undo (future).

## Events and behavior

### Point A
- Send: `PointA`
- Log: `Point A`
- Side effects: `CurrentGameScoreFirst + 1`
- Then: check for game end.
- Handout rule: if previous point was for B, this is a handout to A. Show serve side choice and store `ServeSide`.

### Point B
- Send: `PointB`
- Log: `Point B`
- Side effects: `CurrentGameScoreSecond + 1`
- Then: check for game end.
- Handout rule: if previous point was for A, this is a handout to B. Show serve side choice and store `ServeSide`.

### Yes, let
- Send: `Let`
- Log: `Yes, let`
- Side effects: none.

### Stroke A
- Send: `StrokeA`
- Log: `Stroke A`
- Side effects: `CurrentGameScoreFirst + 1`
- Then: check for game end.
- Handout rule: if previous point was for B, this is a handout to A. Show serve side choice and store `ServeSide`.

### Stroke B
- Send: `StrokeB`
- Log: `Stroke B`
- Side effects: `CurrentGameScoreSecond + 1`
- Then: check for game end.
- Handout rule: if previous point was for A, this is a handout to B. Show serve side choice and store `ServeSide`.

### Conduct stroke A
- Send: `ConductStrokeA`
- Log: `Conduct stroke A`
- Side effects: `CurrentGameScoreFirst + 1`
- Then: check for game end.
- Handout rule: if previous point was for B, this is a handout to A. Show serve side choice and store `ServeSide`.

### Conduct stroke B
- Send: `ConductStrokeB`
- Log: `Conduct stroke B`
- Side effects: `CurrentGameScoreSecond + 1`
- Then: check for game end.
- Handout rule: if previous point was for A, this is a handout to B. Show serve side choice and store `ServeSide`.

### A serves first
- Send: `AServersFirst` or `AServersFirstOnLeft` depending on side
- Log: `{Player A name} serves first on {left|right}`
- Side effects: none.
  - Initialize `LastPointWinner` to A for handout tracking.

### B serves first
- Send: `BServersFirst` or `BServersFirstOnLeft` depending on side
- Log: `{Player B name} serves first on {left|right}`
- Side effects: none.
  - Initialize `LastPointWinner` to B for handout tracking.

### Injury timeout A
- Send: `InjuryTimeoutA`
- Log: `Injury timeout A`
- Side effects: start a timer (TBD).

### Injury timeout B
- Send: `InjuryTimeoutB`
- Log: `Injury timeout B`
- Side effects: start a timer (TBD).

### Equipment issue
- Send: `EquipmentIssue`
- Log: `Equipment issue`
- Side effects: TBD.

### A retires
- Send: `ARetires`
- Log: `A retires`
- Side effects: show confirmation dialog; on confirm navigate to pin screen and mark `B wins match`.

### B retires
- Send: `BRetires`
- Log: `B retires`
- Side effects: show confirmation dialog; on confirm navigate to pin screen and mark `A wins match`.

### A request review
- Send: `ARequestReview`
- Log: `A request review`
- Side effects: TBD.

### B request review
- Send: `BRequestReview`
- Log: `B request review`
- Side effects: TBD.

### Warning A
- Send: `WarningA`
- Log: `Warning A`
- Side effects: TBD (likely UI marker).

### Warning B
- Send: `WarningB`
- Log: `Warning B`
- Side effects: TBD (likely UI marker).

### Undo
- Send: `Undo`
- Log: `Undo`
- Side effects: use `EventHistory` to reverse (TBD).

## Server-side mirror (future)

The same pipeline should be mirrored server-side:
- Validate event sequence.
- Persist `GameLog` with `MatchGameId`.
- Update scores and match/game state consistently.

## Handout behavior

Handout occurs when the point winner changes compared to the previous point:
- Previous point A, current point B → handout to B.
- Previous point B, current point A → handout to A.

When handout happens:
- Show the serve side selection: `Serve on left` / `Serve on right`.
- Save the selection into `ServeSide`.
- Use `ServeSide` to show/guide the next serve position.
