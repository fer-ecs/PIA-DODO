#!/usr/bin/env pwsh

# Script para ejecutar VINOTECA con PowerShell
# Este script compila y ejecuta la aplicación automáticamente

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                  VINOTECA - LAUNCHER                      ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Navegar a la carpeta del proyecto
$projectPath = "C:\Users\erick\OneDrive\Documents\6to\PAL\PIA-DODO\Vinoteca"

if (-not (Test-Path $projectPath)) {
    Write-Host "ERROR: La carpeta del proyecto no existe: $projectPath" -ForegroundColor Red
    exit 1
}

Set-Location $projectPath
Write-Host "📁 Ubicación: $projectPath" -ForegroundColor Green
Write-Host ""

# Limpiar compilación anterior
Write-Host "🧹 Limpiando compilación anterior..." -ForegroundColor Yellow
dotnet clean -q 2>&1 | Out-Null
Write-Host "✅ Limpieza completada" -ForegroundColor Green
Write-Host ""

# Compilar
Write-Host "🔨 Compilando proyecto..." -ForegroundColor Yellow
dotnet build -c Debug -q

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: La compilación falló" -ForegroundColor Red
    Write-Host ""
    Write-Host "Intenta nuevamente compilando con más detalles:" -ForegroundColor Yellow
    Write-Host "dotnet build -c Debug" -ForegroundColor Cyan
    exit 1
}

Write-Host "✅ Compilación exitosa" -ForegroundColor Green
Write-Host ""

# Ejecutar
Write-Host "🚀 Ejecutando VINOTECA..." -ForegroundColor Yellow
Write-Host ""
Write-Host "Espera 10-15 segundos para que se abra la aplicación..." -ForegroundColor Cyan
Write-Host ""

dotnet run --no-build

Write-Host ""
Write-Host "👋 Aplicación cerrada" -ForegroundColor Yellow
