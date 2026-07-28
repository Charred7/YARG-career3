# Finish leftover steps after a partial update-from-upstream.ps1 run.
# Safe to re-run: skips patches that are already present.

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $RepoRoot

function Write-Step([string]$Message) {
    Write-Host ''
    Write-Host ("=== {0} ===" -f $Message) -ForegroundColor Cyan
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

    $endMatch = [regex]::Match($text, '(?ms)(?<classClose>^[ \t]+\})\s*\r?\n\}[ \t]*\r?\n?\s*\z')
    if (-not $endMatch.Success) {
        throw ("Could not find class/namespace closing braces for {0} ({1})" -f $Label, $Path)
    }
    $updated = $text.Insert($endMatch.Groups['classClose'].Index, $MethodsToAdd + "`r`n")
    Set-Content -Path $Path -Value $updated -NoNewline
    Write-Host ("  patched: {0}" -f $Label)
}

Write-Step 'Finish score patches + submodule update'

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
git submodule update --init --recursive

Write-Host ''
Write-Host 'FINISH DONE.' -ForegroundColor Green
Write-Host 'Next: open Unity, MenuScene, then Tools > YARG Careers > Apply Menu Hooks After Upstream Update'
Write-Host 'Backup branch if you need to rollback: backup/pre-update-20260727-183713'
