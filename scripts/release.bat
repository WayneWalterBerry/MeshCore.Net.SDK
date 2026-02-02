@echo off
REM Release Script for MeshCore.Net.SDK (Windows version)
REM This script helps create and push a new release tag to trigger the GitHub Actions workflow

setlocal EnableDelayedExpansion

if "%1"=="" (
    echo [ERROR] No version specified!
    echo Usage: %0 ^<version^>
    echo Example: %0 1.0.0
    echo Example: %0 1.1.0-beta.1
    exit /b 1
)

set VERSION=%1

echo [INFO] Starting release process for version %VERSION%

REM Check if we're on the main branch
for /f "tokens=*" %%a in ('git branch --show-current') do set current_branch=%%a

if not "%current_branch%"=="main" (
    echo [WARNING] You are not on the main branch ^(current: %current_branch%^)
    set /p continue="Continue anyway? (y/N): "
    if /i not "!continue!"=="y" (
        echo [ERROR] Release cancelled
        exit /b 1
    )
)

REM Check if working directory is clean
git diff-index --quiet HEAD -- >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Working directory is not clean. Please commit or stash your changes.
    exit /b 1
)

REM Check if tag already exists
git tag -l | findstr /C:"v%VERSION%" >nul 2>&1
if not errorlevel 1 (
    echo [ERROR] Tag v%VERSION% already exists!
    exit /b 1
)

REM Run tests before creating release
echo [INFO] Running tests...
dotnet test --configuration Release --verbosity minimal
if errorlevel 1 (
    echo [ERROR] Tests failed! Please fix the tests before releasing.
    exit /b 1
)

REM Remind user to update changelog
echo [WARNING] Please ensure CHANGELOG.md has been updated with changes for version %VERSION%
set /p updated="Have you updated CHANGELOG.md? (y/N): "
if /i not "%updated%"=="y" (
    echo [ERROR] Please update CHANGELOG.md before releasing
    exit /b 1
)

REM Confirm release
echo [INFO] You are about to create a release for version: %VERSION%
echo [WARNING] This will:
echo   - Create a git tag v%VERSION%
echo   - Push the tag to GitHub
echo   - Trigger the GitHub Actions workflow to build and release
echo.
set /p confirm="Continue? (y/N): "
if /i not "%confirm%"=="y" (
    echo [ERROR] Release cancelled
    exit /b 1
)

REM Create and push tag
echo [INFO] Creating tag v%VERSION%...
git tag -a "v%VERSION%" -m "Release version %VERSION%"

echo [INFO] Pushing tag to remote...
git push origin "v%VERSION%"

echo [SUCCESS] Release tag v%VERSION% has been created and pushed!
echo [INFO] GitHub Actions will now build and create the release automatically.
echo [INFO] Check the Actions tab at: https://github.com/WayneWalterBerry/MeshCore.Net.SDK/actions

endlocal