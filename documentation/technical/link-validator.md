# Documentation Link Validator

## Why it exists

Markdown links between documents are plain relative paths, a link to the test strategy written as `../technical/TEST_STRATEGY.md`, nothing recalculates them automatically when a file moves. Every changes on a file's path or name can brake those link.

Those breakages are silent: a dead relative link doesn't fail a build, doesn't show up in a diff review unless someone actually clicks through, and only surfaces when a reader hits a 404 in the middle of onboarding or an interview demo. The link validator turns that silent failure into a loud one — a script check that runs the same way every time, instead of a manual "let me click every link" pass after each reorganization.

## How it works

The script is `scripts/Check-DocLinks.ps1`. It:

1. Recursively finds every `*.md` file under `documentation/`.
2. Extracts every markdown link — link text in square brackets followed by a `.md` target in parentheses — using the regex `\[([^\]]+)\]\(([^)]+\.md)\)`.
3. Skips links whose target starts with `http://` or `https://` — those are external and out of scope.
4. Resolves each remaining target as a relative path **from the folder of the file that contains the link** (not from the repo root), matching how a markdown viewer would resolve it.
5. Checks the resolved path with `Test-Path`. Anything that doesn't exist is collected as broken.
6. Prints a table of `File` / `Link` for every broken link and exits with code `1`. If nothing is broken, it prints a confirmation and exits `0`.

It only validates internal `.md`-to-`.md` links. It does not check anchors within a file (`#section-heading`), links to non-markdown files (images, `.sql`, `.tf`), or external URLs — link rot in those categories isn't something this project has hit yet, so the script stays scoped to the actual failure mode it was written for.

## How it's used

**Locally**, from the repo root:

```powershell
.\scripts\Check-DocLinks.ps1
```

Run it after moving, renaming, or deleting anything under `documentation/`, or after adding new cross-references between documents — before committing.

**In CI**, it runs automatically as the `docs-link-check` job in [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml), on every push and pull request to `main`. The job uses `shell: pwsh`, which is preinstalled on GitHub's `ubuntu-latest` runners, so the exact same script runs locally and in CI — no platform-specific rewrite needed.

```yaml
docs-link-check:
  name: Documentation Link Check
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Check documentation links
      shell: pwsh
      run: ./scripts/Check-DocLinks.ps1
```

A broken link now fails the CI job with the offending file and link printed directly in the log, instead of being discovered by a reader.
