<#
    Runtime behavior:
    - When no -Version value is supplied, the script validates the currently documented
      default package version below.
    - For future releases, pass the released package version explicitly, for example:
        ./scripts/Validate-Source-Link-commit-metadata.ps1 -Version '2.1.0'
    - Use -KeepArtifacts only when troubleshooting. By default, downloaded and extracted
      NuGet package verification artifacts are cleaned up before the script exits.
#>

[CmdletBinding()]
param(
    [string]$Version = '2.0.1',
    [switch]$KeepArtifacts
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$expectedRepositoryUrl = 'https://github.com/cdcavell/AsiBackbone'

$packageIds = @(
    'AsiBackbone.Core',
    'AsiBackbone.DependencyInjection',
    'AsiBackbone.Storage.InMemory',
    'AsiBackbone.EntityFrameworkCore',
    'AsiBackbone.AspNetCore',
    'AsiBackbone.Testing',
    'AsiBackbone.Templates',
    'AsiBackbone.Analyzers',
    'AsiBackbone.OpenTelemetry',
    'AsiBackbone.Signing.LocalDevelopment',
    'AsiBackbone.Signing.ManagedKey'
)

$workRoot = Join-Path $PWD "nuget-sourcelink-check-$Version"
$exitCode = 0
$results = @()

try {
    Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $workRoot | Out-Null

    $results = foreach ($packageId in $packageIds) {
        $idLower = $packageId.ToLowerInvariant()

        $packageDirectory = Join-Path $workRoot $packageId
        New-Item -ItemType Directory -Path $packageDirectory | Out-Null

        $nupkgPath = Join-Path $workRoot "$packageId.$Version.nupkg"
        $zipPath = Join-Path $workRoot "$packageId.$Version.zip"

        $packageUrl = "https://api.nuget.org/v3-flatcontainer/$idLower/$Version/$idLower.$Version.nupkg"

        Invoke-WebRequest -Uri $packageUrl -OutFile $nupkgPath
        Copy-Item -LiteralPath $nupkgPath -Destination $zipPath -Force
        Expand-Archive -LiteralPath $zipPath -DestinationPath $packageDirectory -Force

        $nuspecPath = Get-ChildItem -LiteralPath $packageDirectory -Filter '*.nuspec' -Recurse |
            Select-Object -First 1

        if ($null -eq $nuspecPath) {
            throw "No .nuspec found in $packageId $Version."
        }

        [xml]$nuspec = Get-Content -LiteralPath $nuspecPath.FullName -Raw

        $metadataNode = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
        if ($null -eq $metadataNode) {
            throw "No nuspec metadata node found in $packageId $Version."
        }

        $repositoryNode = $metadataNode.SelectSingleNode("*[local-name()='repository']")
        if ($null -eq $repositoryNode) {
            throw "No repository metadata found in $packageId $Version."
        }

        $repositoryType = $repositoryNode.GetAttribute('type')
        $repositoryUrl = $repositoryNode.GetAttribute('url')
        $repositoryCommit = $repositoryNode.GetAttribute('commit')

        if ($repositoryType -ne 'git') {
            throw "$packageId repository type expected 'git' but found '$repositoryType'."
        }

        if ($repositoryUrl -ne $expectedRepositoryUrl) {
            throw "$packageId repository URL expected '$expectedRepositoryUrl' but found '$repositoryUrl'."
        }

        if ([string]::IsNullOrWhiteSpace($repositoryCommit)) {
            throw "$packageId repository commit metadata was empty."
        }

        [pscustomobject]@{
            Package = $packageId
            Version = $Version
            RepositoryType = $repositoryType
            RepositoryUrl = $repositoryUrl
            RepositoryCommit = $repositoryCommit
        }
    }
}
catch {
    $exitCode = 1
    Write-Host "::error::$($_.Exception.Message)"
}
finally {
    if ($results.Count -gt 0) {
        $results | Format-Table -AutoSize
    }

    if (-not $KeepArtifacts) {
        Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($exitCode -ne 0) {
    exit $exitCode
}

Write-Host "Source Link repository metadata validation passed for AsiBackbone $Version."
