#requires -Version 7
<#
.SYNOPSIS
    Builds, self-signs, and installs the Command Palette MSIX for local E2E testing.

.DESCRIPTION
    The script creates (or reuses) a code-signing certificate in cmdpal/.local-cert,
    trusts its public certificate in LocalMachine/TrustedPeople (with a UAC prompt),
    and uses it to sign the freshly built MSIX. Its PFX password is encrypted
    with the current user's DPAPI.
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [ValidateSet('x64', 'arm64', 'ARM64')]
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$projectDir = Join-Path $PSScriptRoot 'PowerTranslatorExtension'
$project = Join-Path $projectDir 'PowerTranslatorExtension.csproj'
$manifest = Join-Path $projectDir 'Package.appxmanifest'
$certificateDir = Join-Path $PSScriptRoot '.local-cert'
$certificatePath = Join-Path $certificateDir 'PowerTranslator.Local.cer'
$pfxPath = Join-Path $certificateDir 'PowerTranslator.Local.pfx'
$passwordPath = Join-Path $certificateDir 'PowerTranslator.Local.pfx.password'
$trustManager = Join-Path $PSScriptRoot 'manage-local-cert-trust.ps1'

$Platform = if ($Platform -ieq 'arm64') { 'ARM64' } else { 'x64' }
$runtime = if ($Platform -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }

if (-not (Test-Path $project)) {
    throw "找不到项目: $project"
}

# Keep this lookup aligned with build-store.ps1 so local installs use the same
# Visual Studio MSBuild toolchain as the packaged builds.
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw "找不到 vswhere.exe。请安装 Visual Studio 2022（含 MSBuild 和 Windows App SDK 工作负载）。"
}
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
    Select-Object -First 1
if (-not $msbuild) {
    throw 'vswhere 未找到 MSBuild。'
}

# The running COM server locks files in the build output. Ask before stopping it
# so an E2E install never terminates a process without the user's consent.
$runningExtensions = @(Get-Process -Name 'PowerTranslatorExtension' -ErrorAction SilentlyContinue |
    Select-Object Id, ProcessName, StartTime)
if ($runningExtensions.Count -gt 0) {
    Write-Host "`n检测到正在运行的旧扩展：" -ForegroundColor Yellow
    $runningExtensions | Format-Table -AutoSize | Out-Host
    $confirmation = Read-Host '是否停止以上进程后继续构建？输入 y 确认'
    if ($confirmation -notmatch '^(?i:y|yes)$') {
        throw '已取消：旧扩展仍在运行。'
    }

    foreach ($extension in $runningExtensions) {
        # Re-check the PID and start time to avoid acting on a recycled PID.
        $currentProcess = Get-Process -Id $extension.Id -ErrorAction SilentlyContinue
        if ($currentProcess -and
            $currentProcess.ProcessName -eq $extension.ProcessName -and
            $currentProcess.StartTime -eq $extension.StartTime) {
            Stop-Process -Id $extension.Id -Force
        }
    }

    foreach ($extension in $runningExtensions) {
        Wait-Process -Id $extension.Id -Timeout 10 -ErrorAction SilentlyContinue
    }
}

Write-Host "`n=== 构建 $Platform / $Configuration ===" -ForegroundColor Cyan
& $msbuild $project '/restore' '/nologo' '/v:minimal' "/p:Configuration=$Configuration" "/p:Platform=$Platform"
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild 退出码 $LASTEXITCODE"
}

$packageRoot = Join-Path $projectDir "bin\$Platform\$Configuration\net9.0-windows10.0.22621.0\$runtime\AppPackages"
$package = Get-ChildItem $packageRoot -Recurse -Filter '*.msix' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\Dependencies\\' } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $package) {
    throw "未在 $packageRoot 下找到 .msix。"
}

