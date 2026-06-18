[CmdletBinding()]
param(
    [switch]$Execute,
    [string]$ProjectRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
} else {
    $ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
}

$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$ProjectParent = Split-Path -Parent $ProjectRoot
$Timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$ArchiveRoot = Join-Path $ProjectParent ("EdgeRunner_Arquivo_{0}" -f $Timestamp)
$Mode = if ($Execute) { 'EXECUTE' } else { 'DRY-RUN' }

$ArchiveCategories = @('Models', 'Reports', 'Results', 'Logs')
$ArchiveItems = New-Object System.Collections.Generic.List[object]
$ArchiveKeys = @{}
$KeptItems = New-Object System.Collections.Generic.List[object]
$ImportantItems = New-Object System.Collections.Generic.List[object]
$Warnings = New-Object System.Collections.Generic.List[string]

function Convert-ToProjectRelativePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetFullPath($ProjectRoot)

    if (-not $root.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $root = $root + [System.IO.Path]::DirectorySeparatorChar
    }

    $rootUri = New-Object System.Uri($root)
    $pathUri = New-Object System.Uri($fullPath)
    $relativeUri = $rootUri.MakeRelativeUri($pathUri)
    return [System.Uri]::UnescapeDataString($relativeUri.ToString()).Replace('/', '\')
}

