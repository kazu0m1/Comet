# Comet v0.1.0 validation report

Date: 2026-09-18

This report records the validation performed on the GitHub-ready source snapshot before handoff.

## Passed in the authoring environment

- Repository/version consistency: `VERSION`, `Directory.Build.props`, `CHANGELOG.md`, and `RELEASE_NOTES.md` agree on `0.1.0`.
- XML/XAML/project/manifest parsing: all checked files are well formed.
- XAML event wiring: declared event handlers exist in their code-behind files.
- Solution references: all project paths referenced by `Comet.sln` exist.
- C# structural scan: balanced delimiters across all source files.
- GitHub Actions YAML parsing: CI and release workflows parse successfully.
- Dependabot YAML parsing: configuration parses successfully.
- Inno Setup static consistency: required sections are present, no duplicate sections, and per-user association registration is present.
- Secret heuristic: no common GitHub/AWS/private-key token patterns were detected.
- Async-reader hardening: stale render requests are canceled/superseded, and ZIP disposal waits for active archive reads before closing the archive.

## Release gate that still requires Windows

The authoring environment is Linux and does not contain the Windows Desktop .NET build chain or Inno Setup. Therefore the following are intentionally delegated to the included Windows CI/release workflows and must pass before publishing a GitHub Release:

1. `dotnet restore Comet.sln`
2. `dotnet build Comet.sln -c Release --no-restore`
3. `Comet.SmokeTests`
4. Windows x64 self-contained publish
5. Inno Setup compilation
6. Runtime UX checks from `docs/RELEASE_CHECKLIST.md`

The Release workflow performs build, smoke tests, publish, installer generation, SHA-256 generation, and only then creates the GitHub prerelease. No locally fabricated Windows binary is included in this source handoff.
