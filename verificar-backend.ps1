#Requires -Version 7.0
<#
.SINOPSIS
    Orquestador ÚNICO de pruebas y cobertura del backend UMBRAL (local y CI).

.DESCRIPCION
    - Restaura y compila UMBRAL.sln (Release).
    - Descubre automáticamente todas las suites de prueba bajo backend/pruebas
      (PruebasUnitarias / PruebasIntegracion) desde `dotnet sln list`.
    - Ejecuta cada suite con cobertura Coverlet usando coverlet.runsettings
      (--collect:"XPlat Code Coverage" => coverage.cobertura.xml por suite).
    - Continúa aunque una suite falle; al final falla si hubo pruebas fallidas.
    - Deduplica los coverage.cobertura.xml por SHA-256 (evita procesar copias
      idénticas que genera el par --collect + logger trx).
    - Calcula la cobertura GLOBAL leyendo directamente los XML Cobertura en
      PowerShell (líneas, métodos y ramas), combinando sobre elementos únicos
      del código de producción (sin ReportGenerator, sin reporte HTML).
    - Compara la cobertura de LÍNEAS con la meta académica (por defecto 90 %).
    - Muestra el resultado en consola y, en CI, en GITHUB_STEP_SUMMARY.

    La misma lógica se usa localmente y en GitHub Actions (ci.yml invoca este
    script), de modo que no existe una lista de suites duplicada.

.PARAMETER Silencioso
    Baja el ruido de logs de .NET y redirige la salida de cada suite a un log.

.PARAMETER ExigirCobertura
    Si la cobertura de líneas < CoberturaMinima, termina con código != 0
    (pero SOLO después de calcular y mostrar todos los resultados).

.PARAMETER CoberturaMinima
    Meta de cobertura de líneas. Por defecto 90.

.EJEMPLO
    pwsh ./verificar-backend.ps1 -Silencioso -CoberturaMinima 90
    pwsh ./verificar-backend.ps1 -Silencioso -CoberturaMinima 90 -ExigirCobertura
#>
[CmdletBinding()]
param(
    [switch]$Silencioso,
    [switch]$ExigirCobertura,
    [decimal]$CoberturaMinima = 90
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding = [System.Text.Encoding]::UTF8
}
catch {
    # Host sin consola interactiva; se ignora.
}

$raiz = $PSScriptRoot
$solucion = Join-Path $raiz 'UMBRAL.sln'
$runsettings = Join-Path $raiz 'coverlet.runsettings'

$dirArtifacts = Join-Path $raiz 'artifacts'
$dirResultados = Join-Path $dirArtifacts 'test-results'

$enGitHub = $env:GITHUB_ACTIONS -eq 'true'

$suitesEsperadas = @(
    'SesionesServicio.PruebasUnitarias',
    'SesionesServicio.PruebasIntegracion',
    'IdentidadServicio.PruebasUnitarias',
    'IdentidadServicio.PruebasIntegracion',
    'JuegosServicio.PruebasUnitarias',
    'JuegosServicio.PruebasIntegracion',
    'RankingServicio.PruebasUnitarias',
    'RankingServicio.PruebasIntegracion'
)

if ($Silencioso) {
    $env:Logging__LogLevel__Default = 'Warning'
    $env:Logging__LogLevel__Microsoft = 'Warning'
    $env:Logging__LogLevel__Microsoft_AspNetCore = 'Warning'
    $env:Logging__LogLevel__Microsoft_EntityFrameworkCore = 'Warning'
}

function Confirmar-UltimoComando {
    param([string]$Nombre)
    if ($LASTEXITCODE -ne 0) {
        throw "$Nombre falló con código de salida $LASTEXITCODE."
    }
}

function Obtener-ResumenTrx {
    param([string]$RutaTrx)

    [xml]$xml = Get-Content $RutaTrx -Raw

    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')

    $counters = $xml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    if ($null -eq $counters) {
        return $null
    }

    $fallidas = @()
    foreach ($resultado in $xml.SelectNodes("//t:UnitTestResult[@outcome='Failed']", $ns)) {
        $testId = $resultado.testId
        $definicion = $xml.SelectSingleNode("//t:UnitTest[@id='$testId']", $ns)
        $nombrePrueba = if ($null -ne $definicion) { $definicion.name } else { $resultado.testName }

        $mensajeNodo = $resultado.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        $mensaje = if ($null -ne $mensajeNodo) { $mensajeNodo.InnerText } else { '' }

        $fallidas += [PSCustomObject]@{ Nombre = $nombrePrueba; Mensaje = $mensaje }
    }

    return [PSCustomObject]@{
        Total    = [int]$counters.total
        Pasaron  = [int]$counters.passed
        Fallaron = [int]$counters.failed
        Omitidas = [int]$counters.notExecuted
        Fallidas = $fallidas
    }
}

