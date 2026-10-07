# Human Review: Broiler.Plate

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.Plate`, which rewrites this file,
`CODE-ASSURANCE.md`, `assurance.manifest.json` and every generated source header from the
product tree.

> **Status: PENDING.** Human-reviewed: 0 of 233 relevant units. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Plate --release`
> fails while any relevant unit is without a decision bound to its current fingerprint.

## 1. How To Use This File

Read it; do not edit it. A decision about a code unit is the `// Broiler-Human:` line on that
unit's declaration, and every table below is read out of those lines. There is nothing here
to fill in and nothing here to leave blank.

## 2. How A Review Is Recorded

In one place: the `// Broiler-Human:` line of the assurance annotation that sits on the
declaration being read. Nothing in this file is edited by hand, no second document carries a
per-item checklist, and no list of permitted aliases exists to be added to.

```csharp
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=4A3BFD
// Broiler-Falsified-If: a negative value reaches the running total
// Broiler-Human:        PENDING
```

The last line has four shapes. A human writes three of them; the generator writes the fourth
and may never invent an alias, which the check asserts in both directions.

| Line | Meaning |
|---|---|
| `PENDING` | Nobody has recorded a decision for this unit. The generator leaves it exactly as it stands. |
| `<alias>` | A human states their own alias and leaves the machine field to the generator, which fills it with the declaration's fingerprint at the next run. |
| `<alias>; Fingerprint=<six hex>` | A decision bound to one exact version of one declaration. |
| `STALE; Previous=<alias>@<fingerprint>` | Written by the generator when the code moved after a decision. Only a human clears it, by stating their alias again. |

A human may state their own `IP=`, `Security=` and `Resources=` assessment beside their alias,
which is how a reader disagrees with the machine assessment on the line above: an assessment is
a comment and moves no fingerprint, so there is nowhere else to say it.

**No branch, commit or tag is recorded in this file.** Each decision names the fingerprint of
the declaration it was made against, and the state machine compares that value with the
declaration as it now stands. A commit says a tree moved; a fingerprint says whether this unit
did, which is the narrower and the more useful of the two.

## 3. Summary

| Metric | Value |
|---|---:|
| Files scanned | 12 |
| Code units | 329 |
| Relevant | 233 |
| Exempt | 96 |
| Assessed | 233 of 233 (100%) |
| Human reviewed | 0 of 233 (0%) |
| Unverified | 233 |
| Aliases naming a decision | 0 |

## 4. Review States

One row per state of the machine that reads the two lines. The states are computed from the
annotations and the current fingerprints; nothing stores them.

| State | Units |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 233 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 96 |

## 5. Aliases In The Tree

No alias appears on a human line anywhere in the product tree. Nobody has recorded a
decision about any unit of this component.

## 6. Coverage By File

One row per covered file, carrying that file's generated header. `Unverified` counts the
relevant units in a state that blocks a release.

| File | Units | Relevant | Exempt | Unverified | IP risk | Security risk | Criteria |
|---|---:|---:|---:|---:|---|---|---:|
| `src/Broiler.Plate.Windows/PlateHostWindow.cs` | 43 | 34 | 9 | 34 | Low | Critical | 25/6 |
| `src/Broiler.Plate.Windows/PlateWindow.cs` | 23 | 19 | 4 | 19 | Low | Critical | 17/7 |
| `src/Broiler.Plate.Windows/PlateWindowsUiHost.cs` | 13 | 7 | 6 | 7 | Low | High | 7/4 |
| `src/Broiler.Plate.Windows/Program.cs` | 8 | 8 | 0 | 8 | Low | High | 6/6 |
| `src/Broiler.Plate/PlateAbout.cs` | 2 | 2 | 0 | 2 | Low | Low | 2/0 |
| `src/Broiler.Plate/PlateApp.cs` | 119 | 68 | 51 | 68 | Low | High | 52/9 |
| `src/Broiler.Plate/PlateFileFormats.cs` | 19 | 14 | 5 | 14 | Low | High | 14/7 |
| `src/Broiler.Plate/PlateImageFormats.cs` | 16 | 16 | 0 | 16 | Low | High | 15/12 |
| `src/Broiler.Plate/PlateNotesDialog.cs` | 13 | 11 | 2 | 11 | Low | Medium | 6/0 |
| `src/Broiler.Plate/PlatePalette.cs` | 18 | 18 | 0 | 18 | None | None | 0/0 |
| `src/Broiler.Plate/PlateUiHost.cs` | 27 | 13 | 14 | 13 | Low | High | 9/2 |
| `src/Broiler.Plate/PlateZoom.cs` | 28 | 23 | 5 | 23 | Low | Medium | 20/0 |

