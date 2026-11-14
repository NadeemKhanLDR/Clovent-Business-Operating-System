@echo off
setlocal EnableExtensions

set "APP_DIR=%~dp0"
set "EXE_PATH=%APP_DIR%Clovent.Desktop.exe"

if not exist "%EXE_PATH%" (
    echo [ERROR] Clovent.Desktop.exe not found at "%EXE_PATH%"
    pause
    exit /b 1
)

:: 1. Resolve QA_ROOT: CLI argument (%~1) -> QA_ROOT -> CBOS_QA_INSTANCE_ROOT -> CBOS_QA_ROOT -> Default %ProgramData%\CBOS_QA_Acceptance
if not "%~1"=="" set "QA_ROOT=%~1"
if not defined QA_ROOT (
    if defined CBOS_QA_INSTANCE_ROOT set "QA_ROOT=%CBOS_QA_INSTANCE_ROOT%"
)
if not defined QA_ROOT (
    if defined CBOS_QA_ROOT set "QA_ROOT=%CBOS_QA_ROOT%"
)
if not defined QA_ROOT (
    set "QA_ROOT=%ProgramData%\CBOS_QA_Acceptance"
)

:: 2. Strip surrounding quotes and whitespace
if defined QA_ROOT set "QA_ROOT=%QA_ROOT:"=%"
if defined QA_ROOT (
    for /f "tokens=* delims= " %%A in ("%QA_ROOT%") do set "QA_ROOT=%%A"
)

if not defined QA_ROOT (
    echo [ERROR] QA Root directory is required. Launch aborted.
    pause
    exit /b 1
)

:: Strip trailing backslash unless drive root (e.g. C:\)
if "%QA_ROOT:~-1%"=="\" if not "%QA_ROOT:~-2%"==":\" set "QA_ROOT=%QA_ROOT:~0,-1%"

:: 3. Validate existence of QA instance root directory
if not exist "%QA_ROOT%\" (
    echo [ERROR] Specified QA Root directory does not exist or is not a directory: "%QA_ROOT%"
    echo Please verify the installation or run setup to configure the acceptance instance.
    pause
    exit /b 1
)

:: 4. Validate target approval pin file exists and is non-empty
set "APPROVED_TARGET_FILE=%QA_ROOT%\Config\qa-target.approved.json"
if not exist "%APPROVED_TARGET_FILE%" (
    echo [ERROR] Target approval configuration not found: "%APPROVED_TARGET_FILE%"
    echo QA acceptance execution requires an explicit approved target pin file.
    pause
    exit /b 1
)

for %%F in ("%APPROVED_TARGET_FILE%") do (
    if %%~zF EQU 0 (
        echo [ERROR] Target approval pin file is empty: "%APPROVED_TARGET_FILE%"
        pause
        exit /b 1
    )
)

:: 5. Clear ambient variables that could leak production connections
set "CBOS_QA_INSTANCE_ROOT="
set "CBOS_QA_ROOT="
set "CBOS_QA_CONNECTION_STRING="
set "CBOS_QA_CONNECTION="
set "CBOS_QA_APPROVED_TARGET="
set "ConnectionStrings__Default="
set "ConnectionStrings__DefaultConnection="
set "ConnectionStrings__Restaurant="

:: 6. Optional secondary parameters (e.g. --diagnostics)
set "EXTRA_PARAMS="
if not "%~2"=="" (
    set "EXTRA_PARAMS=%~2"
)

:: 7. Launch CBOS in strict QA mode confined to the validated QA root
echo [INFO] Launching CBOS in Internal Acceptance Mode...
echo QA Root: %QA_ROOT%
pushd "%QA_ROOT%"
if defined EXTRA_PARAMS (
    "%EXE_PATH%" --qa-root "%QA_ROOT%" %EXTRA_PARAMS%
) else (
    "%EXE_PATH%" --qa-root "%QA_ROOT%"
)
set "EXIT_CODE=%ERRORLEVEL%"
popd

if not "%EXIT_CODE%"=="0" (
    echo [ERROR] Clovent.Desktop.exe exited with code %EXIT_CODE%.
    pause
)
exit /b %EXIT_CODE%