function Obtener-CoberturaXml {
    param([string[]]$Rutas)

    $lineas = @{}   # clave -> [bool] cubierta
    $metodos = @{}  # clave -> [bool] cubierto
    $ramas = @{}    # clave -> objeto { Total; MaxCubiertas; SuitesConAlgo }

    foreach ($ruta in $Rutas) {
        [xml]$xml = Get-Content $ruta -Raw

        foreach ($clase in $xml.SelectNodes('//class')) {
            $paquete = $clase.ParentNode.ParentNode.GetAttribute('name')
            $archivo = ($clase.GetAttribute('filename') -replace '\\', '/').Trim()
            $nombreClase = $clase.GetAttribute('name')

            # ---- Líneas y ramas (nivel clase = unión completa de la clase) ----
            foreach ($linea in $clase.SelectNodes('lines/line')) {
                $num = [int]$linea.GetAttribute('number')
                $hits = [int]$linea.GetAttribute('hits')

                $claveLinea = "$paquete|$archivo|$num"
                if (-not $lineas.ContainsKey($claveLinea)) { $lineas[$claveLinea] = $false }
                if ($hits -gt 0) { $lineas[$claveLinea] = $true }

                $esRama = $linea.GetAttribute('branch').ToLowerInvariant() -eq 'true'
                if ($esRama) {
                    $cc = $linea.GetAttribute('condition-coverage')
                    if ($cc -match '\((\d+)/(\d+)\)') {
                        $x = [int]$Matches[1]
                        $y = [int]$Matches[2]
                        $claveRama = "$paquete|$archivo|$num"
                        if (-not $ramas.ContainsKey($claveRama)) {
                            $ramas[$claveRama] = [PSCustomObject]@{
                                Total        = $y
                                MaxCubiertas = $x
                                SuitesConAlgo = $(if ($x -gt 0) { 1 } else { 0 })
                            }
                        }
                        else {
                            $r = $ramas[$claveRama]
                            if ($y -gt $r.Total) { $r.Total = $y }
                            if ($x -gt $r.MaxCubiertas) { $r.MaxCubiertas = $x }
                            if ($x -gt 0) { $r.SuitesConAlgo++ }
                        }
                    }
                }
            }

            # ---- Métodos ----
            foreach ($metodo in $clase.SelectNodes('methods/method')) {
                $firma = $metodo.GetAttribute('signature')
                $claveMetodo = "$paquete|$nombreClase|$($metodo.GetAttribute('name'))$firma"

                $cubierto = $false
                foreach ($ml in $metodo.SelectNodes('lines/line')) {
                    if ([int]$ml.GetAttribute('hits') -gt 0) { $cubierto = $true; break }
                }
                if (-not $metodos.ContainsKey($claveMetodo)) { $metodos[$claveMetodo] = $false }
                if ($cubierto) { $metodos[$claveMetodo] = $true }
            }
        }
    }

    # ---- Totales de líneas ----
    $lineasTot = $lineas.Count
    $lineasCub = 0
    foreach ($v in $lineas.Values) { if ($v) { $lineasCub++ } }

    # ---- Totales de métodos ----
    $metodosTot = $metodos.Count
    $metodosCub = 0
    foreach ($v in $metodos.Values) { if ($v) { $metodosCub++ } }

    # ---- Totales de ramas (cota inferior) ----
    $ramasTot = 0
    $ramasCub = 0
    $ramasAmbiguas = 0
    foreach ($r in $ramas.Values) {
        $ramasTot += $r.Total
        $ramasCub += $r.MaxCubiertas
        if ($r.MaxCubiertas -lt $r.Total -and $r.SuitesConAlgo -ge 2) { $ramasAmbiguas++ }
    }

    $covLineas = if ($lineasTot -gt 0) { [decimal]$lineasCub / $lineasTot * 100 } else { $null }
    $covMetodos = if ($metodosTot -gt 0) { [decimal]$metodosCub / $metodosTot * 100 } else { $null }
    $covRamas = if ($ramasTot -gt 0) { [decimal]$ramasCub / $ramasTot * 100 } else { $null }

    return [PSCustomObject]@{
        LineasTotales    = $lineasTot
        LineasCubiertas  = $lineasCub
        CoberturaLineas  = $covLineas
        MetodosTotales   = $metodosTot
        MetodosCubiertos = $metodosCub
        CoberturaMetodos = $covMetodos
        RamasTotales     = $ramasTot
        RamasCubiertas   = $ramasCub
        CoberturaRamas   = $covRamas
        RamasAmbiguas    = $ramasAmbiguas
        RamasConfiables  = ($ramasAmbiguas -eq 0)
    }
}

