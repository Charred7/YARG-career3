# CoverFlow Poster System — Reference Guide

## `careerdef.ini` — All Supported Keys

Located in the bundle folder, e.g. `%AppData%\LocalLow\YARC\YARG\nightly\career\bundles\my-campaign\careerdef.ini`

```ini
# ═══════════════════════════════════════════
# REQUIRED FIELDS
# ═══════════════════════════════════════════

name       = My Campaign
gigsFile   = gigs.json

# ═══════════════════════════════════════════
# OPTIONAL METADATA
# ═══════════════════════════════════════════

id          = my-campaign-id
artworkPath = campaign_art.jpg
sourceIcon  = yarg
author      = AuthorName
releaseDate = 2025-01-01

# ═══════════════════════════════════════════
# COVERFLOW VISUALS
# ═══════════════════════════════════════════

use_coverflow  = true     # Enable CoverFlow carousel (default: true)
imagebg        = true     # Use image background (true) or solid colour (false)
bgimg          = background.jpg   # Background image file (in the bundle folder)
accent_color   = #0055ff  # Accent border/glow colour (hex)
fallback_color = #1a1a2e  # Solid colour fallback for background (hex)

# ═══════════════════════════════════════════
# POSTER STRATEGY
# ═══════════════════════════════════════════

# Option A: Use campaign artwork as poster for ALL gigs
useartasgigposter     = true
use_art_as_gig_poster = true   # ALIAS — same as above

# Option B: Use a JSON config file to map per-gig posters
gigposterfile = posters.json

# ═══════════════════════════════════════════
# ADVANCED
# ═══════════════════════════════════════════

giglayoutpath = layouts.ini     # Per-gig layout overrides

# ═══════════════════════════════════════════
# HYPE ENGINE OVERRIDES
# ═══════════════════════════════════════════

custom_vibe_label = Pure Rock & Roll
iconic_artists    = Metallica, Queen, Led Zeppelin
```

---

## Poster Config JSON — `gigposterfile` Format

If you specify `gigposterfile = posters.json` in `careerdef.ini`, create a JSON file alongside it:

```json
{
  "posters": {
    "The First Gig":      "posters/first_gig.png",
    "Arena Showdown":     "posters/arena.jpg",
    "Final Encore":       "posters/finale.png",
    "Battle of the Band": "posters/battle.png"
  },
  "fallback": "posters/missing_poster.png"
}
```

**Rules:**
- **`posters`** (required): A dictionary mapping gig display names to poster file paths
  - **Keys**: Exact gig names as defined in `gigs.json` (matching is case-insensitive)
  - **Values**: Paths relative to the config file's directory (the bundle folder)
- **`fallback`** (optional): A single poster file path used when a gig name is **not found** in the `posters` dictionary. Ignored when `useartasgigposter = true`.
- **File types**: `.png`, `.jpg`, `.jpeg`

---

## Poster Resolution Priority

When a gig card needs a poster, the system tries these in order:

```
1. JSON config (gigposterfile) — posters dictionary
   └─ Exact match → resolved path
   └─ Case-insensitive match → resolved path

2. JSON config (gigposterfile) — fallback key
   └─ Gig name not found in posters dict → fallback path

3. Naming convention in bundle folder
   └─ {sanitized_gig_name}.png/.jpg/.jpeg

4. Naming convention in posters/ subfolder
   └─ posters/{sanitized_gig_name}.png/.jpg/.jpeg

5. useArtAsGigPoster = true
   └─ campaign's artworkPath image

6. No poster (card shows empty/dark)
```

### Gig Name Sanitization

The naming convention converts gig names to filenames like this:

| Gig Name | Sanitized Filename |
|----------|-------------------|
| `The First Gig` | `the_first_gig.png` |
| `Arena Showdown` | `arena_showdown.jpg` |
| `Song 2 (Blur)` | `song_2_(blur).png` |
| `What's My Age Again?` | `whats_my_age_again.png` |
| `Kashmir/Livin' Lovin' Maid` | `kashmirlivin_lovin_maid.png` |

**Transformations:**
- Apostrophes `'` → removed
- Spaces → underscores `_`
- Forward/back slashes `/\` → underscores `_`
- Question marks `?` → removed
- Colons `:` → removed
- All characters → lowercase

---

## Folder Layout Examples

### Example A: Using `posters/` subfolder with naming convention

```
career/bundles/my-campaign/
├── careerdef.ini
├── gigs.json
├── background.jpg
├── campaign_art.jpg
└── posters/
    ├── the_first_gig.png
    ├── arena_showdown.jpg
    └── finale.png
```

No `gigposterfile` needed. Posters are found by naming convention fallback.

### Example B: Using JSON config for per-gig mapping

```
career/bundles/my-campaign/
├── careerdef.ini
├── gigs.json
├── posters.json              <-- gigposterfile = posters.json
├── background.jpg
└── custom_posters/
    ├── gig1_special.png
    ├── arena.jpg
    └── finale_big.png
```

`posters.json`:
```json
{
  "posters": {
    "The First Gig":  "custom_posters/gig1_special.png",
    "Arena Showdown": "custom_posters/arena.jpg",
    "Final Encore":   "custom_posters/finale_big.png"
  }
}
```

### Example C: Single artwork for all gigs

```
career/bundles/my-campaign/
├── careerdef.ini
├── gigs.json
├── campaign_art.jpg
└── background.jpg
```

`careerdef.ini`:
```ini
artworkPath = campaign_art.jpg
use_art_as_gig_poster = true
```

---

## Code Reference — Key Files

| File | Purpose |
|------|---------|
| [`CoverFlowConfig.cs`](Assets/Script/CareersM/CoverFlow/CoverFlowConfig.cs) | Config data class + INI parser for all careerdef.ini CoverFlow fields |
| [`GigPosterConfig.cs`](Assets/Script/CareersM/CoverFlow/GigPosterConfig.cs) | JSON poster mapping data class + loader |
| [`CareerBundleManager.cs`](Assets/Script/CareersM/CareerBundleManager.cs) | Scans bundle folders, parses careerdef.ini, validates files |
| [`CoverFlowController.cs`](Assets/Script/CareersM/CoverFlow/CoverFlowController.cs) | Runtime poster resolution (LoadPosterForGig → ResolveGigPosterFilename) |
| [`CoverFlowBackground.cs`](Assets/Script/CareersM/CoverFlow/CoverFlowBackground.cs) | Background image loader (bgimg resolved to bundle dir) |
| [`CoverFlowCard.cs`](Assets/Script/CareersM/CoverFlow/CoverFlowCard.cs) | Card UI component — SetPoster(texture) |
| [`CoverFlowCardControllerV3.cs`](Assets/Script/CareersM/CoverFlow/CoverFlowCardControllerV3.cs) | Card renderer — PopulateCard(GigCardData) |
| [`GigCardData.cs`](Assets/Script/CareersM/CoverFlow/GigCardData.cs) | Data transfer struct between controller and card |