# careerdef.ini Reference

This is the modder reference for the `careerdef.ini` file that defines a YARG career/campaign bundle.

## Where it lives

Each campaign is a folder ("bundle") under:

```
<PersistentData>/career/bundles/<your-campaign-folder>/careerdef.ini
```

The game scans every subfolder of `bundles/` on load. A folder is only treated as a campaign if it contains a `careerdef.ini` with at least a `name` and a valid `gigsfile`. All file paths inside `careerdef.ini` (artwork, gigs file, background, posters) are resolved **relative to the bundle folder**.

## File format rules

- One `key = value` per line.
- Keys are case-insensitive.
- Blank lines are ignored.
- A line that starts with `#` or `;` is a comment.
- Inside `[CampaignSettings]` and `[Tier_N]` sections, inline comments are also supported (everything from `#` or `;` to end of line is stripped).
- Values may be wrapped in `"double quotes"`; the quotes are removed.
- Section headers like `[Visuals]` are organizational. Most visual/hype keys are read by a "flat" parser that ignores section headers, so those keys work anywhere in the file. Tier progression keys (`[CampaignSettings]`, `[Tier_N]`) are read by a section-aware parser and must be under their correct header.

## Top-level keys (no section)

| Key | Required | Description |
|-----|----------|-------------|
| `name` | Yes | Display name of the campaign. |
| `gigsfile` | Yes | Filename of the gigs JSON (relative to the bundle folder). |
| `id` | No | Stable bundle id. Defaults to the folder name if omitted. |
| `artworkpath` | No | Campaign box-art image (relative path). |
| `sourceicon` | No | Source/label icon id. |
| `author` | No | Author name. Defaults to `Unknown`. |
| `releasedate` | No | Release date (e.g. `2024-05-01`). Used by the "Sort: Date" option. |

## [Visuals]

| Key | Default | Description |
|-----|---------|-------------|
| `use_coverflow` | `true` (auto-added) | Kept for compatibility. The CoverFlow gig view is always used. |
| `imagebg` | `true` | If `true`, use `bgimg` as the background. If `false`, the CoverFlow prefab's fallback background is used. |
| `bgimg` | (none) | Background image filename (relative path). If missing on disk, falls back automatically. |
| `accent_color` | `#0055ff` | Hex color for borders, active frames, and accent glows. `#rrggbb` or `#rrggbbaa`. |
| `fallback_color` | `#1a1a2e` | Hex color used when there is no background image and no fallback sprite. |
| `display_name_on_crate` | `false` | **Set to `true` to show the campaign name on the gear-crate card.** See below. |
| `useartasgigposter` | `false` (auto-added as `true`) | If `true`, gigs use the campaign `artworkpath` as their poster (when no poster config is provided). |
| `gigposterfile` | (none) | Path to a gig poster config file (relative path). |
| `giglayoutpath` | (none) | Path to a per-gig layout override INI (relative path). |

### Turning on the campaign name

By default the box art speaks for itself and the name is **not** drawn on the card. To display the campaign's name on its gear-crate card, add this under `[Visuals]`:

```ini
[Visuals]
display_name_on_crate = true
```

The name text comes from the top-level `name` key. Leave it `false` (or omit it) if your box art already includes a logo/title.

## [HypeEngineOverrides]

| Key | Default | Description |
|-----|---------|-------------|
| `iconic_artists` | (none) | Comma-separated artist names treated as "headliner"/iconic for hype rules. Case-insensitive. |
| `custom_vibe_label` | (none) | Optional override text for the default vibe label. |

Example:

```ini
[HypeEngineOverrides]
iconic_artists = Tay Zonday, Lemon Demon
```

## [CampaignSettings]

| Key | Default | Description |
|-----|---------|-------------|
| `unlockall` | `false` | If `true`, all tier locks are ignored and every gig is unlocked immediately. Has no effect if the campaign has no `[Tier_N]` sections (everything is unlocked anyway). |

## [Tier_N] progression

Define tiers as `[Tier_0]`, `[Tier_1]`, ... in ascending order. Gigs are assigned to tiers in file order based on `gigs_in_tier`. If your tiers cover fewer gigs than the gigs file contains, the surplus gigs are appended to the last tier automatically.

| Key | Default | Description |
|-----|---------|-------------|
| `tiername` | `Tier N` | Display name of the tier. (`name` also works but `tiername` is preferred to avoid clashing with the campaign `name`.) |
| `gigs_in_tier` | `0` | How many gigs belong to this tier. |
| `gigs_required_to_unlock` | `0` | Cumulative number of completed campaign gigs required before this tier unlocks. Tier 0 is usually `0` (unlocked by default). |

## Auto-added defaults

When the game loads a bundle, any of these missing keys are appended to your `careerdef.ini` automatically (existing values are never overwritten):

```ini
[Visuals]
use_coverflow = true
imagebg = false
#bgimg = bg\bg-03.jpg
#accent_color = #ff6600
#fallback_color = #1a1a2e
useartasgigposter = true
#gigposterfile = posters.json

[CampaignSettings]
unlockall = false
```

## Full example

```ini
id = yarg-champ01
name = YARG Champion
artworkPath = yarg02.jpg
sourceIcon = yarg
gigsFile = gigs-yarg1.json
author = ch7
releaseDate = 2024-05-01

[Visuals]
use_coverflow = true
imagebg = false
#bgimg = bg\bg-03.jpg
accent_color = #ff6600
fallback_color = #1a1a2e
display_name_on_crate = true    # show the campaign name on the card
useartasgigposter = true

[HypeEngineOverrides]
iconic_artists = Tay Zonday, Lemon Demon

[CampaignSettings]
unlockall = false

[Tier_0]
tiername = "Local Garage Gigs"
gigs_in_tier = 3
gigs_required_to_unlock = 0

[Tier_1]
tiername = "Canyon Dust Circuit"
gigs_in_tier = 4
gigs_required_to_unlock = 2

[Tier_2]
tiername = "World Tour Finale"
gigs_in_tier = 8
gigs_required_to_unlock = 6
```
