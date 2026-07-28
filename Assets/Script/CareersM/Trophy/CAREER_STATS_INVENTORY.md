# Career Completion Stats — scores.db Inventory

Reference for what positive stats can be honestly shown on the **Campaign Trophy screen** at career completion. Covers the SQLite `scores.db` schema, how stats are computed today, which values are trustworthy, and a curated shortlist for player-facing copy.

**Validation note:** This inventory was produced by code audit only. No local `scores.db` was available on the investigation machine (`{PersistentDataPath}/scores/scores.db`). Spot-check queries are listed in [Validation queries](#validation-queries) for manual verification.

---

## Purpose & scope

| In scope | Out of scope |
|---|---|
| Fields stored in `scores.db` | Career/gig progress in band save data (`CampaignCompletedDates`, gig completion flags) |
| Stats computed by `StatCalculator` for completed campaigns | `MomentumTracker` session streak badges (consecutive gigs in one play session) |
| How `CampaignStatsController` selects and displays stats on trophy cards | Debug preset data (`TrophyDebugData` when `_useDebugStats` is on) |
| Positive-framed stats only | Negative stats (`NotesMissed` — already excluded from display pool) |

**Primary question answered:** *What can we tell the player that is backed by real score data?*

---

## Data flow (trophy screen pipeline)

```mermaid
flowchart LR
    scoresDb["scores.db"]
    statCalc["StatCalculator.ComputeCampaignStats"]
    statsCtrl["CampaignStatsController.MapToViewData"]
    trophyMapper["TrophyDataMapper.MapPlayerCards"]
    trophyUI["CampaignTrophyView / PlayerTrophyCard"]

    scoresDb --> statCalc
    statCalc --> statsCtrl
    statsCtrl --> trophyMapper
    trophyMapper --> trophyUI
```

### Entry points

| Step | File | What happens |
|---|---|---|
| Open trophy | `CampaignTrophyController.Open` | Calls `CampaignStatsController.TryBuildViewData` unless debug mode |
| Completion gate | `CampaignStatsController.IsCampaignCompleted` | All gigs in career must be marked complete |
| Date freeze | `CareerManager.GetCampaignCompletedDate` | Stats capped at first completion timestamp |
| Raw aggregation | `StatCalculator.ComputeCampaignStats` | Queries DB, filters to campaign songs |
| View mapping | `CampaignStatsController.MapToViewData` | Relative accolades, shared+flex stats, band milestone |
| Card binding | `TrophyDataMapper.MapPlayerCards` | High Score extracted to card anchor; grid gets remaining rows |

### Display rules (not in DB — affects what players see)

- **Data model:** machine-wide scores for campaign songs up to completion date — **not** tied to logged-in profiles. Each instrument card uses the **most impressive run per song** (stars → FC → accuracy → score).
- **Card layout (A + little B):** 3 shared celebration stats (`AverageAccuracy`, `FourStarCount`, `UniqueSongsCleared`) + 2 flex stats (accolade prefs, then any positive) + High Score anchor.
- **Flex uniqueness:** soft preference that at most 2 cards share a flex type; force-fills a second flex with any remaining positive if needed.
- **Accolades:** band-relative unique best-fit across instrument cards.
- **Star meter** floored at 4★.
- **Golden Vinyl** can be forced via `_celebrateAllCompletionsAsGoldenVinyl`.
- Debug path uses `TrophyDebugData` when `_useDebugStats` is enabled.
---

## scores.db schema reference

Database path: `{PersistentDataPath}/scores/scores.db` (see `ScoreContainer.Init`).

Managed by `ScoreDatabase` — tables are created from record classes on open.

### `PlayerScores` (`PlayerScoreRecord`)

| Column | Type | Written at save? | Positive use |
|---|---|---|---|
| `Id` | int PK | auto | Join key |
| `GameRecordId` | int | yes | Link to session + song |
| `PlayerId` | Guid | yes | Filter by profile |
| `Instrument` | enum | yes | Instrument family grouping |
| `Difficulty` | enum | yes | Expert/Hard/Medium/Easy counts |
| `EnginePresetId` | Guid | yes | Not used by trophy stats today |
| `Score` | int | yes | High score, total, average |
| `Stars` | `StarAmount` | yes | Star tier counts (1–5, Gold=6) |
| `NotesHit` | int | yes | Total notes, **misused as "streak"** |
| `NotesMissed` | int | yes | Negative — excluded from trophy pool |
| `IsFc` | bool | yes | Full combo counts |
| `IsReplay` | bool | yes | Filter: replays excluded from queries |
| `Percent` | float? | yes | Accuracy thresholds; falls back to `NotesHit / (NotesHit + NotesMissed)` |

### `GameRecords` (`GameRecord`)

| Column | Type | Written at save? | Positive use |
|---|---|---|---|
| `Id` | int PK | auto | Session identity |
| `Date` | DateTime | yes | Date-freeze cutoff for completed campaigns |
| `SongChecksum` | byte[] | yes | Match campaign gig songs |
| `GameVersion` | string | yes | Not used by trophy stats |
| `SongName` / `SongArtist` / `SongCharter` | string | yes | High score song label, top songs list |
| `ReplayFileName` / `ReplayChecksum` | string/bytes | yes | Not used by trophy stats |
| `BandScore` | int | yes | Band total, top songs |
| `BandStars` | `StarAmount` | yes | Band star aggregates |
| `SongSpeed` | float | yes | Save validation only (`>= 1.0`); not aggregated |
| `PlayedWithReplay` | bool | yes | Band queries exclude `PlayedWithReplay = 1` |
| `HasBots` | bool | yes | Not filtered by trophy stats |

### `Players` (`PlayerInfoRecord`)

| Column | Type | Use |
|---|---|---|
| `Id` | Guid PK | Profile identity |
| `Name` | string | Display name (not used in stat math) |

---

## Score write path & filters

### What gets persisted (`GameManager.RecordScores`)

Each human player with a valid solo score produces one `PlayerScoreRecord`:

```
Score, Stars, NotesHit, NotesMissed, IsFc, Percent, Instrument, Difficulty, EnginePresetId, IsReplay
```

Each valid band session produces one `GameRecord` plus linked player rows.

**Not persisted** (available only at end-of-song runtime via `BaseStats`):

- `MaxCombo` — true longest note combo / streak
- `Combo`, `TotalNotes`, Star Power stats, solo bonuses, overstrums, section grades, etc.

The post-game score screen shows `MaxCombo` (`ScoreCard`), but that value is **lost** when the session is saved.

### Save eligibility (`ScoreContainer`)

| Filter | Applies to | Effect on trophy stats |
|---|---|---|
| `IsReplay = 0` | Player score queries | Replay-mode plays excluded |
| `PlayedWithReplay = 0` | Band high-score queries | Sessions played with replay file excluded from band bests |
| `songSpeed >= 1.0` | Save gate | Slowed songs not saved at all |
| `!profile.IsBot` | Save gate + StatCalculator | Bot scores never written; bots excluded from profile loop |
| `player.IsScoreValid` | Save gate | Invalid/cheat scores skipped |
| `SaveScoresWithBots` setting | Band save with bots | Affects whether band score is saved when bots present; not re-filtered at trophy read time |
| Campaign song hash match | StatCalculator | Only plays whose `GameRecord.SongChecksum` resolves to a gig song in the career |
| `gr.Date <= completedDate` | Completed campaigns | Freezes stats at first completion time |

### Aggregation semantics (critical for copy)

| Stat family | Unit of counting | Notes |
|---|---|---|
| Player stats | **Per play session** | Replaying the same campaign song adds another row to every count |
| Band score totals | **Best per song** | One `GameRecord` per campaign song (highest `BandScore` before cutoff) |
| Band play sessions | **Per session** | Every `GameRecord` for a campaign song, including retries |
| "Songs Played" label | Misleading | Actually **play sessions on campaign songs**, not unique songs cleared |

---

## Player stats — implemented (`StatType`)

Computed in `StatCalculator.ComputeProfileStats`. Only stats with value `> 0` are added to `AllStats`. Display labels from `CampaignStatsController.GetStatLabel`.

### Tier 1 — Elite

| StatType | Display label | Formula | Trust | Count semantics |
|---|---|---|---|---|
| `GoldStarSongs` | Gold Star Songs: | `COUNT` where `Stars >= StarGold` (6) | **Real** | Per play |
| `ExpertFCs` | Expert FCs: | `COUNT` where `IsFc && Difficulty == Expert` | **Real** | Per play |
| `PerfectAccuracy` | Perfect Songs: | `COUNT` where `GetPercent() >= 1.0` | **Real** | Per play |
| `NearPerfectAccuracy` | 99%+ Songs: | `COUNT` where `GetPercent() >= 0.99` | **Real** | Per play |
| `FullCombos` | Full Combos: | `COUNT` where `IsFc` | **Real** | Per play; any difficulty |

### Tier 2 — Advanced

| StatType | Display label | Formula | Trust | Count semantics |
|---|---|---|---|---|
| `HighScore` | High Score: | `MAX(Score)`; song name from linked `GameRecord.SongName` | **Real** | Best single play; always shown in card anchor |
| `FiveStarCount` | 5-Star Songs: | `COUNT` where `Stars >= Star5` | **Real** | Per play; includes Gold (6) and 5★ |
| `HighAccuracy` | 95%+ Songs: | `COUNT` where `GetPercent() >= 0.95` | **Real** | Per play |
| `ExpertSongsPlayed` | Expert Songs: | `COUNT` where `Difficulty == Expert` | **Real** | Per play; includes non-FC expert runs |
| `FourStarCount` | 4-Star Songs: | `COUNT` where `Stars >= Star4` | **Real** | Per play; includes 4★, 5★, Gold |

### Tier 3 — Solid

| StatType | Display label | Formula | Trust | Count semantics |
|---|---|---|---|---|
| `AverageAccuracy` | Accuracy: | `AVG(GetPercent())` across all campaign plays | **Real** | Mean of sessions |
| `LongestStreak` | Best Streak: | `MAX(NotesHit)` | **MISLABELED** | Not combo streak — see [Mislabeled stats](#mislabeled--untrustworthy-stats) |
| `HardPlusSongsPlayed` | Hard+ Songs: | `COUNT` where `Difficulty >= Hard` | **Real** | Per play |
| `NinetyPlusAccuracy` | 90%+ Songs: | `COUNT` where `GetPercent() >= 0.90` | **Real** | Per play |
| `TotalScore` | Total Score: | `SUM(Score)` | **Real** | Sums every play (retries inflate) |
| `ThreeStarCount` | 3-Star Songs: | `COUNT` where `Stars >= Star3` | **Real** | Per play; includes 3★ and above |

### Tier 4 — Foundation

| StatType | Display label | Formula | Trust | Count semantics |
|---|---|---|---|---|
| `SongsPlayed` | Songs Played: | `COUNT(*)` campaign plays | **Real but misnamed** | Per play session — prefer label **"Sessions"** |
| `TotalNotesHit` | Notes Hit: | `SUM(NotesHit)` | **Real** | Cumulative across all plays |
| `EightyPlusAccuracy` | 80%+ Songs: | `COUNT` where `GetPercent() >= 0.80` | **Real** | Per play |
| `SeventyPlusAccuracy` | 70%+ Songs: | `COUNT` where `GetPercent() >= 0.70` | **Real** | Per play |
| `MediumSongsPlayed` | Medium Songs: | `COUNT` where `Difficulty == Medium` | **Real** | Per play |
| `EasySongsPlayed` | Easy Songs: | `COUNT` where `Difficulty == Easy` | **Real** | Per play |
| `AverageScore` | Avg Score: | `SUM(Score) / COUNT(*)` | **Real** | Mean per session |
| `NotesMissed` | Notes Missed: | `SUM(NotesMissed)` | Negative | **Not added** to `AllStats` (commented out in code) |

### Derived but not in `StatType` (used by trophy UI)

| Field | Source | Use |
|---|---|---|
| `HighScoreSongName` | `GameRecord.SongName` for high-score play | Subtitle under High Score anchor |
| `BestStarsByInstrument[4]` | `MAX((int)Stars)` per instrument family | Star meter display (floored at 4★ in controller) |

**Total implemented player stat types:** 24 in enum; **23 positive** in the display pool (`NotesMissed` excluded).

---

## Band stats — implemented

Computed in `StatCalculator` band section; displayed via `CampaignStatsController.BuildBandStatRows` and `BuildTopSongs`.

| Band field | Display label (if shown) | Formula | Trust | Count semantics |
|---|---|---|---|---|
| `TotalSongs` | (context only) | Count of unique resolved campaign song hashes | **Real** | Per campaign definition |
| `TotalSongsWithScores` | — | Campaign songs with any band high score | **Real** | Per song (best exists) |
| `TotalBandScore` | Total Band Score: | `SUM` of best `BandScore` per campaign song | **Real** | Best-per-song sum |
| `TotalStarsEarned` | Total Stars Earned: | `SUM((int)BandStars)` from those bests | **Real** | Best-per-song sum |
| `SongsWithGoldStars` | Gold Star Songs: | Best per song where `BandStars >= StarGold` | **Real** | Per song |
| `SongsWithFiveStars` | 5-Star Songs: | Best per song where `BandStars >= Star5` | **Real** | Per song; includes Gold |
| `SongsWithFourStars` | 4-Star Songs: | Best per song where `BandStars >= Star4` | **Real** | Per song |
| `TotalPlaySessions` | Total Sessions: | `COUNT` distinct `GameRecord.Id` for campaign songs | **Real** | Every band play attempt |
| `TopSongs` | Top Songs list | Top 5 best band performances by `BandScore` | **Real** | Best plays; UI shows top 3 |

Milestone tier (`GoldenVinyl`, `GoldRecord`, etc.) is normally derived from `SongsWithFiveStars / TotalSongs` unless `_celebrateAllCompletionsAsGoldenVinyl` overrides to always Golden Vinyl.

---

## Stats NOT in scores.db (runtime-only)

These exist in `BaseStats` during gameplay and may appear on the **post-song score screen**, but cannot be queried for career trophy stats without a schema migration.

| Runtime field | Shown elsewhere? | Trophy impact |
|---|---|---|
| `MaxCombo` | Score card (`ScoreCard._maxStreak`) | **Cannot show true "Best Streak"** |
| `Combo` | HUD streak notifications | Not persisted |
| `TotalNotes` | Engine only | Could derive FC verification; not stored |
| `StarPowerActivationCount`, `TimeInStarPower`, etc. | Engine/debug | Not available |
| `SoloBonuses`, `CodaBonuses`, `NoteScore`, `SustainScore` | Engine breakdown | Not available |
| `Overstrums`, ghost notes, section grades | Instrument-specific | Not available |

**Replay files** (`ReplayFileName` on `GameRecord`) could theoretically be re-analyzed for `MaxCombo`, but trophy stats do not do this today.

---

## Mislabeled / untrustworthy stats

### `LongestStreak` / "Best Streak" — NOT a real streak

**Code:**

```csharp
int maxNotesInOneSong = campaignScores.Max(s => s.NotesHit);
// ...
allStats.Add(new PlayerStat { Type = StatType.LongestStreak, Value = maxNotesInOneSong, ... });
```

**What it actually measures:** The highest `NotesHit` on any single play in the campaign. Long songs naturally produce large numbers. A player who never missed but played short songs could show a lower "streak" than someone who FC'd a marathon chart.

**What players expect:** `MaxCombo` — longest unbroken combo in one song (shown on score screen, not saved).

**Downstream impact:**

- Display label: **"Best Streak:"** (`GetStatLabel`)
- Accolade **STREAK MASTER** (`ImmortalStreak`) triggers when this fake value `>= 1000`
- Debug cards in `TrophyDebugData` show fabricated "Best Streak" values

**Recommendations for copy (document-only phase):**

| Action | Option |
|---|---|
| Remove from pool | Stop adding `LongestStreak` to `AllStats` until `MaxCombo` is stored |
| Relabel honestly | Rename to **"Most Notes in One Song"** or **"Peak Notes Hit"** |
| Fix properly | Add `MaxCombo` column to `PlayerScores` and persist in `RecordScores` |

### `SongsPlayed` — accurate number, misleading name

Counts play **sessions**, not unique songs. If a player retries a gig song 5 times, this increments by 5.

**Safer label:** **"Play Sessions"** or **"Runs"**.

### Presentation-layer inflation (not fake DB data, but affects honesty)

- Star meter floored at 4★ minimum on cards
- Golden Vinyl can be awarded regardless of band star performance
- Weighted random stat selection means two completions of the same campaign may show different secondary stats

---

## Additional positive stats — not yet implemented

All of the following can be derived from **existing** `scores.db` columns without migration.

| Proposed stat | Formula sketch | Why it's positive | Implementable today |
|---|---|---|---|
| **Unique Songs Cleared** | `COUNT(DISTINCT SongChecksum)` | Rewards breadth without retry inflation | **Implemented** (`StatType.UniqueSongsCleared`) |
| **FC Rate** | `COUNT(IsFc) / COUNT(*)` as percentage | Skill consistency | Yes |
| **Best Single Accuracy** | `MAX(GetPercent())` | Peak performance moment | Yes |
| **Best Stars (single play)** | `MAX(Stars)` | Clear headline stat | Yes |
| **Hard FCs** | `COUNT` where `IsFc && Difficulty >= Hard` | Stronger than any-difficulty FC | Yes |
| **Expert 100%** | `COUNT` where `Difficulty == Expert && GetPercent() >= 1.0` | Elite perfection | Yes |
| **Gold + FC plays** | `COUNT` where `IsFc && Stars >= StarGold` | Ultimate runs | Yes |
| **Most-played campaign song** | Mode of `SongChecksum` by play count | Flavor / dedication | Yes |
| **First campaign play date** | `MIN(GameRecord.Date)` for campaign songs | "Journey length" framing | Yes |
| **Days active on campaign** | Distinct dates of campaign plays | Engagement | Yes |
| **Band: songs cleared** | `TotalSongsWithScores / TotalSongs` as % | Completion progress | Yes (partially exists) |
| **Band: avg stars per song** | `TotalStarsEarned / TotalSongsWithScores` | Band quality metric | Yes |
| **Instrument best score** | `MAX(Score)` filtered by instrument family | Per-card flavor (already have instrument index) | Yes |

### Requires schema change

| Proposed stat | Blocked by |
|---|---|
| **True Best Combo / Streak** | `MaxCombo` not in `PlayerScores` |
| **Star Power activations** | Not persisted |
| **Overstrum-free runs** | Not persisted |
| **Longest FC streak (multi-song)** | Would need ordered session analysis + reliable `IsFc`; doable but not implemented; still not the same as note combo |

---

## Accolade assignment (relative unique best-fit)

Accolades are assigned in `CampaignStatsController.AssignAccolades` across the whole band:

1. Score each player on each accolade axis (soft floors apply).
2. Normalize within the band (max → 1.0).
3. Greedy unique assign: highest remaining (player, accolade) pair wins; that player and accolade are removed.
4. Leftovers get Rising Star / Rock Enthusiast / Show Opener.

`ImmortalStreak` (STREAK MASTER) is **not** in the assignable pool (fake streak data).

| Accolade | Title shown | Axis | Soft floor |
|---|---|---|---|
| CampaignMVP | FULL COMBO KING | Full Combos | FCs ≥ 1 |
| GoldenFingers | GOLDEN FINGERS | Gold Star Songs | ≥ 1 |
| Perfectionist | FLAWLESS | Perfect Songs | ≥ 1 |
| PerfectStrings | PERFECT STRINGS | Expert FCs (else FCs) | FCs ≥ 1 |
| ExpertVeteran | EXPERT VETERAN | Expert Songs | ≥ 1 |
| SharpShooter | SHARP SHOOTER | Average Accuracy | ≥ 0.70 |
| RoadWarrior | ROAD WARRIOR | Unique Songs Cleared | ≥ 1 |
| RisingStar | RISING STAR | 4★+ / unique songs | 4★ ≥ 1 |
| RockEnthusiast | ROCK ENTHUSIAST | Sessions | ≥ 1 |
| ShowOpener | SHOW OPENER | participation fallback | always |

Flex stats on each card are chosen from the assigned accolade’s preference list (see `GetFlexPreferences`).

---

## Trophy card stat layout (implemented)

| Slots | Types |
|---|---|
| Shared (same labels every card) | Accuracy, 4-Star Songs, Songs Cleared |
| Flex (accolade + uniqueness) | 2 types from accolade preferences |
| Anchor | High Score (+ song name) |

`LongestStreak` is no longer emitted by `StatCalculator`. `SongsPlayed` displays as **Sessions:**. `UniqueSongsCleared` displays as **Songs Cleared:**.

---

## Recommended trophy stat shortlist

### Shared core (live)

| Stat | Player label |
|---|---|
| Average Accuracy | Accuracy |
| 4-Star Songs | 4-Star Songs |
| Unique Songs Cleared | Songs Cleared |
| High Score | High Score (anchor) |

### Flex pool (accolade-driven)

Full Combos, Expert FCs, Gold Stars, Perfect/99%/95%, Expert/Hard+ runs, 5-Star, Sessions, Notes Hit, etc.

### Band panel shortlist

| Stat | Keep? |
|---|---|
| Total Band Score | Yes |
| Total Stars Earned | Yes |
| Gold Star Songs | Yes |
| 5-Star Songs | Yes |
| 4-Star Songs | Yes (if room) |
| Total Sessions | Yes |
| Top 3 Songs | Yes |

### Exclude

| Stat | Action |
|---|---|
| Best Streak / LongestStreak | Removed from trophy emit path |
| Notes Missed | Keep excluded |

---

## Player-facing label guide

Use these labels in trophy UI copy to avoid misleading players.

| Avoid | Prefer | Reason |
|---|---|---|
| Best Streak | *(removed from trophy)* | Was `MAX(NotesHit)`, not combo |
| Songs Played | Sessions | Counts retries |
| 5-Star Songs | 5-Star+ Performances | Includes gold-star plays |
| 4-Star Songs | 4-Star+ Performances | Includes higher tiers |
| Accuracy (alone) | Average Accuracy | It's a mean across sessions |
| High Score (grid) | *(anchor only)* | Already dedicated row; don't duplicate in grid |

---

## Validation queries

Run read-only against a local `scores.db` to spot-check. Replace `?` placeholders.

```sql
-- Schema sanity
SELECT name FROM sqlite_master WHERE type='table';

-- Null rates on nullable fields
SELECT
  COUNT(*) AS total,
  SUM(CASE WHEN Percent IS NULL THEN 1 ELSE 0 END) AS null_percent,
  SUM(CASE WHEN IsReplay IS NULL THEN 1 ELSE 0 END) AS null_is_replay
FROM PlayerScores;

-- Streak mislabel check: compare Max NotesHit vs what trophy would show
SELECT ps.PlayerId, gr.SongName, ps.NotesHit, ps.IsFc, ps.Score
FROM PlayerScores ps
JOIN GameRecords gr ON ps.GameRecordId = gr.Id
WHERE ps.IsReplay = 0
ORDER BY ps.NotesHit DESC
LIMIT 10;

-- Sessions vs unique songs for one player
SELECT
  COUNT(*) AS play_sessions,
  COUNT(DISTINCT gr.SongChecksum) AS unique_songs
FROM PlayerScores ps
JOIN GameRecords gr ON ps.GameRecordId = gr.Id
WHERE ps.PlayerId = ? AND ps.IsReplay = 0;

-- Band best-per-song vs total sessions for a campaign song set
-- (supply checksum list from campaign resolution)
```

---

## Open questions / future schema additions

1. **Persist `MaxCombo`** on `PlayerScores` at save time — unlocks honest streak stats and fixes STREAK MASTER accolade.
2. **Unique vs session counts** — Should trophy stats dedupe per song (best play only) or count every attempt? Current code is inconsistent (player = sessions, band score = best-per-song).
3. **Instrument filtering** — Player cards are assigned Guitar/Bass/Drums/Vocals by slot index, but `StatCalculator` aggregates **all instruments** for that profile. A guitarist's card may include drum plays if the same profile played drums.
4. **Star Power / overstrums** — Worth persisting if we want deeper "positive flex" stats later.
5. **Replay re-analysis** — Could recover `MaxCombo` for historical data without migration, at cost of complexity and replay file dependency.

---

## Source file index

| Area | Path |
|---|---|
| DB schema | `Assets/Script/Scores/PlayerScoreRecord.cs`, `GameRecord.cs`, `PlayerInfoRecord.cs` |
| DB queries | `Assets/Script/Scores/ScoreDatabase.cs` |
| Score container API | `Assets/Script/Scores/ScoreContainer.cs` |
| Score persistence | `Assets/Script/Gameplay/GameManager.cs` (`RecordScores`) |
| Stat computation | `Assets/Script/CareersM/Motivation/StatCalculator.cs` |
| Stat types | `Assets/Script/CareersM/Motivation/Data/StatDefinition.cs` |
| Formatting | `Assets/Script/CareersM/Motivation/StatFormatter.cs` |
| Trophy mapping/selection | `Assets/Script/CareersM/Motivation/CampaignStatsController.cs` |
| Trophy UI binding | `Assets/Script/CareersM/Trophy/TrophyDataMapper.cs`, `CampaignTrophyController.cs` |
| Runtime stats (not saved) | `YARG.Core/YARG.Core/Engine/BaseStats.cs` |
| Debug fakes | `Assets/Script/CareersM/Trophy/TrophyDebugData.cs` |
| Completion date | `Assets/Script/CareersM/CareerManager.cs` |

---

*Last updated: investigation pass per Career Completion Stats plan. Document-only — no code changes.*