function Test-PathInside {
    param(
        [Parameter(Mandatory = $true)][string]$ChildPath,
        [Parameter(Mandatory = $true)][string]$ParentPath
    )

    $child = [System.IO.Path]::GetFullPath($ChildPath)
    $parent = [System.IO.Path]::GetFullPath($ParentPath)

    if (-not $parent.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $parent = $parent + [System.IO.Path]::DirectorySeparatorChar
    }

    return $child.StartsWith($parent, [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-SafeSourcePath {
    param([Parameter(Mandatory = $true)][string]$SourcePath)

    $fullSource = [System.IO.Path]::GetFullPath($SourcePath)
    if (-not (Test-PathInside -ChildPath $fullSource -ParentPath $ProjectRoot)) {
        throw "Unsafe source outside project root: $fullSource"
    }

    $relative = Convert-ToProjectRelativePath -Path $fullSource
    $blockedPrefixes = @(
        'Assets\EdgeRunner\Scripts\',
        'Assets\EdgeRunner\Scenes\',
        'Assets\EdgeRunner\Prefabs\',
        'Assets\EdgeRunner\Materials\',
        'Assets\EdgeRunner\Sprites\',
        'Assets\EdgeRunner\Config\',
        'ProjectSettings\',
        'Packages\'
    )

    foreach ($prefix in $blockedPrefixes) {
        if ($relative.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Blocked protected project path: $relative"
        }
    }
}

function Add-KeptItem {
    param(
        [Parameter(Mandatory = $true)][string]$Category,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Reason
    )

    $KeptItems.Add([PSCustomObject]@{
        Category = $Category
        Path = $Path
        Reason = $Reason
    }) | Out-Null
}

function Add-ImportantItem {
    param(
        [Parameter(Mandatory = $true)][string]$Category,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Reason
    )

    $ImportantItems.Add([PSCustomObject]@{
        Category = $Category
        Path = $Path
        Reason = $Reason
    }) | Out-Null
}

function Add-ArchiveItem {
    param(
        [Parameter(Mandatory = $true)][string]$Category,
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$Reason,
        [string]$ArchiveSubPath = ''
    )

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        return
    }

    $item = Get-Item -LiteralPath $SourcePath -Force
    $fullSource = [System.IO.Path]::GetFullPath($item.FullName)
    Assert-SafeSourcePath -SourcePath $fullSource

    $key = $fullSource.ToLowerInvariant()
    if ($ArchiveKeys.ContainsKey($key)) {
        return
    }

    if ([string]::IsNullOrWhiteSpace($ArchiveSubPath)) {
        $ArchiveSubPath = Convert-ToProjectRelativePath -Path $fullSource
    }

    $destination = Join-Path (Join-Path $ArchiveRoot $Category) $ArchiveSubPath
    $type = if ($item.PSIsContainer) { 'Directory' } else { 'File' }

    $ArchiveItems.Add([PSCustomObject]@{
        Category = $Category
        Type = $type
        Source = $fullSource
        RelativeSource = Convert-ToProjectRelativePath -Path $fullSource
        Destination = $destination
        ArchiveSubPath = $ArchiveSubPath
        Reason = $Reason
    }) | Out-Null
    $ArchiveKeys[$key] = $true
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Move-ArchiveItem {
    param([Parameter(Mandatory = $true)]$Item)

    if (Test-Path -LiteralPath $Item.Destination) {
        throw "Destination already exists: $($Item.Destination)"
    }

    $parent = Split-Path -Parent $Item.Destination
    Ensure-Directory -Path $parent
    Move-Item -LiteralPath $Item.Source -Destination $Item.Destination
}

function Write-ItemTable {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][object[]]$Items
    )

    Write-Host ''
    Write-Host $Title
    Write-Host ('-' * $Title.Length)

    if ($Items.Count -eq 0) {
        Write-Host '(none)'
        return
    }

    foreach ($item in $Items) {
        if ($null -ne $item.PSObject.Properties['RelativeSource']) {
            Write-Host "- [$($item.Category)] $($item.Type): $($item.RelativeSource) -- $($item.Reason)"
        } elseif ($null -ne $item.PSObject.Properties['Path']) {
            Write-Host "- [$($item.Category)] $($item.Path) -- $($item.Reason)"
        } else {
            Write-Host "- $item"
        }
    }
}

function Get-EvaluationReportFiles {
    $files = New-Object System.Collections.Generic.List[object]
    $seen = @{}
    $rg = Get-Command rg -ErrorAction SilentlyContinue

    if ($null -ne $rg) {
        Push-Location -LiteralPath $ProjectRoot
        try {
            $relativePaths = @(& $rg.Source --files -g 'EdgeRunnerEval_*.txt' -g 'EdgeRunnerEval_*.csv')
            foreach ($relativePath in $relativePaths) {
                $full = Join-Path $ProjectRoot $relativePath
                if (Test-Path -LiteralPath $full) {
                    $resolved = (Resolve-Path -LiteralPath $full).Path
                    $key = $resolved.ToLowerInvariant()
                    if (-not $seen.ContainsKey($key)) {
                        $files.Add((Get-Item -LiteralPath $resolved -Force)) | Out-Null
                        $seen[$key] = $true
                    }
                }
            }
        } finally {
            Pop-Location
        }
    } else {
        $all = @(Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File -Filter 'EdgeRunnerEval_*' -ErrorAction SilentlyContinue)
        foreach ($file in $all) {
            if ($file.Name -match '^EdgeRunnerEval_.*\.(txt|csv)$') {
                $key = $file.FullName.ToLowerInvariant()
                if (-not $seen.ContainsKey($key)) {
                    $files.Add($file) | Out-Null
                    $seen[$key] = $true
                }
            }
        }
    }

    return @($files | Sort-Object FullName)
}

function Test-ImportantReportName {
    param([Parameter(Mandatory = $true)][string]$Name)

    if ($Name -match 'EasyStart02.*Eval50') {
        return $true
    }

    if ($Name -match 'EasyPlus01.*Eval50_FIXED') {
        return $true
    }

    if ($Name -match 'EasyBridge01') {
        return $true
    }

    return $false
}

function Add-ModelNameToKeep {
    param(
        [Parameter(Mandatory = $true)][hashtable]$KeepNames,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Reason
    )

    $KeepNames[$Name.ToLowerInvariant()] = $Reason
    $metaName = "$Name.meta"
    $KeepNames[$metaName.ToLowerInvariant()] = "$Reason meta"
}

function Write-SummaryFile {
    param([Parameter(Mandatory = $true)][object[]]$MovedItems)

    $summaryPath = Join-Path $ArchiveRoot 'CleanupSummary.md'
    $lines = New-Object System.Collections.Generic.List[string]

    $lines.Add('# EdgeRunner ML Cleanup Summary') | Out-Null
    $lines.Add('') | Out-Null
    $lines.Add("- Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')") | Out-Null
    $lines.Add("- Project root: $ProjectRoot") | Out-Null
    $lines.Add("- Archive root: $ArchiveRoot") | Out-Null
    $lines.Add("- Mode: EXECUTE") | Out-Null
    $lines.Add('- Results rule: whitelist. Only results\ER_V5_GoalRelative_V5Gen_EasyStart02 and results\ER_V5_GoalRelative_V5Gen_EasyBridge01 remain active; every other direct folder under results is moved to Results.') | Out-Null
    $lines.Add('') | Out-Null

    $lines.Add('## Moved Items') | Out-Null
    $lines.Add('') | Out-Null
    if ($MovedItems.Count -eq 0) {
        $lines.Add('(none)') | Out-Null
    } else {
        $lines.Add('| Category | Type | Source | Destination | Reason |') | Out-Null
        $lines.Add('| --- | --- | --- | --- | --- |') | Out-Null
        foreach ($item in $MovedItems) {
            $lines.Add("| $($item.Category) | $($item.Type) | $($item.RelativeSource) | $($item.Destination) | $($item.Reason) |") | Out-Null
        }
    }

    $lines.Add('') | Out-Null
    $lines.Add('## Kept Active / Important Items') | Out-Null
    $lines.Add('') | Out-Null

    $keptCombined = @($KeptItems) + @($ImportantItems)
    if ($keptCombined.Count -eq 0) {
        $lines.Add('(none recorded)') | Out-Null
    } else {
        $lines.Add('| Category | Path | Reason |') | Out-Null
        $lines.Add('| --- | --- | --- |') | Out-Null
        foreach ($item in $keptCombined) {
            $lines.Add("| $($item.Category) | $($item.Path) | $($item.Reason) |") | Out-Null
        }
    }

    if ($Warnings.Count -gt 0) {
        $lines.Add('') | Out-Null
        $lines.Add('## Warnings') | Out-Null
        $lines.Add('') | Out-Null
        foreach ($warning in $Warnings) {
            $lines.Add("- $warning") | Out-Null
        }
    }

    Set-Content -LiteralPath $summaryPath -Value $lines -Encoding UTF8
}

# Results
$resultsRoot = Join-Path $ProjectRoot 'results'
$activeResultNames = @(
    'ER_V5_GoalRelative_V5Gen_EasyStart02',
    'ER_V5_GoalRelative_V5Gen_EasyBridge01'
)

if (Test-Path -LiteralPath $resultsRoot) {
    $resultDirs = @(Get-ChildItem -LiteralPath $resultsRoot -Directory -Force | Sort-Object Name)
    foreach ($dir in $resultDirs) {
        if ($activeResultNames -contains $dir.Name) {
            Add-KeptItem -Category 'Results' -Path (Convert-ToProjectRelativePath -Path $dir.FullName) -Reason 'kept by results whitelist'
            continue
        }

        Add-ArchiveItem -Category 'Results' -SourcePath $dir.FullName -ArchiveSubPath $dir.Name -Reason 'not in results whitelist'
    }
} else {
    $Warnings.Add("Missing results folder: $resultsRoot") | Out-Null
}

# Models
$candidatesRoot = Join-Path $ProjectRoot 'Assets\EdgeRunner\ML\Models\Candidates'
$keepModelNames = @{}
$v4Ambiguous = $false

if (Test-Path -LiteralPath $candidatesRoot) {
    $modelFiles = @(Get-ChildItem -LiteralPath $candidatesRoot -File -Force | Where-Object { $_.Name -match '\.onnx(\.meta)?$' } | Sort-Object Name)
    Add-ModelNameToKeep -KeepNames $keepModelNames -Name 'ER_V5_GoalRelative_V5Gen_EasyStart02_Final_Test.onnx' -Reason 'active V5 EasyStart02 final model'
    Add-ModelNameToKeep -KeepNames $keepModelNames -Name 'ER_V5_GoalRelative_V5Gen_EasyBridge01_Final_Test.onnx' -Reason 'active V5 EasyBridge01 final model'

    $v4Onnx = @($modelFiles | Where-Object { $_.Name -match '^ER_V4_.*\.onnx$' })
    $v4FallbackCandidates = @($v4Onnx | Where-Object {
        $_.Name -match '^ER_V4_.*EasierPlus.*Coyote.*Final.*\.onnx$' -or
        $_.Name -match '^ER_V4_.*Fallback.*\.onnx$'
    })

    if ($v4FallbackCandidates.Count -eq 1) {
        Add-ModelNameToKeep -KeepNames $keepModelNames -Name $v4FallbackCandidates[0].Name -Reason 'clear V4 fallback model'
    } elseif ($v4Onnx.Count -gt 0) {
        $v4Ambiguous = $true
        foreach ($v4File in $modelFiles | Where-Object { $_.Name -match '^ER_V4_' }) {
            $keepModelNames[$v4File.Name.ToLowerInvariant()] = 'V4 model kept because fallback is ambiguous'
        }
        if ($v4FallbackCandidates.Count -eq 0) {
            $Warnings.Add('No clear V4 fallback model found; all V4 models will be kept.') | Out-Null
        } else {
            $Warnings.Add('Multiple possible V4 fallback models found; all V4 models will be kept.') | Out-Null
        }
    }

    foreach ($requiredModel in @(
        'ER_V5_GoalRelative_V5Gen_EasyStart02_Final_Test.onnx',
        'ER_V5_GoalRelative_V5Gen_EasyBridge01_Final_Test.onnx'
    )) {
        $modelPath = Join-Path $candidatesRoot $requiredModel
        $metaPath = Join-Path $candidatesRoot "$requiredModel.meta"

        if (-not (Test-Path -LiteralPath $modelPath)) {
            $Warnings.Add("Requested model is missing: $requiredModel") | Out-Null
        }

        if (-not (Test-Path -LiteralPath $metaPath)) {
            $Warnings.Add("Requested model meta is missing: $requiredModel.meta") | Out-Null
        }
    }

    foreach ($modelFile in $modelFiles) {
        $nameKey = $modelFile.Name.ToLowerInvariant()
        if ($keepModelNames.ContainsKey($nameKey)) {
            Add-KeptItem -Category 'Models' -Path (Convert-ToProjectRelativePath -Path $modelFile.FullName) -Reason $keepModelNames[$nameKey]
            continue
        }

        if ($v4Ambiguous -and $modelFile.Name -match '^ER_V4_') {
            Add-KeptItem -Category 'Models' -Path (Convert-ToProjectRelativePath -Path $modelFile.FullName) -Reason 'V4 kept because fallback is ambiguous'
            continue
        }

        Add-ArchiveItem -Category 'Models' -SourcePath $modelFile.FullName -Reason 'old candidate model' -ArchiveSubPath (Convert-ToProjectRelativePath -Path $modelFile.FullName)
    }
} else {
    $Warnings.Add("Missing model candidate folder: $candidatesRoot") | Out-Null
}

# Reports
$reportFiles = @(Get-EvaluationReportFiles)
$bridgeReports = @($reportFiles | Where-Object { $_.Name -match 'EasyBridge01' })
if ($bridgeReports.Count -eq 0) {
    $Warnings.Add('No EasyBridge01 EdgeRunnerEval report found.') | Out-Null
}

foreach ($report in $reportFiles) {
    $relativeReport = Convert-ToProjectRelativePath -Path $report.FullName
    if (Test-ImportantReportName -Name $report.Name) {
        Add-ImportantItem -Category 'Reports' -Path $relativeReport -Reason 'important evaluation report kept in project'
        continue
    }

    Add-ArchiveItem -Category 'Reports' -SourcePath $report.FullName -Reason 'old EdgeRunnerEval report' -ArchiveSubPath $relativeReport
}

$keptOutput = @($KeptItems | Sort-Object Category, Path | Select-Object Category, Path, Reason)
$importantOutput = @($ImportantItems | Sort-Object Category, Path | Select-Object Category, Path, Reason)
$archiveOutput = @($ArchiveItems | Sort-Object Category, RelativeSource | Select-Object Category, Type, RelativeSource, Reason)

Write-Host "ArchiveOldMLArtifacts.ps1"
Write-Host "Mode: $Mode"
Write-Host "Project: $ProjectRoot"
Write-Host "Archive target: $ArchiveRoot"
Write-Host 'Results rule: whitelist; only EasyStart02 and EasyBridge01 stay active.'

Write-ItemTable -Title 'Will stay in active project' -Items $keptOutput
Write-ItemTable -Title 'Important reports highlighted and kept' -Items $importantOutput
Write-ItemTable -Title 'Will be moved to archive' -Items $archiveOutput

if ($Warnings.Count -gt 0) {
    Write-Host ''
    Write-Host 'Warnings'
    Write-Host '--------'
    foreach ($warning in $Warnings) {
        Write-Host "- $warning"
    }
}

if (-not $Execute) {
    Write-Host ''
    Write-Host 'Dry-run only. Nothing was moved.'
    Write-Host "Run with -Execute to create the archive and move the listed items."
    exit 0
}

Ensure-Directory -Path $ArchiveRoot
foreach ($category in $ArchiveCategories) {
    Ensure-Directory -Path (Join-Path $ArchiveRoot $category)
}

$movedItems = New-Object System.Collections.Generic.List[object]
foreach ($archiveItem in $ArchiveItems) {
    Move-ArchiveItem -Item $archiveItem
    $movedItems.Add($archiveItem) | Out-Null
}

Write-SummaryFile -MovedItems @($movedItems)

Write-Host ''
Write-Host "Archive complete: $ArchiveRoot"
Write-Host "Summary: $(Join-Path $ArchiveRoot 'CleanupSummary.md')"