## 7. Decisions Recorded

No unit in this component carries a decision on its human line. Every one of them reads
`PENDING`.

## 8. Decisions The Code Has Outrun

No unit carries a decision that the code has since moved past.

## 9. Where A Decision Is Required First

The units at the top of the security vocabulary, with the observation that would show each
one wrong and the human line it carries. The set is read from the assessments rather than
written out, so a unit that becomes `High` joins it at the next generation.

- `Broiler.Plate.PlateHostWindow` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=Critical, Spec=none cited, `9479CF`, PENDING
  - Falsified if: Ctrl+V in a broken-out dialog pastes text that did not come from the main window's WindowsClipboard read
- `Broiler.Plate.PlateHostWindow.Activate()` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=High, Spec=none cited, `99FA51`, PENDING
  - Falsified if: SetForegroundWindow is called with the zero handle of a dialog whose native window was already destroyed
- `Broiler.Plate.PlateHostWindow.TryGetText(out string)` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=Critical, Spec=none cited, `3E75CC`, PENDING
  - Falsified if: Ctrl+V in a broken-out dialog pastes text other than what the main window's WindowsClipboard read returned
- `Broiler.Plate.PlateHostWindow.SetText(string)` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=Critical, Spec=none cited, `DFC83A`, PENDING
  - Falsified if: text copied in a broken-out dialog does not reach the main window's WindowsClipboard.SetText, so the system clipboard keeps its previous contents
- `Broiler.Plate.PlateHostWindow.CreateImage(ReadOnlySpan<byte>)` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=High, Spec=none cited, `086BBA`, PENDING
  - Falsified if: an encoded image the renderer's decoder rejects with an exception escapes CreateImage instead of returning BImageHandle.Invalid
- `Broiler.Plate.PlateHostWindow.SetForegroundWindow(IntPtr)` in `src/Broiler.Plate.Windows/PlateHostWindow.cs` - Security=High, Spec=none cited, `5B5789`, PENDING
  - Falsified if: the HWND is marshalled narrower than pointer size, so a 64-bit process brings a truncated window handle to the foreground
- `Broiler.Plate.PlateWindow` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=Critical, Spec=none cited, `1FD40B`, PENDING
  - Falsified if: a paste requested before OnCreated has bound the clipboard reaches WindowsClipboard instead of answering null
- `Broiler.Plate.PlateWindow.OnCreated()` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=High, Spec=none cited, `3B311F`, PENDING
  - Falsified if: the clipboard is bound to a zero window handle, so SetClipboardData fails after EmptyClipboard has already cleared the system clipboard
- `Broiler.Plate.PlateWindow.ReadClipboardText()` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=Critical, Spec=none cited, `ABF7C4`, PENDING
  - Falsified if: a paste requested before OnCreated has bound the clipboard reaches WindowsClipboard instead of answering null
- `Broiler.Plate.PlateWindow.WriteClipboardText(string)` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=Critical, Spec=none cited, `53E442`, PENDING
  - Falsified if: a copy requested before OnCreated has bound the clipboard throws NullReferenceException instead of being dropped
- `Broiler.Plate.PlateWindow.CloseNativeWindow()` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=High, Spec=none cited, `CE27BE`, PENDING
  - Falsified if: a close requested while the window is open destroys it inside the command that asked, instead of after that input has been dispatched
- `Broiler.Plate.PlateWindow.CloseNativeWindowNow()` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=High, Spec=none cited, `698F16`, PENDING
  - Falsified if: DestroyWindow is called with a zero or already-destroyed window handle
