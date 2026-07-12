[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$ConfigurationPath = 'eng/documentation-release-claims.json'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}

$repoRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
$directoryBuildPropsPath = Join-Path $repoRoot 'Directory.Build.props'

if (-not (Test-Path -LiteralPath $directoryBuildPropsPath -PathType Leaf)) {
    throw "Directory.Build.props was not found under repository root '$repoRoot'."
}

[xml]$directoryBuildProps = Get-Content -LiteralPath $directoryBuildPropsPath -Raw
$versionPrefixNodes = @(
    $directoryBuildProps.Project.PropertyGroup.ChildNodes |
        Where-Object {
            $_.NodeType -eq [System.Xml.XmlNodeType]::Element -and
            $_.Name -eq 'VersionPrefix'
        }
)

if ($versionPrefixNodes.Count -eq 0) {
    throw "VersionPrefix was not found in '$directoryBuildPropsPath'."
}

$versionPrefix = $versionPrefixNodes[0].InnerText.Trim()
$versionPrefixMatch = [regex]::Match(
    $versionPrefix,
    '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$',
    [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)

if (-not $versionPrefixMatch.Success) {
    throw "VersionPrefix '$versionPrefix' must use MAJOR.MINOR.PATCH format."
}

$currentMajor = $versionPrefixMatch.Groups['major'].Value
$currentMinor = $versionPrefixMatch.Groups['minor'].Value
$currentMajorLine = "$currentMajor.x"
$currentMinorLine = "$currentMajor.$currentMinor.x"

$excludedPathPatterns = @()
$allowedClaims = @()
$configurationFilePath = if ([System.IO.Path]::IsPathRooted($ConfigurationPath)) {
    $ConfigurationPath
}
else {
    Join-Path $repoRoot $ConfigurationPath
}

if (Test-Path -LiteralPath $configurationFilePath -PathType Leaf) {
    try {
        $configuration = Get-Content -LiteralPath $configurationFilePath -Raw | ConvertFrom-Json
    }
    catch {
        throw "Documentation release-claim configuration could not be parsed: $($_.Exception.Message)"
    }

    if ($null -ne $configuration.PSObject.Properties['excludedPaths']) {
        $excludedPathPatterns = @($configuration.excludedPaths | ForEach-Object {
            $pattern = [string]$_
            if ([string]::IsNullOrWhiteSpace($pattern)) {
                throw 'Documentation release-claim excludedPaths entries must not be blank.'
            }

            $pattern.Replace('\', '/')
        })
    }

    if ($null -ne $configuration.PSObject.Properties['allowedClaims']) {
        $allowedClaims = @($configuration.allowedClaims | ForEach-Object {
            $pathProperty = $_.PSObject.Properties['path']
            $linePatternProperty = $_.PSObject.Properties['linePattern']
            $reasonProperty = $_.PSObject.Properties['reason']

            if ($null -eq $pathProperty -or [string]::IsNullOrWhiteSpace([string]$pathProperty.Value)) {
                throw 'Each allowedClaims entry must define a nonblank path.'
            }

            if ($null -eq $linePatternProperty -or [string]::IsNullOrWhiteSpace([string]$linePatternProperty.Value)) {
                throw 'Each allowedClaims entry must define a nonblank linePattern.'
            }

            if ($null -eq $reasonProperty -or [string]::IsNullOrWhiteSpace([string]$reasonProperty.Value)) {
                throw 'Each allowedClaims entry must define a nonblank reason.'
            }

            try {
                $lineRegex = [regex]::new(
                    [string]$linePatternProperty.Value,
                    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor
                        [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
            }
            catch {
                throw "Allowed claim linePattern '$($linePatternProperty.Value)' is not a valid regular expression. $($_.Exception.Message)"
            }

            [pscustomobject]@{
                Path = ([string]$pathProperty.Value).Replace('\', '/')
                LineRegex = $lineRegex
                Reason = [string]$reasonProperty.Value
            }
        })
    }
}

function Test-ExcludedPath {
    param([string]$RelativePath)

    foreach ($pattern in $excludedPathPatterns) {
        if ($RelativePath -like $pattern) {
            return $true
        }
    }

    return $false
}

function Test-AllowedClaim {
    param(
        [string]$RelativePath,
        [string]$Line
    )

    foreach ($allowedClaim in $allowedClaims) {
        if ($RelativePath -like $allowedClaim.Path -and $allowedClaim.LineRegex.IsMatch($Line)) {
            return $true
        }
    }

    return $false
}

function Get-ExpectedVersionToken {
    param([string]$VersionToken)

    if ($VersionToken -match '^\d+\.[xX]$') {
        return $currentMajorLine
    }

    if ($VersionToken -match '^\d+\.\d+\.[xX]$') {
        return $currentMinorLine
    }

    return $versionPrefix
}

function ConvertTo-GitHubCommandValue {
    param([string]$Value)

    return $Value.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
}

$documentationFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
$seenFiles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

function Add-DocumentationFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return
    }

    $file = Get-Item -LiteralPath $Path
    if ($seenFiles.Add($file.FullName)) {
        $documentationFiles.Add($file)
    }
}

foreach ($topLevelDocument in @('README.md', 'CONTRIBUTING.md', 'GOVERNANCE.md', 'SECURITY.md')) {
    Add-DocumentationFile (Join-Path $repoRoot $topLevelDocument)
}

Add-DocumentationFile (Join-Path $repoRoot 'docs/index.md')

$articlesRoot = Join-Path $repoRoot 'docs/articles'
if (Test-Path -LiteralPath $articlesRoot -PathType Container) {
    Get-ChildItem -LiteralPath $articlesRoot -Recurse -Filter '*.md' -File |
        ForEach-Object { Add-DocumentationFile $_.FullName }
}

$sourceRoot = Join-Path $repoRoot 'src'
if (Test-Path -LiteralPath $sourceRoot -PathType Container) {
    Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter 'README.md' -File |
        ForEach-Object { Add-DocumentationFile $_.FullName }
}

if ($documentationFiles.Count -eq 0) {
    throw "No documentation files were found under repository root '$repoRoot'."
}

$versionPattern = '\d+\.(?:[xX]|\d+\.(?:[xX]|\d+))'
$regexOptions = [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor
    [System.Text.RegularExpressions.RegexOptions]::CultureInvariant
$claimPatterns = @(
    [regex]::new(
        '\b(?:current|active|canonical)\b[^\r\n]{0,80}?[`*_~]*v?(?<version>' + $versionPattern + ')[`*_~]*',
        $regexOptions),
    [regex]::new(
        '[`*_~]*v?(?<version>' + $versionPattern + ')[`*_~]*[^\r\n]{0,80}?\b(?:is|remains|represents|defines|establishes)\s+(?:the\s+)?(?:current|active|canonical)\b',
        $regexOptions),
    [regex]::new(
        '\bstable\b[^\r\n]{0,50}?[`*_~]*v?(?<version>' + $versionPattern + ')[`*_~]*[^\r\n]{0,50}?\b(?:package\s+family|package\s+lineup|release\s+line|stable\s+line|major\s+release)\b',
        $regexOptions)
)
$historicalContextPattern = [regex]::new(
    '\b(?:historical|original|initial|previous|prior|superseded|final stable patch|releases? expanded|release established)\b',
    $regexOptions)

$failures = [System.Collections.Generic.List[object]]::new()
$reportedMatches = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

foreach ($documentationFile in @($documentationFiles | Sort-Object FullName)) {
    $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $documentationFile.FullName).Replace('\', '/')
    if (Test-ExcludedPath $relativePath) {
        continue
    }

    $lines = @(Get-Content -LiteralPath $documentationFile.FullName)
    $insideCodeFence = $false

    for ($lineIndex = 0; $lineIndex -lt $lines.Count; $lineIndex++) {
        $line = [string]$lines[$lineIndex]

        if ($line -match '^\s*(?:```|~~~)') {
            $insideCodeFence = -not $insideCodeFence
            continue
        }

        if ($insideCodeFence -or $historicalContextPattern.IsMatch($line)) {
            continue
        }

        foreach ($claimPattern in $claimPatterns) {
            foreach ($claimMatch in $claimPattern.Matches($line)) {
                $versionGroup = $claimMatch.Groups['version']
                $versionToken = $versionGroup.Value
                $expectedToken = Get-ExpectedVersionToken $versionToken

                if ([string]::Equals($versionToken, $expectedToken, [System.StringComparison]::OrdinalIgnoreCase)) {
                    continue
                }

                if (Test-AllowedClaim $relativePath $line) {
                    continue
                }

                $lineNumber = $lineIndex + 1
                $matchKey = "$relativePath|$lineNumber|$($versionGroup.Index)|$versionToken"
                if (-not $reportedMatches.Add($matchKey)) {
                    continue
                }

                $displayLine = $line.Trim()
                if ($displayLine.Length -gt 240) {
                    $displayLine = $displayLine.Substring(0, 237) + '...'
                }

                $failures.Add([pscustomobject]@{
                    Path = $relativePath
                    LineNumber = $lineNumber
                    Version = $versionToken
                    Expected = $expectedToken
                    Text = $displayLine
                })
            }
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Documentation release-claim validation failed for VersionPrefix '$versionPrefix'."

    foreach ($failure in $failures) {
        $message = "$($failure.Path):$($failure.LineNumber): release claim '$($failure.Version)' is stale in '$($failure.Text)'. Expected '$($failure.Expected)' from Directory.Build.props VersionPrefix '$versionPrefix'."
        Write-Host "::error file=$($failure.Path),line=$($failure.LineNumber)::$((ConvertTo-GitHubCommandValue $message))"
        Write-Host "- $message"
    }

    exit 1
}

Write-Host "Documentation release-claim validation passed for VersionPrefix '$versionPrefix' ($currentMajorLine); scanned $($documentationFiles.Count) file(s)."
