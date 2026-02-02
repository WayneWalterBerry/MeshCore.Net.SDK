# Release Script for MeshCore.Net.SDK (PowerShell version)
# This script helps create and push a new release tag to trigger the GitHub Actions workflow

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

# Function to write colored output
function Write-Status {
    param($Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param($Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param($Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param($Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Function to validate version format (semver)
function Test-Version {
    param($Version)
    return $Version -match '^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.-]+)?$'
}

# Validate version format
if (-not (Test-Version $Version)) {
    Write-Error "Invalid version format: $Version"
    Write-Error "Version should follow semver format (e.g., 1.0.0, 2.1.3-beta.1)"
    exit 1
}

Write-Status "Starting release process for version $Version"

# Check if we're on the main branch
$currentBranch = git branch --show-current
if ($currentBranch -ne "main") {
    Write-Warning "You are not on the main branch (current: $currentBranch)"
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y") {
        Write-Error "Release cancelled"
        exit 1
    }
}

# Check if working directory is clean
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Error "Working directory is not clean. Please commit or stash your changes."
    exit 1
}

# Check if tag already exists
$existingTag = git tag -l | Where-Object { $_ -eq "v$Version" }
if ($existingTag) {
    Write-Error "Tag v$Version already exists!"
    exit 1
}

# Run tests before creating release
Write-Status "Running tests..."
$testResult = dotnet test --configuration Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed! Please fix the tests before releasing."
    exit 1
}

# Remind user to update changelog
Write-Warning "Please ensure CHANGELOG.md has been updated with changes for version $Version"
$updated = Read-Host "Have you updated CHANGELOG.md? (y/N)"
if ($updated -ne "y" -and $updated -ne "Y") {
    Write-Error "Please update CHANGELOG.md before releasing"
    exit 1
}

# Confirm release
Write-Status "You are about to create a release for version: $Version"
Write-Warning "This will:"
Write-Host "  - Create a git tag v$Version"
Write-Host "  - Push the tag to GitHub"
Write-Host "  - Trigger the GitHub Actions workflow to build and release"
Write-Host ""

$confirm = Read-Host "Continue? (y/N)"
if ($confirm -ne "y" -and $confirm -ne "Y") {
    Write-Error "Release cancelled"
    exit 1
}

# Create and push tag
Write-Status "Creating tag v$Version..."
git tag -a "v$Version" -m "Release version $Version"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create tag"
    exit 1
}

Write-Status "Pushing tag to remote..."
git push origin "v$Version"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to push tag"
    exit 1
}

Write-Success "Release tag v$Version has been created and pushed!"
Write-Status "GitHub Actions will now build and create the release automatically."
Write-Status "Check the Actions tab at: https://github.com/WayneWalterBerry/MeshCore.Net.SDK/actions"