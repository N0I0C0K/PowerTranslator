#requires -Version 7
<#
.SYNOPSIS
    构建用于 Microsoft Store 提交的 .msixbundle（x64 + arm64）。

.DESCRIPTION
    本项目是单项目 MSIX (EnableMsixTooling=true)，老 UWP 的
    `AppxBundle=Always` / `UapAppxPackageBuildMode=StoreUpload` 不生效，
    必须分别为每个架构跑一次 msbuild，再用 Windows SDK 的 MakeAppx.exe
    手动合成 .msixbundle。

    流程：
      1) 校验 Package.appxmanifest 身份信息；
      2) 清掉旧 AppPackages；
      3) 分别对 x64 / arm64 跑 msbuild（不签名，Store 会重签）；
      4) 收集两个单架构 .msix 到临时目录；
      5) MakeAppx.exe bundle 合成 .msixbundle；
      6) 拷到仓库根 dist/。

    Partner Center 直接接受 .msixbundle 上传（也接受 .msixupload，但单项目
    MSIX 没有原生的 .msixupload 产出路径，bundle 已足够）。

.PARAMETER Configuration
    构建配置，默认 Release。

.PARAMETER Platforms
    bundle 里要包含的架构，默认 @('x64', 'arm64')。

.PARAMETER SkipManifestCheck
    跳过 Package.appxmanifest 占位符检测。CI 或确知无误时使用。

.EXAMPLE
    pwsh .\cmdpal\build-store.ps1

.EXAMPLE
    pwsh .\cmdpal\build-store.ps1 -Platforms x64
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('x64', 'arm64', 'ARM64')]
    [string[]]$Platforms = @('x64', 'arm64'),

    [switch]$SkipManifestCheck
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# === 路径常量 ===
$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$projectDir = Join-Path $PSScriptRoot 'PowerTranslatorExtension'
$csproj = Join-Path $projectDir 'PowerTranslatorExtension.csproj'
$manifest = Join-Path $projectDir 'Package.appxmanifest'
$distDir = Join-Path $repoRoot 'dist'

if (-not (Test-Path $csproj)) {
    throw "找不到项目: $csproj"
}

# 规范化平台名：msbuild 接受 'x64' / 'ARM64'（注意大小写）
$Platforms = $Platforms | ForEach-Object {
    if ($_ -ieq 'arm64') { 'ARM64' } else { 'x64' }
} | Select-Object -Unique

# === 1) 找 msbuild ===
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw "找不到 vswhere.exe。请安装 Visual Studio 2022 (含 'Windows App SDK C# templates' / 'WinUI 应用开发' 工作负载)。"
}
$msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuildPath) {
    throw "vswhere 没找到 msbuild。请确认已装 'MSBuild' 组件。"
}
Write-Host "msbuild  : $msbuildPath" -ForegroundColor DarkGray

# === 2) 找 MakeAppx.exe（取最新版本，优先匹配 csproj 锁定的 SDK 版本） ===
$sdkRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
if (-not (Test-Path $sdkRoot)) {
    throw "找不到 Windows SDK 目录: $sdkRoot"
}
$makeAppxPath = Get-ChildItem $sdkRoot -Recurse -Filter 'makeappx.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\' } |
    Sort-Object { [version]($_.Directory.Parent.Name) } -Descending |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $makeAppxPath) {
    throw "找不到 MakeAppx.exe。请安装 Windows 10/11 SDK。"
}
Write-Host "makeappx : $makeAppxPath" -ForegroundColor DarkGray

