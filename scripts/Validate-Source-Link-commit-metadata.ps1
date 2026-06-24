<#
    Runtime behavior:
    - When no -Version value is supplied, the script validates the currently documented
      default package version below.
    - For future releases, pass the released package version explicitly, for example:
        ./scripts/Validate-Source-Link-commit-metadata.ps1 -Version '1.3.0'
    - Use -KeepArtifacts only when troubleshooting. By default, downloaded and extracted
      NuGet package verification artifacts are cleaned up before the script exits.
#>

[CmdletBinding()]
param(
    [string]$Version = '1.2.1',
    [switch]$KeepArtifacts
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$expectedRepositoryUrl = 'https://github.com/cdcavell/AsiBackbone'

$packageIds = @(
    'CDCavell.AsiBackbone.Core',
    'CDCavell.AsiBackbone.DependencyInjection',
    'CDCavell.AsiBackbone.Storage.InMemory',
    'CDCavell.AsiBackbone.EntityFrameworkCore',
    'CDCavell.AsiBackbone.AspNetCore',
    'CDCavell.AsiBackbone.Testing',
    'CDCavell.AsiBackbone.Templates',
    'CDCavell.AsiBackbone.Analyzers',
    'CDCavell.AsiBackbone.OpenTelemetry',
    'CDCavell.AsiBackbone.Signing.LocalDevelopment',
    'CDCavell.AsiBackbone.Signing.ManagedKey'
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

        $versionNode = $metadataNode.SelectSingleNode("./*[local-name()='version']")
        $repositoryNode = $metadataNode.SelectSingleNode("./*[local-name()='repository']")

        $packageVersion = if ($null -eq $versionNode) { $null } else { $versionNode.InnerText.Trim() }
        $repositoryType = if ($null -eq $repositoryNode) { $null } else { $repositoryNode.GetAttribute('type') }
        $repositoryUrl = if ($null -eq $repositoryNode) { $null } else { $repositoryNode.GetAttribute('url') }
        $repositoryCommit = if ($null -eq $repositoryNode) { $null } else { $repositoryNode.GetAttribute('commit') }

        [pscustomobject]@{
            PackageId = $packageId
            Version = $packageVersion
            RepositoryType = $repositoryType
            RepositoryUrl = $repositoryUrl
            RepositoryCommit = $repositoryCommit
            HasRepositoryCommit = -not [string]::IsNullOrWhiteSpace($repositoryCommit)
            CommitLength = if ([string]::IsNullOrWhiteSpace($repositoryCommit)) { 0 } else { $repositoryCommit.Length }
            DllCount = @(Get-ChildItem -LiteralPath $packageDirectory -Filter '*.dll' -Recurse).Count
            PdbCount = @(Get-ChildItem -LiteralPath $packageDirectory -Filter '*.pdb' -Recurse).Count
        }
    }

    $results | Format-Table -AutoSize

    $failures = @(
        $results | Where-Object {
            $_.Version -ne $Version -or
            $_.RepositoryType -ne 'git' -or
            $_.RepositoryUrl -ne $expectedRepositoryUrl -or
            -not $_.HasRepositoryCommit
        }
    )

    if ($failures.Count -gt 0) {
        Write-Host ''
        Write-Host 'Source Link repository metadata validation failed:' -ForegroundColor Red
        $failures | Format-Table -AutoSize
        $exitCode = 1
    }
    else {
        Write-Host ''
        Write-Host "Source Link repository metadata validation passed for AsiBackbone $Version packages." -ForegroundColor Green
    }
}
catch {
    Write-Error $_
    $exitCode = 1
}
finally {
    if ($KeepArtifacts) {
        Write-Host "Keeping verification artifacts at: $workRoot" -ForegroundColor Yellow
    }
    elseif (Test-Path -LiteralPath $workRoot) {
        Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

exit $exitCode
