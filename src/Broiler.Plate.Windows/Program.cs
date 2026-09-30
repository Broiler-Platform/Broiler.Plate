// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           0
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    High
// Criteria:         6/6
// Resource impact:  8/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Graphics.Imaging;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Broiler.Plate;

/// <summary>Windows entry point for Broiler Plate.</summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=A32934
// Broiler-Falsified-If: a file opened in this head reaches a decoder the composition root did not compose, such as a PDF CCITTFaxDecode or JBIG2Decode stream filter
// Broiler-Human:        PENDING
[SupportedOSPlatform("windows7.0")]
internal static class Program
{
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=492F78
    // Broiler-Falsified-If: a graphic opened in the window is decoded by a codec outside ManagedImageCodecs.CreateCodecs(), because BImageCodecs.Use ran after PlateWindow was created or not at all
    // Broiler-Human:        PENDING
    [STAThread]
    private static int Main()
    {
        _ = SetProcessDpiAwarenessContext(new IntPtr(-4)); // PER_MONITOR_AWARE_V2, best effort.

        // Composition root. A viewer without a codec catalog cannot decode the
        // pictures a document embeds, and could not open a graphics file at all -
        // which for this application is half of what it does.
        BImageCodecs.Use(
            new Broiler.Media.MediaCodecCatalog(Broiler.Media.Image.Managed.ManagedImageCodecs.CreateCodecs()));

        try
        {
            using var window = new PlateWindow(CreateFileFormats());
            return window.Run();
        }
        catch (Exception ex)
        {
            MessageBox(IntPtr.Zero, ex.ToString(), "Broiler Plate", MbIconError | MbOk);
            return 1;
        }
    }

    /// <summary>
    /// The document formats this head offers: the shared four, plus PDF.
    /// </summary>
    /// <remarks>
    /// PDF is registered here rather than in <c>Broiler.Plate.Core</c>
    /// deliberately. Putting it in the shared core would hand it to every head
    /// that references the core - including any future Android or WebAssembly
    /// viewer, whose package-size, memory, trimming and AOT gates it has not
    /// passed - and a codec must not reach a head by being someone else's
    /// transitive reference (PDF roadmap 10.1). Each head that wants it says so,
    /// here.
    ///
    /// The Writer registers this codec for opening only, because PDF export has
    /// its own release gate it has not passed. That distinction does not arise
    /// here: a viewer never writes, so every format it carries is an opening one.
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=078051
    // Broiler-Falsified-If: the PDF format added here is built on a service graph other than CreatePdfServices(), so a PDF opened in this head decodes through filters that method does not compose
    // Broiler-Human:        PENDING
    private static PlateFileFormats CreateFileFormats() =>
        PlateFileFormats.CreateDefault().With(
            new PlateDocumentFormat(
                new Broiler.Documents.Pdf.PdfDocumentCodec(CreatePdfServices()), "PDF"));

    /// <summary>
    /// The PDF service graph this head composes: the JPEG decoder, and nothing
    /// else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Composing a filter is what puts a picture on the page. A decoded image is
    /// admitted by the read's resource policy and projected into the document, so
    /// this is the line that decides whether a PDF's photographs are visible in
    /// the viewer or reported as skipped - which for a viewer is most of what it
    /// was opened to do.
    /// </para>
    /// <para>
    /// <strong>Why JPEG and nothing else.</strong> Not because the others are
    /// uncleared - IP-008 approved JBIG2 and IP-009 retired the fax patent
    /// position - but because both of their decode paths rest on <c>SRC-017</c>,
    /// which is pending: the fax decoder needs T.4's transcribed code tables and
    /// JBIG2's MMR regions decode through that same decoder. A pending row still
    /// blocks, so neither is composed into anything that ships. JPEG 2000 has no
    /// entropy decoder to compose at all.
    /// </para>
    /// <para>
    /// Referencing <c>Broiler.Documents.Pdf.Images</c> links those adapters even
    /// so. Linking is not composing, and this is where the difference is decided.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=F7BE5F
    // Broiler-Falsified-If: the returned graph's StreamFilters holds a CCITTFaxDecode, JBIG2Decode or JPXDecode filter, when JpegStreamFilter is the only filter added to PdfCodecServices.Base
    // Broiler-Human:        PENDING
    internal static Broiler.Documents.Pdf.PdfCodecServices CreatePdfServices() =>
        Broiler.Documents.Pdf.PdfCodecServices.Base.WithStreamFilters(
            new Broiler.Documents.Pdf.Images.JpegStreamFilter());

    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=9A757C
    // Broiler-Human:        PENDING
    private const uint MbOk = 0x00000000;
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=4AE07E
    // Broiler-Human:        PENDING
    private const uint MbIconError = 0x00000010;

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=857A4A
    // Broiler-Falsified-If: the context argument is marshalled narrower than a pointer, so the -4 PER_MONITOR_AWARE_V2 handle reaches user32 truncated in a 64-bit process
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=AE852B
    // Broiler-Falsified-If: the text or caption reaches user32 as ANSI bytes, because CharSet.Unicode does not bind the MessageBoxW entry point
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hwnd, string text, string caption, uint type);
}
