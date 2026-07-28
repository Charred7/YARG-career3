# Update YARG07 from upstream/dev, then lay the Careers mod back on top.
# Strategy: upstream always wins for base game files.
# CareersM quarantine folders are restored from a backup branch tip,
# then small C# hooks are re-applied onto the fresh upstream scripts.
#
# Scenes/prefabs are NOT YAML-merged. After this script finishes, open Unity and run:
#   Tools > YARG Careers > Apply Menu Hooks After Upstream Update
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\Tools\CareersUpdate\update-from-upstream.ps1
#   powershell ... -SkipConfirm
#   powershell ... -BackupBranch 'backup/my-name'

[CmdletBinding()]
param(
    [switch]$SkipConfirm,
    [string]$BackupBranch = ''
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $RepoRoot

function Write-Step([string]$Message) {
    Write-Host ''
    Write-Host ("=== {0} ===" -f $Message) -ForegroundColor Cyan
}

function Assert-GitCleanEnough {
    $status = @(git status --porcelain)
    $blocking = @()
    foreach ($line in $status) {
        if ($line -match 'YARG\.Core') { continue }
        # Allow this toolkit to exist untracked / dirty during development
        if ($line -match 'Tools[/\\]CareersUpdate') { continue }
        $blocking += $line
    }
    if ($blocking.Count -gt 0) {
        Write-Host 'Working tree has uncommitted changes:' -ForegroundColor Yellow
        $blocking | ForEach-Object { Write-Host ("  {0}" -f $_) }
        throw 'Commit or stash your changes first (save commit), then re-run.'
    }
}

function Test-BackupPath([string]$Branch, [string]$Path) {
    git cat-file -e "${Branch}:${Path}" 2>$null
    return ($LASTEXITCODE -eq 0)
}

function Ensure-TextContains([string]$Path, [string]$Needle, [string]$Context) {
    $text = Get-Content -Raw -Path $Path
    if ($text -notlike ("*{0}*" -f $Needle)) {
        throw ("Patch failed for {0}: expected to find '{1}' in {2}" -f $Context, $Needle, $Path)
    }
}

function Add-MethodsToClass(
    [string]$Path,
    [string]$MarkerMethod,
    [string]$MethodsToAdd,
    [string]$Label,
    [string]$InsertBeforePattern = ''
) {
    $text = Get-Content -Raw -Path $Path
    if ($text -like ("*{0}*" -f $MarkerMethod)) {
        Write-Host ("  already present: {0}" -f $Label) -ForegroundColor DarkYellow
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($InsertBeforePattern)) {
        $match = [regex]::Match($text, $InsertBeforePattern)
        if ($match.Success) {
            $updated = $text.Insert($match.Index, $MethodsToAdd + "`r`n")
            Set-Content -Path $Path -Value $updated -NoNewline
            Write-Host ("  patched: {0}" -f $Label)
            return
        }
    }

    # Upstream files typically end with indented class close, then namespace close:
    #   "    }\r\n}\r\n"
    $endMatch = [regex]::Match($text, '(?ms)(?<classClose>^[ \t]+\})\s*\r?\n\}[ \t]*\r?\n?\s*\z')
    if (-not $endMatch.Success) {
        throw ("Could not find class/namespace closing braces for {0} ({1})" -f $Label, $Path)
    }
    $updated = $text.Insert($endMatch.Groups['classClose'].Index, $MethodsToAdd + "`r`n")
    Set-Content -Path $Path -Value $updated -NoNewline
    Write-Host ("  patched: {0}" -f $Label)
}

Write-Step 'YARG Careers update (upstream wins, careers on top)'
Write-Host ("Repo: {0}" -f $RepoRoot)

$branch = (git rev-parse --abbrev-ref HEAD).Trim()
if ($branch -ne 'custom-campaign') {
    Write-Host ("Current branch is '{0}' (expected custom-campaign)." -f $branch) -ForegroundColor Yellow
}

Assert-GitCleanEnough

# Shared base-YARG files Careers touches (scene/prefab wiring only - C# is patched automatically)
$menuWiringFiles = @(
    'Assets/Scenes/MenuScene.unity',
    'Assets/Prefabs/Menu/MainMenu/MainMenu.prefab',
    'Assets/Prefabs/Menu/MainMenu/MenuEntry.prefab'
)

if (-not $SkipConfirm) {
    Write-Host ''
    Write-Host 'This will:' -ForegroundColor White
    Write-Host '  1. Create a backup branch of your current tip'
    Write-Host '  2. HARD RESET custom-campaign to upstream/dev'
    Write-Host '  3. Restore CareersM quarantine folders from the backup'
    Write-Host '  4. Re-apply C# career hooks onto fresh upstream scripts'
    Write-Host '  5. If MenuScene/MainMenu were untouched upstream, keep your CareersM button'
    Write-Host '     Otherwise prompt you to run the Unity editor rewire tool'
    Write-Host ''
    $answer = Read-Host 'Type YES to continue'
    if ($answer -ne 'YES') {
        Write-Host 'Aborted.'
        exit 1
    }
}

Write-Step 'Fetch upstream/dev'
git fetch upstream dev
$upstream = (git rev-parse upstream/dev).Trim()
$head = (git rev-parse HEAD).Trim()
$upstreamOld = (git merge-base HEAD upstream/dev).Trim()
Write-Host ("HEAD:              {0}" -f $head)
Write-Host ("upstream/dev:      {0}" -f $upstream)
Write-Host ("previous fork pt:  {0}" -f $upstreamOld)

$changedMenuWiring = @(git diff --name-only $upstreamOld $upstream -- @menuWiringFiles)
$menuWiringChanged = ($changedMenuWiring.Count -gt 0)
Write-Host ''
if ($menuWiringChanged) {
    Write-Host 'Upstream CHANGED menu wiring files since your last fork point:' -ForegroundColor Yellow
    $changedMenuWiring | ForEach-Object { Write-Host ("  - {0}" -f $_) }
} else {
    Write-Host 'Upstream did NOT change MenuScene / MainMenu / MenuEntry since your last fork point.' -ForegroundColor Green
}

if ([string]::IsNullOrWhiteSpace($BackupBranch)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $BackupBranch = "backup/pre-update-$stamp"
}

Write-Step ("Backup current tip as {0}" -f $BackupBranch)
git branch $BackupBranch HEAD
Write-Host ("Backup created at {0}" -f (git rev-parse $BackupBranch))

Write-Step 'Reset branch to upstream/dev'
git reset --hard upstream/dev

Write-Step 'Restore Careers quarantine from backup'
$quarantine = @(
    'Assets/Script/CareersM',
    'Assets/Script/CareersM.meta',
    'Assets/Prefabs/CareersM',
    'Assets/Prefabs/CareersM.meta',
    'Assets/Art/CareersM',
    'Assets/Art/CareersM.meta',
    'Assets/Resources/CareersM',
    'Assets/Resources/CareersM.meta',
    'Assets/StreamingAssets/CareersM',
    'Assets/StreamingAssets/CareersM.meta',
    'Assets/Editor/CarrersM',
    'Assets/Editor/CarrersM.meta',
    'Assets/Editor/Care.meta',
    'Tools/CareersUpdate',
    'docs/coverflow-poster-reference.md',
    'docs/hypeengine-technical-reference.md'
)

$sfx = @(git ls-tree -r --name-only $BackupBranch -- 'Assets/StreamingAssets/sfx') |
    Where-Object { $_ -match 'UI_(cash|finish_chord|reveal|scroll|select)' }

$restorePaths = New-Object System.Collections.Generic.List[string]
foreach ($p in ($quarantine + $sfx)) {
    if ([string]::IsNullOrWhiteSpace($p)) { continue }
    if (Test-BackupPath $BackupBranch $p) {
        [void]$restorePaths.Add($p)
    }
}

if ($restorePaths.Count -eq 0) {
    throw ("No Careers paths found on backup branch {0}" -f $BackupBranch)
}

git checkout $BackupBranch -- @($restorePaths.ToArray())
Write-Host ("Restored {0} paths from {1}" -f $restorePaths.Count, $BackupBranch)

Write-Step 'MenuScene / MainMenu wiring'
$needUnityEditor = $false
$backupHasCareersButton = $false
if (Test-BackupPath $BackupBranch 'Assets/Scenes/MenuScene.unity') {
    git grep -q -E 'CareersM|m_MethodName: Career' "${BackupBranch}" -- 'Assets/Scenes/MenuScene.unity' 2>$null
    if ($LASTEXITCODE -eq 0) {
        $backupHasCareersButton = $true
    }
}

if (-not $menuWiringChanged -and $backupHasCareersButton) {
    # Upstream left these files alone - keep your CareersM-wired copies from backup
    $restoreWiring = @()
    foreach ($f in $menuWiringFiles) {
        if (Test-BackupPath $BackupBranch $f) {
            $restoreWiring += $f
        }
    }
    if ($restoreWiring.Count -gt 0) {
        git checkout $BackupBranch -- @restoreWiring
        Write-Host '  Restored Careers-wired MenuScene/MainMenu/MenuEntry from backup' -ForegroundColor Green
        Write-Host '  (upstream did not touch them - Unity editor rewire NOT needed)'
        $needUnityEditor = $false
    } else {
        Write-Host '  WARNING: backup missing menu wiring files - Unity editor rewire needed' -ForegroundColor Yellow
        $needUnityEditor = $true
    }
} elseif (-not $backupHasCareersButton) {
    Write-Host '  Backup MenuScene has no CareersM/Career() wiring - Unity editor rewire needed' -ForegroundColor Yellow
    $needUnityEditor = $true
} else {
    Write-Host '  Keeping fresh upstream MenuScene/MainMenu (they changed upstream)' -ForegroundColor Yellow
    Write-Host '  Your Careers button must be re-applied in Unity'
    $needUnityEditor = $true
}

Write-Step 'Ensure CoverFlow tag exists'
$tagPath = 'ProjectSettings/TagManager.asset'
$tagText = Get-Content -Raw $tagPath
if ($tagText -notmatch '(?m)^\s*-\s*CoverFlow\s*$') {
    if (Test-BackupPath $BackupBranch $tagPath) {
        git checkout $BackupBranch -- $tagPath
        Write-Host '  Restored TagManager.asset from backup (includes CoverFlow)'
    } else {
        Write-Host '  WARNING: CoverFlow tag missing and no backup TagManager.' -ForegroundColor Yellow
    }
} else {
    Write-Host '  CoverFlow tag already present'
}

Write-Step 'Patch MenuManager.cs (keep Content, append Career enums)'
$menuManagerPath = 'Assets/Script/Menu/MenuManager.cs'
$mm = Get-Content -Raw $menuManagerPath
if ($mm -match 'CareerCareerModern') {
    Write-Host '  Career enums already present'
} else {
    if ($mm -notmatch '(?m)^\s*Content,\s*$') {
        throw 'Upstream MenuManager.cs no longer has Content enum - update the patch script.'
    }
    $mm = [regex]::Replace(
        $mm,
        '(?m)^(\s*)Content,\s*$',
        @'
$1Content,
$1Career,
$1CareerCareerModern,
$1CareerGig,
$1CareerGigModern,
$1CareerGigClassic,
$1CareerTrophy,
'@,
        1
    )
    Set-Content -Path $menuManagerPath -Value $mm -NoNewline
    Write-Host '  appended Career* enums after Content'
}
Ensure-TextContains $menuManagerPath 'CareerCareerModern' 'MenuManager career enums'
Ensure-TextContains $menuManagerPath 'Content,' 'MenuManager upstream Content'

Write-Step 'Patch MainMenu.cs (keep Content, add Career)'
$mainMenuPath = 'Assets/Script/Menu/Main/MainMenu.cs'
$main = Get-Content -Raw $mainMenuPath
if ($main -match 'public void Career\(') {
    Write-Host '  Career method already present'
} else {
    if ($main -notmatch 'public void Content\(') {
        throw 'Upstream MainMenu.cs no longer has Content method - update the patch script.'
    }
    $careerMethod = @'

        public void Career()
        {
            MenuManager.Instance.PushMenu(MenuManager.Menu.CareerCareerModern);
        }
'@
    $pattern = '(?s)(public void Content\(\)\s*\{.*?\})'
    if ($main -notmatch $pattern) {
        throw 'Could not locate Content method body to insert Career after.'
    }
    $main = [regex]::Replace($main, $pattern, ('${1}' + $careerMethod), 1)
    Set-Content -Path $mainMenuPath -Value $main -NoNewline
    Write-Host '  added Career method'
}
Ensure-TextContains $mainMenuPath 'public void Content(' 'MainMenu Content'
Ensure-TextContains $mainMenuPath 'public void Career(' 'MainMenu Career'

Write-Step 'Patch Navigator.cs (PeekScheme / PopScheme overload)'
$navPath = 'Assets/Script/Menu/Navigation/Navigator.cs'
$nav = Get-Content -Raw $navPath
if ($nav -match 'PeekScheme\(') {
    Write-Host '  PeekScheme already present'
} else {
    $navInsert = @'

        /// <summary>
        /// Returns the scheme currently on top of the stack, or null if the stack is empty.
        /// </summary>
        public NavigationScheme PeekScheme()
        {
            return _schemeStack.Count > 0 ? _schemeStack.Peek() : null;
        }

        /// <summary>
        /// Pops the top scheme only if it is the expected instance.
        /// Returns true if a scheme was popped.
        /// </summary>
        public bool PopScheme(NavigationScheme expected)
        {
            if (_schemeStack.Count > 0 && ReferenceEquals(_schemeStack.Peek(), expected))
            {
                PopScheme();
                return true;
            }

            return false;
        }
'@
    if ($nav -notmatch 'public void PopAllSchemes\(\)') {
        throw 'Could not find PopAllSchemes in Navigator.cs'
    }
    $nav = [regex]::Replace($nav, '(public void PopAllSchemes\(\))', ($navInsert + "`r`n`r`n        " + '${1}'), 1)
    Set-Content -Path $navPath -Value $nav -NoNewline
    Write-Host '  added PeekScheme / PopScheme(expected)'
}
Ensure-TextContains $navPath 'PeekScheme(' 'Navigator PeekScheme'

Write-Step 'Patch ViewObject.cs null-guards'
$viewPath = 'Assets/Script/Menu/Common/ListMenu/ViewObject.cs'
$view = Get-Content -Raw $viewPath
if ($view -match 'if \(_canvasGroup != null\)') {
    Write-Host '  ViewObject null-guards already present'
} elseif (Test-BackupPath $BackupBranch $viewPath) {
    git checkout $BackupBranch -- $viewPath
    Write-Host '  Restored ViewObject.cs from backup (null-guards)'
} else {
    Write-Host '  WARNING: Could not restore ViewObject null-guards' -ForegroundColor Yellow
}

Write-Step 'Patch ScoreContainer / ScoreDatabase date-filtered APIs'
$scoreContainer = 'Assets/Script/Scores/ScoreContainer.cs'
$scoreDb = 'Assets/Script/Scores/ScoreDatabase.cs'
$sc = Get-Content -Raw $scoreContainer
if ($sc -match 'GetGameRecordsUpToDate') {
    Write-Host '  ScoreContainer UpToDate APIs already present'
} else {
    $scoreContainerMethods = @'

        // Date-filtered queries (for campaign trophy screen)
        public static List<GameRecord> GetGameRecordsUpToDate(DateTime cutoffDate)
        {
            try
            {
                return _db.QueryGameRecordsUpToDate(cutoffDate);
            }
            catch (Exception e)
            {
                YargLogger.LogException(e, "Failed to load date-filtered GameRecords.");
            }
            return null;
        }

        public static List<PlayerScoreRecord> GetScoresByPlayerIdUpToDate(Guid playerId, DateTime cutoffDate)
        {
            try
            {
                return _db.QueryPlayerScoresUpToDate(playerId, cutoffDate);
            }
            catch (Exception e)
            {
                YargLogger.LogException(e, "Failed to load date-filtered PlayerScoreRecords.");
            }
            return null;
        }

        public static GameRecord GetBandHighScoreUpToDate(HashWrapper songChecksum, DateTime cutoffDate)
        {
            try
            {
                return _db.QueryBandSongHighScoreUpToDate(songChecksum, cutoffDate);
            }
            catch (Exception e)
            {
                YargLogger.LogException(e, "Failed to load date-filtered band high score.");
            }
            return null;
        }
'@
    Add-MethodsToClass $scoreContainer 'GetGameRecordsUpToDate' $scoreContainerMethods 'ScoreContainer UpToDate'
}

$sd = Get-Content -Raw $scoreDb
if ($sd -match 'QueryGameRecordsUpToDate') {
    Write-Host '  ScoreDatabase UpToDate APIs already present'
} else {
    $scoreDbMethods = @'

        #region Date-filtered queries (for campaign trophy screen)

        public List<GameRecord> QueryGameRecordsUpToDate(DateTime cutoffDate)
        {
            return Query<GameRecord>(
                @"SELECT * FROM GameRecords
                WHERE Date <= ?
                ORDER BY Date DESC",
                cutoffDate
            );
        }

        public List<PlayerScoreRecord> QueryPlayerScoresUpToDate(Guid playerId, DateTime cutoffDate)
        {
            return Query<PlayerScoreRecord>(
                @"SELECT ps.* FROM PlayerScores ps
                INNER JOIN GameRecords gr ON ps.GameRecordId = gr.Id
                WHERE ps.PlayerId = ?
                    AND ps.IsReplay = 0
                    AND gr.Date <= ?",
                playerId,
                cutoffDate
            );
        }

        public GameRecord QueryBandSongHighScoreUpToDate(HashWrapper songChecksum, DateTime cutoffDate)
        {
            return FindWithQuery<GameRecord>(
                @"SELECT * FROM GameRecords
                WHERE SongChecksum = ?
                    AND PlayedWithReplay = 0
                    AND Date <= ?
                ORDER BY BandScore DESC
                LIMIT 1",
                songChecksum.HashBytes,
                cutoffDate
            );
        }

        #endregion
'@
    Add-MethodsToClass $scoreDb 'QueryGameRecordsUpToDate' $scoreDbMethods 'ScoreDatabase UpToDate' '(?m)^    \}\s*\r?\n\s*public static class SortOrderingExtensions'
}

Write-Step 'Submodule update'
git submodule update --init --recursive --force

Write-Step 'Patch YARG.Core Careers SFX samples (append-only)'
$audioEnums = 'YARG.Core/YARG.Core/Audio/AudioEnums.cs'
$audioHelpers = 'YARG.Core/YARG.Core/Audio/AudioHelpers.cs'
if (-not (Test-Path $audioEnums)) {
    Write-Host '  WARNING: AudioEnums.cs not found - skip SFX patch' -ForegroundColor Yellow
} else {
    $enums = Get-Content -Raw $audioEnums
    if ($enums -match 'ScrollCantScroll') {
        Write-Host '  Careers SfxSample values already present'
    } else {
        if ($enums -notmatch 'Rewind\s*\r?\n\s*\}') {
            throw 'Could not find Rewind end of SfxSample enum - update the patch script.'
        }
        $enums = [regex]::Replace(
            $enums,
            '(Rewind)\s*(\r?\n\s*\})',
            @'
$1,
        // Careers / CoverFlow UI (append only - ordinals index SfxSamples[])
        ScrollCantScroll,
        ScrollMain,
        Cash,
        UIReveal,$2
'@,
            1
        )
        Set-Content -Path $audioEnums -Value $enums -NoNewline
        Write-Host '  appended SfxSample Careers values'
    }

    $helpers = Get-Content -Raw $audioHelpers
    if ($helpers -match 'SfxSample\.ScrollCantScroll') {
        Write-Host '  Careers SfxSamples helper entries already present'
    } else {
        if ($helpers -notmatch 'SfxSample\.Rewind') {
            throw 'Could not find SfxSample.Rewind in AudioHelpers - update the patch script.'
        }
        $helpers = [regex]::Replace(
            $helpers,
            '(new Sample<SfxSample>\(SfxSample\.Rewind,[^\r\n]+\)),',
            @'
$1,
            // Careers / CoverFlow UI (files live in StreamingAssets/sfx/)
            new Sample<SfxSample>(SfxSample.ScrollCantScroll, "UI_scroll0_cantscroll"),
            new Sample<SfxSample>(SfxSample.ScrollMain, "UI_scroll1_main"),
            new Sample<SfxSample>(SfxSample.Cash, "UI_cash"),
            new Sample<SfxSample>(SfxSample.UIReveal, "UI_reveal"),
'@,
            1
        )
        Set-Content -Path $audioHelpers -Value $helpers -NoNewline
        Write-Host '  appended AudioHelpers Careers SFX entries'
    }
}

Write-Host ''
Write-Host 'DONE (code side).' -ForegroundColor Green
Write-Host ("Backup branch: {0}" -f $BackupBranch)
Write-Host ''
Write-Host 'Working tree is modified but NOT committed. Review with: git status' -ForegroundColor White
Write-Host ''

if ($needUnityEditor) {
    Write-Host '==============================================================' -ForegroundColor Yellow
    Write-Host ' UNITY EDITOR REWIRE REQUIRED' -ForegroundColor Yellow
    Write-Host '==============================================================' -ForegroundColor Yellow
    Write-Host 'Upstream changed MenuScene and/or MainMenu prefabs, OR your'
    Write-Host 'backup is missing the CareersM button. After Unity compiles:'
    Write-Host ''
    Write-Host '  1. Open Assets/Scenes/MenuScene.unity'
    Write-Host '  2. Tools > YARG Careers > Apply Menu Hooks After Upstream Update'
    Write-Host '  3. Save the scene (Ctrl+S)'
    Write-Host '  4. Play Mode: confirm Careers opens'
    Write-Host ''
    if (-not $SkipConfirm) {
        $editorAck = Read-Host "Type EDITOR to confirm you will run the Unity menu item (or SKIP to continue without)"
        if ($editorAck -ne 'EDITOR' -and $editorAck -ne 'SKIP') {
            Write-Host 'Unrecognized response - continuing. Remember to run the Unity tool if Careers is missing.' -ForegroundColor Yellow
        } elseif ($editorAck -eq 'EDITOR') {
            Write-Host 'Got it - run the Unity tool before you commit.' -ForegroundColor Green
        } else {
            Write-Host 'Skipping editor ack - Careers button may be missing until you rewire.' -ForegroundColor Yellow
        }
    }
} else {
    Write-Host '==============================================================' -ForegroundColor Green
    Write-Host ' UNITY EDITOR REWIRE NOT NEEDED' -ForegroundColor Green
    Write-Host '==============================================================' -ForegroundColor Green
    Write-Host 'MenuScene / MainMenu / MenuEntry were unchanged upstream, and'
    Write-Host 'your CareersM-wired copies were restored from backup.'
    Write-Host 'Still open Unity once to confirm compile + Play Mode Careers.'
    Write-Host ''
}

Write-Host 'When happy:'
Write-Host '       git add -A'
Write-Host '       git commit -m "Update from upstream/dev and re-apply Careers mod"'
Write-Host '       git push origin custom-campaign --force-with-lease'
Write-Host ''
Write-Host ("If something went wrong:  git reset --hard {0}" -f $BackupBranch)
