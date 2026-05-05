@echo off
REM Script para ejecutar VINOTECA
REM Cambia al directorio del proyecto
cd /d "C:\Users\erick\OneDrive\Documents\6to\PAL\PIA-DODO\Vinoteca"

REM Limpiar compilación anterior (opcional)
echo Limpiando compilación anterior...
dotnet clean > nul 2>&1

REM Compilar
echo Compilando proyecto...
dotnet build -c Debug

REM Verificar si la compilación fue exitosa
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: La compilación falló
    pause
    exit /b 1
)

REM Ejecutar la aplicación
echo.
echo Ejecutando VINOTECA...
dotnet run --no-build

pause
