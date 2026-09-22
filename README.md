# Broiler.Plate

[![CI](https://github.com/Broiler-Platform/Broiler.Plate/actions/workflows/ci.yml/badge.svg)](https://github.com/Broiler-Platform/Broiler.Plate/actions/workflows/ci.yml)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)

Broiler Plate is the viewer of the [Broiler](https://github.com/Broiler-Platform/Broiler)
managed-code application stack for .NET. It opens a file and shows it. Nothing else.

Where [Broiler.Writer](https://github.com/Broiler-Platform/Broiler.Writer) is the word
processor, this is the read-only half of the same machinery: the same document codecs, the
same `StandardRichEdit` surface, the same Broiler-drawn shell — with editing, saving and
formatting taken out. That makes it the smallest honest demonstration of what the platform
can already read, and the place to add each new thing the platform learns to read.

Everything below the application — document codecs, DOM, graphics, media, input and the UI
toolkit — lives in its own repository and is consumed here through versioned NuGet packages.

> **First version.** This is `Broiler.Plate` at its very beginning: one Windows head, one
> file open at a time, opened from the toolbar, the File menu or `Ctrl+O`. Public APIs,
> repository layout and behaviour are not frozen.
>
> The document codecs parse untrusted input — RTF control words, Open XML packages, HTML and
> PDF object graphs — and must be treated as security-sensitive; no fuzzing campaign,
> dependency scan or independent security audit is recorded for this revision. Substantial
> implementation work was AI-assisted, and this repository carries no `HUMAN_REVIEW.md`, so
> no checkout of it should be described as human-approved.
>
> Intended for evaluation, testing and contribution — not production, security-critical or
> safety-critical use.

## Getting started

Clone the repository:

```bash
git clone https://github.com/Broiler-Platform/Broiler.Plate.git
cd Broiler.Plate
```

[`NuGet.config`](NuGet.config) maps `Broiler.*` packages to the Broiler-Platform GitHub
Packages feed and other packages to nuget.org. Configure local NuGet credentials for the
`github-broiler` source with a token that can read those packages before restoring. Keep
credentials in your user-level NuGet configuration or environment, never in this repository.
CI supplies its GitHub token through the setup action.

Then build and run the Windows head:

```bash
dotnet run --project src/Broiler.Plate.Windows/Broiler.Plate.Windows.csproj
```

### Prerequisites

- .NET 10 SDK
- Windows, for the only head this repository currently has

## What it opens

| Kind | Formats |
|---|---|
| Documents | RTF, DOCX, HTML, Markdown, PDF |
| Graphics | PNG, JPEG, GIF, BMP, TIFF, WebP |

Documents are read through `Broiler.Documents` and drawn by `StandardRichEdit` in read-only
mode — the same control the Writer edits in, so a document looks the same in both. Graphics
are uploaded to the renderer and drawn by `StandardImageView`, scaled to fit and matted so a
picture with transparency or a white border still has an edge.

Which of the two a file is is decided by its leading bytes, not its name: a magic number is
checked first, the document codecs are asked second, and the extension is consulted only as a
last resort, for a format the renderer may decode but this build has no signature for.

PDF is registered by the Windows head rather than by the shared core. A codec must not reach
a head by being someone else's transitive reference — see `CreateFileFormats` in
[`Program.cs`](src/Broiler.Plate.Windows/Program.cs).

## How big it is drawn

A toolbar under the menu carries the one command a viewer has and the controls for how large
what it opened is drawn: **Open**, then **−**, a picker and **+**. Everything else the shell
can do stays in the menus, because a bar of buttons that are mostly unavailable is a bar that
has to be read before it can be used.

The ladder is 25% to 400%, the Writer's, so a percentage means the same thing in both. Every
way of choosing a level goes through the same command — the picker, *View ▸ Zoom*,
`Ctrl` with `+`, `-` or `0`, and `Ctrl` with the wheel — so none of them can disagree with
what is on screen, and the status bar and the picker report the level rather than restating
the gesture.

Each view has its own zoom, because they are not the same quantity. A document is read at a
percentage of the size it states, and that carries from one document to the next. A picture is
drawn at a percentage of its own pixels, or fitted to the window — which is no fixed
percentage at all, and so is written as *Fit* rather than as a number, offered only where it
means something, and where every picture starts. Stepping in from *Fit* steps off the size the
picture is actually being shown at, not off 100%.

A picture larger than the window is centred and clipped to the view; there is nothing to pan
with yet.

## Behaviour worth knowing

- **A failed open changes nothing.** If a file is rejected — no codec recognized it, a reader
  recovered no content, no image codec decoded it — what is already on display stays, and the
  status bar says why.
- **Notes.** A document that opens with diagnostics against it says so in the status bar, and
  *Help ▸ Notes for this file* lists them. Notes recorded by a *refused* open are kept too,
  because they are the reason it was refused, but they are never counted against the document
  that is actually on display.
- **Read-only, not inert.** The document view still selects and copies. That is what a viewer
  is for.
- **Zoom is not part of the file.** It is how something is being looked at, and nothing writes
  it anywhere. Every picture opens fitted, whatever the one before it was left at.

## About

*Help ▸ About Broiler Plate* opens the standard Broiler About dialog with the product
version and component versions. The product version comes from `Broiler.Plate.Core`,
preserves the prerelease label, and omits the build commit hash. Enter or Escape closes
the dialog and returns focus to the viewer.

The default version is `0.1.0-preview.1`, shared by Plate's projects through
`Directory.Build.props`. Override it at build or publish time with
`-p:BroilerPlateVersion=0.1.0-preview.2`.

## Solutions

`.slnx` files are generated, never hand-edited. Declare the entry point in
[`eng/solutions.json`](eng/solutions.json) and run:

```bash
pwsh scripts/update-solutions.ps1
```

`-Verify` fails instead of writing, which is what CI should run.

| Solution | Contents |
|---|---|
| `Broiler.Windows.Plate.slnx` | Windows head and shared viewer core (2 projects) |
| `Broiler.Plate.Tests.slnx` | Windows tests, head and shared viewer core (3 projects) |

## Continuous integration

[`ci.yml`](.github/workflows/ci.yml) runs four jobs on push, pull request and manual
dispatch:

| Job | Runner | What it protects |
|---|---|---|
| Solution manifest | `ubuntu-latest` | The checked-in `.slnx` still matches the real reference graph. Catches a hand-edit, and the subtler case of adding a `ProjectReference` without regenerating — which leaves the new project out of the solution, so it is never built and never seen to break. |
| Windows head | `windows-latest` | `dotnet build -c Release` of the whole solution. |
| Tests | `windows-latest` | PDF composition and About dialog behavior through `dotnet test -c Release`. |
| Publish win-x64 (NativeAOT) | `windows-latest` | `Release-Windows` — a configuration the solution does not declare, so nothing else exercises it — published with NativeAOT, then started to check it stays up. |

The publish job earns its place twice. `Release-Windows` can only be built at project level (a
solution-level build with an undeclared configuration fails `MSB4126`), so it is the only
thing standing between a `Directory.Build.props` regression and a release built unoptimized
with neither `RELEASE` nor `WINDOWS` defined. And NativeAOT is the one build mode that fails
on reflection every other mode accepts: adding `Activator.CreateInstance` or reflection-based
serialization to the head or to `Broiler.Plate.Core` breaks it while leaving an ordinary build
green. `PublishAot` is passed on the command line, never set in a `.csproj`, so day-to-day
builds stay framework-dependent and fast.

The setup action installs .NET 10 and configures GitHub Packages authentication. Both
workflows grant `packages: read`, and only the publish workflow's release job may write
(to push its tag and draft the release); no submodule checkout is required.

[`publish.yml`](.github/workflows/publish.yml) is dispatch-only and builds the Windows Plate
with NativeAOT. It comes out as one zip artifact on the run, `broiler-plate-win-x64-<version>`,
holding a single executable, `Broiler.Plate.Windows.exe`, which is the whole application: no
.NET runtime to install and nothing beside it. Nothing is pushed to a feed. The run fails if
the publish ever emits anything beside the executable other than symbols or XML docs, because
an artifact without that file would be broken, and it starts the executable to check it stays
up.

Each run also drafts a GitHub pre-release, *Broiler Plate <version>*, with the executable
zipped as `Broiler.Plate-<version>-win-x64.zip`. It stays a draft until someone publishes it
under Releases; [`eng/release-draft.sh`](eng/release-draft.sh) builds it and can be run by
hand from a run's artifacts.

Its inputs are `nuget-source`, the feed the `Broiler.*` dependencies are restored from, and
an optional `version-suffix` such as `preview.7`. With `broiler-github`, the default, the
build restores exactly as `NuGet.config` says. With `nuget.org`, the `Broiler.*` mapping is
dropped for the run and those packages come from nuget.org like everything else, which needs
the pinned versions to be published there.

Each run takes the next preview version: `BroilerPlateVersion` in `Directory.Build.props` is
the floor, raised past every earlier run's `plate-v*` tag. A run tags its commit only after
the build succeeded, so preview numbers only ever increase and a failed run leaves its number
free. The version is stamped into the build, so the About dialog and the file version match
the release. The logic is [`eng/resolve-preview-version.mjs`](eng/resolve-preview-version.mjs),
with tests beside it.

## Repository layout

| Path | Contents |
|---|---|
| `src/Broiler.Plate` | Shared application (`Broiler.Plate.Core`) — window, menu, toolbar, the two views, the zoom ladder, format registry, palette |
| `src/Broiler.Plate.Windows` | Windows head — `WinExe`, Direct2D, Win32 clipboard, and the break-out host that gives each dialog its own OS window |
| `src/Broiler.App` | Source-only directory shared by desktop heads — per-platform clipboards. It has no project of its own; each head links the files it needs. |
| `eng/`, `scripts/` | Solution manifest and generator, preview version resolver, release drafting |
| `.github/` | CI and release workflows, and the `setup-broiler` composite action |
| `Directory.Build.props` | Product version and configuration decomposition |

## Build configuration

The head declares four configurations. `Debug` and `Release` build framework-dependent;
`Debug-Windows` and `Release-Windows` pin `win-x64`. MSBuild only understands `Debug` and
`Release` on its own, so `Directory.Build.props` decomposes the compound names into a base
configuration and a target OS. Without it, `-c Release-Windows` would build unoptimized and
with neither `RELEASE` nor `WINDOWS` defined.

## Dependencies

The application consumes the following components as NuGet packages, directly or
transitively. Versions are pinned in the project files.

| Component | Purpose |
|---|---|
| `Broiler.Documents` | Document model and the RTF, DOCX, HTML, Markdown and PDF codecs |
| `Broiler.DOM` | Canonical DOM, HTML tokenization, parsing, traversal, serialization |
| `Broiler.Graphics` | Managed bitmap/codec/raster core plus platform backends |
| `Broiler.Media` | Image, audio and video abstractions and managed codecs |
| `Broiler.Input` | Keyboard, mouse, pen, touch and text input abstractions |
| `Broiler.UI` | Platform-neutral retained-mode UI toolkit |

## Roadmap

The first version deliberately does one thing. In rough order:

- Panning a picture that is larger than the window
- Several documents and graphics open at once
- Audio and video, once the viewer has a surface for them
- Everything else the Broiler platform learns to read
- File management

## License

Apache License 2.0 — see [LICENSE](LICENSE).
