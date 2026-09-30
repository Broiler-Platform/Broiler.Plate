// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   16
// Annotated:        16/16
// Exempt:           0
// Human-reviewed:   0/16
// IP risk:          Low
// Security risk:    High
// Criteria:         15/12
// Resource impact:  3/10 max
// Unverified:       16
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Buffers.Binary;
using System.IO;
using Broiler.Graphics;
using Broiler.Graphics.Geometry;

namespace Broiler.Plate;

/// <summary>
/// Recognizes the graphics files the viewer will open and reads their pixel size
/// straight out of the encoded header.
/// </summary>
/// <remarks>
/// <para>
/// The size is read here rather than by decoding, so the view can report a
/// picture's dimensions without a pixel buffer, and can do it before — or
/// without — a renderer that can upload the image. A format whose header is not
/// understood still opens; the view simply has no dimensions to report for it.
/// </para>
/// <para>
/// This table decides only what the viewer <em>offers</em>. What actually
/// decodes is the codec catalog the head composed — see the composition root in
/// the Windows head — so a format listed here that the catalog cannot decode
/// fails at upload and is reported, rather than being silently absent from the
/// Open dialog.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=3F4645
// Broiler-Falsified-If: a BMP header whose height field is 0x80000000 makes MeasureDisplaySize throw OverflowException instead of returning the default extent
// Broiler-Human:        PENDING
internal static class PlateImageFormats
{
    /// <summary>The size a picture of unknown dimensions is reported at.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=1BAC27
    // Broiler-Human:        PENDING
    private const double DefaultExtent = 200;

    /// <summary>
    /// The extensions the Open dialog offers as graphics, in the order its
    /// per-format filters list them. Each entry is the extension and the name
    /// the dialog shows.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=3A3FF7
    // Broiler-Falsified-If: an extension in Formats is absent from AllExtensions, so the Open dialog offers a graphic the open path does not treat as one
    // Broiler-Human:        PENDING
    public static readonly (string Extension, string DisplayName)[] Formats =
    [
        (".png", "PNG"),
        (".jpg", "JPEG"),
        (".gif", "GIF"),
        (".bmp", "Bitmap"),
        (".tif", "TIFF"),
        (".webp", "WebP"),
    ];

    /// <summary>
    /// Every extension recognized as a graphic, including the spellings
    /// <see cref="Formats"/> folds together (.jpeg onto .jpg, and so on).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=3C452A
    // Broiler-Falsified-If: an extension that FilterPattern offers, such as .tiff or .dib, is missing from AllExtensions
    // Broiler-Human:        PENDING
    private static readonly string[] AllExtensions =
    [
        ".png", ".jpg", ".jpeg", ".jpe", ".gif", ".bmp", ".dib", ".tif", ".tiff", ".webp",
    ];

    /// <summary>The dialog patterns for one entry of <see cref="Formats"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=EA4277
    // Broiler-Falsified-If: the .jpg entry's pattern omits *.jpeg or *.jpe although AllExtensions recognises both
    // Broiler-Human:        PENDING
    public static string FilterPattern(string extension) => extension switch
    {
        ".jpg" => "*.jpg;*.jpeg;*.jpe",
        ".bmp" => "*.bmp;*.dib",
        ".tif" => "*.tif;*.tiff",
        _ => "*" + extension,
    };

    /// <summary>The pattern matching every graphic the viewer offers.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=00D616
    // Broiler-Falsified-If: the pattern omits an extension listed in AllExtensions
    // Broiler-Human:        PENDING
    public static string AllFilterPattern =>
        string.Join(";", Array.ConvertAll(AllExtensions, static extension => "*" + extension));

