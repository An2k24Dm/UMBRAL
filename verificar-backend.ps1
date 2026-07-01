[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# El script vive en la raíz del repositorio: la raíz es su propia carpeta.
$raiz = $PSScriptRoot
$resultados = Join-Path $raiz 'artifacts\test-results'

# Todas las suites de pruebas del backend disponibles. Se ejecutan todas por
# defecto (restore + build una sola vez, luego cada suite con --no-build).
$suites = @(
    @{ Nombre = 'sesiones-unitarias'
       Proyecto = 'backend\pruebas\sesiones\SesionesServicio.PruebasUnitarias\SesionesServicio.PruebasUnitarias.csproj' }
    @{ Nombre = 'sesiones-integracion'
       Proyecto = 'backend\pruebas\sesiones\SesionesServicio.PruebasIntegracion\SesionesServicio.PruebasIntegracion.csproj' }
    @{ Nombre = 'identidad-unitarias'
       Proyecto = 'backend\pruebas\identidad\IdentidadServicio.PruebasUnitarias\IdentidadServicio.PruebasUnitarias.csproj' }
    @{ Nombre = 'identidad-integracion'
       Proyecto = 'backend\pruebas\identidad\IdentidadServicio.PruebasIntegracion\IdentidadServicio.PruebasIntegracion.csproj' }
    @{ Nombre = 'juegos-unitarias'
       Proyecto = 'backend\pruebas\juegos\JuegosServicio.PruebasUnitarias\JuegosServicio.PruebasUnitarias.csproj' }
)

function Confirmar-UltimoComando {
    param([string]$Nombre)

    if ($LASTEXITCODE -ne 0) {
        throw "$Nombre falló con código de salida $LASTEXITCODE."
    }
}

Push-Location $raiz
try {
    dotnet restore UMBRAL.sln
    Confirmar-UltimoComando 'dotnet restore'

    dotnet build UMBRAL.sln --no-restore --configuration Release
    Confirmar-UltimoComando 'dotnet build'

    foreach ($suite in $suites) {
        $nombre = $suite.Nombre
        dotnet test $suite.Proyecto `
            --no-build `
            --configuration Release `
            --collect:'XPlat Code Coverage' `
            --logger "trx;LogFileName=$nombre.trx" `
            --results-directory (Join-Path $resultados $nombre)
        Confirmar-UltimoComando "Pruebas: $nombre"
    }

    Write-Host "Verificación backend completada."
    Write-Host "Resultados y cobertura: $resultados"
}
finally {
    Pop-Location
}