function Formatear-Porcentaje {
    param($Valor)
    if ($null -eq $Valor) { return 'no disponible' }
    return ('{0:0.00} %' -f [decimal]$Valor)
}

Push-Location $raiz
$codigoSalidaFinal = 0

try {
    $restosViejos = @(
        (Join-Path $dirArtifacts 'coverage-input'),
        (Join-Path $dirArtifacts 'coverage-report')
    )
    foreach ($d in (@($dirResultados) + $restosViejos)) {
        if (Test-Path $d) { Remove-Item $d -Recurse -Force }
    }
    New-Item -ItemType Directory -Force -Path $dirResultados | Out-Null

    $verbosidad = if ($Silencioso) { 'quiet' } else { 'minimal' }

    Write-Host 'Restaurando paquetes...'
    dotnet restore $solucion --verbosity $verbosidad
    Confirmar-UltimoComando 'dotnet restore'

    Write-Host 'Compilando solución (Release)...'
    dotnet build $solucion --no-restore --configuration Release --verbosity $verbosidad
    Confirmar-UltimoComando 'dotnet build'

    Write-Host ''
    Write-Host 'Descubriendo suites de prueba...'

    $listaProyectos = @(dotnet sln $solucion list)
    Confirmar-UltimoComando 'dotnet sln list'

    $suites = @()
    foreach ($linea in $listaProyectos) {
        $ruta = "$linea".Trim()
        if (-not $ruta.EndsWith('.csproj')) { continue }

        $normal = $ruta.Replace('\', '/')
        if ($normal -notmatch '(?i)backend/pruebas/') { continue }
        if ($normal -notmatch '(?i)(PruebasUnitarias|PruebasIntegracion)\.csproj$') { continue }

        $rutaAbsoluta = Join-Path $raiz ($ruta -replace '[\\/]+', [IO.Path]::DirectorySeparatorChar)
        $nombreSuite = [IO.Path]::GetFileNameWithoutExtension($ruta)

        $suites += [PSCustomObject]@{
            Nombre   = $nombreSuite
            Proyecto = $rutaAbsoluta
        }
    }

    $suites = @($suites | Sort-Object Nombre -Unique)

    if ($suites.Count -eq 0) {
        throw 'No se descubrió ninguna suite de prueba bajo backend/pruebas.'
    }

    $nombresDescubiertos = @($suites | ForEach-Object { $_.Nombre })
    $faltantes = @($suitesEsperadas | Where-Object { $_ -notin $nombresDescubiertos })
    if ($faltantes.Count -gt 0) {
        throw "Faltan suites esperadas en el descubrimiento: $($faltantes -join ', ')"
    }

    Write-Host "  Suites detectadas ($($suites.Count)):"
    foreach ($s in $suites) { Write-Host "   - $($s.Nombre)" }

    Write-Host ''
    Write-Host 'Ejecutando suites de prueba con cobertura...'
    Write-Host ''

    $resumenGlobal = @()

    foreach ($suite in $suites) {
        $nombre = $suite.Nombre
        $directorioSuite = Join-Path $dirResultados $nombre
        $logSuite = Join-Path $directorioSuite "$nombre.log"
        New-Item -ItemType Directory -Force -Path $directorioSuite | Out-Null

        Write-Host "Ejecutando: $nombre"

        $argumentos = @(
            'test', $suite.Proyecto,
            '--no-build',
            '--configuration', 'Release',
            '--settings', $runsettings,
            '--collect:XPlat Code Coverage',
            '--logger', "trx;LogFileName=$nombre.trx",
            '--results-directory', $directorioSuite,
            '--verbosity', $verbosidad
        )

        if ($Silencioso) {
            dotnet @argumentos *> $logSuite
        }
        else {
            dotnet @argumentos 2>&1 | Tee-Object -FilePath $logSuite
        }
        $codigoSalida = $LASTEXITCODE

        $trx = Get-ChildItem $directorioSuite -Recurse -Filter *.trx -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        $resumen = if ($null -ne $trx) { Obtener-ResumenTrx $trx.FullName } else { $null }

        if ($null -eq $resumen) {
            Write-Host "  Sin resumen TRX para $nombre. Log: $logSuite" -ForegroundColor Yellow
            if ($codigoSalida -ne 0) { $codigoSalidaFinal = 1 }
            $resumenGlobal += [PSCustomObject]@{
                Suite = $nombre; Total = 0; Pasaron = 0; Fallaron = 0; Omitidas = 0
            }
            continue
        }

        $resumenGlobal += [PSCustomObject]@{
            Suite    = $nombre
            Total    = $resumen.Total
            Pasaron  = $resumen.Pasaron
            Fallaron = $resumen.Fallaron
            Omitidas = $resumen.Omitidas
        }

        if ($resumen.Fallaron -eq 0 -and $codigoSalida -eq 0) {
            Write-Host "  OK  - Total=$($resumen.Total) Pasaron=$($resumen.Pasaron) Fallaron=$($resumen.Fallaron) Omitidas=$($resumen.Omitidas)" -ForegroundColor Green
        }
        else {
            $codigoSalidaFinal = 1
            Write-Host "  FALLO - Total=$($resumen.Total) Pasaron=$($resumen.Pasaron) Fallaron=$($resumen.Fallaron) Omitidas=$($resumen.Omitidas)" -ForegroundColor Red
            foreach ($fallida in $resumen.Fallidas) {
                Write-Host "   - $($fallida.Nombre)" -ForegroundColor Red
                if (-not [string]::IsNullOrWhiteSpace($fallida.Mensaje)) {
                    Write-Host "     $($fallida.Mensaje)" -ForegroundColor DarkRed
                }
            }
            Write-Host "  Log: $logSuite" -ForegroundColor Yellow
        }
    }

    Write-Host ''
    Write-Host 'Resumen de pruebas:' -ForegroundColor Cyan
    $resumenGlobal | Format-Table Suite, Total, Pasaron, Fallaron, Omitidas -AutoSize | Out-String | Write-Host

    $totalPruebas = ($resumenGlobal | Measure-Object Total -Sum).Sum
    $totalPasaron = ($resumenGlobal | Measure-Object Pasaron -Sum).Sum
    $totalFallaron = ($resumenGlobal | Measure-Object Fallaron -Sum).Sum
    $totalOmitidas = ($resumenGlobal | Measure-Object Omitidas -Sum).Sum

    Write-Host "Total: $totalPruebas | Pasaron: $totalPasaron | Fallaron: $totalFallaron | Omitidas: $totalOmitidas"

    Write-Host ''
    Write-Host 'Recolectando cobertura (Coverlet)...'

    $hashesVistos = @{}
    $xmlUnicos = @()
    $duplicados = 0

    $todosLosXml = Get-ChildItem $dirResultados -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue |
        Sort-Object FullName

    foreach ($archivo in $todosLosXml) {
        $hash = (Get-FileHash $archivo.FullName -Algorithm SHA256).Hash
        if ($hashesVistos.ContainsKey($hash)) { $duplicados++; continue }
        $hashesVistos[$hash] = $true
        $xmlUnicos += $archivo.FullName
    }

    Write-Host "  Reportes Cobertura únicos: $($xmlUnicos.Count) (duplicados exactos omitidos: $duplicados)"

    if ($xmlUnicos.Count -eq 0) {
        throw 'No se encontró ningún coverage.cobertura.xml para calcular la cobertura.'
    }

    $cob = Obtener-CoberturaXml $xmlUnicos

    $covLineas = $cob.CoberturaLineas
    $metaAlcanzada = ($null -ne $covLineas) -and ([decimal]$covLineas -ge $CoberturaMinima)
    $estado = if ($null -eq $covLineas) { 'SIN DATOS' }
              elseif ($metaAlcanzada) { 'META ALCANZADA' }
              else { 'META NO ALCANZADA' }

    $faltante = if ($null -ne $covLineas) {
        $d = [decimal]$CoberturaMinima - [decimal]$covLineas
        if ($d -lt 0) { [decimal]0 } else { $d }
    }
    else { $null }

    $ramasTexto = if ($null -eq $cob.CoberturaRamas) {
        'no disponible (sin datos de ramas en los reportes)'
    }
    elseif (-not $cob.RamasConfiables) {
        '{0} (cota inferior; {1} líneas de rama no combinables con exactitud)' -f (Formatear-Porcentaje $cob.CoberturaRamas), $cob.RamasAmbiguas
    }
    else {
        Formatear-Porcentaje $cob.CoberturaRamas
    }

    Write-Host ''
    Write-Host '============================================='
    Write-Host 'COBERTURA DEL BACKEND'
    Write-Host '============================================='
    Write-Host ("Líneas cubiertas: {0} de {1}" -f $cob.LineasCubiertas, $cob.LineasTotales)
    Write-Host ("Cobertura de líneas: {0}" -f (Formatear-Porcentaje $covLineas))
    Write-Host ("Métodos cubiertos: {0} de {1}" -f $cob.MetodosCubiertos, $cob.MetodosTotales)
    Write-Host ("Cobertura de métodos: {0}" -f (Formatear-Porcentaje $cob.CoberturaMetodos))
    Write-Host ("Ramas cubiertas: {0} de {1}" -f $cob.RamasCubiertas, $cob.RamasTotales)
    Write-Host ("Cobertura de ramas: {0}" -f $ramasTexto)
    Write-Host ("Meta académica: {0} %" -f $CoberturaMinima)
    if ($null -ne $faltante) {
        Write-Host ("Diferencia hasta la meta: {0:0.00} puntos" -f $faltante)
    }
    $colorEstado = if ($metaAlcanzada) { 'Green' } else { 'Yellow' }
    Write-Host ("Estado: {0}" -f $estado) -ForegroundColor $colorEstado
    Write-Host ''
    Write-Host ("Cobertura de líneas del backend: {0}" -f (Formatear-Porcentaje $covLineas))

    if (-not [string]::IsNullOrEmpty($env:GITHUB_STEP_SUMMARY)) {
        $estadoTests = if ($totalFallaron -eq 0) { '✅' } else { '❌' }
        $md = @()
        $md += '## Cobertura del backend (.NET 9)'
        $md += ''
        $md += '| Métrica | Resultado | Meta |'
        $md += '| --- | --- | --- |'
        $md += ("| Líneas | {0} | {1} % |" -f (Formatear-Porcentaje $covLineas), $CoberturaMinima)
        $md += ("| Ramas | {0} | informativa |" -f $ramasTexto)
        $md += ("| Métodos | {0} | informativa |" -f (Formatear-Porcentaje $cob.CoberturaMetodos))
        $md += ("| Pruebas | {0} {1} aprobadas de {2} | todas |" -f $estadoTests, $totalPasaron, $totalPruebas)
        $md += ''
        $md += ("**Estado:** {0}" -f $estado)
        $md -join "`n" | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding utf8 -Append
    }

    if ($enGitHub -and $null -ne $covLineas -and -not $metaAlcanzada) {
        $mensajeMeta = "Cobertura de líneas $((Formatear-Porcentaje $covLineas)) por debajo de la meta $CoberturaMinima %."
        if ($ExigirCobertura) {
            Write-Host "::error::$mensajeMeta"
        }
        else {
            Write-Host "::warning::$mensajeMeta"
        }
    }

    if ($totalFallaron -gt 0 -or $codigoSalidaFinal -ne 0) {
        Write-Host ''
        Write-Host 'Resultado: hubo pruebas fallidas.' -ForegroundColor Red
        $codigoSalidaFinal = 1
    }
    elseif ($ExigirCobertura -and $null -ne $covLineas -and -not $metaAlcanzada) {
        Write-Host ''
        Write-Host 'Resultado: pruebas OK, pero cobertura por debajo de la meta (modo estricto).' -ForegroundColor Red
        $codigoSalidaFinal = 2
    }
    elseif ($ExigirCobertura -and $null -eq $covLineas) {
        Write-Host ''
        Write-Host 'Resultado: modo estricto sin métricas de cobertura legibles.' -ForegroundColor Red
        $codigoSalidaFinal = 2
    }
    elseif ($null -ne $covLineas -and -not $metaAlcanzada) {
        Write-Host ''
        Write-Host 'ADVERTENCIA: cobertura por debajo de la meta (modo informativo, no falla).' -ForegroundColor Yellow
    }
    else {
        Write-Host ''
        Write-Host 'Verificación backend completada correctamente.' -ForegroundColor Green
    }
}
catch {
    Write-Host ''
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $codigoSalidaFinal = 1
}
finally {
    Pop-Location

    if ($Silencioso) {
        Remove-Item Env:\Logging__LogLevel__Default -ErrorAction SilentlyContinue
        Remove-Item Env:\Logging__LogLevel__Microsoft -ErrorAction SilentlyContinue
        Remove-Item Env:\Logging__LogLevel__Microsoft_AspNetCore -ErrorAction SilentlyContinue
        Remove-Item Env:\Logging__LogLevel__Microsoft_EntityFrameworkCore -ErrorAction SilentlyContinue
    }
}

exit $codigoSalidaFinal
