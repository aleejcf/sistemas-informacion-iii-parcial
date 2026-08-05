# Genera el icono de la aplicación a partir de la misma geometría vectorial que
# usa el logotipo en Application.xaml: cuadrado redondeado azul noche con el
# avión ámbar inclinado 45°, igual que LogoAlas. Se escribe un .ico
# multi-resolución.
#
#   powershell -ExecutionPolicy Bypass -File GenerarIcono.ps1 <destino.ico>

Add-Type -AssemblyName System.Drawing

$destino = $args[0]

$AzulNoche = [System.Drawing.Color]::FromArgb(255,  8, 24, 47)   # #08182F
$AzulAlto  = [System.Drawing.Color]::FromArgb(255, 29, 78, 122)  # #1D4E7A
$Ambar     = [System.Drawing.Color]::FromArgb(255, 242, 176, 30) # #F2B01E

# El avión del logotipo, tal cual está en Application.xaml (caja de 24×24)
$GeoAvion = @(
    @(12.0, 1.5), @(13.6, 9.0), @(22.5, 13.6), @(22.5, 15.8), @(13.6, 13.4),
    @(13.6, 19.2), @(16.3, 21.2), @(16.3, 22.8), @(12.0, 21.6), @(7.7, 22.8),
    @(7.7, 21.2), @(10.4, 19.2), @(10.4, 13.4), @(1.5, 15.8), @(1.5, 13.6),
    @(10.4, 9.0)
)

function New-IconoBitmap {
    param([int]$s)

    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    # ---- Fondo: cuadrado de esquinas redondeadas con el degradado del cielo ----
    $d = [float]($s * 0.42)          # diámetro del arco de la esquina
    $fondo = New-Object System.Drawing.Drawing2D.GraphicsPath
    $fondo.AddArc(0, 0, $d, $d, 180, 90)
    $fondo.AddArc($s - $d, 0, $d, $d, 270, 90)
    $fondo.AddArc($s - $d, $s - $d, $d, $d, 0, 90)
    $fondo.AddArc(0, $s - $d, $d, $d, 90, 90)
    $fondo.CloseFigure()

    # El degradado va de arriba-izquierda a abajo-derecha, como BrushCielo
    $cajaFondo = New-Object System.Drawing.Rectangle(0, 0, $s, $s)
    $brochaFondo = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $cajaFondo, $AzulNoche, $AzulAlto, 55.0)
    $g.FillPath($brochaFondo, $fondo)

    # ---- Estela: solo cuando hay píxeles de sobra para que se entienda ----
    # Por debajo de 48 px es un borrón que solo ensucia el avión.
    # Va con brocha degradada y no con un color plano: una estela que se corta de
    # golpe parece un rayón: tiene que nacer de la nada y espesar hacia el avión.
    if ($s -ge 48) {
        $cajaEstela = New-Object System.Drawing.RectangleF(
            [float]($s * 0.08), [float]($s * 0.52), [float]($s * 0.40), [float]($s * 0.44))
        $brochaEstela = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            $cajaEstela,
            [System.Drawing.Color]::FromArgb(0, 255, 255, 255),
            [System.Drawing.Color]::FromArgb(165, 255, 255, 255),
            -45.0)

        $lapizEstela = New-Object System.Drawing.Pen($brochaEstela, [float]($s * 0.032))
        $lapizEstela.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $lapizEstela.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawBezier($lapizEstela,
            [float]($s * 0.12), [float]($s * 0.90),
            [float]($s * 0.24), [float]($s * 0.82),
            [float]($s * 0.31), [float]($s * 0.75),
            [float]($s * 0.40), [float]($s * 0.63))
        $lapizEstela.Dispose(); $brochaEstela.Dispose()
    }

    # ---- Avión ----
    # Se arma en la caja original de 24×24 y luego se gira, se escala y se centra.
    # El orden en que se encadenan las operaciones es el inverso al que se aplica:
    # con Prepend, el punto se rota primero y se traslada al final.
    $avion = New-Object System.Drawing.Drawing2D.GraphicsPath
    $puntos = $GeoAvion | ForEach-Object {
        New-Object System.Drawing.PointF([float]$_[0], [float]$_[1])
    }
    $avion.AddPolygon([System.Drawing.PointF[]]$puntos)

    $e = [float]($s / 24.0 * 0.64)
    $o = [float](($s - 24 * $e) / 2)

    $m = New-Object System.Drawing.Drawing2D.Matrix
    $m.Translate($o, $o)
    $m.Scale($e, $e)
    $m.RotateAt(45.0, (New-Object System.Drawing.PointF(12.0, 12.0)))
    $avion.Transform($m)

    $brochaAvion = New-Object System.Drawing.SolidBrush($Ambar)
    $g.FillPath($brochaAvion, $avion)

    $brochaFondo.Dispose(); $brochaAvion.Dispose(); $m.Dispose()
    $g.Dispose()
    return $bmp
}

