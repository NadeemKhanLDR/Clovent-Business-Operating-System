@echo off
setlocal EnableExtensions

set "APP_DIR=%~dp0"
set "EXE_PATH=%APP_DIR%Clovent.Desktop.exe"

if not exist "%EXE_PATH%" (
    echo [ERROR] Clovent.Desktop.exe not found at "%EXE_PATH%"
    pause
    exit /b 1
)

:: Require an explicit QA root and approved target.
set "QA_ROOT=%~1"
if "%QA_ROOT%"=="" set "QA_ROOT=%CBOS_QA_ROOT%"

if "%QA_ROOT%"=="" (
    echo ===============================================================================
    echo CLOVENT BUSINESS OPERATING SYSTEM - INTERNAL QA LAUNCHER
    echo ===============================================================================
    echo This is an internal QA installation. Normal-mode startup is disabled.
    echo QA mode requires an explicit validated QA root directory and approved target.
    echo.
    echo Please enter the absolute path to your isolated QA instance root:
    echo (e.g. C:\CBOS_ManualQA_20261111\qa-instance)
    set /p "QA_ROOT=QA Root: "
)

if "%QA_ROOT%"=="" (
    echo [ERROR] QA Root directory is required. Launch aborted.
    pause
    exit /b 1
)

:: Strip surrounding quotes if present
set "QA_ROOT=%QA_ROOT:"=%"

if not exist "%QA_ROOT%" (
    echo [ERROR] Specified QA Root directory does not exist: "%QA_ROOT%"
    pause
    exit /b 1
)

set "APPROVED_TARGET_FILE=%QA_ROOT%\Config\qa-target.approved.json"
if not exist "%APPROVED_TARGET_FILE%" (
    echo [ERROR] Target approval configuration not found: "%APPROVED_TARGET_FILE%"
    echo QA execution requires an explicit approved target pin file.
    pause
    exit /b 1
)

:: Clear ambient variables that could leak production connections
set "CBOS_QA_ROOT="
set "CBOS_QA_CONNECTION="
set "CBOS_QA_APPROVED_TARGET="
set "ConnectionStrings__Default="
set "ConnectionStrings__DefaultConnection="
set "ConnectionStrings__Restaurant="

echo [INFO] Launching CBOS in QA isolation...
echo QA Root: %QA_ROOT%
pushd "%QA_ROOT%"
"%EXE_PATH%" --qa-root "%QA_ROOT%"
set "EXIT_CODE=%ERRORLEVEL%"
popd

if not "%EXIT_CODE%"=="0" (
    echo [ERROR] Clovent.Desktop.exe exited with code %EXIT_CODE%.
    pause
)
exit /b %EXIT_CODE%
