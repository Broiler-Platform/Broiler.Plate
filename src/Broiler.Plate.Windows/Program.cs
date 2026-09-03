using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Broiler.Plate;

/// <summary>Windows entry point for Broiler Plate.</summary>
[SupportedOSPlatform("windows7.0")]
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        _ = SetProcessDpiAwarenessContext(new IntPtr(-4)); // PER_MONITOR_AWARE_V2, best effort.

        // Composition root. A viewer without a codec catalog cannot decode the
        // pictures a document embeds, and could not open a graphics file at all -
        // which for this application is half of what it does.
        Broiler.Graphics.BImageCodecs.Use(
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
    internal static Broiler.Documents.Pdf.PdfCodecServices CreatePdfServices() =>
        Broiler.Documents.Pdf.PdfCodecServices.Base.WithStreamFilters(
            new Broiler.Documents.Pdf.Images.JpegStreamFilter());

    private const uint MbOk = 0x00000000;
    private const uint MbIconError = 0x00000010;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hwnd, string text, string caption, uint type);
}
