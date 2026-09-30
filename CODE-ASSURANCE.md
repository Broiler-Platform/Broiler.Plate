# Broiler.Plate Code Assurance

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.Plate`, which rewrites this file,
`HUMAN_REVIEW.md`, `assurance.manifest.json` and every generated source header from the
product tree.

**No code unit in this component carries a decision on its human line yet.** This report
records that absence precisely. It is not a claim that the code is reviewed, assured or safe,
and the figures below are the measurement of how far from that claim the per-unit record is.

## Summary

| Metric | Value |
|---|---:|
| Files scanned | 13 |
| Files not covered | 0 |
| Files carrying an annotation | 13 |
| Code units | 344 |
| Relevant | 248 |
| Exempt by predicate | 96 |
| Annotated | 248 of 248 (100%) |
| Human reviewed | 0 of 248 (0%) |
| Unverified | 248 |

## Review states

| State | Count |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 248 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 96 |

## IP risk

| Value | Units |
|---|---:|
| None | 121 |
| Low | 127 |
| Medium | 0 |
| High | 0 |
| Unknown | 0 |
| *not annotated* | 0 |

## Security risk

| Value | Units |
|---|---:|
| None | 37 |
| Low | 86 |
| Medium | 57 |
| High | 58 |
| Critical | 10 |
| *not annotated* | 0 |

## Resource impact

| Metric | Value |
|---|---:|
| Maximum | 8 / 10 |
| Average over annotated units | 1.9 / 10 |
| Units scored | 248 |

## High-security review areas

- `Broiler.App.WindowsClipboard` in `src/Broiler.App/WindowsClipboard.cs` - Security=Critical, human line PENDING
- `Broiler.App.WindowsClipboard.CfUnicodeText` in `src/Broiler.App/WindowsClipboard.cs` - Security=Critical, human line PENDING
- `Broiler.App.WindowsClipboard.GmemMoveable` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.TryGetText(out string)` in `src/Broiler.App/WindowsClipboard.cs` - Security=Critical, human line PENDING
- `Broiler.App.WindowsClipboard.SetText(string)` in `src/Broiler.App/WindowsClipboard.cs` - Security=Critical, human line PENDING
- `Broiler.App.WindowsClipboard.OpenClipboard(IntPtr)` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.CloseClipboard()` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.EmptyClipboard()` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.IsClipboardFormatAvailable(uint)` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.GetClipboardData(uint)` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.SetClipboardData(uint, IntPtr)` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.GlobalAlloc(uint, UIntPtr)` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.GlobalFree(IntPtr)` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.GlobalLock(IntPtr)` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.App.WindowsClipboard.GlobalUnlock(IntPtr)` in `src/Broiler.App/WindowsClipboard.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateHostWindow` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=Critical, human line PENDING
- `Broiler.Plate.PlateHostWindow.Activate()` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateHostWindow.TryGetText(out string)` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=Critical, human line PENDING
- `Broiler.Plate.PlateHostWindow.SetText(string)` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=Critical, human line PENDING
- `Broiler.Plate.PlateHostWindow.CreateImage(ReadOnlySpan<byte>)` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateHostWindow.SetForegroundWindow(IntPtr)` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateWindow` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=Critical, human line PENDING
- `Broiler.Plate.PlateWindow.OnCreated()` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateWindow.ReadClipboardText()` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=Critical, human line PENDING
- `Broiler.Plate.PlateWindow.WriteClipboardText(string)` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=Critical, human line PENDING
- `Broiler.Plate.PlateWindow.CloseNativeWindow()` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateWindow.CloseNativeWindowNow()` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateWindow.DestroyWindow(IntPtr)` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateWindowsUiHost` in `src/Broiler.Plate.Windows/PlateWindowsUiHost.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateWindowsUiHost.ToScreenPlacement(BWindow?, UiHostWindowRequest)` in `src/Broiler.Plate.Windows/PlateWindowsUiHost.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateWindowsUiHost.NativePoint` in `src/Broiler.Plate.Windows/PlateWindowsUiHost.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateWindowsUiHost.ClientToScreen(IntPtr, ref NativePoint)` in `src/Broiler.Plate.Windows/PlateWindowsUiHost.cs` - Security=High, human line PENDING
- `Broiler.Plate.Program` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, human line PENDING
- `Broiler.Plate.Program.Main()` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, human line PENDING
- `Broiler.Plate.Program.CreateFileFormats()` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, human line PENDING
- `Broiler.Plate.Program.CreatePdfServices()` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, human line PENDING
- `Broiler.Plate.Program.SetProcessDpiAwarenessContext(IntPtr)` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, human line PENDING
- `Broiler.Plate.Program.MessageBox(IntPtr, string, string, uint)` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateApp` in `src/Broiler.Plate/PlateApp.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateApp.SignatureLength` in `src/Broiler.Plate/PlateApp.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateApp.PlateApp(PlateUiHost, Action, PlateFileFormats?)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateApp.OpenFile(string)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateApp.ShowOpenDialog()` in `src/Broiler.Plate/PlateApp.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateApp.OpenDocument(string)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateApp.OpenImage(string)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateApp.ReadSignature(string, out bool)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateApp.GetDialogDirectory()` in `src/Broiler.Plate/PlateApp.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateDocumentFormat` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateDocumentFormat.PlateDocumentFormat(DocumentCodec, string)` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateFileFormats` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateFileFormats.PlateFileFormats(IEnumerable<PlateDocumentFormat>)` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateFileFormats.CreateDefault()` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateFileFormats.With(params PlateDocumentFormat[])` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateFileFormats.CreateOpenCatalog()` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.AllExtensions` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.HasImageExtension(string)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.ContentTypeFor(string, ReadOnlySpan<byte>)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.MeasureDisplaySize(ReadOnlySpan<byte>, double)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.TryReadPixelSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.ContentTypeForSignature(ReadOnlySpan<byte>)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.TryReadPngSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.TryReadJpegSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.TryReadGifSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.TryReadBmpSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateImageFormats.StartsWith(ReadOnlySpan<byte>, ReadOnlySpan<byte>)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateUiHost` in `src/Broiler.Plate/PlateUiHost.cs` - Security=High, human line PENDING
- `Broiler.Plate.PlateUiHost.CreateImage(ReadOnlySpan<byte>)` in `src/Broiler.Plate/PlateUiHost.cs` - Security=High, human line PENDING

## Falsification criteria

| Metric | Value |
|---|---:|
| Units carrying a criterion | 188 |
| Units required to carry one | 68 |
| Required and missing | 0 |

A `Broiler-Falsified-If:` line states, at the declaration, the observation that would make
the unit wrong. `Security=High` says a unit is risky, which is a set and not a test; the
criterion is the test. It is required where `Security` is `High` or `Critical`, permitted
elsewhere, and `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Plate` names every unit that owes one and carries none.

The line is a comment, so it is outside every fingerprint by construction: rewording a
criterion moves no recorded value here, in a file header or in
`assurance.manifest.json`, and invalidates nothing. That is the intended reading - a
criterion is an instruction to whoever reads the unit, not part of what a review is bound to.

## Exemption

Exemption is decided by one predicate in `CSharpAssuranceScanner`, not per unit, so
that the rule is reviewable in one place rather than in several hundred.

| Case | Units |
|---|---:|
| TrivialPropertyOrAccessor | 19 |
| ParameterAssigningConstructor | 0 |
| TrivialExpressionBodiedMember | 7 |
| CompilerSuppliedRecordOrEnumMember | 0 |
| DelegatingOverrideOrOperator | 0 |
| InsideAssemblyMarker | 0 |
| FieldDeclaringStorage | 64 |
| EnumMemberOfADeclaredVocabulary | 6 |
| DeclaredInSource | 0 |

## Per-unit exemptions

| Metric | Value |
|---|---:|
| Per-unit exemptions | 0 |

A per-unit `EXEMPT=<reason>` line exempts one unit by a reason a human wrote, for what the
predicate cannot see. Nothing mechanical checks that the reason is true, that it describes
the unit it sits on, or that it says anything at all, so every use is counted and named
here.

No unit in this component states a per-unit exemption.

## Files not covered

No file under a covered project's directory, and no file a covered project compiles in
through a `<Compile Include>` it states, is left out of the record.

## Change detection

`assurance.manifest.json` lists **every** code unit in the 2 covered assemblies -
344 of them, exempt and relevant alike - with the fingerprint of its declaration.
This manifest is a change-detection record, not a review. A unit listed there is watched, not reviewed:
the entry records what the declaration's tokens hashed to when the generator last ran, and
nothing else. What the manifest adds is that a unit the exemption predicate treats as
trivial is no longer invisible: a semantic change to one moves a value in a generated file
the check compares byte for byte. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Plate` holds the manifest to the tree.

