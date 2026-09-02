using Broiler.Graphics;

namespace Broiler.Plate;

/// <summary>
/// The viewer's colours. A viewer shows other people's content, so the shell is
/// deliberately quiet: one accent, everything else a neutral, and the surface
/// the content sits on is plain white so a document's own colours are the only
/// strong ones on screen.
/// </summary>
internal static class PlatePalette
{
    public static readonly BColor Canvas = BColor.FromArgb(0xFF, 0xF4, 0xF6, 0xF8);
    public static readonly BColor Page = BColor.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
    public static readonly BColor Title = BColor.FromArgb(0xFF, 0x1E, 0x2A, 0x36);
    public static readonly BColor Muted = BColor.FromArgb(0xFF, 0x5F, 0x6E, 0x7D);
    public static readonly BColor Accent = BColor.FromArgb(0xFF, 0x2A, 0x73, 0xC5);
    public static readonly BColor WindowBorder = BColor.FromArgb(0xFF, 0xC8, 0xD2, 0xDC);
    public static readonly BColor ViewBorder = BColor.FromArgb(0xFF, 0xB8, 0xC4, 0xD0);
    public static readonly BColor MenuSurface = BColor.FromArgb(0xFF, 0xFB, 0xFC, 0xFE);
    public static readonly BColor MenuPopup = BColor.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
    public static readonly BColor MenuSelected = BColor.FromArgb(0xFF, 0xDF, 0xEC, 0xFA);
    public static readonly BColor MenuRule = BColor.FromArgb(0xFF, 0xD8, 0xE0, 0xE8);
    public static readonly BColor ToolbarSurface = BColor.FromArgb(0xFF, 0xF0, 0xF4, 0xF8);
    public static readonly BColor ToolbarButton = BColor.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
    public static readonly BColor ToolbarButtonHover = BColor.FromArgb(0xFF, 0xF2, 0xF7, 0xFF);
    public static readonly BColor ToolbarButtonPressed = BColor.FromArgb(0xFF, 0xD8, 0xE8, 0xFC);
    public static readonly BColor ToolbarButtonBorder = BColor.FromArgb(0xFF, 0xC4, 0xD2, 0xE0);

    /// <summary>
    /// What a picture is matted against. Deliberately darker than
    /// <see cref="Page"/>: an image with transparency or a white border has to be
    /// distinguishable from the surface it is drawn on.
    /// </summary>
    public static readonly BColor ImageMat = BColor.FromArgb(0xFF, 0x2B, 0x33, 0x3B);
}
