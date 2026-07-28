# HypeEngine External Configuration System — Technical Reference

## 1. SYSTEM OVERVIEW

The HypeEngine is a data-driven menu text evaluation system that runs when the CoverFlow carousel UI is active in the CareersM game mode. It evaluates a set of prioritized rules against the current gig's metadata (song durations, campaign progress, artist names) and outputs up to two encouragement strings displayed on the active CoverFlow card.

**V4 Architecture:** The hardcoded Tier 1/2/3 rule evaluation logic has been replaced with an external `hypeengine.ini` configuration file. Rules are parsed once when the CoverFlow menu first opens, cached in static memory, and evaluated via a generic loop over sorted rules grouped by tier.

**Key properties:**
- Lazy one-time parse (never at global game launch)
- Section-aware INI parser (unlike the flattened `careerdef.ini` parser)
- Static in-memory cache (`HypeEngineCache`)
- Scales from 3 to 5 tiers without code changes
- Two global boolean flags control tier prioritization behavior
- Fallback chain: external INI → StreamingAssets INI → `HypeThresholds` defaults

---

## 2. FILE INVENTORY

### Source Files (namespace: `YARG.Menu.Career.CoverFlow`)

| File | Type | Purpose |
|------|------|---------|
| `Assets/Script/CareersM/CoverFlow/HypeEngineConfig.cs` | New | Data models: `HypeEngineGlobalSettings`, `HypeEngineRule`, `HypeEngineTierConfig`, `HypeEvaluationContext`, `HypeEngineCache` |
| `Assets/Script/CareersM/CoverFlow/HypeEngineConfigParser.cs` | New | Section-aware INI parser with path resolution |
| `Assets/Script/CareersM/CoverFlow/HypeEngineConditionEvaluator.cs` | New | Static dispatch mapping condition type strings to evaluation delegates |
| `Assets/Script/CareersM/CoverFlow/HypeEngine.cs` | Modified | `HypeThresholds` struct (retained) + `HypeEngine` class (refactored to data-driven) |
| `Assets/Script/CareersM/CoverFlow/CoverFlowController.cs` | Modified | Added `HypeEngineCache.LoadIfNeeded()` call in `SetCareer()` |

### Asset Files

| File | Purpose |
|------|---------|
| `Assets/StreamingAssets/CareersM/hypeengine.ini` | Default config shipped with game |
| `{PathHelper.PersistentDataPath}/career/hypeengine.ini` | User-writable override (created by user) |

### Dependencies (existing, unchanged)

| File | Usage |
|------|-------|
| `Assets/Script/CareersM/CareerData.cs` | `CareerInfo`, `GigInfo` types |
| `Assets/Script/CareersM/SongMatchData.cs` | `GigDurationData`, `CareerMatchCache` types |
| `Assets/Script/CareersM/SongMatchCache.cs` | `SongMatchCache.GetOrLoadCache()` |
| `Assets/Script/CareersM/CoverFlow/CoverFlowConfig.cs` | `CoverFlowConfig` type, `IconicArtists` list |
| `Assets/Script/Helpers/PathHelper.cs` | `PersistentDataPath`, `StreamingAssetsPath` |
| `YARG.Core.Song.SongEntry` | Song metadata (Artist, Name, SongLengthSeconds) |

---

## 3. DATA MODEL REFERENCE

### 3.1 `HypeEngineGlobalSettings` (class)

```csharp
public class HypeEngineGlobalSettings
{
    public static HypeEngineGlobalSettings Default { get; }
}
```

**Status:** Reserved for future use. All tier prioritisation is hardcoded — see §5 Evaluation Flow.

### 3.2 `HypeEngineRule` (class)

```csharp
public class HypeEngineRule
{
    public string RuleId;           // Section name, e.g. "Rule.CampaignFinale"
    public int Tier;                // 0=fallback, 1-5=normal tiers
    public int PriorityRank;        // Lower = higher priority within tier

    // Primary condition (required)
    public string ConditionType;    // e.g. "remaining_gigs_total"
    public string Operator;         // "eq", "lt", "lte", "gt", "gte"
    public string Value;            // String parsed at eval time

    // Secondary condition (optional, AND logic)
    public string ConditionType2;   // default: string.Empty
    public string Operator2;        // default: string.Empty
    public string Value2;           // default: string.Empty

    // Output
    public string Slot1;            // Text for slot 1, may contain "{0}"
    public string Slot2;            // Text for slot 2, default: string.Empty
    public string Slot1Param;       // Substitution for {0} in Slot1, default: string.Empty
    public string Slot2Param;       // Substitution for {0} in Slot2, default: string.Empty
}
```