- `Broiler.Plate.PlateWindow.DestroyWindow(IntPtr)` in `src/Broiler.Plate.Windows/PlateWindow.cs` - Security=High, Spec=none cited, `B255F0`, PENDING
  - Falsified if: the HWND is marshalled narrower than pointer size, so a 64-bit process destroys a truncated window handle
- `Broiler.Plate.PlateWindowsUiHost` in `src/Broiler.Plate.Windows/PlateWindowsUiHost.cs` - Security=High, Spec=none cited, `0F2F4A`, PENDING
  - Falsified if: ClientToScreen writes the owner's client origin into a NativePoint that is not two sequential 32-bit integers
- `Broiler.Plate.PlateWindowsUiHost.ToScreenPlacement(BWindow?, UiHostWindowRequest)` in `src/Broiler.Plate.Windows/PlateWindowsUiHost.cs` - Security=High, Spec=none cited, `B8790F`, PENDING
  - Falsified if: an owner client origin of (300, 200) physical pixels at Scale 1.5 moves the placement by anything other than (200, 133.3) device-independent pixels
- `Broiler.Plate.PlateWindowsUiHost.NativePoint` in `src/Broiler.Plate.Windows/PlateWindowsUiHost.cs` - Security=High, Spec=none cited, `57F17C`, PENDING
  - Falsified if: the struct is not two sequential 32-bit signed fields, so ClientToScreen writes a POINT that does not fit it
- `Broiler.Plate.PlateWindowsUiHost.ClientToScreen(IntPtr, ref NativePoint)` in `src/Broiler.Plate.Windows/PlateWindowsUiHost.cs` - Security=High, Spec=none cited, `0F77BE`, PENDING
  - Falsified if: a failed ClientToScreen is marshalled as true because the result is not a 4-byte Win32 BOOL, so an unconverted origin offsets the dialog
- `Broiler.Plate.Program` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, Spec=none cited, `A32934`, PENDING
  - Falsified if: a file opened in this head reaches a decoder the composition root did not compose, such as a PDF CCITTFaxDecode or JBIG2Decode stream filter
- `Broiler.Plate.Program.Main()` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, Spec=none cited, `492F78`, PENDING
  - Falsified if: a graphic opened in the window is decoded by a codec outside ManagedImageCodecs.CreateCodecs(), because BImageCodecs.Use ran after PlateWindow was created or not at all
- `Broiler.Plate.Program.CreateFileFormats()` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, Spec=none cited, `078051`, PENDING
  - Falsified if: the PDF format added here is built on a service graph other than CreatePdfServices(), so a PDF opened in this head decodes through filters that method does not compose
- `Broiler.Plate.Program.CreatePdfServices()` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, Spec=none cited, `F7BE5F`, PENDING
  - Falsified if: the returned graph's StreamFilters holds a CCITTFaxDecode, JBIG2Decode or JPXDecode filter, when JpegStreamFilter is the only filter added to PdfCodecServices.Base
- `Broiler.Plate.Program.SetProcessDpiAwarenessContext(IntPtr)` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, Spec=none cited, `857A4A`, PENDING
  - Falsified if: the context argument is marshalled narrower than a pointer, so the -4 PER_MONITOR_AWARE_V2 handle reaches user32 truncated in a 64-bit process
- `Broiler.Plate.Program.MessageBox(IntPtr, string, string, uint)` in `src/Broiler.Plate.Windows/Program.cs` - Security=High, Spec=none cited, `AE852B`, PENDING
  - Falsified if: the text or caption reaches user32 as ANSI bytes, because CharSet.Unicode does not bind the MessageBoxW entry point
- `Broiler.Plate.PlateApp` in `src/Broiler.Plate/PlateApp.cs` - Security=High, Spec=none cited, `A2E9EA`, PENDING
  - Falsified if: a hostile file opened through OpenFile ends the viewer with an exception instead of leaving a refused-open line in the status bar
- `Broiler.Plate.PlateApp.SignatureLength` in `src/Broiler.Plate/PlateApp.cs` - Security=High, Spec=none cited, `461D0D`, PENDING
  - Falsified if: a signature that ContentTypeForSignature recognizes needs more leading bytes than SignatureLength, so a graphic of that format is handed to the document codecs first
