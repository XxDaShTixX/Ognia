@echo off
setlocal

set /p VERSION=Version (e.g. 0.0.2): 
if "%VERSION%"=="" (
    echo No version entered.
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build.ps1" -Version "%VERSION%"

if errorlevel 1 (
    echo.
    echo Build failed.
    pause
    exit /b 1
)

pause