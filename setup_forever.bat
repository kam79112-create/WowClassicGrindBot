@echo off
REM ============================================================
REM  WowClassicGrindBot - WoW: Forever setup script
REM  Run once from the repo root on your Windows machine.
REM ============================================================

echo [1/4] Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet not found. Install .NET 10 SDK from https://dot.net
    pause & exit /b 1
)

echo [2/4] Building solution (Release)...
dotnet build MasterOfPuppets.sln -c Release --nologo -v quiet
if errorlevel 1 (
    echo ERROR: Build failed. See output above.
    pause & exit /b 1
)
echo Build OK.

echo [3/4] Installing addon...
REM Change WOW_PATH to your actual WoW: Forever install directory.
set WOW_PATH=C:\Program Files (x86)\World of Warcraft\_forever_

if not exist "%WOW_PATH%\Interface\AddOns" (
    echo Creating AddOns directory...
    mkdir "%WOW_PATH%\Interface\AddOns"
)

REM Copy addon files
xcopy /E /Y /I "Addons\DataToColor" "%WOW_PATH%\Interface\AddOns\DataToColor\"

REM Apply Forever TOC (swap _Forever.toc over DataToColor.toc)
copy /Y "%WOW_PATH%\Interface\AddOns\DataToColor\DataToColor_Forever.toc" ^
         "%WOW_PATH%\Interface\AddOns\DataToColor\DataToColor.toc"
echo Addon installed with Interface: 16001

echo [4/4] Done! Now:
echo   1. Start WoW: Forever, log in
echo   2. Enable DataToColor addon in addon list
echo   3. Check in chat: /dump C_AddOns.GetAddOnMetadata("DataToColor","Notes")
echo      Expected: "Displays data as colors (Forever)"
echo   4. Paste the "DataToColor: Forever build ... missing globals: ..." line here
echo   5. Start BlazorServer: dotnet run --project BlazorServer
echo      Then open http://localhost:5000 and pick Mage_22_Frost_Forever profile
echo.
echo NOTE: Json/dbc/forever is a copy of SoM data (interim).
echo       Spell IDs for the Mage profile are correct.
echo       Talent trainer UI may show wrong data until you replace it.
pause