**Sorting:** Rules are sorted by `Tier` ascending, then `PriorityRank` ascending. Tier 0 (fallback) is pushed to the very end regardless of `PriorityRank`. During evaluation, the first rule whose conditions match wins — see §5.

### 3.3 `HypeEngineTierConfig` (class)

```csharp
public class HypeEngineTierConfig
{
    public int Tier;
    public float? BlacklistTotalMinutes;      // nullable
    public float? BlacklistAvgTrackMinutes;   // nullable
}
```

**Semantics:** If `BlacklistTotalMinutes` is set and the gig's total duration exceeds it, the entire tier is skipped. Same for `BlacklistAvgTrackMinutes` with average track duration. If `DurationData` is null or `resolvedSongCount == 0`, the tier is always skipped.

### 3.4 `HypeEvaluationContext` (struct)

```csharp
public struct HypeEvaluationContext
{
    public GigInfo Gig;
    public CareerInfo Career;
    public CoverFlowConfig Config;
    public List<YARG.Core.Song.SongEntry> SongEntries;
    public GigDurationData DurationData;
    public int RemainingGigsTotal;
    public int RemainingGigsInTier;
    public double CampaignAvgDuration;       // seconds
    public double GigTotalSeconds;           // seconds
    public double GigAvgTrackSeconds;        // seconds
    public string[] HeadlinerArtistNames;
}
```

**Construction:** Built by `HypeEngine.BuildContext()` which calls `CareerManager.Instance.GetGigs()`, `CareerManager.Instance.ResolveGig()`, `SongMatchCache.GetOrLoadCache()`, and `HypeEngineConditionEvaluator.ComputeCampaignAvgDuration()`.

**Note:** `RemainingGigsTotal` and `RemainingGigsInTier` are still computed for condition evaluation but are no longer used for tier-prioritisation decisions (the first-match-wins loop is unconditional).

### 3.5 `HypeEngineCache` (static class)

```csharp
public static class HypeEngineCache
{
    public static bool IsLoaded { get; }
    public static HypeEngineGlobalSettings GlobalSettings { get; }
    public static List<HypeEngineRule> Rules { get; }          // Sorted
    public static List<HypeEngineTierConfig> TierConfigs { get; }

    public static void LoadIfNeeded();    // Idempotent, triggers parse on first call
    public static void Invalidate();      // Resets IsLoaded, clears all collections

    public struct ParseResult             // Return type of HypeEngineConfigParser.Parse()
    {
        public HypeEngineGlobalSettings GlobalSettings;
        public List<HypeEngineRule> Rules;
        public List<HypeEngineTierConfig> TierConfigs;
    }
}
```

**Lifecycle:**
1. `CoverFlowController.SetCareer()` calls `HypeEngineCache.LoadIfNeeded()`
2. First call: `HypeEngineConfigParser.Parse()` reads INI from disk, populates collections, sorts rules
3. Subsequent calls: no-op (`IsLoaded == true`)
4. `Invalidate()` resets for hot-reload or menu exit

### 3.6 `HypeThresholds` (struct, retained from V3)

```csharp
[System.Serializable]
public struct HypeThresholds
{
    public float BlitzTotalMinutesMax;           // default: 9.0f
    public float BlitzAvgTrackMinutesMax;        // default: 3.5f
    public float BiteSizeAvgTrackMinutesMax;     // default: 3.0f
    public float Tier2BlacklistTotalMinutes;     // default: 10.0f
    public float Tier2BlacklistAvgTrackMinutes;  // default: 3.5f
    public string[] HeadlinerArtistNames;        // default: ["METALLICA","QUEEN","ROCK BAND"]
    public string DefaultFallbackText;           // default: "SETLIST VIBE: PURE ROCK & ROLL"
    public static HypeThresholds Default { get; }
}
```

**Role in V4:** Populated from `CoverFlowController`'s serialized Inspector fields via `BuildHypeThresholds()`. Used as:
1. Source of `HeadlinerArtistNames` for the `has_headliner_artist` condition
2. Fallback text when `HypeEngineCache.Rules` is empty (no INI found)
3. Backward compatibility — existing Inspector values continue to work

---

## 4. INI FORMAT SPECIFICATION

### 4.1 File Location Resolution

