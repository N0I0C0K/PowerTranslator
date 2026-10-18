#requires -Version 7
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Path).Path)
try {
    if (@($archive.Entries | Where-Object { -not $_.FullName.StartsWith('Translator/', [StringComparison]::Ordinal) }).Count -gt 0) {
        throw 'The ZIP must contain only the Translator top-level directory.'
    }
    $manifest = $archive.GetEntry('Translator/plugin.json')
    $assembly = $archive.GetEntry('Translator/Translator.dll')
    if ($null -eq $manifest -or $null -eq $assembly) {
        throw 'The ZIP must contain Translator/plugin.json and Translator/Translator.dll.'
    }

    $reader = [IO.StreamReader]::new($manifest.Open())
    try { $version = ($reader.ReadToEnd() | ConvertFrom-Json).Version }
    finally { $reader.Dispose() }

    $temporaryAssembly = New-TemporaryFile
    try {
        [IO.Compression.ZipFileExtensions]::ExtractToFile($assembly, $temporaryAssembly.FullName, $true)
        $fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($temporaryAssembly.FullName).FileVersion
        if ([version]$fileVersion -ne [version]$version) {
            throw "DLL version $fileVersion does not match plugin.json version $version."
        }
    } finally { Remove-Item -LiteralPath $temporaryAssembly.FullName -Force }

    Write-Output "Verified $Path (version $version)."
} finally { $archive.Dispose() }
