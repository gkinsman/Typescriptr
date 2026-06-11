# Build Pipeline Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the AppVeyor + Cake + semantic-release stack with GitHub Actions + Bullseye/SimpleExec + MinVer, retarget to net8.0, and ship everything as `v3.0.0`.

**Architecture:** A small C# console project (`build/`) defines build targets with Bullseye and shells out via SimpleExec; it is runnable identically locally and in CI. MinVer derives the package version from the `v*` git tag. Two GitHub Actions workflows: `ci.yml` (build + test on every push/PR) and `release.yml` (tag-triggered pack → OIDC NuGet push → GitHub release). NuGet auth uses Trusted Publishing (OIDC) — no stored token.

**Tech Stack:** .NET 8, Bullseye 6.1.0, SimpleExec 13.0.0, MinVer 7.0.0, GitHub Actions, `NuGet/login@v1`, `gh` CLI.

**Spec:** `docs/superpowers/specs/2026-06-11-build-pipeline-migration-design.md`

**Branch:** `chore/build-migration` (already created off `origin/master`). All work happens here; feature branches merge in at the end (Task 8).

**Pre-verified:** net8.0 + the existing test package versions restore clean (NuGet audit passes) and all 29 tests pass on master's code. The audit failure on master was solely the `netcoreapp2.1` runtime.

---

## File Structure

| File | Responsibility | Action |
|------|----------------|--------|
| `src/Typescriptr/Typescriptr.csproj` | Library: net8 target, MinVer, single Release config | Modify |
| `src/Typescript.Tests/Typescript.Tests.csproj` | Tests: net8 target, single Release config | Modify |
| `src/Typescriptr.sln` | Solution config list `Debug;Release` | Modify |
| `build/build.csproj` | Build-orchestrator project (Bullseye + SimpleExec) | Create |
| `build/Program.cs` | Build target graph + changelog extraction | Create |
| `.github/workflows/ci.yml` | CI: build + test on push/PR | Create |
| `.github/workflows/release.yml` | Release: tag → pack → OIDC push → GH release | Create |
| `build.ps1` | Thin local shim → `dotnet run --project build` | Replace |
| `build.cake`, `appveyor.yml`, `release.config.js` | Legacy stack | Delete |
| `CHANGELOG.md` | Manual release notes (read by `release-notes` target) | Author (Task 8) |

`build/` lives at the repo root (not under `src/`), so it is intentionally **not** part of `src/Directory.Build.props` (no `TreatWarningsAsErrors`) and **not** in the solution — it is tooling, not product code.

---

## Task 1: Retarget library + tests to net8.0 and collapse build configs

**Files:**
- Modify: `src/Typescriptr/Typescriptr.csproj`
- Modify: `src/Typescript.Tests/Typescript.Tests.csproj`
- Modify: `src/Typescriptr.sln`

- [ ] **Step 1: Rewrite the library csproj**

Replace the entire contents of `src/Typescriptr/Typescriptr.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <Configurations>Debug;Release</Configurations>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Platforms>AnyCPU</Platforms>
    </PropertyGroup>

    <PropertyGroup>
        <Authors>George Kinsman</Authors>
        <Description>
            A C# to TypeScript converter that focuses on ease of use and client side awesomeness.
        </Description>
        <PackageProjectUrl>https://github.com/gkinsman/Typescriptr</PackageProjectUrl>
        <RepositoryUrl>https://github.com/gkinsman/Typescriptr</RepositoryUrl>
        <PackageLicenseExpression>MIT</PackageLicenseExpression>
    </PropertyGroup>

    <PropertyGroup>
        <AllowedOutputExtensionsInPackageBuildOutputFolder>$(AllowedOutputExtensionsInPackageBuildOutputFolder);.pdb</AllowedOutputExtensionsInPackageBuildOutputFolder>
        <PublishRepositoryUrl>true</PublishRepositoryUrl>
        <EmbedUntrackedSources>true</EmbedUntrackedSources>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.SourceLink.GitHub" Version="1.0.0" PrivateAssets="All" />
    </ItemGroup>

</Project>
```

(Removed: OS-conditional `TargetFramework(s)` for `netstandard2.0`/`net461`, and the `ReleaseLinux` `Optimize` block. MinVer is added in Task 2.)