# ---- Convertir un bitmap al formato DIB que espera un icono clásico ----
# Es BITMAPINFOHEADER + los píxeles BGRA de abajo hacia arriba + una máscara AND.
# Con 32 bits por píxel la transparencia la da el canal alfa, así que la máscara
# va en ceros; pero tiene que estar y con el tamaño exacto o el icono no carga.
function ConvertTo-Dib {
    param([System.Drawing.Bitmap]$bmp)

    $ancho = $bmp.Width
    $alto = $bmp.Height

    $rect = New-Object System.Drawing.Rectangle(0, 0, $ancho, $alto)
    $datos = $bmp.LockBits($rect,
        [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    $paso = $datos.Stride
    $pixeles = New-Object byte[] ($paso * $alto)
    [System.Runtime.InteropServices.Marshal]::Copy($datos.Scan0, $pixeles, 0, $pixeles.Length)
    $bmp.UnlockBits($datos)

    $ms = New-Object System.IO.MemoryStream
    $w = New-Object System.IO.BinaryWriter($ms)

    # BITMAPINFOHEADER — el alto va DOBLE porque describe imagen + máscara
    $w.Write([UInt32]40)
    $w.Write([Int32]$ancho)
    $w.Write([Int32]($alto * 2))
    $w.Write([UInt16]1)
    $w.Write([UInt16]32)
    $w.Write([UInt32]0)               # sin compresión
    $w.Write([UInt32]($ancho * $alto * 4))
    $w.Write([Int32]0); $w.Write([Int32]0)
    $w.Write([UInt32]0); $w.Write([UInt32]0)

    # Píxeles, de la última fila a la primera
    for ($y = $alto - 1; $y -ge 0; $y--) {
        $w.Write($pixeles, $y * $paso, $ancho * 4)
    }

    # Máscara AND: 1 bit por píxel, filas alineadas a 4 bytes
    $bytesFila = [int]([Math]::Floor(($ancho + 31) / 32) * 4)
    $ceros = New-Object byte[] $bytesFila
    for ($y = 0; $y -lt $alto; $y++) { $w.Write($ceros, 0, $bytesFila) }

    $w.Flush()
    $salida = $ms.ToArray()
    $w.Dispose(); $ms.Dispose()

    # La coma es obligatoria: sin ella PowerShell desenrolla el arreglo en la
    # tubería y lo devuelve como Object[], con lo que BinaryWriter.Write ya no
    # encuentra la sobrecarga de Byte[] y no escribe ni un byte.
    return , $salida
}

# ---- Armar el archivo .ico ----
# Cabecera de 6 bytes + una entrada de 16 bytes por imagen + los datos.
# Los tamaños chicos van en DIB y solo el de 256 en PNG: es lo que hacen las
# herramientas de iconos, porque no todo el shell de Windows lee PNG incrustado.
$tamanos = @(16, 24, 32, 48, 64, 128, 256)
$imagenes = @()

foreach ($s in $tamanos) {
    $bmp = New-IconoBitmap -s $s

    if ($s -ge 256) {
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $datos = $ms.ToArray()
        $ms.Dispose()
    } else {
        $datos = [byte[]](ConvertTo-Dib -bmp $bmp)
    }

    Write-Host ("   {0,3} px -> {1} bytes" -f $s, $datos.Length)
    $imagenes += , @{ Tamano = $s; Datos = $datos }
    $bmp.Dispose()
}

$salida = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($salida)

$w.Write([UInt16]0)                    # reservado
$w.Write([UInt16]1)                    # tipo 1 = icono
$w.Write([UInt16]$imagenes.Count)

# Los datos empiezan después de la cabecera y de todas las entradas
$desplazamiento = 6 + (16 * $imagenes.Count)
foreach ($img in $imagenes) {
    # 256 se escribe como 0: el campo es de un solo byte
    $lado = if ($img.Tamano -ge 256) { 0 } else { $img.Tamano }
    $w.Write([Byte]$lado)              # ancho
    $w.Write([Byte]$lado)              # alto
    $w.Write([Byte]0)                  # colores de la paleta (0 = sin paleta)
    $w.Write([Byte]0)                  # reservado
    $w.Write([UInt16]1)                # planos
    $w.Write([UInt16]32)               # bits por píxel
    $w.Write([UInt32]$img.Datos.Length)
    $w.Write([UInt32]$desplazamiento)
    $desplazamiento += $img.Datos.Length
}

foreach ($img in $imagenes) { $w.Write($img.Datos) }

$w.Flush()
[System.IO.File]::WriteAllBytes($destino, $salida.ToArray())
$w.Dispose(); $salida.Dispose()

Write-Host ("Icono generado: {0}  ({1:N0} bytes, {2} resoluciones)" -f $destino, (Get-Item $destino).Length, $imagenes.Count)
