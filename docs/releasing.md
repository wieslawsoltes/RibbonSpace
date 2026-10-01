# Releasing

## Versioning

`VersionPrefix` in `Directory.Build.props` holds the next version. CI builds are `<prefix>-ci.<run>`, and release
tags (`v1.2.3`, `v1.3.0-preview.1`) set the exact version.

## Workflows

| Workflow | Trigger | Does |
|---|---|---|
| `build.yml` | push, PR | Core unit tests (Linux, Windows, macOS); library, gallery and runtime-test builds; runtime UI tests under Xvfb; CI packages |
| `pages.yml` | push to `main` | Publishes the WebAssembly gallery to GitHub Pages |
| `release.yml` | tag `v*` or manual | Verifies, packs `RibbonSpace.Core` and `RibbonSpace.Uno` for **all** targets on Windows (desktop, WebAssembly, Android, iOS, WinAppSDK), creates the GitHub release with checksums, and publishes to NuGet with **Trusted Publishing** |

## Repository setup (one-time)

1. *Settings → Pages → Build and deployment → Source*: select **GitHub Actions**. `pages.yml` cannot enable Pages
   with the default `GITHUB_TOKEN`, so the first deployment fails until this is set.
2. *Settings → Code security → Private vulnerability reporting*: enable it so the advisory link in `SECURITY.md`
   works.
3. Configure NuGet Trusted Publishing (below).

## NuGet Trusted Publishing (one-time setup)

1. On nuget.org, open *Account → Trusted Publishing* and add a policy:
   - owner `wieslawsoltes`
   - repository `RibbonSpace`
   - workflow `release.yml`
   - environment `nuget`
2. In the repository settings, create the environment `nuget`. Optionally restrict it to `v*` tags and add
   reviewers.
3. Add the repository (or environment) variable `NUGET_USER` with your nuget.org profile name.
4. No API key is stored. `NuGet/login` exchanges the GitHub OIDC token for a short-lived key.

## Cutting a release

```bash
git tag v1.0.0
git push origin v1.0.0
```

For a dry run, start `release.yml` manually with a version. It builds and uploads artifacts without publishing.
