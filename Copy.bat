@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "WORK_ROOT="

if exist "%SCRIPT_DIR%Dignus" (
    set "WORK_ROOT=%SCRIPT_DIR%"
) else if exist "%SCRIPT_DIR%..\Dignus" (
    set "WORK_ROOT=%SCRIPT_DIR%..\"
) else if exist "%SCRIPT_DIR%..\..\..\..\Dignus" (
    set "WORK_ROOT=%SCRIPT_DIR%..\..\..\..\"
) else (
    echo [ERROR] Source root not found.
    echo Script Path: %SCRIPT_DIR%
    pause
    exit /b 1
)

for %%I in ("%WORK_ROOT%") do set "WORK_ROOT=%%~fI"

set "SOURCE_DIGNUS=%WORK_ROOT%\Dignus"
set "SOURCE_DIGNUS_UNITY=%WORK_ROOT%\Dignus.Unity"

if exist "%SCRIPT_DIR%Lib" (
    set "DEST_ROOT=%SCRIPT_DIR%Lib"
) else (
    set "DEST_ROOT=%WORK_ROOT%\UnityTest\Assets\Plugins\Lib"
)

set "DEST=%DEST_ROOT%\Dignus"

echo ---------------------------
echo [COPY] Dignus → Unity Plugins (filtered)
echo Source: %SOURCE_DIGNUS%
echo Target: %DEST%
echo ---------------------------

REM 기존 복사본 제거
if exist "%DEST%" (
    echo Removing old copy...
    rmdir /S /Q "%DEST%"
)

REM 복사 - 불필요한 디렉토리/파일 제외
robocopy "%SOURCE_DIGNUS%" "%DEST%" /E /XD bin obj Properties .vs *.dll /XF *.csproj.user

if %ERRORLEVEL% LSS 8 (
    echo [OK] Dignus copied with filters.
) else (
    echo [ERROR] Robocopy failed.
    pause
    exit /b 1
)

set "DEST=%DEST_ROOT%\Dignus.Unity"

echo ---------------------------
echo [COPY] Dignus.Unity → Unity Plugins (filtered)
echo Source: %SOURCE_DIGNUS_UNITY%
echo Target: %DEST%
echo ---------------------------

REM 기존 복사본 제거
if exist "%DEST%" (
    echo Removing old copy...
    rmdir /S /Q "%DEST%"
)

REM 복사 - 불필요한 디렉토리/파일 제외
robocopy "%SOURCE_DIGNUS_UNITY%" "%DEST%" /E /XD bin obj Properties .vs *.dll /XF *.csproj.user

if %ERRORLEVEL% LSS 8 (
    echo [OK] Dignus.Unity copied with filters.
) else (
    echo [ERROR] Robocopy failed.
    pause
    exit /b 1
)
