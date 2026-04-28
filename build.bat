@echo off
echo Building Auto Typer...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
if %errorlevel% neq 0 (
    echo BUILD ERROR!
    pause
    exit /b 1
)

echo.
echo Build completed!
echo Executable created: publish\AutoTyper.exe
echo.
echo Starting application...
start publish\AutoTyper.exe
