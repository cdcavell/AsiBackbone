[CmdletBinding()]
param(
    [ValidateSet('Inventory', 'Enforce')]
    [string]$Mode = 'Inventory',

    [string[]]$Project = @(),

    [string]$Configuration = 'Release',

    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,

    [string]$ProjectListPath = 'eng/xml-docs/public-api-projects.txt',

    [string]$EnforcedProjectListPath = 'eng/xml-docs/staged-enforcement-projects.txt',

    [string]$OutputPath = 'artifacts/xml-docs/cs1591-inventory.md',

    [switch]$NoRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepositoryPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path $RepositoryRoot $Path)
}

function Get-RepositoryRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    try {
        return [System.IO.Path]::GetRelativePath($RepositoryRoot, $Path)
    }
    catch {
        return $Path
    }
}

function Read-ProjectList {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $resolvedPath = Resolve-RepositoryPath -Path $Path

    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        return @()
    }

    return @(
        Get-Content -LiteralPath $resolvedPath |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_ -and -not $_.StartsWith('#') }
    )
}

function Escape-MarkdownTableValue {
    param(
        [AllowNull()]
        [string]$Value
    )

    if ($null -eq $Value) {
        return ''
    }

    return $Value.Replace('|', '\|').Replace("`r", ' ').Replace("`n", ' ')
}

$resolvedRepositoryRoot = Resolve-Path -LiteralPath $RepositoryRoot
$RepositoryRoot = $resolvedRepositoryRoot.Path

if ($Project.Count -eq 0) {
    if ($Mode -eq 'Enforce') {
        $Project = Read-ProjectList -Path $EnforcedProjectListPath
    }
    else {
        $Project = Read-ProjectList -Path $ProjectListPath
    }
}

$findings = New-Object System.Collections.Generic.List[object]
$projectSummaries = New-Object System.Collections.Generic.List[object]
$buildFailures = New-Object System.Collections.Generic.List[object]
$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ssZ')
$enforceXmlDocs = if ($Mode -eq 'Enforce') { 'true' } else { 'false' }

foreach ($projectPath in $Project) {
    $absoluteProjectPath = Resolve-RepositoryPath -Path $projectPath

    if (-not (Test-Path -LiteralPath $absoluteProjectPath)) {
        throw "Project path '$projectPath' does not exist."
    }

    $displayProjectPath = Get-RepositoryRelativePath -Path $absoluteProjectPath
    Write-Host "Validating XML documentation coverage for $displayProjectPath"

    $buildArguments = @(
        'build',
        $absoluteProjectPath,
        '--configuration',
        $Configuration,
        '--nologo',
        '/p:ContinuousIntegrationBuild=true',
        '/p:GenerateDocumentationFile=true',
        '/p:AsiBackboneSuppressMissingXmlDocs=false',
        "/p:AsiBackboneEnforceMissingXmlDocs=$enforceXmlDocs"
    )

    if ($NoRestore) {
        $buildArguments += '--no-restore'
    }

    $projectOutput = & dotnet @buildArguments 2>&1
    $exitCode = $LASTEXITCODE
    $cs1591Count = 0
    $pattern = '^(?<path>.+?\.(?:cs|vb))\((?<line>\d+),(?<column>\d+)\):\s+(?<level>warning|error)\s+CS1591:\s+(?<message>.+?)(?:\s+\[(?<project>.+?\.csproj)\])?$'

    foreach ($line in $projectOutput) {
        $text = [string]$line

        if ($text -match $pattern) {
            $cs1591Count++
            $sourcePath = $Matches['path']
            $displaySourcePath = if ([System.IO.Path]::IsPathRooted($sourcePath)) {
                Get-RepositoryRelativePath -Path $sourcePath
            }
            else {
                $sourcePath
            }

            $findings.Add([pscustomobject]@{
                Project = $displayProjectPath
                File = $displaySourcePath
                Line = $Matches['line']
                Column = $Matches['column']
                Level = $Matches['level']
                Message = $Matches['message']
            })
        }
    }

    $projectSummaries.Add([pscustomobject]@{
        Project = $displayProjectPath
        CS1591 = $cs1591Count
        ExitCode = $exitCode
    })

    if ($exitCode -ne 0) {
        $buildFailures.Add([pscustomobject]@{
            Project = $displayProjectPath
            ExitCode = $exitCode
        })
    }
}

$outputAbsolutePath = Resolve-RepositoryPath -Path $OutputPath
$outputDirectory = Split-Path -Parent $outputAbsolutePath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$report = New-Object System.Collections.Generic.List[string]
$report.Add('# Public API XML Documentation Inventory')
$report.Add('')
$report.Add("Generated: $generatedAt")
$report.Add('')
$report.Add("Mode: `$Mode`")
$report.Add('')
$report.Add('This report is produced by `scripts/Validate-XmlDocumentation.ps1` with `CS1591` unsuppressed for the selected public package projects. Inventory mode records gaps without treating them as release-blocking. Enforce mode treats `CS1591` as an error for projects listed in `eng/xml-docs/staged-enforcement-projects.txt` or passed with `-Project`.')
$report.Add('')

if ($Project.Count -eq 0) {
    $report.Add('No projects were configured for this mode.')
    $report.Add('')
}
else {
    $report.Add('## Project summary')
    $report.Add('')
    $report.Add('| Project | CS1591 count | Build exit code |')
    $report.Add('| --- | ---: | ---: |')

    foreach ($summary in $projectSummaries) {
        $projectName = Escape-MarkdownTableValue -Value $summary.Project
        $report.Add('| {0} | {1} | {2} |' -f $projectName, $summary.CS1591, $summary.ExitCode)
    }

    $report.Add('')
}

if ($findings.Count -gt 0) {
    $report.Add('## CS1591 findings')
    $report.Add('')
    $report.Add('| Project | File | Line | Member/message |')
    $report.Add('| --- | --- | ---: | --- |')

    foreach ($finding in $findings) {
        $projectName = Escape-MarkdownTableValue -Value $finding.Project
        $fileName = Escape-MarkdownTableValue -Value $finding.File
        $message = Escape-MarkdownTableValue -Value $finding.Message
        $report.Add('| {0} | {1} | {2} | {3} |' -f $projectName, $fileName, $finding.Line, $message)
    }

    $report.Add('')
}
else {
    $report.Add('No `CS1591` gaps were reported for the selected projects.')
    $report.Add('')
}

if ($buildFailures.Count -gt 0) {
    $report.Add('## Build failures')
    $report.Add('')
    $report.Add('| Project | Exit code |')
    $report.Add('| --- | ---: |')

    foreach ($failure in $buildFailures) {
        $projectName = Escape-MarkdownTableValue -Value $failure.Project
        $report.Add('| {0} | {1} |' -f $projectName, $failure.ExitCode)
    }

    $report.Add('')
}

$report | Set-Content -LiteralPath $outputAbsolutePath -Encoding utf8
$relativeOutputPath = Get-RepositoryRelativePath -Path $outputAbsolutePath
Write-Host "XML documentation report written to $relativeOutputPath"

if ($buildFailures.Count -gt 0) {
    throw 'One or more XML documentation validation builds failed.'
}

if ($Mode -eq 'Enforce' -and $findings.Count -gt 0) {
    throw "Found $($findings.Count) CS1591 public API XML documentation gaps in enforced projects."
}

if ($findings.Count -gt 0) {
    Write-Warning "Found $($findings.Count) CS1591 public API XML documentation gaps."
}
