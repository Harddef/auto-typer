@echo off
echo ========================================
echo Auto Typer - Build and Run
echo ========================================
echo.

echo Checking .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not installed!
    echo Download from https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo .NET SDK found!
echo.

echo Building project...
dotnet build
if %errorlevel% neq 0 (
    echo.
    echo BUILD ERROR!
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo Starting application...
echo.

dotnet run
