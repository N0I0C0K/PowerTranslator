#requires -Version 7
<#
.SYNOPSIS
    重新生成 cmdpal 扩展模板自带的 Asset PNG 文件。

.DESCRIPTION
    cmdpal 模板默认生成的 PNG 在某次 commit 中被某个工具按 UTF-8 文本读了，
    所有非 ASCII 字节被替换成 U+FFFD，文件已损坏（git blob 里就是坏的）。

    此脚本以仓库里干净的 translator.light.png (48x48) 为底图，居中缩放贴到
    Store / WinUI3 包要求的各个尺寸画布上，输出标准 PNG。

    需要的源文件 translator.light.png 必须是真实 PNG。所有输出会覆盖现有
    文件。

.EXAMPLE
    pwsh .\cmdpal\generate-assets.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$assetsDir = Join-Path $PSScriptRoot 'PowerTranslatorExtension\Assets'
$source = Join-Path $assetsDir 'translator.light.png'

if (-not (Test-Path $source)) {
    throw "找不到源图: $source"
}

Add-Type -AssemblyName System.Drawing

# 校验源图是真 PNG
$srcBytes = [System.IO.File]::ReadAllBytes($source)
if (-not ($srcBytes[0] -eq 0x89 -and $srcBytes[1] -eq 0x50 -and $srcBytes[2] -eq 0x4E -and $srcBytes[3] -eq 0x47)) {
    throw "源图 translator.light.png 本身也是坏的（不是 PNG），无法继续。"
}
Write-Host "源图: $source ($(([System.Drawing.Image]::FromFile($source)).Width)x$([System.Drawing.Image]::FromFile($source).Height))" -ForegroundColor DarkGray

# 目标 (filename, w, h, [iconScale 0-1, 0.8 = 图标占画布 80% 边长])
$targets = @(
    @{ Name = 'StoreLogo.png';                                        W = 50;   H = 50;  Scale = 0.85 },
    @{ Name = 'Square44x44Logo.scale-200.png';                        W = 88;   H = 88;  Scale = 0.85 },
    @{ Name = 'Square44x44Logo.targetsize-24_altform-unplated.png';   W = 24;   H = 24;  Scale = 1.00 },
    @{ Name = 'Square150x150Logo.scale-200.png';                      W = 300;  H = 300; Scale = 0.60 },
    @{ Name = 'Wide310x150Logo.scale-200.png';                        W = 620;  H = 300; Scale = 0.50 },
    @{ Name = 'SplashScreen.scale-200.png';                           W = 1240; H = 600; Scale = 0.30 },
    @{ Name = 'LockScreenLogo.scale-200.png';                         W = 48;   H = 48;  Scale = 0.85 }
)

$srcImg = [System.Drawing.Image]::FromFile($source)
try {
    foreach ($t in $targets) {
        $outPath = Join-Path $assetsDir $t.Name
        $canvas = New-Object System.Drawing.Bitmap($t.W, $t.H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($canvas)
        try {
            $g.Clear([System.Drawing.Color]::Transparent)
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

            # 等比缩放 + 居中
            $iconSide = [int]([Math]::Min($t.W, $t.H) * $t.Scale)
            $x = [int](($t.W - $iconSide) / 2)
            $y = [int](($t.H - $iconSide) / 2)
            $g.DrawImage($srcImg, $x, $y, $iconSide, $iconSide)
        } finally {
            $g.Dispose()
        }
        $canvas.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $canvas.Dispose()

        # 验证产物
        $b = [System.IO.File]::ReadAllBytes($outPath)
        $magic = ($b[0..7] | ForEach-Object { '{0:x2}' -f $_ }) -join ' '
        $ok = $b[0] -eq 0x89 -and $b[1] -eq 0x50 -and $b[2] -eq 0x4E -and $b[3] -eq 0x47
        $status = if ($ok) { '✓' } else { '✗' }
        Write-Host ("  {0} {1,-50} {2,4}x{3,-4}  {4,6} B  magic={5}" -f $status, $t.Name, $t.W, $t.H, $b.Length, $magic)
    }
} finally {
    $srcImg.Dispose()
}

Write-Host "`n完成。下一步：" -ForegroundColor Cyan
Write-Host "  1) git add cmdpal/PowerTranslatorExtension/Assets/*.png"
Write-Host "  2) 重跑 pwsh .\cmdpal\build-store.ps1 出新 bundle"