# === 3) 检查 manifest ===
if (-not $SkipManifestCheck) {
    [xml]$xml = Get-Content $manifest -Raw
    $identity = $xml.Package.Identity
    $name = "$($identity.Name)"
    $publisher = "$($identity.Publisher)"
    $version = "$($identity.Version)"

    Write-Host "`n=== manifest 身份检查 ===" -ForegroundColor Cyan
    Write-Host "  Name      : $name"
    Write-Host "  Publisher : $publisher"
    Write-Host "  Version   : $version"

    $warnings = @()
    $placeholderNames = @('PowerTranslatorExtension')
    $placeholderPublishers = @('CN=N0I0C0K')
    if ($name -in $placeholderNames) {
        $warnings += "Identity.Name 还是默认值 '$name'。Partner Center 给的应是 'Publisher.AppName' 形式（如 NickXii.PowerTranslator）。"
    }
    if ($publisher -in $placeholderPublishers) {
        $warnings += "Publisher 还是默认值 '$publisher'，必须改成 Partner Center 给的 'CN=<GUID>' 形式。"
    }
    if ($publisher -notmatch '^CN=') {
        $warnings += "Publisher '$publisher' 不是 'CN=...' 形式，Store 会拒收。"
    }
    if ($version -notmatch '^\d+\.\d+\.\d+\.0$') {
        $warnings += "Version '$version' 末位不是 0。Store 要求 X.Y.Z.0 格式（末位保留给重新提交）。"
    }

    if ($warnings.Count -gt 0) {
        Write-Warning "以下问题会导致 Store 拒收，强烈建议先在 Package.appxmanifest 里改好："
        foreach ($w in $warnings) { Write-Warning "  - $w" }
        $resp = Read-Host "`n是否继续构建？(y/N)"
        if ($resp -ne 'y' -and $resp -ne 'Y') {
            Write-Host "已取消。" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "✓ 看起来没有占位符。" -ForegroundColor Green
    }
} else {
    [xml]$xml = Get-Content $manifest -Raw
    $name = "$($xml.Package.Identity.Name)"
    $version = "$($xml.Package.Identity.Version)"
}

# === 4) 清旧产物 ===
Write-Host "`n清理旧 AppPackages..." -ForegroundColor DarkGray
foreach ($p in @('x64', 'ARM64', 'arm64')) {
    $platBin = Join-Path $projectDir "bin\$p"
    if (Test-Path $platBin) {
        Get-ChildItem $platBin -Filter 'AppPackages' -Recurse -Directory -ErrorAction SilentlyContinue |
            ForEach-Object {
                Write-Host "  rm $($_.FullName)" -ForegroundColor DarkGray
                Remove-Item $_.FullName -Recurse -Force
            }
    }
}

# === 5) 分平台 msbuild ===
$producedMsix = @()
foreach ($plat in $Platforms) {
    Write-Host "`n=== msbuild $plat ===" -ForegroundColor Cyan
    $args = @(
        $csproj,
        '/restore',
        '/nologo',
        '/v:minimal',
        "/p:Configuration=$Configuration",
        "/p:Platform=$plat",
        '/p:GenerateAppxPackageOnBuild=true',
        '/p:AppxPackageSigningEnabled=false'
    )
    & $msbuildPath @args
    if ($LASTEXITCODE -ne 0) {
        throw "msbuild $plat 退出码 $LASTEXITCODE"
    }

    # 找出这次 build 产出的 msix：bin\<plat> 下、不在 Dependencies 子目录、
    # 不是 .msixsym/.msixbundle，取最新写入的那个
    $platBin = Join-Path $projectDir "bin\$plat"
    $msix = Get-ChildItem $platBin -Recurse -Filter '*.msix' -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Extension -eq '.msix' -and
            $_.FullName -notmatch '\\Dependencies\\' -and
            $_.Directory.FullName -match '\\AppPackages\\'
        } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $msix) {
        $hint = Get-ChildItem $platBin -Recurse -Filter '*.msix*' -ErrorAction SilentlyContinue |
            ForEach-Object { "    $($_.FullName)" }
        throw ("没在 $platBin 下找到 $plat 的 .msix。当前候选：`n" + ($hint -join "`n"))
    }
    Write-Host "  ✓ $($msix.Name)" -ForegroundColor Green
    $producedMsix += $msix.FullName
}

# === 6) MakeAppx bundle ===
Write-Host "`n=== 合成 .msixbundle ===" -ForegroundColor Cyan
$bundleStaging = Join-Path $env:TEMP "ptbundle_$(Get-Random)"
New-Item -ItemType Directory -Path $bundleStaging | Out-Null
try {
    foreach ($m in $producedMsix) {
        Copy-Item $m -Destination $bundleStaging
    }

    if (-not (Test-Path $distDir)) {
        New-Item -ItemType Directory -Path $distDir | Out-Null
    }
    $bundleName = "${name}_${version}_x64_arm64.msixbundle"
    $bundlePath = Join-Path $distDir $bundleName

    if (Test-Path $bundlePath) {
        Remove-Item $bundlePath -Force
    }

    Write-Host "  staging : $bundleStaging" -ForegroundColor DarkGray
    Write-Host "  output  : $bundlePath" -ForegroundColor DarkGray
    & $makeAppxPath bundle /d $bundleStaging /p $bundlePath /bv $version /o
    if ($LASTEXITCODE -ne 0) {
        throw "MakeAppx bundle 退出码 $LASTEXITCODE"
    }

    $info = Get-Item $bundlePath
    $sizeMB = [Math]::Round($info.Length / 1MB, 2)
    Write-Host ("`n✓ {0}  ({1} MB)" -f $bundlePath, $sizeMB) -ForegroundColor Green
} finally {
    if (Test-Path $bundleStaging) {
        Remove-Item $bundleStaging -Recurse -Force
    }
}

Write-Host "`n下一步：" -ForegroundColor Cyan
Write-Host "  1) 登录 https://partner.microsoft.com/dashboard"
Write-Host "  2) 选你的 app → Submissions → Start your submission"
Write-Host "  3) Packages 页面把 dist\$bundleName 拖进去"
Write-Host "  4) 填 listing / pricing → Submit"
