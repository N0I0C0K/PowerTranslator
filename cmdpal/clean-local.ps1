#requires -Version 7
<#
.SYNOPSIS
    Removes the local Command Palette development certificate artifacts.

.DESCRIPTION
    Removes cmdpal/.local-cert and the matching certificate from LocalMachine
    TrustedPeople (with a UAC prompt), plus stale current-user trust entries.
    It deliberately does not uninstall the app.
#>

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$certificateDir = Join-Path $PSScriptRoot '.local-cert'
$certificatePath = Join-Path $certificateDir 'PowerTranslator.Local.cer'
$trustManager = Join-Path $PSScriptRoot 'manage-local-cert-trust.ps1'

if (Test-Path $certificatePath) {
    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
    if (-not (Test-Path $trustManager)) {
        throw "找不到证书信任管理脚本：$trustManager"
    }
    $trustArgs = @(
        '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$trustManager`"",
        '-Action', 'Remove', '-CertificatePath', "`"$certificatePath`""
    )
    $trustProcess = Start-Process -FilePath 'pwsh' -Verb RunAs -Wait -PassThru -ArgumentList $trustArgs
    if ($trustProcess.ExitCode -ne 0) {
        throw "移除 LocalMachine\\TrustedPeople 信任失败，退出码 $($trustProcess.ExitCode)。"
    }
    foreach ($store in @('Root', 'TrustedPeople')) {
        & "$env:SystemRoot\System32\certutil.exe" -user -delstore $store $certificate.Thumbprint *> $null
    }
    $legacyCertificates = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object {
            $_.Subject -eq $certificate.Subject -and
            $_.FriendlyName -eq 'PowerTranslator Local Development'
        }
    foreach ($legacyCertificate in $legacyCertificates) {
        & "$env:SystemRoot\System32\certutil.exe" -user -delstore My $legacyCertificate.Thumbprint *> $null
    }
    Write-Host '已移除本地开发证书信任。' -ForegroundColor DarkGray
}

if (Test-Path $certificateDir) {
    Remove-Item -Path $certificateDir -Recurse -Force
    Write-Host '已删除 cmdpal/.local-cert。' -ForegroundColor DarkGray
} else {
    Write-Host '未找到本地开发证书文件，无需清理。' -ForegroundColor DarkGray
}
