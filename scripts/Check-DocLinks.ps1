<#
.SYNOPSIS
    Verifies that every relative markdown link under documentation/ resolves to an existing file.
.DESCRIPTION
    Scans all *.md files under the documentation/ folder for [text](target.md) links,
    resolves each relative target against the linking file's folder, and reports any
    link that does not resolve to a real file. External (http/https) links are skipped.
    Links inside fenced code blocks (``` ... ```) are skipped too, since those are
    illustrative examples (the Superseded-status template in the ADR index),
    not real cross-references meant to be validated.
    The documentation/process/ folder is skipped entirely: its files are document
    templates that illustrate links to fictitious documents, not real cross-references.
    Exits with code 1 if any broken link is found, so it can be used as a CI gate.
#>

$ErrorActionPreference = 'Stop'

$root = Join-Path $PSScriptRoot '..\documentation' | Resolve-Path
$processDir = Join-Path $root.Path 'process'
$files = Get-ChildItem -Path $root -Recurse -Filter '*.md' |
    Where-Object { $_.FullName -notlike "$processDir\*" }
$pattern = '\[([^\]]+)\]\(([^)]+\.md)\)'
$codeFencePattern = '(?s)```.*?```'
$broken = @()

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $content = [regex]::Replace($content, $codeFencePattern, '')
    foreach ($match in [regex]::Matches($content, $pattern)) {
        $target = $match.Groups[2].Value
        if ($target -match '^https?://') { continue }

        $resolved = Join-Path (Split-Path $file.FullName -Parent) $target
        $resolved = [System.IO.Path]::GetFullPath($resolved)

        if (-not (Test-Path $resolved)) {
            $broken += [PSCustomObject]@{
                File = $file.FullName.Substring($root.Path.Length + 1)
                Link = $target
            }
        }
    }
}

if ($broken.Count -eq 0) {
    Write-Output "All documentation links resolve correctly."
    exit 0
}

Write-Output "Broken documentation links found:"
$broken | Format-Table -AutoSize
exit 1
