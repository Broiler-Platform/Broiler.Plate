using System.Globalization;
using System.Text;
using Broiler.Documents;
using Broiler.Documents.Model;
using Broiler.Documents.Pdf;
using Broiler.Documents.Pdf.Filters;
using Broiler.Media.Image;
using Broiler.Media.Image.Managed;

namespace Broiler.Plate.Windows.Tests;

/// <summary>
/// What this head composes into the PDF codec, and what that composition is
/// worth to someone looking at a document.
/// </summary>
/// <remarks>
/// <para>
/// The pairing is the point, and it is why both halves are here rather than one.
/// A viewer that composes no decoder still opens a PDF, still shows its text, and
/// still reports what it skipped — so nothing fails, and the photographs are
/// simply absent. That is how this head shipped for a while: the codec was
/// registered with no services at all, and every JPEG in every document came back
/// as <c>pdf.image.dct.tuple-unsupported</c>. A test that only asserted the file
/// opens would have stayed green through all of it.
/// </para>
/// <para>
/// <strong>And nothing else.</strong> The upper bound is as much of the contract
/// as the lower one. CCITT fax and JBIG2 both decode in the composed adapters and
/// both rest on <c>SRC-017</c>, which is pending; JPEG 2000 decodes a Part 1
/// codestream and sits outside what this product has cleared to ship. Composing
/// any of them is a decision with a licence position behind it, not a
/// convenience, and it must not happen by someone adding a filter to a list.
/// </para>
/// </remarks>
public sealed class PlatePdfCompositionTests
{
    [Fact]
    public void The_Head_Composes_Jpeg_And_Nothing_Else()
    {
        PdfCodecServices composed = Program.CreatePdfServices();

        Assert.True(composed.SupportsFilter(PdfFilterNames.Dct));

        Assert.False(composed.SupportsFilter(PdfFilterNames.CcittFax));
        Assert.False(composed.SupportsFilter(PdfFilterNames.Jbig2));
        Assert.False(composed.SupportsFilter(PdfFilterNames.Jpx));

        // Linking the adapters is not composing their filters. The head
        // references Broiler.Documents.Pdf.Images, which brings every decoder in
        // it along; the base graph still decodes none of them, so the decision
        // stays this head's and is visible in one place.
        Assert.False(PdfCodecServices.Base.SupportsFilter(PdfFilterNames.Dct));
    }

    [Fact]
    public void A_Jpeg_In_A_Pdf_Is_Shown_Rather_Than_Reported()
    {
        byte[] pdf = PdfWithJpeg(32, 16);

        DocumentReadResult result = Read(pdf, Program.CreatePdfServices());

        Assert.True(result.IsUsable);
        InlineImage image = Assert.Single(ImagesIn(result.Document));
        Assert.Equal(32, image.Resource.PixelWidth);
        Assert.Equal(16, image.Resource.PixelHeight);

        // No note against it either: an image that arrived is not a skip.
        Assert.DoesNotContain(
            result.Diagnostics,
            d => d.Code == PdfDiagnosticCodes.FilterDctUnsupported
                || d.Code == PdfDiagnosticCodes.ImageNotComposed
                || d.Code == PdfDiagnosticCodes.ImageDecodedNotProjected);
    }

    [Fact]
    public void Without_The_Decoder_The_Same_File_Reports_Instead()
    {
        // The other half of the boundary, and the state this head was in: the
        // document still opens and still reads, and the picture is gone with a
        // note naming the exact tuple that would have decoded it.
        byte[] pdf = PdfWithJpeg(32, 16);

        DocumentReadResult result = Read(pdf, PdfCodecServices.Base);

        Assert.True(result.IsUsable);
        Assert.Empty(ImagesIn(result.Document));

        DocumentDiagnostic skipped = Assert.Single(
            result.Diagnostics.Where(d => d.Code == PdfDiagnosticCodes.FilterDctUnsupported));
        Assert.Contains("32x16 8bpc DeviceRGB DCTDecode", skipped.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Reads the way <c>PlateApp.OpenDocument</c> does: through the codec, with
    /// no read options at all. A viewer passes none, so the defaults are what
    /// actually decide whether a decoded picture reaches the model, and a test
    /// that supplied its own policy would be proving something no user gets.
    /// </summary>
    private static DocumentReadResult Read(byte[] pdf, PdfCodecServices services)
    {
        using var stream = new MemoryStream(pdf, writable: false);
        return new PdfDocumentCodec(services).Read(stream);
    }

    private static List<InlineImage> ImagesIn(RichTextDocument document)
    {
        var images = new List<InlineImage>();
        foreach (RichTextParagraph paragraph in document.Paragraphs)
        {
            foreach (StyleRun run in paragraph.Runs)
            {
                if (run.Style.Image is InlineImage image)
                    images.Add(image);
            }
        }

        return images;
    }

    /// <summary>
    /// A one-page PDF drawing a JPEG, assembled object by object.
    /// </summary>
    /// <remarks>
    /// Nothing is committed: the JPEG is encoded here by the same managed codec
    /// the viewer composes, and the file around it is five objects and a
    /// cross-reference table.
    /// </remarks>
    private static byte[] PdfWithJpeg(int width, int height)
    {
        byte[] rgba = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = ((y * width) + x) * 4;
                rgba[i] = (byte)(x * 255 / Math.Max(1, width - 1));
                rgba[i + 1] = (byte)(y * 255 / Math.Max(1, height - 1));
                rgba[i + 2] = 96;
                rgba[i + 3] = 255;
            }
        }

        byte[] jpeg = JpegImageCodec.Encode(new ImageBuffer(width, height, rgba), quality: 90);
        string content = string.Create(
            CultureInfo.InvariantCulture,
            $"q {width} 0 0 {height} 40 700 cm /Im0 Do Q");

        var objects = new List<byte[]>
        {
            Latin1("<< /Type /Catalog /Pages 2 0 R >>"),
            Latin1("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Latin1(
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
                "/Resources << /XObject << /Im0 5 0 R >> >> /Contents 4 0 R >>"),
            Stream(
                Latin1("<< /Length " + content.Length.ToString(CultureInfo.InvariantCulture) + " >>"),
                Latin1(content)),
            Stream(
                Latin1(string.Create(
                    CultureInfo.InvariantCulture,
                    $"<< /Type /XObject /Subtype /Image /Width {width} /Height {height} " +
                    $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpeg.Length} >>")),
                jpeg),
        };

        var file = new List<byte>();
        void Append(string text) => file.AddRange(Latin1(text));

        Append("%PDF-1.7\n");
        var offsets = new List<int>();
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(file.Count);
            Append((i + 1).ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
            file.AddRange(objects[i]);
            Append("\nendobj\n");
        }

        int startxref = file.Count;
        Append("xref\n0 " + (objects.Count + 1).ToString(CultureInfo.InvariantCulture) + "\n");
        Append("0000000000 65535 f \n");
        foreach (int offset in offsets)
            Append(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");

        Append("trailer\n<< /Size " + (objects.Count + 1).ToString(CultureInfo.InvariantCulture) + " /Root 1 0 R >>\n");
        Append("startxref\n" + startxref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");
        return file.ToArray();

        static byte[] Stream(byte[] dictionary, byte[] data)
        {
            var bytes = new List<byte>(dictionary);
            bytes.AddRange(Latin1("\nstream\n"));
            bytes.AddRange(data);
            bytes.AddRange(Latin1("\nendstream"));
            return bytes.ToArray();
        }
    }

    private static byte[] Latin1(string text) => Encoding.Latin1.GetBytes(text);
}
