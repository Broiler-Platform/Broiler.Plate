// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   13
// Annotated:        13/13
// Exempt:           14
// Human-reviewed:   0/13
// IP risk:          Low
// Security risk:    High
// Criteria:         9/2
// Resource impact:  8/10 max
// Unverified:       13
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.Graphics;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Rendering;
using Broiler.Graphics.RenderList;
using Broiler.Graphics.Resources;
using Broiler.UI;

namespace Broiler.Plate;

/// <summary>
/// The viewer's platform-neutral <see cref="IUiHost"/>. It is unsealed so a head that can open
/// real secondary windows — currently only the Windows one — can add the optional
/// <c>IUiWindowHost</c> capability on top without restating the rendering, clipboard, caret and
/// image policy recorded here.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=5D8FD8
// Broiler-Falsified-If: bytes from an opened file that make the renderer's decoder throw take down the frame instead of leaving the view to draw the picture's outline
// Broiler-Human:        PENDING
internal class PlateUiHost : IUiHost, IUiClipboardHost, IUiTextInputHost, IUiImageHost, IDisposable
{
    private readonly Func<BSize> _getViewportSize;
    private readonly Func<double> _getScale;
    private readonly Action _invalidate;
    private readonly Action<BRenderList> _present;
    private readonly Func<string?>? _getClipboardText;
    private readonly Action<string>? _setClipboardText;
    private readonly Action<UiTextCaretInfo?>? _caretChanged;
    private readonly Func<IBroilerRenderer?>? _getRenderer;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=ED3053
    // Broiler-Falsified-If: a null invalidate or present delegate is accepted at construction and throws later from RequestInvalidate or Present
    // Broiler-Human:        PENDING
    public PlateUiHost(
        Func<BSize> getViewportSize,
        Func<double> getScale,
        Action invalidate,
        Action<BRenderList> present,
        Func<string?>? getClipboardText = null,
        Action<string>? setClipboardText = null,
        Action<UiTextCaretInfo?>? caretChanged = null,
        Func<IBroilerRenderer?>? getRenderer = null)
    {
        _getViewportSize = getViewportSize ?? throw new ArgumentNullException(nameof(getViewportSize));
        _getScale = getScale ?? throw new ArgumentNullException(nameof(getScale));
        _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
        _present = present ?? throw new ArgumentNullException(nameof(present));
        _getClipboardText = getClipboardText;
        _setClipboardText = setClipboardText;
        _caretChanged = caretChanged;
        _getRenderer = getRenderer;
    }

    public BSize ViewportSize => _getViewportSize();

    public double Scale => _getScale();

    public bool IsInvalidated { get; private set; } = true;

    public BRenderList? LastRenderList { get; private set; }

    public UiTextCaretInfo? LastCaret { get; private set; }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=2; Fingerprint=516BFD
    // Broiler-Human:        PENDING
    public BRenderList CreateRenderList(int capacity = 0) => new(capacity);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=8DF0B0
    // Broiler-Falsified-If: a request leaves IsInvalidated false or does not reach the head's invalidate callback
    // Broiler-Human:        PENDING
    public void RequestInvalidate()
    {
        IsInvalidated = true;
        _invalidate();
    }

    public void Invalidate(UiInvalidation invalidation) => RequestInvalidate();

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=BAD786
    // Broiler-Falsified-If: IsInvalidated stays true after a Present, or LastRenderList is not the list just presented
    // Broiler-Human:        PENDING
    public void Present(BRenderList renderList)
    {
        _present(renderList);
        LastRenderList = renderList;
        IsInvalidated = false;
    }

    /// <summary>
    /// The platform clipboard, or none at all.
    ///
    /// A viewer only ever writes to it — copying a selection out of the document
    /// on display — but the file dialog's name box reads from it, so both halves
    /// are wired. There is deliberately no private string standing in when the
    /// shell wired no accessor: a fallback makes paste appear to work while
    /// interoperating with nothing else on the machine. With none, the commands
    /// report themselves unavailable, which is true.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=DB3A40
    // Broiler-Falsified-If: a clipboard accessor that returns null or an empty string makes TryGetText return true
    // Broiler-Human:        PENDING
    public bool TryGetText(out string text)
    {
        text = _getClipboardText?.Invoke() ?? string.Empty;
        return text.Length > 0;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=3; Fingerprint=C75AAF
    // Broiler-Falsified-If: a null text reaches the clipboard writer as null rather than as an empty string
    // Broiler-Human:        PENDING
    public void SetText(string text) => _setClipboardText?.Invoke(text ?? string.Empty);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=F19308
    // Broiler-Human:        PENDING
    public void PublishCaret(UiTextCaretInfo caret)
    {
        LastCaret = caret;
        _caretChanged?.Invoke(caret);
    }

    /// <summary>
    /// Uploads an image to the renderer backing this host — both the pictures a
    /// document embeds and the graphics file the viewer was pointed at. A host
    /// built without a renderer — the headless test host, and any surface that
    /// only captures render lists — reports failure, and the view then draws the
    /// picture's outline instead.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=7D1AD8
    // Broiler-Falsified-If: a decoder exception other than OutOfMemoryException, such as InvalidDataException for a truncated PNG, escapes CreateImage instead of yielding BImageHandle.Invalid
    // Broiler-Human:        PENDING
    public BImageHandle CreateImage(ReadOnlySpan<byte> encodedImage)
    {
        IBroilerRenderer? renderer = _getRenderer?.Invoke();
        if (renderer is null || encodedImage.IsEmpty)
            return BImageHandle.Invalid;

        try
        {
            return renderer.CreateImage(encodedImage);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A file the user opened can carry any bytes at all; a decoder that
            // rejects them must not take down the frame.
            return BImageHandle.Invalid;
        }
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=463A53
    // Broiler-Falsified-If: an invalid handle, such as BImageHandle.Invalid, is passed on to the renderer's ReleaseImage
    // Broiler-Human:        PENDING
    public void ReleaseImage(BImageHandle image)
    {
        if (!image.IsValid)
            return;

        _getRenderer?.Invoke()?.ReleaseImage(image);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=41443D
    // Broiler-Falsified-If: clearing the caret for an element that does not own the last caret discards another element's caret
    // Broiler-Human:        PENDING
    public void ClearCaret(UiElement owner)
    {
        if (LastCaret?.Owner == owner)
        {
            LastCaret = null;
            _caretChanged?.Invoke(null);
        }
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=D74ECE
    // Broiler-Human:        PENDING
    public void Dispose() => Dispose(true);

    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=F52983
    // Broiler-Human:        PENDING
    protected virtual void Dispose(bool disposing)
    {
    }
}