```
Priority 1: {PathHelper.PersistentDataPath}/career/hypeengine.ini
Priority 2: {PathHelper.StreamingAssetsPath}/CareersM/hypeengine.ini
Priority 3: (none — HypeThresholds.Default used as fallback)

`[Global]` section settings (`guarantee_highest_tier`, `use_only_highest_tier`) have been removed. The engine unconditionally uses a first-match-wins strategy: Tier 1 (priority order) → Tier 2 (priority order) → ... → Tier 0 fallback.
```

`PathHelper.PersistentDataPath` resolves to:
- Editor/Test: `{Application.persistentDataPath}/dev`
- Nightly: `{Application.persistentDataPath}/nightly`
- Release: `{Application.persistentDataPath}/release`

On Windows, `Application.persistentDataPath` is `%AppData%/LocalLow/{company}/{product}`.

### 4.2 Syntax Rules

- Lines starting with `#` or `;` are comments (ignored)
- Empty lines are ignored
- Section headers: `[SectionName]` (case-insensitive for dispatch, case-preserved for RuleId)
- Key-value pairs: `key = value`
- Keys are lowercased during parsing
- Values wrapped in double quotes have quotes stripped
- Whitespace around `=` and at line ends is trimmed

### 4.3 `[Global]` Section

| Key | Type | Values | Default | Description |
|-----|------|--------|---------|-------------|
| `guarantee_highest_tier` | bool | `true`, `false` | `false` | Re-run only highest matched tier to fill both slots |
| `use_only_highest_tier` | bool | `true`, `false` | `false` | Stop at first matching tier, stop within tier at first match |

### 4.4 `[Rule.{RuleId}]` Section

| Key | Required | Type | Values | Description |
|-----|----------|------|--------|-------------|
| `tier` | YES | int | `0`-`5` | Tier number. 0 = fallback (always evaluated last) |
| `priority_rank` | YES | int | any integer | Sort order within tier (lower = higher priority) |
| `condition` | YES | string | See §5 | Condition type identifier |
| `operator` | YES | string | `eq`, `lt`, `lte`, `gt`, `gte` | Comparison operator |
| `value` | YES | string | Depends on condition | Threshold value parsed at eval time |
| `condition2` | NO | string | See §5 | Second condition type (AND logic) |
| `operator2` | NO | string | `eq`, `lt`, `lte`, `gt`, `gte` | Second operator |
| `value2` | NO | string | Depends on condition | Second threshold value |
| `slot1` | YES | string | Any text | Output for slot 1. May contain `{0}` placeholder |
| `slot2` | NO | string | Any text | Output for slot 2. May contain `{0}` placeholder |
| `slot1_param` | NO | string | Literal number or token | Substitution for `{0}` in slot1 |
| `slot2_param` | NO | string | Literal number or token | Substitution for `{0}` in slot2 |

**Validation:** Rules missing `tier`, `priority_rank`, `condition`, `operator`, `value`, or `slot1` are skipped with a warning log.

### 4.5 `[Tier.{N}]` Section

| Key | Required | Type | Values | Description |
|-----|----------|------|--------|-------------|
| `blacklist_total_minutes` | NO | float | Positive number | Skip tier if gig total duration > this |
| `blacklist_avg_track_minutes` | NO | float | Positive number | Skip tier if gig avg track duration > this |

**Blacklist logic:** If `DurationData` is null or `resolvedSongCount == 0`, the tier is ALWAYS skipped regardless of thresholds. If either threshold is exceeded, the tier is skipped.

---

## 5. CONDITION TYPE REFERENCE

### 5.1 `always`
- **Operators:** `eq` only
- **Value:** `true`
- **Context fields used:** none
- **Behavior:** Always returns true when value is `"true"`. Used for fallback rules.
- **Example:** `condition = always`, `operator = eq`, `value = true`

### 5.2 `remaining_gigs_total`
- **Operators:** `eq`, `lt`, `lte`, `gt`, `gte`
- **Value:** integer
- **Context fields used:** `RemainingGigsTotal`
- **Behavior:** Compares number of incomplete gigs in the campaign against the threshold.
- **Example:** `condition = remaining_gigs_total`, `operator = eq`, `value = 1`

### 5.3 `remaining_gigs_in_tier`
- **Operators:** `eq`, `lt`, `lte`, `gt`, `gte`
- **Value:** integer
- **Context fields used:** `RemainingGigsInTier`
- **Behavior:** Compares number of incomplete gigs in the current tier against the threshold. Tier is computed as `gigIndex / (totalGigs / 3)`.
- **Example:** `condition = remaining_gigs_in_tier`, `operator = eq`, `value = 1`