Beside the units it lists **every covered file** - 13 of them - with a
fingerprint over the complete token stream of its compilation unit. A unit entry exists only
for a declaration kind the scanner enumerates, and an enumeration is a whitelist: an
`[assembly: ...]` attribute is a member of nothing and can be in no unit at all.
Nothing in a covered file can change without something moving here, whatever kind of declaration it is. Comments are outside the stream, because a token's
text is its own characters, so the generated header above and the annotation lines below move
no file fingerprint - which is what lets one generation be a fixed point.

## Verification

The generator and the check are one computation: `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Plate` works out what the
generator would write and compares it with the tree byte for byte, so a record edited by
hand, or left behind by code that moved, is reported rather than trusted.

| Mode | Command | Effect |
|---|---|---|
| Generate | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.Plate` | Fills every `Fingerprint=TBF`, refreshes a decision the code has outrun into `STALE; Previous=...`, rewrites the generated headers, `HUMAN_REVIEW.md`, `assurance.manifest.json` and this file. |
| Check | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Plate` | Reports every generated artefact that is not byte-identical to what the generator would produce, every relevant unit with no annotation, every annotation this system cannot read, every fingerprint out of date and every unit at the top of the security vocabulary without a criterion. |
| Release | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Plate --release` | The check, and additionally every relevant unit left in a state that blocks a release. |

The fingerprint is six hex characters - 24 bits - of SHA-256 over the declaration's token
texts, joined by single spaces. Trivia is excluded because a token's text is its own
characters and never the comments or whitespace around it, so `dotnet format` moves no
fingerprint and an annotation is never part of what it describes. The value answers whether a
unit changed since it was reviewed. It is not a collision-free identifier across units and it
is not a cryptographic commitment.
