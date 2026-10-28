Add-Type -AssemblyName System.Drawing

function Create-LogoBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $padding = [float]($size * 0.06)
    $w = [float]($size - ($padding * 2))
    $h = [float]($size - ($padding * 2))
    $corner = [float]($size * 0.22)

    # Rounded rectangle path for the background badge
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($padding, $padding, $corner, $corner, 180, 90)
    $path.AddArc(($padding + $w - $corner), $padding, $corner, $corner, 270, 90)
    $path.AddArc(($padding + $w - $corner), ($padding + $h - $corner), $corner, $corner, 0, 90)
    $path.AddArc($padding, ($padding + $h - $corner), $corner, $corner, 90, 90)
    $path.CloseFigure()

    # Rich royal blue to vibrant cobalt gradient
    $rect = New-Object System.Drawing.RectangleF($padding, $padding, $w, $h)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 30, 58, 138),  # Deep Blue (#1E3A8A)
        [System.Drawing.Color]::FromArgb(255, 37, 99, 235),  # Vibrant Blue (#2563EB)
        45.0
    )
    $g.FillPath($brush, $path)

    # Subtle inner border
    $borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 96, 165, 250), [float]($size * 0.02))
    $g.DrawPath($borderPen, $path)

    # Envelope shape in crisp white
    $envLeft = [float]($size * 0.22)
    $envTop = [float]($size * 0.30)
    $envWidth = [float]($size * 0.56)
    $envHeight = [float]($size * 0.40)
    $envCorner = [float]($size * 0.06)

    $envPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $envPath.AddArc($envLeft, $envTop, $envCorner, $envCorner, 180, 90)
    $envPath.AddArc(($envLeft + $envWidth - $envCorner), $envTop, $envCorner, $envCorner, 270, 90)
    $envPath.AddArc(($envLeft + $envWidth - $envCorner), ($envTop + $envHeight - $envCorner), $envCorner, $envCorner, 0, 90)
    $envPath.AddArc($envLeft, ($envTop + $envHeight - $envCorner), $envCorner, $envCorner, 90, 90)
    $envPath.CloseFigure()

    $envBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(245, 255, 255, 255))
    $g.FillPath($envBrush, $envPath)

    # Envelope Flap (Blue fold line)
    $flapPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 203, 213, 225), [float]($size * 0.03))
    $flapPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $flapPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $flapPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $point1 = New-Object System.Drawing.PointF(($envLeft + ($size * 0.02)), ($envTop + ($size * 0.03)))
    $point2 = New-Object System.Drawing.PointF(($size * 0.50), ($envTop + ($envHeight * 0.65)))
    $point3 = New-Object System.Drawing.PointF(($envLeft + $envWidth - ($size * 0.02)), ($envTop + ($size * 0.03)))

    $g.DrawLines($flapPen, @($point1, $point2, $point3))

    # AI Sparkle Star (4-point star on the top right)
    $starCenterX = [float]($size * 0.76)
    $starCenterY = [float]($size * 0.24)
    $starR1 = [float]($size * 0.14)
    $starR2 = [float]($size * 0.04)

    $starPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pts = @(
        (New-Object System.Drawing.PointF($starCenterX, ($starCenterY - $starR1))),
        (New-Object System.Drawing.PointF(($starCenterX + $starR2), ($starCenterY - $starR2))),
        (New-Object System.Drawing.PointF(($starCenterX + $starR1), $starCenterY)),
        (New-Object System.Drawing.PointF(($starCenterX + $starR2), ($starCenterY + $starR2))),
        (New-Object System.Drawing.PointF($starCenterX, ($starCenterY + $starR1))),
        (New-Object System.Drawing.PointF(($starCenterX - $starR2), ($starCenterY + $starR2))),
        (New-Object System.Drawing.PointF(($starCenterX - $starR1), $starCenterY)),
        (New-Object System.Drawing.PointF(($starCenterX - $starR2), ($starCenterY - $starR2)))
    )
    $starPath.AddPolygon($pts)

    # Bright Cyan Sparkle (#38BDF8)
    $starBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 56, 189, 248))
    $g.FillPath($starBrush, $starPath)

    # Small secondary sparkle star
    $s2X = [float]($size * 0.28)
    $s2Y = [float]($size * 0.74)
    $s2R1 = [float]($size * 0.08)
    $s2R2 = [float]($size * 0.02)
    $pts2 = @(
        (New-Object System.Drawing.PointF($s2X, ($s2Y - $s2R1))),
        (New-Object System.Drawing.PointF(($s2X + $s2R2), ($s2Y - $s2R2))),
        (New-Object System.Drawing.PointF(($s2X + $s2R1), $s2Y)),
        (New-Object System.Drawing.PointF(($s2X + $s2R2), ($s2Y + $s2R2))),
        (New-Object System.Drawing.PointF($s2X, ($s2Y + $s2R1))),
        (New-Object System.Drawing.PointF(($s2X - $s2R2), ($s2Y + $s2R2))),
        (New-Object System.Drawing.PointF(($s2X - $s2R1), $s2Y)),
        (New-Object System.Drawing.PointF(($s2X - $s2R2), ($s2Y - $s2R2)))
    )
    $starPath2 = New-Object System.Drawing.Drawing2D.GraphicsPath
    $starPath2.AddPolygon($pts2)
    $g.FillPath($starBrush, $starPath2)

    $g.Dispose()
    return $bmp
}

# Create high-res PNG
$logo256 = Create-LogoBitmap 256
$logo256.Save("src/MailCraftAI/Assets/Logo.png", [System.Drawing.Imaging.ImageFormat]::Png)

# Generate multi-res images for ICO: 256, 48, 32, 16
$sizes = @(256, 48, 32, 16)
$pngBytesList = New-Object System.Collections.Generic.List[byte[]]

foreach ($s in $sizes) {
    $bmp = Create-LogoBitmap $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytesList.Add($ms.ToArray())
    $ms.Dispose()
    $bmp.Dispose()
}

# Write Windows ICO format
$icoFile = [System.IO.File]::Create("src/MailCraftAI/Assets/AppIcon.ico")
$bw = New-Object System.IO.BinaryWriter($icoFile)

# ICONDIR
$bw.Write([uint16]0) # Reserved
$bw.Write([uint16]1) # Icon type
$bw.Write([uint16]$sizes.Count) # Number of images

# Calculate offset: 6 + (16 * count)
$offset = 6 + (16 * $sizes.Count)

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $data = $pngBytesList[$i]
    
    $wByte = if ($s -ge 256) { [byte]0 } else { [byte]$s }
    $hByte = if ($s -ge 256) { [byte]0 } else { [byte]$s }

    $bw.Write($wByte)          # Width
    $bw.Write($hByte)          # Height
    $bw.Write([byte]0)         # Colors
    $bw.Write([byte]0)         # Reserved
    $bw.Write([uint16]1)       # Color planes
    $bw.Write([uint16]32)      # Bits per pixel
    $bw.Write([uint32]$data.Length) # Size of image data
    $bw.Write([uint32]$offset)      # Offset of image data

    $offset += $data.Length
}

# Write PNG payloads
foreach ($data in $pngBytesList) {
    $bw.Write($data)
}

$bw.Flush()
$bw.Close()
$icoFile.Close()

Write-Output "Successfully generated Logo.png and AppIcon.ico in src/MailCraftAI/Assets/"
