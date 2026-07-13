#requires -Version 7
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Install', 'Remove')]
    [string]$Action,

    [Parameter(Mandatory)]
    [string]$CertificatePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw '需要管理员权限以修改 LocalMachine\\TrustedPeople。'
}

$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($CertificatePath)
$store = 'Cert:\LocalMachine\TrustedPeople'

if ($Action -eq 'Install') {
    $existing = Get-ChildItem $store |
        Where-Object { $_.Thumbprint -eq $certificate.Thumbprint } |
        Select-Object -First 1
    if (-not $existing) {
        Import-Certificate -FilePath $CertificatePath -CertStoreLocation $store | Out-Null
    }
} else {
    Get-ChildItem $store |
        Where-Object { $_.Thumbprint -eq $certificate.Thumbprint } |
        Remove-Item -Force
}