### 5.4 `total_minutes`
- **Operators:** `lt`, `lte`, `gt`, `gte` (NOT `eq`)
- **Value:** float (invariant culture)
- **Context fields used:** `GigTotalSeconds` (converted to minutes: `/ 60.0`)
- **Behavior:** Compares total duration of all resolved songs in the gig against the threshold.
- **Example:** `condition = total_minutes`, `operator = lte`, `value = 9.0`

### 5.5 `avg_track_minutes`
- **Operators:** `lt`, `lte`, `gt`, `gte` (NOT `eq`)
- **Value:** float (invariant culture)
- **Context fields used:** `GigAvgTrackSeconds` (converted to minutes: `/ 60.0`)
- **Behavior:** Compares average track duration against the threshold.
- **Example:** `condition = avg_track_minutes`, `operator = lt`, `value = 3.5`

### 5.6 `has_headliner_artist`
- **Operators:** `eq` only
- **Value:** `true` or `false`
- **Context fields used:** `SongEntries`, `HeadlinerArtistNames`, `Config.IconicArtists`
- **Behavior:** Returns true if any song's artist matches an entry in `HeadlinerArtistNames` (case-insensitive) OR `Config.IconicArtists`. Only meaningful when value is `"true"`.
- **Example:** `condition = has_headliner_artist`, `operator = eq`, `value = true`

### 5.7 `avg_track_below_campaign_avg`
- **Operators:** `eq` only
- **Value:** `true` (value is ignored — always checks for `true` behavior)
- **Context fields used:** `DurationData.avgTrackSeconds`, `CampaignAvgDuration`
- **Behavior:** Returns true if `DurationData` is valid AND `avgTrackSeconds < CampaignAvgDuration`.
- **Example:** `condition = avg_track_below_campaign_avg`, `operator = eq`, `value = true`

### 5.8 Adding a New Condition Type

1. Add a new `case` in `HypeEngineConditionEvaluator.EvaluateSingleCondition()` switch statement
2. Implement the evaluation method (private static)
3. If the condition needs new context fields, add them to `HypeEvaluationContext` and populate in `HypeEngine.BuildContext()`
4. No other code changes required

---

## 6. EVALUATION ALGORITHM

### 6.1 Entry Point

```
CoverFlowController.BuildGigCardData(gig, gigIndex, state)
  → _hypeEngine.Evaluate(gig, career, config, thresholds, out slot1, out slot2)
```

### 6.2 Algorithm Pseudocode

```
FUNCTION Evaluate(gig, career, config, thresholds, OUT slot1, OUT slot2):
    slot1 = null, slot2 = null
    IF gig == null OR career == null OR config == null: RETURN

    ctx = BuildContext(gig, career, config, thresholds)
    HypeEngineCache.LoadIfNeeded()

    IF HypeEngineCache.Rules is empty:
        slot1 = thresholds.DefaultFallbackText
        RETURN

    // Group rules by tier (excluding Tier 0 fallback), ordered by tier ascending
    tierGroups = Rules.Where(tier > 0).GroupBy(tier).OrderBy(tier)
    highestSatisfiedTier = -1

    FOR EACH tierGroup IN tierGroups:
        tierCfg = TierConfigs.FirstOrDefault(tier == tierGroup.Key)
        IF IsTierBlacklisted(tierCfg, ctx): CONTINUE  // skip this tier

        tierHadMatch = false

        FOR EACH rule IN tierGroup.OrderBy(priority_rank):
            IF EvaluateCondition(rule, ctx):
                AssignNextSlot(slot1, slot2, FormatSlotText(rule.Slot1, rule.Slot1Param, ctx))
                IF rule.Slot2 is not empty:
                    AssignNextSlot(slot1, slot2, FormatSlotText(rule.Slot2, rule.Slot2Param, ctx))

                tierHadMatch = true
                highestSatisfiedTier = tierGroup.Key

                IF GlobalSettings.UseOnlyHighestTier: BREAK  // stop rules in this tier

        IF tierHadMatch AND GlobalSettings.UseOnlyHighestTier: BREAK  // stop all lower tiers

    // GuaranteeHighestTier post-processing
    IF GlobalSettings.GuaranteeHighestTier AND highestSatisfiedTier > 0:
        slot1 = null, slot2 = null
        highestRules = Rules.Where(tier == highestSatisfiedTier).OrderBy(priority_rank)
        FOR EACH rule IN highestRules:
            IF EvaluateCondition(rule, ctx):
                AssignNextSlot(slot1, slot2, FormatSlotText(rule.Slot1, rule.Slot1Param, ctx))
                IF rule.Slot2 is not empty:
                    AssignNextSlot(slot1, slot2, FormatSlotText(rule.Slot2, rule.Slot2Param, ctx))

    // Fallback
    IF slot1 == null AND slot2 == null:
        fallback = Rules.FirstOrDefault(tier == 0)
        slot1 = fallback?.Slot1 ?? thresholds.DefaultFallbackText
    ELSE IF slot1 == null:
        slot1 = slot2
        slot2 = null
```

