// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   18
// Annotated:        18/18
// Exempt:           0
// Human-reviewed:   0/18
// IP risk:          None
// Security risk:    None
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       18
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Graphics;
using Broiler.Graphics.Color;

namespace Broiler.Plate;

/// <summary>
/// The viewer's colours. A viewer shows other people's content, so the shell is
/// deliberately quiet: one accent, everything else a neutral, and the surface
/// the content sits on is plain white so a document's own colours are the only
/// strong ones on screen.
/// </summary>
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=98943D
// Broiler-Human:        PENDING
internal static class PlatePalette
{
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=D233CE
    // Broiler-Human:        PENDING
    public static readonly BColor Canvas = BColor.FromArgb(0xFF, 0xF4, 0xF6, 0xF8);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=A3FA8E
    // Broiler-Human:        PENDING
    public static readonly BColor Page = BColor.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=C0969D
    // Broiler-Human:        PENDING
    public static readonly BColor Title = BColor.FromArgb(0xFF, 0x1E, 0x2A, 0x36);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=F3E810
    // Broiler-Human:        PENDING
    public static readonly BColor Muted = BColor.FromArgb(0xFF, 0x5F, 0x6E, 0x7D);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=6BCAC7
    // Broiler-Human:        PENDING
    public static readonly BColor Accent = BColor.FromArgb(0xFF, 0x2A, 0x73, 0xC5);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=CAEBF1
    // Broiler-Human:        PENDING
    public static readonly BColor WindowBorder = BColor.FromArgb(0xFF, 0xC8, 0xD2, 0xDC);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=06C01B
    // Broiler-Human:        PENDING
    public static readonly BColor ViewBorder = BColor.FromArgb(0xFF, 0xB8, 0xC4, 0xD0);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=CA8078
    // Broiler-Human:        PENDING
    public static readonly BColor MenuSurface = BColor.FromArgb(0xFF, 0xFB, 0xFC, 0xFE);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=B6B0D6
    // Broiler-Human:        PENDING
    public static readonly BColor MenuPopup = BColor.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=059326
    // Broiler-Human:        PENDING
    public static readonly BColor MenuSelected = BColor.FromArgb(0xFF, 0xDF, 0xEC, 0xFA);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=8D48A8
    // Broiler-Human:        PENDING
    public static readonly BColor MenuRule = BColor.FromArgb(0xFF, 0xD8, 0xE0, 0xE8);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=804848
    // Broiler-Human:        PENDING
    public static readonly BColor ToolbarSurface = BColor.FromArgb(0xFF, 0xF0, 0xF4, 0xF8);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=B43D39
    // Broiler-Human:        PENDING
    public static readonly BColor ToolbarButton = BColor.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=E34EE9
    // Broiler-Human:        PENDING
    public static readonly BColor ToolbarButtonHover = BColor.FromArgb(0xFF, 0xF2, 0xF7, 0xFF);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=A01836
    // Broiler-Human:        PENDING
    public static readonly BColor ToolbarButtonPressed = BColor.FromArgb(0xFF, 0xD8, 0xE8, 0xFC);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=36CABA
    // Broiler-Human:        PENDING
    public static readonly BColor ToolbarButtonBorder = BColor.FromArgb(0xFF, 0xC4, 0xD2, 0xE0);

    /// <summary>
    /// What a picture is matted against. Deliberately darker than
    /// <see cref="Page"/>: an image with transparency or a white border has to be
    /// distinguishable from the surface it is drawn on.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=17527B
    // Broiler-Human:        PENDING
    public static readonly BColor ImageMat = BColor.FromArgb(0xFF, 0x2B, 0x33, 0x3B);
}