- [ ] **Step 2: Rewrite the test csproj**

Replace the entire contents of `src/Typescript.Tests/Typescript.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <IsPackable>false</IsPackable>
        <Configurations>Debug;Release</Configurations>
        <Platforms>AnyCPU</Platforms>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Assent" Version="1.7.0" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.2.0" />
        <PackageReference Include="xunit" Version="2.4.1" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.4.3" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Typescriptr\Typescriptr.csproj" />
    </ItemGroup>

</Project>
```

- [ ] **Step 3: Update the solution configuration list**

In `src/Typescriptr.sln`, replace the `SolutionConfigurationPlatforms` section and all `ProjectConfigurationPlatforms` entries so only `Debug` and `Release` exist. Replace:

```
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		ReleaseWindows|Any CPU = ReleaseWindows|Any CPU
		ReleaseLinux|Any CPU = ReleaseLinux|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.ReleaseWindows|Any CPU.ActiveCfg = ReleaseWindows|Any CPU
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.ReleaseWindows|Any CPU.Build.0 = ReleaseWindows|Any CPU
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.ReleaseLinux|Any CPU.ActiveCfg = ReleaseLinux|Any CPU
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.ReleaseLinux|Any CPU.Build.0 = ReleaseLinux|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.ReleaseWindows|Any CPU.ActiveCfg = ReleaseWindows|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.ReleaseWindows|Any CPU.Build.0 = ReleaseWindows|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.ReleaseLinux|Any CPU.ActiveCfg = ReleaseLinux|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.ReleaseLinux|Any CPU.Build.0 = ReleaseLinux|Any CPU
	EndGlobalSection
```

with:

```
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{E0E2012B-FB54-463F-9DAE-FD8559E342EC}.Release|Any CPU.Build.0 = Release|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A9ABBA06-1180-43E9-811E-015B663D5A2E}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
```

- [ ] **Step 4: Verify the solution builds and tests pass in Release**

Run: `dotnet test src/Typescriptr.sln -c Release --nologo`
Expected: restore succeeds with no NU1902/NU1903 errors; `Passed!  - Failed: 0, Passed: 29`.

- [ ] **Step 5: Commit**

```bash
git add src/Typescriptr/Typescriptr.csproj src/Typescript.Tests/Typescript.Tests.csproj src/Typescriptr.sln
git commit -m "build: retarget to net8.0 and collapse to Debug;Release configs"
```

---

## Task 2: Add MinVer for tag-driven versioning

**Files:**
- Modify: `src/Typescriptr/Typescriptr.csproj`

- [ ] **Step 1: Add the MinVer package reference and tag prefix**

In `src/Typescriptr/Typescriptr.csproj`, add `<MinVerTagPrefix>v</MinVerTagPrefix>` to the first `PropertyGroup` (so it reads):

```xml
    <PropertyGroup>
        <Configurations>Debug;Release</Configurations>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Platforms>AnyCPU</Platforms>
        <MinVerTagPrefix>v</MinVerTagPrefix>
    </PropertyGroup>
```

and add MinVer to the existing `ItemGroup` with the package references:

```xml
    <ItemGroup>
        <PackageReference Include="Microsoft.SourceLink.GitHub" Version="1.0.0" PrivateAssets="All" />
        <PackageReference Include="MinVer" Version="7.0.0" PrivateAssets="All" />
    </ItemGroup>
```

- [ ] **Step 2: Verify MinVer computes a version from the existing tags**

Run: `dotnet pack src/Typescriptr/Typescriptr.csproj -c Release -o artifacts --nologo`
Expected: produces `artifacts/Typescriptr.<version>.nupkg` where `<version>` is a prerelease above the latest tag `v2.0.0` (e.g. `2.0.1-alpha.0.N`), confirming MinVer reads tags with the `v` prefix.

Run: `ls artifacts`
Expected: a `Typescriptr.2.0.1-alpha.*.nupkg` (exact suffix depends on commit distance).

- [ ] **Step 3: Commit**

```bash
git add src/Typescriptr/Typescriptr.csproj
git commit -m "build: version with MinVer using the v tag prefix"
```

---

## Task 3: Create the Bullseye/SimpleExec build program