### 6.3 `IsTierBlacklisted` Algorithm

```
FUNCTION IsTierBlacklisted(tierCfg, ctx):
    IF tierCfg == null: RETURN false
    IF ctx.DurationData == null OR ctx.DurationData.resolvedSongCount == 0: RETURN true

    totalMin = ctx.GigTotalSeconds / 60.0
    avgMin = ctx.GigAvgTrackSeconds / 60.0

    IF tierCfg.BlacklistTotalMinutes.HasValue AND totalMin > tierCfg.BlacklistTotalMinutes.Value:
        RETURN true
    IF tierCfg.BlacklistAvgTrackMinutes.HasValue AND avgMin > tierCfg.BlacklistAvgTrackMinutes.Value:
        RETURN true

    RETURN false
```

### 6.4 `EvaluateCondition` Algorithm

```
FUNCTION EvaluateCondition(rule, ctx):
    IF rule == null: RETURN false

    primaryResult = EvaluateSingleCondition(rule.ConditionType, rule.Operator, rule.Value, ctx)
    IF NOT primaryResult: RETURN false

    IF rule.ConditionType2 is not empty AND rule.Operator2 is not empty AND rule.Value2 is not empty:
        secondaryResult = EvaluateSingleCondition(rule.ConditionType2, rule.Operator2, rule.Value2, ctx)
        IF NOT secondaryResult: RETURN false

    RETURN true
```

### 6.5 `FormatSlotText` Algorithm

```
FUNCTION FormatSlotText(template, param, ctx):
    IF template is empty: RETURN ""
    IF param is empty: RETURN template

    replacement = ResolveParamValue(param, ctx)
    RETURN template.Replace("{0}", replacement)

FUNCTION ResolveParamValue(param, ctx):
    SWITCH param.ToLower():
        case "avg_track_minutes":    RETURN RoundToInt(ctx.GigAvgTrackSeconds / 60.0)
        case "total_minutes":        RETURN RoundToInt(ctx.GigTotalSeconds / 60.0)
        case "remaining_gigs_total": RETURN ctx.RemainingGigsTotal
        case "remaining_gigs_in_tier": RETURN ctx.RemainingGigsInTier
        default:
            IF float.TryParse(param): RETURN RoundToInt(parsed)
            ELSE: RETURN param  // return as-is
```

---

## 7. INTEGRATION POINTS

### 7.1 CoverFlowController

**File:** `Assets/Script/CareersM/CoverFlow/CoverFlowController.cs`

**Awake (line 147):**
```csharp
_hypeEngine = new HypeEngine();
BuildHypeThresholds();
```
Instantiates the engine and populates `_hypeThresholds` from serialized Inspector fields.

**SetCareer (line 192):**
```csharp
_currentCareer = career;
HypeEngineCache.LoadIfNeeded();  // V4: Lazy-init
```
Triggers the one-time INI parse on first CoverFlow open.

**BuildGigCardData (line ~555):**
```csharp
_hypeEngine.Evaluate(gig, _currentCareer, _config, _hypeThresholds, out string h1, out string h2);
```
Called per-card during population. Output strings go into `GigCardData.hypeSlot1` and `GigCardData.hypeSlot2`.

### 7.2 GigCardData

**File:** `Assets/Script/CareersM/CoverFlow/GigCardData.cs`

```csharp
public string hypeSlot1;  // Pre-computed HypeEngine slot 1 string
public string hypeSlot2;  // Pre-computed HypeEngine slot 2 string
```

### 7.3 CoverFlowCardControllerV3

**File:** `Assets/Script/CareersM/CoverFlow/CoverFlowCardControllerV3.cs`

