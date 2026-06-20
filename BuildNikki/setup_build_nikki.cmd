@echo off
chcp 65001 >nul
cd /d %~dp0

@echo [prepare version]
cd /d ..\BetterInfinityNikki
set "script=Get-Content 'BetterInfinityNikki.csproj' | Select-String -Pattern '<Version>(.*)</Version>' | ForEach-Object { $_.Matches.Groups[1].Value }"
for /f "usebackq delims=" %%i in (`powershell -NoLogo -NoProfile -Command "%script%"`) do set version=%%i
echo current version is %version%
if "%~1"=="" (
    set /p "b=请输入自定义版本号（直接回车使用文件内版本号 %version%）："
) else (
    set "b=%~1"
)
if "%b%"=="" ( set "b=%version%" )

cd /d %~dp0
set "publishDir=%~dp0..\BetterInfinityNikki\bin\x64\Release\net8.0-windows10.0.22621.0\publish\win-x64"

@echo [build app]
if exist "%publishDir%" rd /s /q "%publishDir%"
cd /d ..
dotnet publish BetterInfinityNikki\BetterInfinityNikki.csproj -c Release -p:Platform=x64 -p:PublishSingleFile=true -p:SelfContained=false -p:RuntimeIdentifier=win-x64 -p:PublishDir=bin\x64\Release\net8.0-windows10.0.22621.0\publish\win-x64\ -p:Version=%b%

@echo [clean unnecessary files]
cd /d %~dp0
del /f /q "%publishDir%\*.lib" 2>nul
del /f /q "%publishDir%\*ffmpeg*.dll" 2>nul
del /f /q "%publishDir%\*.pdb" 2>nul

set "iscc="
for %%p in (
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    "D:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    "C:\Program Files\Inno Setup 6\ISCC.exe"
    "D:\Program Files\Inno Setup 6\ISCC.exe"
) do if exist %%p set "iscc=%%~p"
if not defined iscc (
    echo [错误] 未找到 Inno Setup，请先安装：
    echo   winget install JRSoftware.InnoSetup
    goto :end
)

@echo [build setup using Inno Setup]
set "BETTERIN_VERSION=%b%"
"%iscc%" betterin.iss
if exist "BetterIN_Setup_%b%.exe" (
    echo.
    echo ========================================
    echo  离线安装包已生成: BetterIN_Setup_%b%.exe
    echo ========================================
) else (
    echo [错误] Inno Setup 生成安装包失败。
)

:end
@pause
