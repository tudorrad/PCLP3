@echo off
setlocal
cd /d "%~dp0"

set LOGFILE=%~dp0build_log.txt

echo =====================================================
echo   STORY ENGINE - Compilare CURATA si pornire
echo =====================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo EROARE: nu gasesc comanda "dotnet" in sistem.
    echo Instaleaza .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Sterg fisiere de build vechi (bin/obj) pentru o
echo compilare 100%% curata, fara cache...
echo.

for /d /r %%d in (bin,obj) do (
    if exist "%%d" (
        echo   Sterg: %%d
        rmdir /s /q "%%d" 2>nul
    )
)

echo. > "%LOGFILE%"
echo Compilare curata terminata. Pornesc build-ul...
echo.

echo [1/4] Compilez StoryEngine.Model...
dotnet build "StoryEngine.Model\StoryEngine.Model.csproj" -c Debug --no-incremental >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo EROARE la compilarea StoryEngine.Model! Vezi build_log.txt
    type "%LOGFILE%"
    pause
    exit /b 1
)
echo       OK

echo [2/4] Compilez StoryEngine.Engine...
dotnet build "StoryEngine.Engine\StoryEngine.Engine.csproj" -c Debug --no-incremental >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo EROARE la compilarea StoryEngine.Engine! Vezi build_log.txt
    type "%LOGFILE%"
    pause
    exit /b 1
)
echo       OK

echo [3/4] Compilez StoryEngine.Persistence...
dotnet build "StoryEngine.Persistence\StoryEngine.Persistence.csproj" -c Debug --no-incremental >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo EROARE la compilarea StoryEngine.Persistence! Vezi build_log.txt
    type "%LOGFILE%"
    pause
    exit /b 1
)
echo       OK

echo [4/4] Compilez StoryEngine.Player...
dotnet build "StoryEngine.Player\StoryEngine.Player.csproj" -c Debug --no-incremental >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo EROARE la compilarea StoryEngine.Player! Vezi build_log.txt
    echo.
    echo ----- Continut build_log.txt -----
    type "%LOGFILE%"
    echo -----------------------------------
    pause
    exit /b 1
)
echo       OK
echo.
echo Toate proiectele compilate CURAT, fara cache!
echo.

set EXE=StoryEngine.Player\bin\Debug\net8.0-windows\StoryEngine.Player.exe

if not exist "%EXE%" (
    echo EROARE: build-ul a raportat succes dar nu gasesc:
    echo   %EXE%
    dir /s /b "StoryEngine.Player\bin" 2>nul
    pause
    exit /b 1
)

echo Fisier executabil: %EXE%
for %%F in ("%EXE%") do echo Data compilare: %%~tF

echo.
echo Pornesc aplicatia...
start "" "%EXE%"

echo.
echo Aplicatia a fost lansata. Aceasta fereastra se poate inchide.
echo.
echo Daca tot nu apar imagini, verifica:
echo   %%TEMP%%\StoryEngine_Player_diagnostic.log
echo.
timeout /t 5
endlocal