Consumes `GigCardData.hypeSlot1` → `_avgTrackText` (TMP text component)
Consumes `GigCardData.hypeSlot2` → `_intensityText` (TMP text component)

---

## 8. DEFAULT CONFIGURATION

The shipped `Assets/StreamingAssets/CareersM/hypeengine.ini` contains 7 rules across 3 tiers + 1 fallback, reproducing the original hardcoded behavior:

| Rule ID | Tier | Priority | Condition | Output |
|---------|------|----------|-----------|--------|
| `Rule.CampaignFinale` | 1 | 10 | `remaining_gigs_total eq 1` | "👑 FINAL BOSS SETLIST UNLOCKS NEXT" |
| `Rule.VenueGate` | 1 | 20 | `remaining_gigs_in_tier eq 1` | "🎸 VENUE FINALE: ONE MORE TO GO" |
| `Rule.BlitzPlay` | 2 | 10 | `total_minutes lte 9.0 AND avg_track_minutes lt 3.5` | "⚡ FAST TRACK (Under 9 Mins Total)" |
| `Rule.BiteSized` | 2 | 20 | `total_minutes gt 9.0 AND avg_track_minutes lt 3.0` | "⏱️ Avg. Track: ~{avg} MINS" |
| `Rule.HeadlinerHook` | 3 | 10 | `has_headliner_artist eq true` | "🔥 Crowd Energy: Stadium Anthem Status" |
| `Rule.StaminaMitigation` | 3 | 20 | `avg_track_below_campaign_avg eq true` | "😎 Setlist Intensity: Casual / High Vibe" |
| `Rule.Fallback` | 0 | 999 | `always eq true` | "🎸 Setlist Vibe: Pure Rock & Roll" |

Tier 2 has a blacklist: `blacklist_total_minutes = 10.0`, `blacklist_avg_track_minutes = 3.5`.

---

## 9. EXTENSION GUIDE

### 9.1 Adding a 4th Tier

1. Add `[Tier.4]` section to `hypeengine.ini` (optional blacklist thresholds)
2. Add `[Rule.{NewRule}]` sections with `tier = 4`
3. No code changes required if using existing condition types

### 9.2 Adding a 5th Tier

Same as §9.1 — the `GroupBy(r => r.Tier)` loop handles any tier count.

### 9.3 Adding a New Condition Type

1. Add a `case "new_condition_name":` in `HypeEngineConditionEvaluator.EvaluateSingleCondition()` (line 63)
2. Implement the evaluation method
3. If new context data is needed, add fields to `HypeEvaluationContext` and populate in `HypeEngine.BuildContext()`

### 9.4 Adding a New Slot Parameter Token

Add a `case "new_token":` in `HypeEngine.ResolveParamValue()` (line 313).

### 9.5 Adding a New Tier-Level Config Key

1. Add the field to `HypeEngineTierConfig`
2. Parse it in `HypeEngineConfigParser.ParseTierSection()`
3. Use it in `HypeEngine.IsTierBlacklisted()` or the main evaluation loop

---

## 10. ERROR HANDLING & LOGGING

| Scenario | Behavior |
|----------|----------|
| No INI file at either path | `YargLogger.LogWarning`, cache stays empty, `HypeThresholds.DefaultFallbackText` used |
| INI file exists but is malformed | Individual rules/sections are skipped with `YargLogger.LogWarning`; valid sections still parse |
| Rule missing required key | Rule skipped with warning: `"Rule '{name}' missing or invalid '{key}'. Skipping."` |
| Unknown condition type | `YargLogger.LogWarning`, condition returns `false` |
| Unparseable numeric value | Condition returns `false` (no log — silent fail) |
| `DurationData` is null | Tier blacklist returns `true` (skip tier); duration-based conditions return `false` |
| `SongEntries` is null/empty | `has_headliner_artist` returns `false` |

---

## 11. THREADING & LIFECYCLE

- All code runs on the Unity main thread
- `HypeEngineCache.LoadIfNeeded()` is called from `CoverFlowController.SetCareer()` which runs on the main thread
- `File.ReadAllLines()` in the parser is synchronous (acceptable for a one-time parse of a small file)
- `HypeEngineCache.Invalidate()` can be called to force re-parse (e.g., on returning to main menu or during hot-reload)
- The `HypeEngine` instance is created once in `CoverFlowController.Awake()` and lives for the GameObject's lifetime