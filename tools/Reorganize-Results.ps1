[CmdletBinding()]
param(
    [string]$ResultsRoot = '',
    [string]$ArchiveLabel = '2026-04-pre-reorg',
    [switch]$DeleteSourceAfterVerification
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RunSelection {
    [PSCustomObject]@{
        Active = @(
            'EdgeRunner_V2_Gap01_04',
            'EdgeRunnerBasic_Gap02_BC_01',
            'EdgeRunnerBasic_Gap03_Bridge01',
            'EdgeRunnerBasic_Gap03_From50092_02',
            'EdgeRunnerBasic_GapDropBridge_02',
            'EdgeRunnerBasic_GapJump_BC_05',
            'EdgeRunnerBasic_SafeDrop_01'
        )
        Archive = @(
            'EdgeRunner_V2_Test_01',
            'EdgeRunner_V2_Gap01_01',
            'EdgeRunner_V2_Gap01_02',
            'EdgeRunner_V2_Gap01_03',
            'EdgeRunnerBasic_01',
            'EdgeRunnerBasic_gap_01',
            'EdgeRunnerBasic_gap_02',
            'EdgeRunnerBasic_Gap03_01',
            'EdgeRunnerBasic_Gap03_02',
            'EdgeRunnerBasic_Gap03_From50092_01',
            'EdgeRunnerBasic_GapDropBridge_01',
            'EdgeRunnerBasic_Height01',
            'EdgeRunnerBasic_jumpPenalty_01',
            'EdgeRunnerBasic_MiniLevel01_fromScratch_jump003_01',
            'EdgeRunnerBasic_MiniLevel01_groundAhead_01',
            'EdgeRunnerBasic_MiniLevel01_jump003_01',
            'EdgeRunnerBasic_MiniLevel01_unnecessaryJumpPenalty_01',
            'EdgeRunnerBasic_MiniLevel01Easy_jump003_01',
            'EdgeRunnerBasic_MiniLevel02_01',
            'EdgeRunnerBasic_MiniLevel03_01',
            'EdgeRunnerBasic_MiniLevel04_01',
            'EdgeRunnerBasic_MiniLevel04_dropAware_01',
            'EdgeRunner_V2_Flat01_01',
            'EdgeRunner_V2_Flat01_NoJump_01'
        )
    }
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Get-FileSummary {
    param([Parameter(Mandatory = $true)][string]$Path)

    $files = @(Get-ChildItem -LiteralPath $Path -Recurse -File -ErrorAction SilentlyContinue)
    [PSCustomObject]@{
        FileCount = $files.Count
        TotalBytes = ($files | Measure-Object -Property Length -Sum).Sum
    }
}

function Sync-Directory {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    Ensure-Directory -Path $Destination

    $arguments = @(
        $Source,
        $Destination,
        '/E',
        '/R:1',
        '/W:1',
        '/NFL',
        '/NDL',
        '/NJH',
        '/NJS',
        '/NP'
    )

    $null = & robocopy @arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ge 8) {
        throw "robocopy failed for '$Source' -> '$Destination' with exit code $exitCode"
    }
}

function Remove-DirectoryIfPresent {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ResultsRoot)) {
    $ResultsRoot = Join-Path $scriptRoot '..\results'
}
$ResultsRoot = [System.IO.Path]::GetFullPath($ResultsRoot)
$activeRoot = Join-Path $ResultsRoot 'active'
$archiveRoot = Join-Path $ResultsRoot ('archive\' + $ArchiveLabel)
$manifestPath = Join-Path $ResultsRoot ('reorg-manifest-' + $ArchiveLabel + '.csv')

Ensure-Directory -Path $ResultsRoot
Ensure-Directory -Path $activeRoot
Ensure-Directory -Path $archiveRoot

$selection = Get-RunSelection
$manifest = New-Object System.Collections.Generic.List[object]

foreach ($runName in $selection.Active) {
    $source = Join-Path $ResultsRoot $runName
    $destination = Join-Path $activeRoot $runName

    if (-not (Test-Path -LiteralPath $source)) {
        $manifest.Add([PSCustomObject]@{
            Run = $runName
            Bucket = 'active'
            SourceExists = $false
            Destination = $destination
            SourceFiles = 0
            DestinationFiles = 0
            Status = 'missing-source'
        })
        continue
    }

    Sync-Directory -Source $source -Destination $destination
    $sourceSummary = Get-FileSummary -Path $source
    $destSummary = Get-FileSummary -Path $destination
    $status = if ($sourceSummary.FileCount -eq $destSummary.FileCount -and $sourceSummary.TotalBytes -eq $destSummary.TotalBytes) { 'copied-ok' } else { 'copy-mismatch' }

    if ($DeleteSourceAfterVerification -and $status -eq 'copied-ok') {
        Remove-DirectoryIfPresent -Path $source
        $status = 'copied-and-deleted-source'
    }

    $manifest.Add([PSCustomObject]@{
        Run = $runName
        Bucket = 'active'
        SourceExists = $true
        Destination = $destination
        SourceFiles = $sourceSummary.FileCount
        DestinationFiles = $destSummary.FileCount
        Status = $status
    })
}

foreach ($runName in $selection.Archive) {
    $source = Join-Path $ResultsRoot $runName
    $destination = Join-Path $archiveRoot $runName

    if (-not (Test-Path -LiteralPath $source)) {
        $manifest.Add([PSCustomObject]@{
            Run = $runName
            Bucket = 'archive'
            SourceExists = $false
            Destination = $destination
            SourceFiles = 0
            DestinationFiles = 0
            Status = 'missing-source'
        })
        continue
    }

    Sync-Directory -Source $source -Destination $destination
    $sourceSummary = Get-FileSummary -Path $source
    $destSummary = Get-FileSummary -Path $destination
    $status = if ($sourceSummary.FileCount -eq $destSummary.FileCount -and $sourceSummary.TotalBytes -eq $destSummary.TotalBytes) { 'copied-ok' } else { 'copy-mismatch' }

    if ($DeleteSourceAfterVerification -and $status -eq 'copied-ok') {
        Remove-DirectoryIfPresent -Path $source
        $status = 'copied-and-deleted-source'
    }

    $manifest.Add([PSCustomObject]@{
        Run = $runName
        Bucket = 'archive'
        SourceExists = $true
        Destination = $destination
        SourceFiles = $sourceSummary.FileCount
        DestinationFiles = $destSummary.FileCount
        Status = $status
    })
}

$manifest | Export-Csv -LiteralPath $manifestPath -NoTypeInformation -Encoding UTF8
$manifest | Sort-Object Bucket, Run | Format-Table -AutoSize

Write-Host ''
Write-Host "Manifest written to: $manifestPath"
Write-Host 'Review the copied runs before deleting originals.'
