[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$validatorPath = Join-Path $PSScriptRoot 'Validate-DocumentationReleaseClaims.ps1'
$fixturesRoot = Join-Path $repoRoot 'eng/test-fixtures/documentation-release-claims'
$pwshFileName = if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' }
$pwshPath = Join-Path $PSHOME $pwshFileName

foreach ($requiredPath in @($validatorPath, $fixturesRoot, $pwshPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required documentation release-claim test path was not found: $requiredPath"
    }
}

function Invoke-ValidationFixture {
    param([string]$FixtureName)

    $fixturePath = Join-Path $fixturesRoot $FixtureName
    if (-not (Test-Path -LiteralPath $fixturePath -PathType Container)) {
        throw "Documentation release-claim fixture was not found: $fixturePath"
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $pwshPath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.ArgumentList.Add('-NoLogo')
    $startInfo.ArgumentList.Add('-NoProfile')
    $startInfo.ArgumentList.Add('-File')
    $startInfo.ArgumentList.Add($validatorPath)
    $startInfo.ArgumentList.Add('-RepositoryRoot')
    $startInfo.ArgumentList.Add($fixturePath)

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "Failed to start PowerShell for fixture '$FixtureName'."
    }

    $standardOutput = $process.StandardOutput.ReadToEnd()
    $standardError = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    return [pscustomobject]@{
        FixtureName = $FixtureName
        ExitCode = $process.ExitCode
        Output = ($standardOutput + $standardError).Trim()
    }
}

function Assert-FixturePasses {
    param([string]$FixtureName)

    $result = Invoke-ValidationFixture $FixtureName
    if ($result.ExitCode -ne 0) {
        throw "Fixture '$FixtureName' should pass but exited with $($result.ExitCode).`n$($result.Output)"
    }
}

Assert-FixturePasses 'valid-current'
Assert-FixturePasses 'historical-mention'
Assert-FixturePasses 'excluded-historical'

$staleResult = Invoke-ValidationFixture 'stale-current'
if ($staleResult.ExitCode -eq 0) {
    throw "Fixture 'stale-current' should fail but passed.`n$($staleResult.Output)"
}

foreach ($expectedText in @('README.md:1', "release claim '2.x'", "Expected '3.x'")) {
    if (-not $staleResult.Output.Contains($expectedText, [System.StringComparison]::Ordinal)) {
        throw "Fixture 'stale-current' output did not contain '$expectedText'.`n$($staleResult.Output)"
    }
}

Write-Host 'Documentation release-claim validator fixtures passed.'
