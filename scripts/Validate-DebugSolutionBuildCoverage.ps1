[CmdletBinding()]
param(
    [Parameter()]
    [string] $SolutionPath = "AsiBackbone.slnx"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $SolutionPath)) {
    throw "Solution file was not found: $SolutionPath"
}

$resolvedSolutionPath = (Resolve-Path -LiteralPath $SolutionPath).Path
[xml] $solution = Get-Content -LiteralPath $resolvedSolutionPath -Raw

$allowedDebugExclusions = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
[void] $allowedDebugExclusions.Add("benchmarks/AsiBackbone.Benchmarks/AsiBackbone.Benchmarks.csproj")
[void] $allowedDebugExclusions.Add("benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet/AsiBackbone.Benchmarks.BenchmarkDotNet.csproj")
[void] $allowedDebugExclusions.Add("samples/PlainAspNetCoreHost/AsiBackbone.Samples.PlainAspNetCoreHost.csproj")

$excludedProjects = @(
    $solution.SelectNodes("//Project[Build[@Solution='Debug|*' and @Project='false']]") |
        ForEach-Object { $_.Path }
)

$unexpectedExclusions = @(
    $excludedProjects |
        Where-Object { -not $allowedDebugExclusions.Contains($_) } |
        Sort-Object
)

if ($unexpectedExclusions.Count -gt 0) {
    Write-Error (@"
Unexpected Debug solution build exclusions were found in $SolutionPath.

First-party package and test projects must participate in Debug solution builds. Remove the exclusion or document and add a reviewed allowance in this script.

Unexpected exclusions:
$($unexpectedExclusions -join [Environment]::NewLine)
"@.Trim())
}

$allowedPresent = @(
    $excludedProjects |
        Where-Object { $allowedDebugExclusions.Contains($_) } |
        Sort-Object
)

Write-Host "Debug solution build coverage validation passed."
Write-Host "Allowed intentional Debug exclusions: $($allowedPresent.Count)"
foreach ($project in $allowedPresent) {
    Write-Host "  - $project"
}
