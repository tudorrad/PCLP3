@echo off
setlocal
cd /d "%~dp0"

set LOGFILE=%~dp0build_log_editor.txt

echo =====================================================  > "%LOGFILE%"
echo   STORY EDITOR - Compilare si pornire automata          >> "%LOGFILE%"
echo =====================================================  >> "%LOGFILE%"

echo =====================================================
echo   STORY EDITOR - Compilare si pornire automata
echo =====================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo EROARE: nu gasesc comanda "dotnet" in sistem.
    echo Instaleaza .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [1/4] Compilez StoryEngine.Model...
dotnet build "StoryEngine.Model\StoryEngine.Model.csproj" -c Debug >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo EROARE la compilarea StoryEngine.Model! Vezi build_log_editor.txt
    type "%LOGFILE%"
    pause
    exit /b 1
)
echo       OK

echo [2/4] Compilez StoryEngine.Engine...
dotnet build "StoryEngine.Engine\StoryEngine.Engine.csproj" -c Debug >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo EROARE la compilarea StoryEngine.Engine! Vezi build_log_editor.txt
    type "%LOGFILE%"
    pause
    exit /b 1
)
echo       OK

echo [3/4] Compilez StoryEngine.Persistence...
dotnet build "StoryEngine.Persistence\StoryEngine.Persistence.csproj" -c Debug >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo EROARE la compilarea StoryEngine.Persistence! Vezi build_log_editor.txt
    type "%LOGFILE%"
    pause
    exit /b 1
)
echo       OK

echo [4/4] Compilez StoryEngine.Editor...
dotnet build "StoryEngine.Editor\StoryEngine.Editor.csproj" -c Debug >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo EROARE la compilarea StoryEngine.Editor! Vezi build_log_editor.txt
    echo.
    echo ----- Continut build_log_editor.txt -----
    type "%LOGFILE%"
    echo -------------------------------------------
    pause
    exit /b 1
)
echo       OK
echo.
echo Toate proiectele compilate cu succes!
echo.

set EXE=StoryEngine.Editor\bin\Debug\net8.0-windows\StoryEngine.Editor.exe

if not exist "%EXE%" (
    echo EROARE: build-ul a raportat succes dar nu gasesc:
    echo   %EXE%
    pause
    exit /b 1
)

echo Pornesc aplicatia: %EXE%
start "" "%EXE%"

echo Aplicatia a fost lansata. Aceasta fereastra se poate inchide.
timeout /t 5
endlocal
