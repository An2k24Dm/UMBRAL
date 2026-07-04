[CmdletBinding()]
param(
    [switch]$Silencioso
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$raiz = $PSScriptRoot
$resultados = Join-Path $raiz 'artifacts\test-results'

if ($Silencioso) {
    $env:Logging__LogLevel__Default = "Warning"
    $env:Logging__LogLevel__Microsoft = "Warning"
    $env:Logging__LogLevel__Microsoft_AspNetCore = "Warning"
    $env:Logging__LogLevel__Microsoft_EntityFrameworkCore = "Warning"
}

$suites = @(
    @{
        Nombre = 'sesiones-unitarias'
        Proyecto = 'backend\pruebas\sesiones\SesionesServicio.PruebasUnitarias\SesionesServicio.PruebasUnitarias.csproj'
    },
    @{
        Nombre = 'sesiones-integracion'
        Proyecto = 'backend\pruebas\sesiones\SesionesServicio.PruebasIntegracion\SesionesServicio.PruebasIntegracion.csproj'
    },
    @{
        Nombre = 'identidad-unitarias'
        Proyecto = 'backend\pruebas\identidad\IdentidadServicio.PruebasUnitarias\IdentidadServicio.PruebasUnitarias.csproj'
    },
    @{
        Nombre = 'identidad-integracion'
        Proyecto = 'backend\pruebas\identidad\IdentidadServicio.PruebasIntegracion\IdentidadServicio.PruebasIntegracion.csproj'
    },
    @{
        Nombre = 'juegos-unitarias'
        Proyecto = 'backend\pruebas\juegos\JuegosServicio.PruebasUnitarias\JuegosServicio.PruebasUnitarias.csproj'
    }
)

function Confirmar-UltimoComando {
    param(
        [string]$Nombre
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$Nombre fallo con codigo de salida $LASTEXITCODE."
    }
}

function Obtener-ResumenTrx {
    param(
        [string]$RutaTrx
    )

    [xml]$xml = Get-Content $RutaTrx

    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

    $counters = $xml.SelectSingleNode("//t:ResultSummary/t:Counters", $ns)

    if ($null -eq $counters) {
        return $null
    }

    $fallidas = @()
    $resultadosFallidos = $xml.SelectNodes("//t:UnitTestResult[@outcome='Failed']", $ns)

    foreach ($resultado in $resultadosFallidos) {
        $testId = $resultado.testId
        $definicion = $xml.SelectSingleNode("//t:UnitTest[@id='$testId']", $ns)

        if ($null -ne $definicion) {
            $nombrePrueba = $definicion.name
        }
        else {
            $nombrePrueba = $resultado.testName
        }

        $mensajeNodo = $resultado.SelectSingleNode("t:Output/t:ErrorInfo/t:Message", $ns)

        if ($null -ne $mensajeNodo) {
            $mensaje = $mensajeNodo.InnerText
        }
        else {
            $mensaje = ""
        }

        $fallidas += [PSCustomObject]@{
            Nombre = $nombrePrueba
            Mensaje = $mensaje
        }
    }

    return [PSCustomObject]@{
        Total    = [int]$counters.total
        Pasaron  = [int]$counters.passed
        Fallaron = [int]$counters.failed
        Omitidas = [int]$counters.notExecuted
        Fallidas = $fallidas
    }
}

Push-Location $raiz

try {
    Write-Host "Restaurando paquetes..."
    dotnet restore UMBRAL.sln --verbosity quiet
    Confirmar-UltimoComando 'dotnet restore'

    Write-Host "Compilando solucion..."
    dotnet build UMBRAL.sln --no-restore --configuration Release --verbosity quiet
    Confirmar-UltimoComando 'dotnet build'

    Write-Host ""
    Write-Host "Ejecutando suites de pruebas..."
    Write-Host ""

    $resumenGlobal = @()

    foreach ($suite in $suites) {
        $nombre = $suite.Nombre
        $directorioSuite = Join-Path $resultados $nombre
        $logSuite = Join-Path $directorioSuite "$nombre.log"

        if (Test-Path $directorioSuite) {
            Remove-Item $directorioSuite -Recurse -Force
        }

        New-Item -ItemType Directory -Force -Path $directorioSuite | Out-Null

        Write-Host "Ejecutando: $nombre"

        dotnet test $suite.Proyecto `
            --no-build `
            --configuration Release `
            --verbosity quiet `
            --collect:"XPlat Code Coverage" `
            --logger "trx;LogFileName=$nombre.trx" `
            --results-directory $directorioSuite *> $logSuite

        $codigoSalida = $LASTEXITCODE

        $trx = Get-ChildItem $directorioSuite -Recurse -Filter *.trx |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if ($null -eq $trx) {
            Write-Host "  No se encontro archivo TRX para $nombre." -ForegroundColor Yellow
            Write-Host "  Revisa el log: $logSuite" -ForegroundColor Yellow

            if ($codigoSalida -ne 0) {
                throw "Pruebas: $nombre fallo con codigo de salida $codigoSalida."
            }

            continue
        }

        $resumen = Obtener-ResumenTrx $trx.FullName

        if ($null -eq $resumen) {
            Write-Host "  No se pudo leer resumen TRX de $nombre." -ForegroundColor Yellow
            Write-Host "  TRX: $($trx.FullName)" -ForegroundColor Yellow
            Write-Host "  Log: $logSuite" -ForegroundColor Yellow

            if ($codigoSalida -ne 0) {
                throw "Pruebas: $nombre fallo con codigo de salida $codigoSalida."
            }

            continue
        }

        $resumenGlobal += [PSCustomObject]@{
            Suite    = $nombre
            Total    = $resumen.Total
            Pasaron  = $resumen.Pasaron
            Fallaron = $resumen.Fallaron
            Omitidas = $resumen.Omitidas
            Log      = $logSuite
            Trx      = $trx.FullName
        }

        if ($resumen.Fallaron -eq 0 -and $codigoSalida -eq 0) {
            Write-Host "  OK - Total=$($resumen.Total), Pasaron=$($resumen.Pasaron), Fallaron=$($resumen.Fallaron), Omitidas=$($resumen.Omitidas)" -ForegroundColor Green
        }
        else {
            Write-Host "  FALLO - Total=$($resumen.Total), Pasaron=$($resumen.Pasaron), Fallaron=$($resumen.Fallaron), Omitidas=$($resumen.Omitidas)" -ForegroundColor Red
            Write-Host "  Pruebas fallidas:" -ForegroundColor Red

            foreach ($fallida in $resumen.Fallidas) {
                Write-Host "   - $($fallida.Nombre)" -ForegroundColor Red

                if (-not [string]::IsNullOrWhiteSpace($fallida.Mensaje)) {
                    Write-Host "     $($fallida.Mensaje)" -ForegroundColor DarkRed
                }
            }

            Write-Host "  Log completo: $logSuite" -ForegroundColor Yellow
            throw "Pruebas: $nombre fallo con codigo de salida $codigoSalida."
        }
    }

    Write-Host ""
    Write-Host "Resumen final de pruebas:" -ForegroundColor Cyan
    Write-Host ""

    $resumenGlobal | Format-Table Suite, Total, Pasaron, Fallaron, Omitidas -AutoSize

    $total = ($resumenGlobal | Measure-Object Total -Sum).Sum
    $pasaron = ($resumenGlobal | Measure-Object Pasaron -Sum).Sum
    $fallaron = ($resumenGlobal | Measure-Object Fallaron -Sum).Sum
    $omitidas = ($resumenGlobal | Measure-Object Omitidas -Sum).Sum

    Write-Host ""
    Write-Host "Total general: $total"
    Write-Host "Pasaron: $pasaron" -ForegroundColor Green

    if ($fallaron -eq 0) {
        Write-Host "Fallaron: $fallaron" -ForegroundColor Green
    }
    else {
        Write-Host "Fallaron: $fallaron" -ForegroundColor Red
    }

    Write-Host "Omitidas: $omitidas"
    Write-Host ""
    Write-Host "Verificacion backend completada correctamente." -ForegroundColor Green
    Write-Host "Resultados y cobertura: $resultados"
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