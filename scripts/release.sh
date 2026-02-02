#!/bin/bash

# Release Script for MeshCore.Net.SDK
# This script helps create and push a new release tag to trigger the GitHub Actions workflow

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to validate version format
validate_version() {
    if [[ $1 =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.-]+)?$ ]]; then
        return 0
    else
        return 1
    fi
}

# Main release function
create_release() {
    local version=$1
    
    print_status "Starting release process for version $version"
    
    # Check if we're on the main branch
    current_branch=$(git branch --show-current)
    if [ "$current_branch" != "main" ]; then
        print_warning "You are not on the main branch (current: $current_branch)"
        read -p "Continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            print_error "Release cancelled"
            exit 1
        fi
    fi
    
    # Check if working directory is clean
    if ! git diff-index --quiet HEAD --; then
        print_error "Working directory is not clean. Please commit or stash your changes."
        exit 1
    fi
    
    # Check if tag already exists
    if git tag -l | grep -q "^v$version$"; then
        print_error "Tag v$version already exists!"
        exit 1
    fi
    
    # Run tests before creating release
    print_status "Running tests..."
    if ! dotnet test --configuration Release --verbosity minimal; then
        print_error "Tests failed! Please fix the tests before releasing."
        exit 1
    fi
    
    # Update changelog (manual step - just remind the user)
    print_warning "Please ensure CHANGELOG.md has been updated with changes for version $version"
    read -p "Have you updated CHANGELOG.md? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_error "Please update CHANGELOG.md before releasing"
        exit 1
    fi
    
    # Create and push tag
    print_status "Creating tag v$version..."
    git tag -a "v$version" -m "Release version $version"
    
    print_status "Pushing tag to remote..."
    git push origin "v$version"
    
    print_success "Release tag v$version has been created and pushed!"
    print_status "GitHub Actions will now build and create the release automatically."
    print_status "Check the Actions tab at: https://github.com/WayneWalterBerry/MeshCore.Net.SDK/actions"
}

# Script main logic
if [ $# -eq 0 ]; then
    print_error "No version specified!"
    echo "Usage: $0 <version>"
    echo "Example: $0 1.0.0"
    echo "Example: $0 1.1.0-beta.1"
    exit 1
fi

VERSION=$1

# Validate version format
if ! validate_version "$VERSION"; then
    print_error "Invalid version format: $VERSION"
    print_error "Version should follow semver format (e.g., 1.0.0, 2.1.3-beta.1)"
    exit 1
fi

# Confirm release
print_status "You are about to create a release for version: $VERSION"
print_warning "This will:"
echo "  - Create a git tag v$VERSION"
echo "  - Push the tag to GitHub"
echo "  - Trigger the GitHub Actions workflow to build and release"
echo
read -p "Continue? (y/N): " -n 1 -r
echo

if [[ $REPLY =~ ^[Yy]$ ]]; then
    create_release "$VERSION"
else
    print_error "Release cancelled"
    exit 1
fi