- `Broiler.Plate.PlateApp.PlateApp(PlateUiHost, Action, PlateFileFormats?)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, Spec=none cited, `4B8E45`, PENDING
  - Falsified if: a PlateApp constructed without formats opens a PDF, although CreateDefault composes only the RTF, DOCX, HTML and Markdown codecs
- `Broiler.Plate.PlateApp.OpenFile(string)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, Spec=none cited, `C3CEFC`, PENDING
  - Falsified if: a file whose leading bytes carry a PNG, JPEG, GIF, BMP, TIFF or WebP signature is handed to a document codec before the image decoder sees it
- `Broiler.Plate.PlateApp.ShowOpenDialog()` in `src/Broiler.Plate/PlateApp.cs` - Security=High, Spec=none cited, `AF9E20`, PENDING
  - Falsified if: a cancelled dialog, or one accepted with an empty or whitespace name, still calls OpenFile
- `Broiler.Plate.PlateApp.OpenDocument(string)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, Spec=none cited, `91DBBF`, PENDING
  - Falsified if: a document the codecs' default read limits refuse is read whole into memory first, because it reaches SelectAndRead other than as the open file stream
- `Broiler.Plate.PlateApp.OpenImage(string)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, Spec=none cited, `889289`, PENDING
  - Falsified if: a file of several hundred megabytes that carries an image signature is read whole into memory by File.ReadAllBytes before any decoder limit applies
- `Broiler.Plate.PlateApp.ReadSignature(string, out bool)` in `src/Broiler.Plate/PlateApp.cs` - Security=High, Spec=none cited, `59B600`, PENDING
  - Falsified if: a file shorter than SignatureLength yields bytes past those read, such as trailing zeros that complete a signature
- `Broiler.Plate.PlateApp.GetDialogDirectory()` in `src/Broiler.Plate/PlateApp.cs` - Security=High, Spec=none cited, `698B87`, PENDING
  - Falsified if: a directory that no longer exists is handed to the file dialog as its starting directory
- `Broiler.Plate.PlateDocumentFormat` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, Spec=none cited, `C46794`, PENDING
  - Falsified if: a format is constructed over a codec whose CanRead is false and is then offered in the Open dialog
- `Broiler.Plate.PlateDocumentFormat.PlateDocumentFormat(DocumentCodec, string)` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, Spec=none cited, `EDE1AB`, PENDING
  - Falsified if: a codec whose descriptor lists no file extension is accepted instead of being refused with an ArgumentException
- `Broiler.Plate.PlateFileFormats` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, Spec=none cited, `52CA12`, PENDING
  - Falsified if: two registered formats claiming one extension that differs only in case are both accepted into one set
- `Broiler.Plate.PlateFileFormats.PlateFileFormats(IEnumerable<PlateDocumentFormat>)` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, Spec=none cited, `32128D`, PENDING
  - Falsified if: a document format claiming an extension that HasImageExtension accepts, such as .jpeg, is composed without an ArgumentException
- `Broiler.Plate.PlateFileFormats.CreateDefault()` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, Spec=none cited, `E957B9`, PENDING
  - Falsified if: the catalog of a default set holds a codec other than the RTF, DOCX, HTML and Markdown ones, such as the PDF codec
- `Broiler.Plate.PlateFileFormats.With(params PlateDocumentFormat[])` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, Spec=none cited, `DAA2C7`, PENDING
  - Falsified if: a format added through With that claims an extension the set already holds is accepted instead of refused
- `Broiler.Plate.PlateFileFormats.CreateOpenCatalog()` in `src/Broiler.Plate/PlateFileFormats.cs` - Security=High, Spec=none cited, `6CF15E`, PENDING
  - Falsified if: the returned catalog holds a codec that is not the Codec of one of the registered document formats
- `Broiler.Plate.PlateImageFormats` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `3F4645`, PENDING
  - Falsified if: a BMP header whose height field is 0x80000000 makes MeasureDisplaySize throw OverflowException instead of returning the default extent