**Files:**
- Create: `build/build.csproj`
- Create: `build/Program.cs`

- [ ] **Step 1: Create the build project file**

Create `build/build.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
        <RootNamespace>Build</RootNamespace>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bullseye" Version="6.1.0" />
        <PackageReference Include="SimpleExec" Version="13.0.0" />
    </ItemGroup>

</Project>
```

- [ ] **Step 2: Create the build program with the target graph**

Create `build/Program.cs`:

```csharp
using System.Text.RegularExpressions;
using static Bullseye.Targets;
using static SimpleExec.Command;

const string configuration = "Release";
const string solution = "src/Typescriptr.sln";
const string packageProject = "src/Typescriptr/Typescriptr.csproj";
const string artifactsDir = "artifacts";
const string changelog = "CHANGELOG.md";

Target("clean", () =>
{
    if (Directory.Exists(artifactsDir))
        Directory.Delete(artifactsDir, recursive: true);
});

Target("restore", () => Run("dotnet", $"restore {solution}"));

Target("build", dependsOn: ["restore"], () =>
    Run("dotnet", $"build {solution} -c {configuration} --no-restore"));

Target("test", dependsOn: ["build"], () =>
    Run("dotnet", $"test {solution} -c {configuration} --no-build"));

Target("pack", dependsOn: ["test"], () =>
    Run("dotnet", $"pack {packageProject} -c {configuration} -o {artifactsDir}"));

Target("release-notes", dependsOn: ["pack"], () =>
{
    var version = ResolveVersion();
    var notes = ChangelogSection(changelog, version);
    File.WriteAllText(Path.Combine(artifactsDir, "release-notes.md"), notes);
    Console.WriteLine($"Wrote release notes for {version} to {artifactsDir}/release-notes.md");
});

Target("default", dependsOn: ["test"]);

await RunTargetsAndExitAsync(args);

// Version source of truth: the produced package. RELEASE_VERSION env overrides
// (used for local testing / to decouple from a built package).
static string ResolveVersion()
{
    var fromEnv = Environment.GetEnvironmentVariable("RELEASE_VERSION");
    if (!string.IsNullOrWhiteSpace(fromEnv))
        return fromEnv.TrimStart('v');

    var nupkg = Directory.GetFiles("artifacts", "Typescriptr.*.nupkg")
        .Select(Path.GetFileName)
        .First(f => f is not null && !f.EndsWith(".snupkg"))!;
    return Regex.Match(nupkg, @"^Typescriptr\.(.+)\.nupkg$").Groups[1].Value;
}

// Finds the changelog section whose header contains the version (prerelease
// suffixes are matched against the base version, e.g. 3.0.0-rc.1 -> 3.0.0) and
// returns the body up to the next header.
static string ChangelogSection(string path, string version)
{
    var baseVersion = version.Split('-')[0];
    var lines = File.ReadAllLines(path);
    var header = new Regex($@"^#{{1,6}}\s+\[?{Regex.Escape(baseVersion)}(\]|\s|\(|$)");
    var anyHeader = new Regex(@"^#{1,6}\s");

    var start = Array.FindIndex(lines, header.IsMatch);
    if (start < 0)
        throw new InvalidOperationException(
            $"No CHANGELOG.md section found for version {baseVersion}. Add an entry before tagging.");

    var body = new List<string>();
    for (var i = start + 1; i < lines.Length; i++)
    {
        if (anyHeader.IsMatch(lines[i])) break;
        body.Add(lines[i]);
    }

    return string.Join('\n', body).Trim() + "\n";
}
```

- [ ] **Step 3: Verify the `test` target runs end to end**

Run: `dotnet run --project build -- test`
Expected: Bullseye prints the `restore`/`build`/`test` target sequence; tests pass (`Passed!  - Failed: 0, Passed: 29`); process exits 0.

- [ ] **Step 4: Verify the `pack` target produces a package**

Run: `dotnet run --project build -- clean pack`
Expected: `artifacts/Typescriptr.<version>.nupkg` exists; exit 0.

- [ ] **Step 5: Commit**

```bash
git add build/build.csproj build/Program.cs
git commit -m "build: add Bullseye/SimpleExec build program"
```

---

## Task 4: Verify changelog extraction against a known section

