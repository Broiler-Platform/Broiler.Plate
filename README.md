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
toolkit — lives in its own repository and is consumed here as a submodule.

> **First version.** This is `Broiler.Plate` at its very beginning: one Windows head, one
> file open at a time, opened through the File menu or `Ctrl+O`. Public APIs, repository
> layout and behaviour are not frozen.
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

The dependency components are submodules, so the checkout must be recursive:

```bash
git clone --recursive https://github.com/Broiler-Platform/Broiler.Plate.git
```

If you already cloned without `--recursive`:

```bash
git submodule update --init --recursive
```

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

## Solutions

`.slnx` files are generated, never hand-edited. Declare the entry point in
[`eng/solutions.json`](eng/solutions.json) and run:

```bash
pwsh scripts/update-solutions.ps1
```

`-Verify` fails instead of writing, which is what CI should run.

| Solution | Contents |
|---|---|
| `Broiler.Windows.Plate.slnx` | Windows viewer and its transitive dependencies (46 projects) |

## Continuous integration

[`ci.yml`](.github/workflows/ci.yml) runs three jobs on push, pull request and manual
dispatch:

| Job | Runner | What it protects |
|---|---|---|
| Solution manifest | `ubuntu-latest` | The checked-in `.slnx` still matches the real reference graph. Catches a hand-edit, and the subtler case of adding a `ProjectReference` without regenerating — which leaves the new project out of the solution, so it is never built and never seen to break. |
| Windows head | `windows-latest` | `dotnet build -c Release` of the whole solution. |
| Publish win-x64 (NativeAOT) | `windows-latest` | `Release-Windows` — a configuration the solution does not declare, so nothing else exercises it — published with NativeAOT, then started to check it stays up. |

The publish job earns its place twice. `Release-Windows` can only be built at project level (a
solution-level build with an undeclared configuration fails `MSB4126`), so it is the only
thing standing between a `Directory.Build.props` regression and a release built unoptimized
with neither `RELEASE` nor `WINDOWS` defined. And NativeAOT is the one build mode that fails
on reflection every other mode accepts: adding `Activator.CreateInstance` or reflection-based
serialization to the head or to `Broiler.Plate.Core` breaks it while leaving an ordinary build
green. `PublishAot` is passed on the command line, never set in a `.csproj`, so day-to-day
builds stay framework-dependent and fast.

There is no nested-submodule step. `submodules: true` — top-level, non-recursive — is enough,
for the reason given under [Dependencies](#dependencies); this was rehearsed against a fresh
clone with the nested checkouts left empty.

## Repository layout

| Path | Contents |
|---|---|
| `src/Broiler.Plate` | Shared application (`Broiler.Plate.Core`) — window, menu, the two views, format registry, palette |
| `src/Broiler.Plate.Windows` | Windows head — `WinExe`, Direct2D, Win32 clipboard, and the break-out host that gives each dialog its own OS window |
| `src/Broiler.App` | Source-only directory shared by desktop heads — per-platform clipboards. It has no project of its own; each head links the files it needs. |
| `eng/`, `scripts/` | Solution manifest and generator |
| `.github/` | CI workflow and the `setup-broiler` composite action |
| `Directory.Build.props` | Configuration decomposition and the measured warning suppressions |

## Build configuration

The head declares four configurations. `Debug` and `Release` build framework-dependent;
`Debug-Windows` and `Release-Windows` pin `win-x64`. MSBuild only understands `Debug` and
`Release` on its own, so `Directory.Build.props` decomposes the compound names into a base
configuration and a target OS. Without it, `-c Release-Windows` would build unoptimized and
with neither `RELEASE` nor `WINDOWS` defined.

A clean rebuild emits 24 warnings, all from `Broiler.Dom.Html`, all nullable-annotation
warnings in a component that has not finished its nullable pass. Those three codes are
suppressed and documented in `Directory.Build.props`; the list was measured, not copied, and
should be re-measured after a submodule bump.

## Dependencies

Six components are submodules, pinned to `main`:

| Component | Purpose |
|---|---|
| `Broiler.Documents` | Document model and the RTF, DOCX, HTML, Markdown and PDF codecs |
| `Broiler.DOM` | Canonical DOM, HTML tokenization, parsing, traversal, serialization |
| `Broiler.Graphics` | Managed bitmap/codec/raster core plus platform backends |
| `Broiler.Media` | Image, audio and video abstractions and managed codecs |
| `Broiler.Input` | Keyboard, mouse, pen, touch and text input abstractions |
| `Broiler.UI` | Platform-neutral retained-mode UI toolkit |

Each of those repositories carries nested checkouts of the components *it* depends on, so
that it still builds standalone. `git submodule update --init --recursive` restores the whole
set — but at the revisions pinned here, nothing in this repository's project closure reaches
those nested copies, so a non-recursive `--init` is enough to build.

That is a change from the Writer, whose README still describes components compiling up to
five times through nested checkouts. `Broiler.UI`, `Broiler.Documents` and `Broiler.Graphics`
now reach their dependencies through `$(BroilerGraphicsRoot)`, `$(BroilerInputRoot)`,
`$(BroilerDocumentsRoot)` and `$(BroilerDomRoot)`, which `Directory.Build.props` points at
this repository's own top-level checkouts. Measured rather than assumed: every one of the 46
`ProjectReference` resolutions in this solution's closure lands on a top-level path, and a
clean `Rebuild` emits 46 assemblies, all distinct. The folding table in
`scripts/update-solutions.ps1` is therefore inert here; it is kept as insurance in case a
future bump reintroduces a literal relative path.

## Roadmap

The first version deliberately does one thing. In rough order:

- Several documents and graphics open at once
- Audio and video, once the viewer has a surface for them
- Everything else the Broiler platform learns to read
- File management

## License

Apache License 2.0 — see [LICENSE](LICENSE).