[xml]$packageManifest = Get-Content $manifest -Raw
$publisher = "$($packageManifest.Package.Identity.Publisher)"
if ((Test-Path $certificatePath) -or (Test-Path $pfxPath) -or (Test-Path $passwordPath)) {
    if (-not ((Test-Path $certificatePath) -and (Test-Path $pfxPath) -and (Test-Path $passwordPath))) {
        throw "本地开发证书不完整：$certificateDir。请运行 just cmdpal-clean-local 后重试。"
    }

    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
    if ($certificate.Subject -ne $publisher -or $certificate.NotAfter -le (Get-Date)) {
        throw "本地开发证书无效或已过期：$certificatePath。请运行 just cmdpal-clean-local 后重试。"
    }
    $securePassword = Get-Content $passwordPath -Raw | ConvertTo-SecureString
} else {
    Write-Host '创建项目目录中的本地代码签名证书...' -ForegroundColor DarkGray
    New-Item -ItemType Directory -Path $certificateDir -Force | Out-Null

    $randomBytes = [byte[]]::new(32)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($randomBytes)
    $plainPassword = [Convert]::ToBase64String($randomBytes)
    $securePassword = ConvertTo-SecureString -String $plainPassword -AsPlainText -Force
    $temporaryCertificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $publisher `
        -FriendlyName 'PowerTranslator Local Development' `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -HashAlgorithm SHA256 `
        -KeySpec Signature `
        -NotAfter (Get-Date).AddYears(5)
    try {
        Export-Certificate -Cert $temporaryCertificate -FilePath $certificatePath | Out-Null
        Export-PfxCertificate -Cert $temporaryCertificate -FilePath $pfxPath -Password $securePassword | Out-Null
        ConvertFrom-SecureString -SecureString $securePassword | Set-Content -Path $passwordPath -NoNewline
    } finally {
        Remove-Item -Path "Cert:\CurrentUser\My\$($temporaryCertificate.Thumbprint)" -Force -ErrorAction SilentlyContinue
    }
    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
}

if (-not (Test-Path $trustManager)) {
    throw "找不到证书信任管理脚本：$trustManager"
}

# Previous revisions placed the certificate in current-user stores. AppX package
# deployment checks LocalMachine\TrustedPeople, so remove those stale entries.
foreach ($store in @('Root', 'TrustedPeople')) {
    # The PowerShell certificate provider can require an interactive UI when
    # removing from CurrentUser\Root. CertUtil performs the same best-effort
    # cleanup without UI; a stale entry must never block a local install.
    & "$env:SystemRoot\System32\certutil.exe" -user -delstore $store $certificate.Thumbprint *> $null
}

$machineTrustedCertificate = Get-ChildItem Cert:\LocalMachine\TrustedPeople |
    Where-Object { $_.Thumbprint -eq $certificate.Thumbprint } |
    Select-Object -First 1
if (-not $machineTrustedCertificate) {
    Write-Host '信任本地开发证书（需要管理员确认）...' -ForegroundColor DarkGray
    $trustArgs = @(
        '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$trustManager`"",
        '-Action', 'Install', '-CertificatePath', "`"$certificatePath`""
    )
    $trustProcess = Start-Process -FilePath 'pwsh' -Verb RunAs -Wait -PassThru -ArgumentList $trustArgs
    if ($trustProcess.ExitCode -ne 0) {
        throw "导入 LocalMachine\\TrustedPeople 失败，退出码 $($trustProcess.ExitCode)。"
    }
}

$sdkBin = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
$signTool = Get-ChildItem $sdkBin -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.Directory.Name -eq 'x64' } |
    Sort-Object { [version]$_.Directory.Parent.Name } -Descending |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $signTool) {
    throw '找不到 signtool.exe。请安装 Windows 10/11 SDK。'
}

Write-Host "`n=== 自签名 $($package.Name) ===" -ForegroundColor Cyan
$plainPassword = [System.Net.NetworkCredential]::new('', $securePassword).Password
& $signTool sign /fd SHA256 /f $pfxPath /p $plainPassword $package.FullName
if ($LASTEXITCODE -ne 0) {
    throw "SignTool 签名失败，退出码 $LASTEXITCODE"
}

Write-Host "`n=== 安装本地 MSIX ===" -ForegroundColor Cyan
# Add-AppxPackage validates the MSIX signature against LocalMachine\TrustedPeople.
Add-AppxPackage -Path $package.FullName -ForceApplicationShutdown -ForceUpdateFromAnyVersion

Write-Host "`n✓ 已安装 $($package.Name)" -ForegroundColor Green
Write-Host "  开发证书位于 $certificateDir，公钥仅信任于 LocalMachine\\TrustedPeople。" -ForegroundColor DarkGray