This validates the `release-notes` logic without needing a `v3.0.0` tag yet, using the existing `2.0.0` changelog entry via the `RELEASE_VERSION` override.

**Files:** none (verification only)

- [ ] **Step 1: Ensure a package exists for the target (release-notes depends on pack)**

Run: `dotnet run --project build -- pack`
Expected: exit 0; `artifacts/` contains a `.nupkg`.

- [ ] **Step 2: Extract the existing 2.0.0 section via the env override**

Run (PowerShell): `$env:RELEASE_VERSION="2.0.0"; dotnet run --project build -- release-notes; Remove-Item Env:\RELEASE_VERSION`
Expected: exit 0; message `Wrote release notes for 2.0.0 ...`.

- [ ] **Step 3: Confirm the extracted notes are correct**

Run: `Get-Content artifacts/release-notes.md`
Expected: contains the 2.0.0 body — the `Merge pull request #47 ...` line and the `### BREAKING CHANGES` / `Switch off module nesting by default` text, and does **not** contain the `# [2.0.0]...` header line or any `1.5.0` content.

- [ ] **Step 4: Confirm a missing section fails loudly**

Run (PowerShell): `$env:RELEASE_VERSION="9.9.9"; dotnet run --project build -- release-notes; Remove-Item Env:\RELEASE_VERSION`
Expected: non-zero exit; error text `No CHANGELOG.md section found for version 9.9.9`.

(No commit — verification only.)

---

## Task 5: Remove the legacy build stack and add a local shim

**Files:**
- Delete: `build.cake`, `appveyor.yml`, `release.config.js`
- Replace: `build.ps1`

- [ ] **Step 1: Delete the legacy files**

```bash
git rm build.cake appveyor.yml release.config.js
```

- [ ] **Step 2: Replace `build.ps1` with a thin shim**

Replace the entire contents of `build.ps1` with:

```powershell
#!/usr/bin/env pwsh
# Convenience shim. Forwards all arguments to the Bullseye build program.
# Examples:  ./build.ps1            (default target: test)
#            ./build.ps1 pack
dotnet run --project build -- @args
```

- [ ] **Step 3: Verify the shim works and no legacy references remain**

Run: `./build.ps1`
Expected: runs the `default` (test) target; tests pass.

Run: `Select-String -Path *.md,*.yml,*.ps1 -Pattern "build.cake|appveyor|semantic-release|Cake.Tool" 2>$null`
Expected: no matches in active build/config files (matches inside `docs/` specs or `CHANGELOG.md` history are fine).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "build: remove Cake/AppVeyor/semantic-release stack, add pwsh shim"
```

---

## Task 6: Add the CI workflow

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Create the CI workflow**

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: ['**']
    tags-ignore: ['**']
  pull_request:

concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0          # MinVer needs full history + tags

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Build and test
        run: dotnet run --project build -- test
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add GitHub Actions build/test workflow"
```

---

## Task 7: Add the release workflow

**Files:**
- Create: `.github/workflows/release.yml`

- [ ] **Step 1: Create the release workflow**

Create `.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags: ['v*']

permissions:
  contents: write     # create the GitHub release
  id-token: write     # OIDC token for NuGet Trusted Publishing

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Pack and build release notes
        run: dotnet run --project build -- release-notes   # depends on pack -> test -> build

      - name: NuGet login (OIDC -> short-lived API key)
        uses: NuGet/login@v1
        id: login
        with:
          user: ${{ secrets.NUGET_USER }}

      - name: Push to NuGet
        run: >
          dotnet nuget push "artifacts/*.nupkg"
          --api-key ${{ steps.login.outputs.NUGET_API_KEY }}
          --source https://api.nuget.org/v3/index.json
          --skip-duplicate

      - name: Create GitHub release
        env:
          GH_TOKEN: ${{ github.token }}
        run: gh release create "${{ github.ref_name }}" --notes-file artifacts/release-notes.md artifacts/*.nupkg
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/release.yml
git commit -m "ci: add tag-triggered release workflow with OIDC trusted publishing"
```

---

## Task 8: Push, validate CI, then assemble v3.0.0

This is the rollout (spec §8). The pipeline is proven on master's code **before** the breaking features merge.

