# Build Pipeline Migration: AppVeyor + Cake + semantic-release → GitHub Actions + Bullseye/SimpleExec + MinVer

**Date:** 2026-06-11
**Status:** Approved (design)

## Goal

Replace the existing build/release stack with a leaner, C#-native one and gain
tighter control over versioning.

| Concern        | Old                                  | New                          |
| -------------- | ------------------------------------ | ---------------------------- |
| CI             | AppVeyor (Linux + Windows matrix)    | GitHub Actions               |
| Build script   | Cake (`build.cake`, addins)          | Bullseye + SimpleExec (C#)   |
| Versioning     | semantic-release (commit-derived)    | MinVer (git-tag-derived)     |
| Release trigger| Auto on every `master` commit        | Push a `v*` git tag          |
| Changelog      | semantic-release auto-generated      | Manual `CHANGELOG.md`        |

The core philosophy shift: **nothing versions or releases unless you tag.** The
git tag is the single source of truth.

## Decisions

1. **Release trigger:** pushing a `v1.2.3` git tag. CI builds, packs that exact
   version, pushes to NuGet, and creates the GitHub release.
2. **Versioning:** MinVer reads the version from tags. No version logic in the
   build script.
3. **Changelog:** maintained manually in `CHANGELOG.md` (authored by the
   maintainer). CI extracts the matching version section for the release body.
4. **Build configs:** collapse `Debug;ReleaseWindows;ReleaseLinux` →
   `Debug;Release`. Build/test/pack/publish on `ubuntu-latest` only.
5. **`gh release` step** lives in the workflow (where `gh` auth is native), not
   in the build program.

## 1. Versioning — MinVer

- Add a single `MinVer` `PackageReference` to `Directory.Build.props` so it
  applies to packable projects.
- Set `<MinVerTagPrefix>v</MinVerTagPrefix>` — existing tags already use the `v`
  prefix (highest is `v2.0.0`), so the baseline exists with zero bootstrapping.
- On a tagged commit (`v1.2.3`) → version is exactly `1.2.3`.
- On untagged commits → MinVer emits a prerelease (e.g. `1.2.4-alpha.0.N`).
  These are never published (publish fires only on tags), so they are harmless
  and give meaningful CI/local versions.

## 2. Build orchestration — Bullseye + SimpleExec

A new `build/` console project (`build/build.csproj`, excluded from package
output and ideally outside the packaged solution graph) with `Program.cs`
defining targets via Bullseye and shelling out via SimpleExec. Runnable
identically locally and in CI:

```
dotnet run --project build -- <target>
```

Targets:

| Target          | Depends on | Action                                                                 |
| --------------- | ---------- | ---------------------------------------------------------------------- |
| `clean`         | —          | remove `artifacts/`, `**/bin`, `**/obj`                                |
| `restore`       | —          | `dotnet restore`                                                       |
| `build`         | `restore`  | `dotnet build -c Release --no-restore`                                 |
| `test`          | `build`    | `dotnet test -c Release --no-build`                                    |
| `pack`          | `test`     | `dotnet pack src/Typescriptr -c Release -o artifacts` (MinVer version) |
| `publish`       | `pack`     | `dotnet nuget push artifacts/*.nupkg` using `NUGET_TOKEN` env          |
| `release-notes` | —          | extract current version's section from `CHANGELOG.md` → `artifacts/release-notes.md` |
| `default`       | —          | `test`                                                                 |

Optional convenience shim: a thin `build.ps1` / `build.sh` that just calls
`dotnet run --project build`.

## 3. Build configs

Collapse `Debug;ReleaseWindows;ReleaseLinux` → `Debug;Release` across the
`.csproj` files, `Directory.Build.props`, and the `.sln`. Remove the
OS-conditional `Optimize` block (Release optimizes by default).

## 4. CI — GitHub Actions

### `.github/workflows/ci.yml` — on push to `master` + all PRs

- `actions/checkout` with **`fetch-depth: 0`** (MinVer needs full history + tags)
- `actions/setup-dotnet` (net8)
- `dotnet run --project build -- test`

### `.github/workflows/release.yml` — on push of tag `v*`

- `actions/checkout` with `fetch-depth: 0`
- `actions/setup-dotnet` (net8)
- `dotnet run --project build -- publish` (build → test → pack tagged version →
  NuGet push; `NUGET_TOKEN` from repo secrets)
- `dotnet run --project build -- release-notes`
- `gh release create "$TAG" --notes-file artifacts/release-notes.md artifacts/*.nupkg`
  (uses built-in `GITHUB_TOKEN`)

## 5. Changelog

`CHANGELOG.md` is maintained manually. The `release-notes` target reads the
section whose heading matches the tag's version and writes it to
`artifacts/release-notes.md`, which becomes the GitHub release body. No tooling.

## 6. Files removed / added / edited

- **Remove:** `appveyor.yml`, `build.cake`, `build.ps1` (or repurpose as shim),
  `release.config.js`, `tools/` (Cake cache)
- **Add:** `build/build.csproj`, `build/Program.cs`,
  `.github/workflows/ci.yml`, `.github/workflows/release.yml`,
  optional `.github/release.yml`
- **Edit:** `Directory.Build.props` / `Typescriptr.csproj` (MinVer + configs),
  solution configuration cleanup

## 7. One-time / manual setup

- Add `NUGET_TOKEN` as a GitHub repo secret (the old one was AppVeyor-encrypted
  and must be re-issued for GitHub).
- No MinVer bootstrapping needed — `v2.0.0` already exists as the baseline.

## 8. Rollout & validation

Debut the new pipeline on a low-stakes minor release from `master`, then perform
the real major once the pipeline is trusted.

1. Branch the build migration off `master` (`chore/build-migration`) —
   build-system changes only, **no library/API change**.
2. Merge to `master`.
3. Tag **`v2.1.0`** — **pure pipeline smoke test**. Package contents are
   functionally identical to `v2.0.0`; its only purpose is to prove the new
   pipeline end-to-end (MinVer → pack → NuGet push → GitHub release with notes).
4. Once green, merge `feat/nullable-reference-types`, then
   `feat/member-and-type-ordering` (stacked on it) into `master`. Both carry
   breaking/behavior changes.
5. Update `CHANGELOG.md` for the breaking changes and tag **`v3.0.0`** using the
   now-trusted pipeline.

### Branch context (as of 2026-06-11)

- `master` @ `2b43e61` — pre-both feature branches.
- `feat/nullable-reference-types` — breaking change (`feat!:` nullable reference
  types).
- `feat/member-and-type-ordering` — stacked on the nullable branch; adds
  deterministic member/type ordering.
- Highest existing tag: `v2.0.0`.
