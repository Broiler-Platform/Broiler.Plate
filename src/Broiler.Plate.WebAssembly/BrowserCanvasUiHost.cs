// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Broiler.Graphics;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Imaging;
using Broiler.Graphics.RenderList;
using Broiler.Graphics.Resources;
using Broiler.Graphics.WebAssembly;
using Broiler.UI;

namespace Broiler.Plate.WebAssembly;

[SupportedOSPlatform("browser")]
internal sealed class BrowserUiDispatcher : IUiDispatcher
{
    private readonly Queue<Action> _queue = [];
    private readonly Action _schedule;

    internal BrowserUiDispatcher(Action schedule) => _schedule = schedule;

    public bool CheckAccess() => true;

    public void Post(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _queue.Enqueue(callback);
        _schedule();
    }

    internal int Drain()
    {
        int count = 0;
        while (_queue.TryDequeue(out Action? callback))
        {
            callback();
            count++;
        }
        return count;
    }
}

[SupportedOSPlatform("browser")]
internal sealed class BrowserCanvasUiHost : PlateUiHost, IUiCursorHost
{
    private sealed class State
    {
        public BSize ViewportSize = new(1280, 800);
        public double Scale = 1.0;
        public bool ClipboardEventActive;
        public string? ClipboardReadText;
        public string? ClipboardWriteText;
        public UiTextCaretInfo? Caret;
    }

    private readonly BrowserCanvasRenderer _renderer;
    private readonly State _state;
    private bool _disposed;

    internal BrowserCanvasUiHost(BrowserCanvasRenderer renderer)
        : this(renderer, new State())
    {
    }

    private BrowserCanvasUiHost(BrowserCanvasRenderer renderer, State state)
        : base(
            getViewportSize: () => state.ViewportSize,
            getScale: () => state.Scale,
            invalidate: BrowserInterop.ScheduleFrame,
            present: renderList =>
            {
                int backingWidth = Math.Max(1, (int)Math.Ceiling(state.ViewportSize.Width * state.Scale));
                int backingHeight = Math.Max(1, (int)Math.Ceiling(state.ViewportSize.Height * state.Scale));
                renderer.PresentFrame(
                    renderList,
                    backingWidth,
                    backingHeight,
                    state.Scale,
                    state.ViewportSize.Width,
                    state.ViewportSize.Height,
                    PlatePalette.Canvas);
            },
            getClipboardText: () => state.ClipboardEventActive ? state.ClipboardReadText : null,
            setClipboardText: text =>
            {
                if (state.ClipboardEventActive)
                    state.ClipboardWriteText = text;
            },
            caretChanged: caret => state.Caret = caret)
    {
        _renderer = renderer;
        _state = state;
    }

    internal UiTextCaretInfo? CurrentCaret => _state.Caret;

    internal bool IsClipboardEventActive => _state.ClipboardEventActive;

    internal long FrameIndex => _renderer.FrameIndex;

    public override BImageHandle CreateImage(ReadOnlySpan<byte> encodedImage)
    {
        if (_disposed || encodedImage.IsEmpty)
            return BImageHandle.Invalid;

        try
        {
            using BBitmap bitmap = BBitmap.Decode(encodedImage);
            return _renderer.CreateImage(bitmap.ToPixelBuffer());
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            return BImageHandle.Invalid;
        }
    }

    public override BImageHandle CreateImage(BPixelBuffer pixels)
    {
        if (_disposed || pixels is null)
            return BImageHandle.Invalid;

        try
        {
            return _renderer.CreateImage(pixels);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            return BImageHandle.Invalid;
        }
    }

    public override void ReleaseImage(BImageHandle image)
    {
        if (!_disposed && image.IsValid)
            _renderer.ReleaseImage(image);
    }

    public void SetCursor(UiCursorShape shape) => BrowserInterop.SetCursor(shape.ToString());

    internal void BeginClipboardEvent(string? readText)
    {
        _state.ClipboardEventActive = true;
        _state.ClipboardReadText = readText;
        _state.ClipboardWriteText = null;
    }

    internal string EndClipboardEvent()
    {
        string result = _state.ClipboardWriteText ?? string.Empty;
        _state.ClipboardEventActive = false;
        _state.ClipboardReadText = null;
        _state.ClipboardWriteText = null;
        return result;
    }

    internal bool Resize(double width, double height, double dpr)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!double.IsFinite(width) || !double.IsFinite(height) || !double.IsFinite(dpr) ||
            width <= 0 || height <= 0 || dpr <= 0)
            return false;

        if (_state.ViewportSize.Width.Equals(width) && _state.ViewportSize.Height.Equals(height) && _state.Scale.Equals(dpr))
            return true;

        _state.ViewportSize = new BSize(width, height);
        _state.Scale = dpr;
        BrowserInterop.ScheduleFrame();
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;
        _state.Caret = null;
        _state.ClipboardEventActive = false;
        _state.ClipboardReadText = null;
        _state.ClipboardWriteText = null;
        base.Dispose(disposing);
    }
}
