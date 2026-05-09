# Vinoteca

Aplicacion WinUI 3 para gestion de vinoteca con roles de administrador, supervisor y cliente.

## Requisitos

- Windows 10 1809 o superior
- .NET 8 SDK
- Acceso a NuGet para restaurar paquetes

## Ejecutar en cualquier computadora

Desde la carpeta donde clonaste el repositorio:

```powershell
.\ejecutar.ps1
```

Si PowerShell bloquea scripts en tu equipo:

```powershell
powershell -ExecutionPolicy Bypass -File .\ejecutar.ps1
```

Tambien puedes usar:

```bat
ejecutar.bat
```

## Comandos manuales

```powershell
dotnet restore Vinoteca.sln
dotnet build Vinoteca.sln
dotnet run --project Vinoteca.csproj
```

## Datos locales

Los datos de prueba y ejecucion se guardan por usuario en:

```text
%LOCALAPPDATA%\Vinoteca\Data
```

Esto evita depender de rutas de la computadora donde se programo el sistema.