    /// <summary>
    /// Whether <paramref name="path"/> is named like a graphic. This is the
    /// fallback for a file whose bytes did not identify themselves; the
    /// signature check in <see cref="ContentTypeForSignature"/> is what decides
    /// first, because a name can lie and a magic number rarely does.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=FCED51
    // Broiler-Falsified-If: a path whose image extension is not its last one, such as report.png.rtf, is reported as an image
    // Broiler-Human:        PENDING
    public static bool HasImageExtension(string path)
    {
        string extension = Path.GetExtension(path);
        foreach (string known in AllExtensions)
        {
            if (string.Equals(extension, known, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The media type for an image file, from its extension and confirmed
    /// against the bytes. Returns null when this is not an image format the
    /// document codecs carry.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=B486D0
    // Broiler-Falsified-If: a file whose bytes carry the PNG signature but whose name ends in .gif is given image/gif instead of image/png
    // Broiler-Human:        PENDING
    public static string? ContentTypeFor(string path, ReadOnlySpan<byte> data)
    {
        string? signature = ContentTypeForSignature(data);
        if (signature is not null)
            return signature;

        // A file whose bytes are unrecognized but whose extension is a known
        // image type is still worth opening: the renderer, not this table,
        // decides what decodes.
        return Path.GetExtension(path).TrimStart('.').ToLowerInvariant() switch
        {
            "png" => "image/png",
            "jpg" or "jpeg" or "jpe" => "image/jpeg",
            "gif" => "image/gif",
            "bmp" or "dib" => "image/bmp",
            "tif" or "tiff" => "image/tiff",
            "webp" => "image/webp",
            _ => null,
        };
    }

    /// <summary>
    /// The size to show an image at: its own pixel size, scaled down to
    /// <paramref name="maxWidth"/> so a photo straight from a camera does not
    /// arrive several thousand units wide.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=A76F6A
    // Broiler-Falsified-If: a header stating a width or height of zero or below is returned as the display size instead of the default extent
    // Broiler-Human:        PENDING
    public static BSize MeasureDisplaySize(ReadOnlySpan<byte> data, double maxWidth)
    {
        if (!TryReadPixelSize(data, out int width, out int height) || width <= 0 || height <= 0)
            return new BSize(DefaultExtent, DefaultExtent);

        if (maxWidth <= 0 || width <= maxWidth)
            return new BSize(width, height);

        double scale = maxWidth / width;
        return new BSize(maxWidth, Math.Max(1, Math.Round(height * scale)));
    }

    /// <summary>Reads the pixel dimensions from an encoded image header.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=305C07
    // Broiler-Falsified-If: an input truncated right after a recognised signature, such as the four bytes GIF8, throws instead of returning false
    // Broiler-Human:        PENDING
    public static bool TryReadPixelSize(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (StartsWith(data, [0x89, (byte)'P', (byte)'N', (byte)'G']))
            return TryReadPngSize(data, out width, out height);
        if (StartsWith(data, [0xFF, 0xD8]))
            return TryReadJpegSize(data, out width, out height);
        if (StartsWith(data, "GIF8"u8))
            return TryReadGifSize(data, out width, out height);
        if (StartsWith(data, "BM"u8))
            return TryReadBmpSize(data, out width, out height);

        return false;
    }

    /// <summary>
    /// The media type the leading bytes identify, or null when they identify no
    /// image format. Recognizing a file costs only its header, so the viewer can
    /// decide what it is holding before committing to read the whole of it.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=A9D172
    // Broiler-Falsified-If: a RIFF file whose bytes 8 to 11 are not WEBP, such as a WAVE file, is identified as image/webp
    // Broiler-Human:        PENDING
    public static string? ContentTypeForSignature(ReadOnlySpan<byte> data)
    {
        if (StartsWith(data, [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]))
            return "image/png";
        if (StartsWith(data, [0xFF, 0xD8, 0xFF]))
            return "image/jpeg";
        if (StartsWith(data, "GIF87a"u8) || StartsWith(data, "GIF89a"u8))
            return "image/gif";
        if (StartsWith(data, "BM"u8))
            return "image/bmp";
        if (StartsWith(data, [0x49, 0x49, 0x2A, 0x00]) || StartsWith(data, [0x4D, 0x4D, 0x00, 0x2A]))
            return "image/tiff";
        if (data.Length >= 12 && StartsWith(data, "RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";

        return null;
    }

    /// <summary>PNG: the IHDR chunk is first and holds width then height, big-endian.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=8C5CD8
    // Broiler-Falsified-If: a PNG shorter than 24 bytes, or whose bytes 12 to 15 are not IHDR, reports a size instead of returning false
    // Broiler-Human:        PENDING
    private static bool TryReadPngSize(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length < 24 || !data[12..16].SequenceEqual("IHDR"u8))
            return false;

        width = BinaryPrimitives.ReadInt32BigEndian(data[16..20]);
        height = BinaryPrimitives.ReadInt32BigEndian(data[20..24]);
        return true;
    }

    /// <summary>JPEG: walk the marker segments to the start-of-frame that states the size.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=159C5D
    // Broiler-Falsified-If: a segment length that carries the offset past the end of the data makes the walk index outside the span instead of returning false
    // Broiler-Human:        PENDING
    private static bool TryReadJpegSize(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = 0;
        height = 0;
        int offset = 2;
        while (offset + 4 <= data.Length)
        {
            if (data[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            byte marker = data[offset + 1];
            offset += 2;
            if (marker is 0xD8 or 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                continue;
            if (offset + 2 > data.Length)
                return false;

            int length = BinaryPrimitives.ReadUInt16BigEndian(data[offset..(offset + 2)]);
            if (length < 2)
                return false;

            // SOF0..SOF15, excluding the DHT/JPG/DAC markers interleaved in that range.
            bool isStartOfFrame = marker >= 0xC0 && marker <= 0xCF && marker is not (0xC4 or 0xC8 or 0xCC);
            if (isStartOfFrame)
            {
                if (offset + 7 > data.Length)
                    return false;

                height = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 3)..(offset + 5)]);
                width = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 5)..(offset + 7)]);
                return width > 0 && height > 0;
            }

            offset += length;
        }

        return false;
    }

    /// <summary>GIF: the logical screen descriptor follows the six-byte signature, little-endian.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=C642D2
    // Broiler-Falsified-If: a GIF shorter than 10 bytes reports a size instead of returning false
    // Broiler-Human:        PENDING
    private static bool TryReadGifSize(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length < 10)
            return false;

        width = BinaryPrimitives.ReadUInt16LittleEndian(data[6..8]);
        height = BinaryPrimitives.ReadUInt16LittleEndian(data[8..10]);
        return true;
    }

    /// <summary>BMP: the DIB header states the size; a negative height means top-down rows.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=0306F2
    // Broiler-Falsified-If: a BMP whose height field is 0x80000000 (int.MinValue) throws OverflowException from Math.Abs instead of returning false
    // Broiler-Human:        PENDING
    private static bool TryReadBmpSize(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length < 26)
            return false;

        width = BinaryPrimitives.ReadInt32LittleEndian(data[18..22]);
        height = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(data[22..26]));
        return true;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=8BD7BE
    // Broiler-Falsified-If: data shorter than the prefix is sliced and throws instead of returning false
    // Broiler-Human:        PENDING
    private static bool StartsWith(ReadOnlySpan<byte> data, ReadOnlySpan<byte> prefix) =>
        data.Length >= prefix.Length && data[..prefix.Length].SequenceEqual(prefix);
}
