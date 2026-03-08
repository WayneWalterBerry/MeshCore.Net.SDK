# How to Release MeshCore.Net.SDK

This guide walks you through creating a new release of MeshCore.Net.SDK. Don't worry if you've never done a GitHub release before—we'll cover everything step by step.

## What Happens When You Release?

When you create a release, two things happen automatically:

1. **GitHub Release Page**: A release page is created on GitHub with:
   - The NuGet package file (`.nupkg`) attached for download
   - Release notes and version information
   - A link to compare changes from the previous version

2. **NuGet Package** (optional): If configured, the package is published to NuGet.org, making it available for developers to install via `dotnet add package MeshCore.Net.SDK`

## Prerequisites

Before you release, make sure:

- ✅ All your changes are **merged to the `main` branch** (no unpushed commits)
- ✅ You have **write access** to the repository (or it's already configured for you)
- ✅ You've decided on the **version number** (see [Versioning Convention](#versioning-convention) below)
- ✅ You've **updated CHANGELOG.md** with release notes (recommended but not required by the automation)

## Step-by-Step: Creating a Release

### 1. Make Sure You're on the Latest Main Branch

```bash
git checkout main
git pull upstream main
```

This ensures you have the latest changes.

### 2. Decide Your Version Number

Choose your version following [semantic versioning](#versioning-convention) (e.g., `0.0.8`, `1.2.3`).

The tag must start with a lowercase `v` followed by the version number: `v0.0.8`

### 3. Create a Git Tag

```bash
git tag v0.0.8
```

Replace `0.0.8` with your actual version number. The **`v` prefix is required**—the CI workflow watches for tags matching `v*` to trigger the release.

### 4. Push the Tag to GitHub

```bash
git push origin v0.0.8
```

⚠️ **Important**: Push to `origin`, not `upstream`. The tag will be visible on the upstream repo automatically.

Once you push the tag, the CI workflow starts automatically. **Don't create the GitHub release manually**—the workflow does it for you.

## What Happens Next (Automated by CI)

After you push the tag, GitHub Actions runs automatically:

1. **Build Job** (~2-3 minutes)
   - Checks out your code
   - Runs the full build
   - Runs all tests

2. **Pack Job** (~1-2 minutes)
   - Creates the NuGet package (`.nupkg` file)
   - Uses the version from your tag (e.g., `v0.0.8` becomes `0.0.8`)
   - Uploads the package to GitHub

3. **Release Job** (~1 minute)
   - Creates a GitHub Release page
   - Attaches the `.nupkg` file
   - Optionally publishes to NuGet.org (if `NUGET_API_KEY` is configured)

The whole process typically takes 5-10 minutes. **You don't need to do anything**—just watch the progress.

## Monitoring the Release on GitHub

1. Go to the repository on GitHub
2. Click **Actions** (the tab near the top)
3. Find the workflow run that matches your tag name
4. Watch the jobs complete:
   - 🟢 Green checkmarks = success
   - 🔴 Red X = failure

If a job fails, click on it to see error logs. Common issues are listed in [Troubleshooting](#troubleshooting) below.

### After the Release Completes Successfully

1. Go to **Releases** tab on GitHub
2. You'll see a new release with your version number
3. The `.nupkg` file will be attached
4. Release notes are auto-generated

## Optional: Set Up NuGet.org Publishing

By default, the release is created on GitHub, but the package is **not automatically published to NuGet.org**. To enable automatic publishing:

### 1. Get an API Key from NuGet.org

- Go to https://www.nuget.org/account/apikeys
- Log in with your NuGet account (create one if needed)
- Create a new API key with appropriate permissions
- Copy the key to your clipboard

### 2. Add the Secret to Your Repository

- Go to your repository on GitHub
- Click **Settings** → **Secrets and variables** → **Actions**
- Click **New repository secret**
- Name: `NUGET_API_KEY`
- Value: Paste the API key from NuGet.org
- Click **Add secret**

### 3. Next Time You Release

The CI workflow will automatically publish to NuGet.org. You don't need to change anything in your code or workflow files.

### Verify NuGet Publishing

After the workflow completes, check NuGet.org:

```bash
# Search on the command line
dotnet package search MeshCore.Net.SDK
```

Or visit: https://www.nuget.org/packages/MeshCore.Net.SDK/

## Versioning Convention

This project uses **Semantic Versioning**. Format: `v{MAJOR}.{MINOR}.{PATCH}` or `v{MAJOR}.{MINOR}.{PATCH}-{PRERELEASE}`

### Examples

| Version | Use When |
|---------|----------|
| `v1.0.0` | First stable release |
| `v1.1.0` | New features added (backward compatible) |
| `v1.1.1` | Bug fixes only |
| `v2.0.0` | Breaking changes (incompatible with v1) |
| `v1.0.0-alpha` | Early testing, expect lots of changes |
| `v1.0.0-rc.1` | Release candidate, stable features but final testing |

### Guidelines

- **MAJOR**: Increment when you make breaking changes (incompatible API changes)
- **MINOR**: Increment when you add features in a backward-compatible manner
- **PATCH**: Increment when you fix bugs or make other non-breaking changes
- **Pre-release**: Add suffixes like `-alpha`, `-beta`, `-rc.1` for pre-release versions

## Troubleshooting

### The Workflow Didn't Start

**Problem**: You pushed a tag but the CI workflow didn't run.

**Solutions**:
- Make sure the tag starts with `v` (lowercase, not `V`)
- Verify you pushed to `origin`, not to a different remote
- Check that you're working on the `main` branch (or a branch that CI watches)

### Workflow Job Failed

**Problem**: The build, pack, or release job failed.

**Solutions**:
1. Go to the failed job on GitHub Actions
2. Expand the log to see the error message
3. Common issues:
   - **Build failed**: Check that your code compiles locally (`dotnet build`)
   - **Test failed**: Run tests locally (`dotnet test`) to debug
   - **Release failed**: Usually a permission issue; check that `contents: write` is in the workflow permissions (it should be)

### NuGet Publishing Failed

**Problem**: The release was created but NuGet.org publishing failed.

**Solutions**:
- If `NUGET_API_KEY` is not set, publishing is skipped (this is expected)
- If the key is set but publishing fails, check:
  - The API key has the correct permissions on NuGet.org
  - The API key hasn't expired
  - You're not trying to re-publish the same version (NuGet doesn't allow overwriting)

### Accidentally Pushed the Wrong Tag

**Problem**: You pushed a tag to GitHub but it was the wrong one, or it had the wrong format.

**Solutions**:
```bash
# Delete the tag locally
git tag -d v0.0.7

# Delete from remote
git push origin --delete v0.0.7

# Then create and push the correct tag
git tag v0.0.8
git push origin v0.0.8
```

If a release was already created on GitHub, you can delete it manually from the Releases page.

## Quick Reference Checklist

Before releasing, verify:

- [ ] All changes merged to `main`
- [ ] Latest main branch pulled locally
- [ ] Version number decided (e.g., `v0.0.8`)
- [ ] CHANGELOG.md updated (optional but recommended)
- [ ] Ready to create the release

Releasing:

- [ ] Create tag: `git tag v0.0.8`
- [ ] Push tag: `git push origin v0.0.8`
- [ ] Check Actions tab to monitor workflow
- [ ] Verify release created on Releases page

## Questions?

If something goes wrong or you're not sure about a step, check:

1. The workflow file: `.github/workflows/build-and-release.yml`
2. The [Troubleshooting](#troubleshooting) section above
3. GitHub Actions logs for detailed error messages
4. Ask in the project's discussions or issues on GitHub

Happy releasing! 🚀
