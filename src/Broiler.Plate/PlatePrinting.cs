// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           6
// Human-reviewed:   0/6
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  3/10 max
// Unverified:       6
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.IO;
using System.Threading;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Imaging;
using Broiler.Graphics.Paging;
using Broiler.Graphics.Resources;

namespace Broiler.Plate;

/// <summary>What a file's own rules allow a viewer to do with it on paper.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=880573
// Broiler-Human:        PENDING
internal enum PlatePrintPermission
{
    /// <summary>The file may be printed.</summary>
    Allowed = 0,

    /// <summary>
    /// The file may be printed, but not faithfully: a PDF that grants Print without High-quality
    /// print (ISO 32000-1, table 22, bit 12) is printed at a low resolution.
    /// </summary>
    LowResolution = 1,

    /// <summary>The file says it must not be printed.</summary>
    Refused = 2,
}

/// <summary>
/// A picture as something a print dialog can lay out: one page, the picture as large as the
/// page's content box allows without being cropped or stretched, centred.
/// </summary>
/// <remarks>
/// The file is decoded again for printing rather than taken from the view, because the view's
/// image is a renderer handle and a printed page has to carry its own pixels.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=463E8C
// Broiler-Human:        PENDING
internal sealed class PlatePicturePrintDocument : IBPagedDocument
{
    private readonly string _path;
    private readonly BSize _pixelSize;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=7609C6
    // Broiler-Human:        PENDING
    public PlatePicturePrintDocument(string path, BSize pixelSize)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        _pixelSize = pixelSize;
        Title = Path.GetFileName(path);
    }

    public string Title { get; }

    /// <summary>A4 with half-inch margins, turned to the picture's own orientation.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=CF03D4
    // Broiler-Human:        PENDING
    public BPageSetup PreferredSetup => new(
        BPaperSize.A4,
        _pixelSize.Width > _pixelSize.Height ? BPageOrientation.Landscape : BPageOrientation.Portrait,
        BPageMargins.Uniform(36));

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=68CC25
    // Broiler-Human:        PENDING
    public IBPageSource Paginate(BPageSetup setup, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(setup);
        cancellationToken.ThrowIfCancellationRequested();

        using BBitmap bitmap = BBitmap.Decode(File.ReadAllBytes(_path));
        var builder = new BPageBuilder(setup.PageSize);
        BImageHandle image = builder.AddImage(bitmap.ToPixelBuffer(copy: false));
        builder.Content.DrawImage(image, new BRect(0, 0, bitmap.Width, bitmap.Height), Fit(new BSize(bitmap.Width, bitmap.Height), setup.ContentBox));
        return new BPageCollection([builder.Build()]);
    }

    /// <summary>The largest rectangle of <paramref name="picture"/>'s proportions inside <paramref name="box"/>, centred.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=850D4A
    // Broiler-Human:        PENDING
    internal static BRect Fit(BSize picture, BRect box)
    {
        if (picture.IsEmpty || box.IsEmpty)
            return BRect.Empty;

        double scale = Math.Min(box.Width / picture.Width, box.Height / picture.Height);
        double width = picture.Width * scale;
        double height = picture.Height * scale;
        return new BRect(box.Left + ((box.Width - width) / 2), box.Top + ((box.Height - height) / 2), width, height);
    }
}
