@echo off
setlocal

set SOURCE=..\..\..\Dignus
set DEST=Lib\Dignus

echo ---------------------------
echo [COPY] Dignus → Unity Plugins (filtered)
echo Source: %SOURCE%
echo Target: %DEST%
echo ---------------------------

REM 기존 복사본 제거
if exist %DEST% (
    echo Removing old copy...
    rmdir /S /Q %DEST%
)

REM 복사 - 불필요한 디렉토리/파일 제외
robocopy %SOURCE% %DEST% /E /XD bin obj Properties .vs *.dll /XF *.csproj.user

if %ERRORLEVEL% LSS 8 (
    echo [OK] Dignus copied with filters.
) else (
    echo [ERROR] Robocopy failed.
    pause
    exit /b 1
)


set SOURCE=..\..\..\Dignus.Unity
set DEST=Lib\Dignus.Unity

echo ---------------------------
echo [COPY] Dignus.Unity → Unity Plugins (filtered)
echo Source: %SOURCE%
echo Target: %DEST%
echo ---------------------------

REM 기존 복사본 제거
if exist %DEST% (
    echo Removing old copy...
    rmdir /S /Q %DEST%
)

REM 복사 - 불필요한 디렉토리/파일 제외
robocopy %SOURCE% %DEST% /E /XD bin obj Properties .vs *.dll /XF *.csproj.user

if %ERRORLEVEL% LSS 8 (
    echo [OK] Dignus.Unity copied with filters.
) else (
    echo [ERROR] Robocopy failed.
    pause
    exit /b 1
)