**Files:**
- Author: `CHANGELOG.md` (new `3.0.0` entry)

- [ ] **Step 1: Push the branch and confirm CI is green**

```bash
git push -u origin chore/build-migration
```
Then open the repo's Actions tab (or `gh run watch`) and confirm the **CI** workflow succeeds (build + 29 tests) on `ubuntu-latest`. This validates the new build/test pipeline against real code with no NuGet deploy.

- [ ] **Step 2: Complete the one-time nuget.org / secret setup** (manual, see spec §7)

- Create the Trusted Publishing policy on nuget.org: owner `gkinsman`, repository `Typescriptr`, workflow file `release.yml`.
- Add repo secret `NUGET_USER` = your nuget.org **profile name** (not email).

- [ ] **Step 3: Merge the feature branches**

```bash
git merge feat/nullable-reference-types
git merge feat/member-and-type-ordering
```
**Conflict resolution:** Conflicts are expected in `src/Typescriptr/Typescriptr.csproj` and `src/Typescript.Tests/Typescript.Tests.csproj` (both branches also retarget to net8 but keep `Debug;ReleaseWindows;ReleaseLinux`). Resolve by **keeping this branch's versions**: `<TargetFramework>net8.0</TargetFramework>`, `<Configurations>Debug;Release</Configurations>`, the MinVer reference, and no OS-conditional `Optimize` block. Take the feature branches' changes to all non-csproj source files.

- [ ] **Step 4: Verify the merged result is green**

Run: `dotnet run --project build -- pack`
Expected: build + tests pass (test count now higher than 29 — includes nullable + ordering tests); a `.nupkg` is produced.

- [ ] **Step 5: Author the `3.0.0` CHANGELOG entry**

Prepend a section to `CHANGELOG.md` (header must contain `3.0.0` so the extractor matches), e.g.:

```markdown
# [3.0.0](https://github.com/gkinsman/Typescriptr/compare/v2.0.0...v3.0.0) (2026-06-11)

### Features

* Support nullable reference types in generated TypeScript
* Deterministic, alphabetically-ordered members and namespace-grouped types

### BREAKING CHANGES

* Library now targets net8.0 (dropped netstandard2.0 / net461)
* Generated member and type output order changed (now alphabetical)
```

- [ ] **Step 6: Verify release notes extract for 3.0.0**

Run (PowerShell): `$env:RELEASE_VERSION="3.0.0"; dotnet run --project build -- release-notes; Remove-Item Env:\RELEASE_VERSION`
Run: `Get-Content artifacts/release-notes.md`
Expected: the Features + BREAKING CHANGES body for 3.0.0, no header line.

- [ ] **Step 7: Commit and merge to master**

```bash
git add CHANGELOG.md
git commit -m "docs: changelog for 3.0.0"
git push
```
Open a PR from `chore/build-migration` to `master`, confirm CI green, and merge.

- [ ] **Step 8: (Optional) Rehearse the publish path with a prerelease tag**

```bash
git checkout master && git pull
git tag v3.0.0-rc.1 && git push origin v3.0.0-rc.1
```
Confirm the **Release** workflow runs end to end: OIDC login succeeds, `3.0.0-rc.1` pushes to NuGet, and a GitHub prerelease is created. This validates the OIDC + push last mile without burning the final version.

- [ ] **Step 9: Tag the real release**

```bash
git tag v3.0.0 && git push origin v3.0.0
```
Confirm the Release workflow publishes `3.0.0` to NuGet and creates the `v3.0.0` GitHub release with the changelog body and the `.nupkg` attached.

---

## Notes

- **Local SDK is .NET 10**, project targets net8.0 — this builds fine (net8 reference assemblies ship with the SDK). CI pins `8.0.x` via `setup-dotnet`.
- **`build/` is excluded from the solution and `src/Directory.Build.props`** on purpose; it is build tooling, runnable via `dotnet run --project build -- <target>` or `./build.ps1 <target>`.
- **`artifacts/` and `tools/` are already gitignored.** The legacy Cake `tools/` cache needs no git removal.
- If a future change makes MinVer compute an unexpected version, run `dotnet run --project build -- pack` locally and inspect the `.nupkg` name — MinVer's version equals the nearest `v*` tag (or a prerelease above it).
