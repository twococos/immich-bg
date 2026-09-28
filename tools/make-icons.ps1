# Regenerates the app icons from the source logo:
#   src/ImmichBg/Assets/logo.png        full logo (big sizes, sidebar)
#   src/ImmichBg/Assets/logo-small.png  flower crop (readable at 16-48 px, tray)
#   src/ImmichBg/app.ico                exe/installer icon: flower up to 48 px, full logo from 64 px
# Usage: .\tools\make-icons.ps1 -Source path\to\logo.png
param([Parameter(Mandatory)][string]$Source)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $root 'src\ImmichBg\Assets'
New-Item -ItemType Directory -Force $assets | Out-Null

$src = [System.Drawing.Image]::FromFile((Resolve-Path $Source))
# Square around the flower in the 1254x1254 source (scaled if the source has another size).
$k = $src.Width / 1254.0
$flowerRect = New-Object System.Drawing.RectangleF (417 * $k), (420 * $k), (420 * $k), (420 * $k)
$fullRect = New-Object System.Drawing.RectangleF 0, 0, $src.Width, $src.Height

# Background colour of the source, used to hide the monitor edges that fall inside the flower crop.
$probe = New-Object System.Drawing.Bitmap $src
$bg = $probe.GetPixel([int](20 * $k), [int](20 * $k))
$probe.Dispose()

function Render([System.Drawing.RectangleF]$from, [int]$size, [double]$radius) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.PixelOffsetMode = 'HighQuality'
    $g.CompositingQuality = 'HighQuality'
    # Rounded-corner clip (transparent outside).
    $r = [float]($size * $radius); $d = 2 * $r
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90); $path.AddArc($size - $d, 0, $d, $d, 270, 90)
    $path.AddArc($size - $d, $size - $d, $d, $d, 0, 90); $path.AddArc(0, $size - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $g.SetClip($path)
    $dest = New-Object System.Drawing.RectangleF 0, 0, $size, $size
    $g.DrawImage($src, $dest, $from, [System.Drawing.GraphicsUnit]::Pixel)
    if ($from.Width -lt $src.Width) {
        # Cover the lower corners (edges of the left/right monitors) with the background colour.
        $scale = $size / $from.Width
        $brush = New-Object System.Drawing.SolidBrush $bg
        $cornerTop = (680 * $k - $from.Y) * $scale
        $g.FillRectangle($brush, 0, $cornerTop, (446 * $k - $from.X) * $scale, $size)
        $rightX = (810 * $k - $from.X) * $scale
        $g.FillRectangle($brush, $rightX, $cornerTop, $size - $rightX, $size)
        $brush.Dispose()
    }
    $g.Dispose()
    return $bmp
}

# Small icon frames as 32-bit DIB (read everywhere), big ones as PNG (compact).
function DibBytes($bmp) {
    $w = $bmp.Width; $h = $bmp.Height
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    $bw.Write([uint32]40); $bw.Write([int32]$w); $bw.Write([int32]($h * 2)); $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]0); $bw.Write([uint32]0); $bw.Write([int32]0); $bw.Write([int32]0); $bw.Write([uint32]0); $bw.Write([uint32]0)
    for ($y = $h - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $w; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$c.B); $bw.Write([byte]$c.G); $bw.Write([byte]$c.R); $bw.Write([byte]$c.A)
        }
    }
    $maskRow = [int]([math]::Ceiling($w / 32.0) * 4)
    $bw.Write((New-Object byte[] ($maskRow * $h)))
    $bw.Flush()
    return , $ms.ToArray()
}

function PngBytes($bmp) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    return , $ms.ToArray()
}

(Render $fullRect 512 0.18).Save((Join-Path $assets 'logo.png'), [System.Drawing.Imaging.ImageFormat]::Png)
(Render $flowerRect 256 0.22).Save((Join-Path $assets 'logo-small.png'), [System.Drawing.Imaging.ImageFormat]::Png)

$sizes = 16, 20, 24, 32, 48, 64, 128, 256
$pngs = foreach ($s in $sizes) {
    $from = if ($s -le 48) { $flowerRect } else { $fullRect }
    $frame = Render $from $s ($(if ($s -le 48) { 0.22 } else { 0.18 }))
    if ($s -le 48) { DibBytes $frame } else { PngBytes $frame }
}
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $out
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $d = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $bw.Write([byte]$d); $bw.Write([byte]$d); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$pngs[$i].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $root 'src\ImmichBg\app.ico'), $out.ToArray())
$src.Dispose()
Write-Host "OK: logo.png, logo-small.png, app.ico"
