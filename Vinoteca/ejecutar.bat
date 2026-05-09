@echo off
setlocal

set "PROJECT_ROOT=%~dp0"
set "PROJECT_FILE=%PROJECT_ROOT%Vinoteca.csproj"

echo VINOTECA - launcher portable
echo.

if not exist "%PROJECT_FILE%" (
    echo ERROR: No se encontro Vinoteca.csproj en "%PROJECT_ROOT%"
    pause
    exit /b 1
)

where dotnet > nul 2>&1
if errorlevel 1 (
    echo ERROR: No se encontro dotnet. Instala .NET 8 SDK y vuelve a intentar
    pause
    exit /b 1
)

pushd "%PROJECT_ROOT%"

echo Ubicacion: %PROJECT_ROOT%
for /f "delims=" %%v in ('dotnet --version') do echo SDK dotnet: %%v
echo.

echo Limpiando compilacion anterior...
dotnet clean "Vinoteca.sln" -c Debug -v quiet
if errorlevel 1 goto error
echo.

echo Restaurando paquetes...
dotnet restore "Vinoteca.sln"
if errorlevel 1 goto error
echo.

echo Compilando proyecto...
dotnet build "Vinoteca.sln" -c Debug --no-restore
if errorlevel 1 goto error
echo.

echo Ejecutando VINOTECA...
dotnet run --project "%PROJECT_FILE%" --no-build
set "EXIT_CODE=%ERRORLEVEL%"
popd
pause
exit /b %EXIT_CODE%

:error
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo ERROR: No se pudo preparar o ejecutar el proyecto
popd
pause
exit /b %EXIT_CODE%
