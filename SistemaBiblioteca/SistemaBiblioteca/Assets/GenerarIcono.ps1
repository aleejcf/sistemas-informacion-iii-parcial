# Genera el icono de la aplicación a partir de la misma geometría vectorial que
# usa el logotipo en Application.xaml: cuadrado redondeado verde tinta con un
# libro abierto dorado. Se escribe un .ico multi-resolución.

Add-Type -AssemblyName System.Drawing

$destino = $args[0]

$VerdeTinta = [System.Drawing.Color]::FromArgb(255, 20, 40, 30)    # #14281E
$Dorado     = [System.Drawing.Color]::FromArgb(255, 201, 162, 39)  # #C9A227

function New-IconoBitmap {
    param([int]$s)

    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    # ---- Fondo: cuadrado de esquinas redondeadas ----
    $d = [float]($s * 0.42)          # diámetro del arco de la esquina
    $fondo = New-Object System.Drawing.Drawing2D.GraphicsPath
    $fondo.AddArc(0, 0, $d, $d, 180, 90)
    $fondo.AddArc($s - $d, 0, $d, $d, 270, 90)
    $fondo.AddArc($s - $d, $s - $d, $d, $d, 0, 90)
    $fondo.AddArc(0, $s - $d, $d, $d, 90, 90)
    $fondo.CloseFigure()

    $brochaFondo = New-Object System.Drawing.SolidBrush($VerdeTinta)
    $g.FillPath($brochaFondo, $fondo)

    # ---- Libro abierto ----
    # La geometría original está definida en una caja de 24×24; se escala al 78 %
    # del icono y se centra.
    $e = [float]($s / 24.0 * 0.78)
    $o = [float](($s - 24 * $e) / 2)
    function T { param([float]$v) return [float]($o + $v * $e) }

    $libro = New-Object System.Drawing.Drawing2D.GraphicsPath
    # Tapa izquierda
    $libro.AddBezier((T 12), (T 6.2), (T 9.6), (T 4.4), (T 6.6), (T 3.7), (T 3), (T 3.9))
    $libro.AddLine((T 3), (T 3.9), (T 3), (T 18.3))
    # Página inferior izquierda hasta el centro
    $libro.AddBezier((T 3), (T 18.3), (T 6.6), (T 18.1), (T 9.6), (T 18.8), (T 12), (T 20.6))
    # Página inferior derecha
    $libro.AddBezier((T 12), (T 20.6), (T 14.4), (T 18.8), (T 17.4), (T 18.1), (T 21), (T 18.3))
    $libro.AddLine((T 21), (T 18.3), (T 21), (T 3.9))
    # Tapa derecha de regreso al centro
    $libro.AddBezier((T 21), (T 3.9), (T 17.4), (T 3.7), (T 14.4), (T 4.4), (T 12), (T 6.2))
    $libro.CloseFigure()

    $brochaLibro = New-Object System.Drawing.SolidBrush($Dorado)
    $g.FillPath($brochaLibro, $libro)

    # ---- Lomo ----
    # En 16 px la línea del lomo solo ensucia el dibujo, así que se omite.
    if ($s -ge 24) {
        $grosor = [float]([Math]::Max(1.0, 1.4 * $e))
        $lapiz = New-Object System.Drawing.Pen($VerdeTinta, $grosor)
        $lapiz.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $lapiz.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawLine($lapiz, (T 12), (T 6.2), (T 12), (T 20.6))
        $lapiz.Dispose()
    }

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

    Write-Host ("   {0,3} px -> {1} bytes  (tipo {2})" -f $s, $datos.Length, $datos.GetType().Name)
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
