param(
    [switch]$SkipClean
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$projectFile = Join-Path $projectRoot "Vinoteca.csproj"

Write-Host "VINOTECA - launcher portable" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $projectFile)) {
    Write-Host "ERROR: No se encontro Vinoteca.csproj en: $projectRoot" -ForegroundColor Red
    exit 1
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: No se encontro dotnet. Instala .NET 8 SDK y vuelve a intentar" -ForegroundColor Red
    exit 1
}

Push-Location $projectRoot
try {
    Write-Host "Ubicacion: $projectRoot" -ForegroundColor Green
    Write-Host "SDK dotnet: $(dotnet --version)" -ForegroundColor Green
    Write-Host ""

    if (-not $SkipClean) {
        Write-Host "Limpiando compilacion anterior..." -ForegroundColor Yellow
        dotnet clean "Vinoteca.sln" -c Debug -v quiet
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        Write-Host "Limpieza completada" -ForegroundColor Green
        Write-Host ""
    }

    Write-Host "Restaurando paquetes..." -ForegroundColor Yellow
    dotnet restore "Vinoteca.sln"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host ""

    Write-Host "Compilando proyecto..." -ForegroundColor Yellow
    dotnet build "Vinoteca.sln" -c Debug --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "ERROR: La compilacion fallo" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "Ejecutando VINOTECA..." -ForegroundColor Yellow
    dotnet run --project $projectFile --no-build
}
finally {
    Pop-Location
}