- `Broiler.Plate.PlateImageFormats.AllExtensions` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `3C452A`, PENDING
  - Falsified if: an extension that FilterPattern offers, such as .tiff or .dib, is missing from AllExtensions
- `Broiler.Plate.PlateImageFormats.HasImageExtension(string)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `FCED51`, PENDING
  - Falsified if: a path whose image extension is not its last one, such as report.png.rtf, is reported as an image
- `Broiler.Plate.PlateImageFormats.ContentTypeFor(string, ReadOnlySpan<byte>)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `B486D0`, PENDING
  - Falsified if: a file whose bytes carry the PNG signature but whose name ends in .gif is given image/gif instead of image/png
- `Broiler.Plate.PlateImageFormats.MeasureDisplaySize(ReadOnlySpan<byte>, double)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `A76F6A`, PENDING
  - Falsified if: a header stating a width or height of zero or below is returned as the display size instead of the default extent
- `Broiler.Plate.PlateImageFormats.TryReadPixelSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `305C07`, PENDING
  - Falsified if: an input truncated right after a recognised signature, such as the four bytes GIF8, throws instead of returning false
- `Broiler.Plate.PlateImageFormats.ContentTypeForSignature(ReadOnlySpan<byte>)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `A9D172`, PENDING
  - Falsified if: a RIFF file whose bytes 8 to 11 are not WEBP, such as a WAVE file, is identified as image/webp
- `Broiler.Plate.PlateImageFormats.TryReadPngSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `8C5CD8`, PENDING
  - Falsified if: a PNG shorter than 24 bytes, or whose bytes 12 to 15 are not IHDR, reports a size instead of returning false
- `Broiler.Plate.PlateImageFormats.TryReadJpegSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `159C5D`, PENDING
  - Falsified if: a segment length that carries the offset past the end of the data makes the walk index outside the span instead of returning false
- `Broiler.Plate.PlateImageFormats.TryReadGifSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `C642D2`, PENDING
  - Falsified if: a GIF shorter than 10 bytes reports a size instead of returning false
- `Broiler.Plate.PlateImageFormats.TryReadBmpSize(ReadOnlySpan<byte>, out int, out int)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `0306F2`, PENDING
  - Falsified if: a BMP whose height field is 0x80000000 (int.MinValue) throws OverflowException from Math.Abs instead of returning false
- `Broiler.Plate.PlateImageFormats.StartsWith(ReadOnlySpan<byte>, ReadOnlySpan<byte>)` in `src/Broiler.Plate/PlateImageFormats.cs` - Security=High, Spec=none cited, `8BD7BE`, PENDING
  - Falsified if: data shorter than the prefix is sliced and throws instead of returning false
- `Broiler.Plate.PlateUiHost` in `src/Broiler.Plate/PlateUiHost.cs` - Security=High, Spec=none cited, `5D8FD8`, PENDING
  - Falsified if: bytes from an opened file that make the renderer's decoder throw take down the frame instead of leaving the view to draw the picture's outline
- `Broiler.Plate.PlateUiHost.CreateImage(ReadOnlySpan<byte>)` in `src/Broiler.Plate/PlateUiHost.cs` - Security=High, Spec=none cited, `7D1AD8`, PENDING
  - Falsified if: a decoder exception other than OutOfMemoryException, such as InvalidDataException for a truncated PNG, escapes CreateImage instead of yielding BImageHandle.Invalid

## 10. What This Record Does Not Say

It is not an approval of the component, and a full table above would not be one either. It
records which declarations somebody stated a decision about, and against which version of
each. It does not record what they read, how long they spent, or whether they were right.

A fingerprint is six hex characters of SHA-256 over a declaration's token texts. It answers
whether a unit changed since a decision was recorded against it. It is not a collision-free
identifier across units and it is not a cryptographic commitment, so it detects a change and
does not resist a forger with commit access.

An assessment is a comment, so changing one moves no fingerprint anywhere, and nothing
mechanical checks that it is right; the check holds its values to their vocabularies and no
further.

233 of the 233 assessed units declare `Origin=AI`. Reading a declaration is the only thing
that makes it read.
