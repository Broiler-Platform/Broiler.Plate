// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Imaging;
using Broiler.Graphics.Resources;
using Broiler.Graphics.Text;
using Broiler.Input.Keyboard;
using Broiler.Input.Mouse;
using Broiler.UI;
using Broiler.UI.Label;
using Broiler.UI.Label.Standard;

namespace Broiler.Plate;

internal sealed record PlateGalleryEntry(string Path, bool IsDirectory)
{
    public string Name => System.IO.Path.GetFileName(Path);
}

/// <summary>A scrolling grid that only decodes visible thumbnails, off the UI thread.</summary>
internal sealed class PlateGallery : UiElement
{
    private const double CellWidth = 180, CellHeight = 164, Gap = 12;
    private readonly PlateUiHost _host;
    private readonly Action<PlateGalleryEntry> _open;
    private readonly Dictionary<int, BImageHandle> _thumbnails = [];
    private readonly PlateThumbnailCache _thumbnailCache = new(CreateThumbnail);
    private readonly StandardLabel _label = new() { Trimming = UiTextTrimming.CharacterEllipsis, Font = new BFontStyle("Segoe UI", 13) };
    private CancellationTokenSource _cancellation = new();
    private Task<(int Index, byte[]? Bytes)[]>? _loading;
    private int _columns = 1, _selected = -1, _pressed = -1;
    private double _scroll;
    internal IReadOnlyList<PlateGalleryEntry> Entries { get; private set; } = Array.Empty<PlateGalleryEntry>();
    internal double ScrollOffset => _scroll;
    internal int SelectedIndex => _selected;

    internal PlateGallery(PlateUiHost host, Action<PlateGalleryEntry> open)
    {
        _host = host;
        _open = open;
        Focusable = true;
    }

    internal static PlateGalleryEntry[] ReadFolder(string path) => new DirectoryInfo(path)
        .EnumerateFileSystemInfos()
        .Select(entry => new PlateGalleryEntry(entry.FullName, (entry.Attributes & FileAttributes.Directory) != 0))
        .OrderByDescending(entry => entry.IsDirectory)
        .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToArray();

    internal void SetEntries(IReadOnlyList<PlateGalleryEntry> entries)
    {
        ClearThumbnails();
        Entries = entries;
        _selected = entries.Count == 0 ? -1 : 0;
        _pressed = -1;
        _scroll = 0;
        Invalidate(UiInvalidationKind.Measure | UiInvalidationKind.Arrange | UiInvalidationKind.Render);
    }

    internal void ClearThumbnails()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        _cancellation = new CancellationTokenSource();
        _loading = null;
        foreach (BImageHandle image in _thumbnails.Values)
            _host.ReleaseImage(image);
        _thumbnails.Clear();
    }

    protected override BSize MeasureCore(BSize availableSize) => new(
        double.IsFinite(availableSize.Width) ? availableSize.Width : 760,
        double.IsFinite(availableSize.Height) ? availableSize.Height : 520);

    protected override void ArrangeCore(BRect rect)
    {
        _columns = Math.Max(1, (int)((rect.Width - 12) / CellWidth));
        _scroll = Math.Clamp(_scroll, 0, MaxScroll);
    }

    private double MaxScroll => Math.Max(0, Math.Ceiling((double)Entries.Count / _columns) * CellHeight - Bounds.Height);
    private double Width => Math.Max(0, (Bounds.Width - 12) / _columns);
    private BRect Cell(int index) => new(Bounds.Left + index % _columns * Width + 6,
        Bounds.Top + index / _columns * CellHeight - _scroll + 6, Math.Max(0, Width - Gap), CellHeight - Gap);

    protected override void RenderCore(UiRenderContext context)
    {
        if (_loading is { IsCompletedSuccessfully: true })
        {
            foreach (var result in _loading.Result)
                _thumbnails[result.Index] = result.Bytes is null ? BImageHandle.Invalid : _host.CreateImage(result.Bytes);
            _loading = null;
        }

        int first = Math.Max(0, (int)(_scroll / CellHeight) * _columns);
        int last = Math.Min(Entries.Count, (int)Math.Ceiling((_scroll + Bounds.Height) / CellHeight) * _columns);
        // Evict only between frames, before any commands refer to the old handles.
        foreach (int index in _thumbnails.Keys.Where(i => i < first || i >= last).ToArray())
        {
            _host.ReleaseImage(_thumbnails[index]);
            _thumbnails.Remove(index);
        }
        if (_loading is null && last > first)
        {
            var pending = Enumerable.Range(first, last - first).Where(i => !_thumbnails.ContainsKey(i) && !Entries[i].IsDirectory)
                .Select(i => (Index: i, Entries[i].Path)).ToArray();
            if (pending.Length > 0)
            {
                CancellationToken token = _cancellation.Token;
                _loading = Task.Run(() => pending.Select(item => (item.Index, Bytes: _thumbnailCache.GetOrCreate(item.Path, token))).ToArray());
                _ = _loading.ContinueWith(_ => { if (!token.IsCancellationRequested) _host.RequestInvalidate(); }, TaskScheduler.Default);
            }
        }

        context.RenderList.FillRect(Bounds, PlatePalette.ImageMat);
        context.RenderList.PushClip(Bounds);
        if (Entries.Count == 0)
            DrawLabel(context, "This folder is empty.", new BRect(Bounds.Left + 20, Bounds.Top + 20, Math.Max(0, Bounds.Width - 40), 30));
        for (int i = first; i < last; i++)
        {
            BRect cell = Cell(i);
            context.RenderList.FillRect(cell, i == _selected ? PlatePalette.MenuSelected : PlatePalette.Page);
            BRect preview = new(cell.Left + 10, cell.Top + 10, Math.Max(0, cell.Width - 20), 104);
            if (_thumbnails.TryGetValue(i, out BImageHandle image) && image.IsValid)
                context.RenderList.DrawImage(image, new BRect(0, 0, image.PixelSize.Width, image.PixelSize.Height), PlatePicturePrintDocument.Fit(image.PixelSize, preview));
            else
            {
                context.RenderList.FillRect(preview, PlatePalette.ToolbarSurface);
                string kind = Entries[i].IsDirectory ? "Folder" : Path.GetExtension(Entries[i].Name).TrimStart('.').ToUpperInvariant();
                DrawLabel(context, kind.Length == 0 ? "File" : kind, new BRect(preview.Left + 10, preview.Top + 42, Math.Max(0, preview.Width - 20), 24));
            }
            DrawLabel(context, Entries[i].Name, new BRect(cell.Left + 10, cell.Bottom - 29, Math.Max(0, cell.Width - 20), 23));
        }
        if (MaxScroll > 0)
        {
            double height = Math.Max(20, Bounds.Height * Bounds.Height / (MaxScroll + Bounds.Height));
            context.RenderList.FillRect(new BRect(Bounds.Right - 6, Bounds.Top + _scroll / MaxScroll * (Bounds.Height - height), 4, height), PlatePalette.Muted);
        }
        context.RenderList.PopClip();
    }

    private void DrawLabel(UiRenderContext context, string text, BRect bounds)
    {
        _label.Text = text;
        _label.Foreground = PlatePalette.Title;
        _label.Measure(bounds.Size);
        _label.Arrange(bounds);
        _label.Render(context);
    }

    internal static byte[]? CreateThumbnail(string path, CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return null;
        try
        {
            using FileStream stream = File.OpenRead(path);
            if (stream.Length == 0 || stream.Length > 32 * 1024 * 1024)
                return null;
            Span<byte> signature = stackalloc byte[32];
            int read = stream.Read(signature);
            if (PlateImageFormats.ContentTypeForSignature(signature[..read]) is null && !PlateImageFormats.HasImageExtension(path))
                return null;
            stream.Position = 0;
            using BBitmap source = BBitmap.Decode(stream);
            if (cancellation.IsCancellationRequested)
                return null;
            double scale = Math.Min(1, Math.Min(320.0 / source.Width, 208.0 / source.Height));
            using BBitmap thumbnail = source.ResizeNearest(Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale)));
            return thumbnail.Encode();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A missing, unsupported, or damaged file remains visible as a named tile.
            return null;
        }
    }

    protected override bool OnInput(UiInputEvent input)
    {
        if (input.Kind == UiInputEventKind.PointerWheel)
        {
            _scroll = Math.Clamp(_scroll - input.WheelDeltaNotches * 72, 0, MaxScroll);
            Invalidate(UiInvalidationKind.Render);
            return true;
        }
        if (input.Kind == UiInputEventKind.PointerButton && input.MouseButton == MouseButton.Left)
        {
            int column = Width > 0 ? (int)((input.Position.X - Bounds.Left) / Width) : -1;
            int row = (int)((input.Position.Y - Bounds.Top + _scroll) / CellHeight);
            int index = row * _columns + column;
            if (column < 0 || column >= _columns || index < 0 || index >= Entries.Count || !Cell(index).Contains(input.Position))
                index = -1;
            if (input.MouseButtonTransition == MouseButtonTransition.Down)
            {
                Focus();
                _pressed = index;
                _selected = index;
                Invalidate(UiInvalidationKind.Render);
            }
            else if (input.MouseButtonTransition == MouseButtonTransition.Up)
            {
                int pressed = _pressed;
                _pressed = -1;
                if (index >= 0 && pressed == index)
                    _open(Entries[index]);
            }
            return true;
        }
        if (input.Kind != UiInputEventKind.KeyboardKey || input.KeyTransition != KeyboardKeyTransition.Down || Entries.Count == 0)
            return false;
        string? key = input.NativeKeyCode switch
        {
            0x0D => "Enter", 0x21 => "PageUp", 0x22 => "PageDown", 0x23 => "End", 0x24 => "Home",
            0x25 => "Left", 0x26 => "Up", 0x27 => "Right", 0x28 => "Down", _ => input.KeyName,
        };
        int? next = key switch
        {
            "Left" => _selected - 1, "Right" => _selected + 1,
            "Up" => _selected - _columns, "Down" => _selected + _columns,
            "Home" => 0, "End" => Entries.Count - 1,
            "PageUp" => _selected - _columns * Math.Max(1, (int)(Bounds.Height / CellHeight)),
            "PageDown" => _selected + _columns * Math.Max(1, (int)(Bounds.Height / CellHeight)),
            _ => null,
        };
        if (key == "Enter" && _selected >= 0)
        {
            _open(Entries[_selected]);
            return true;
        }
        if (next is null)
            return false;
        _selected = Math.Clamp(next.Value, 0, Entries.Count - 1);
        double top = _selected / _columns * CellHeight;
        _scroll = Math.Clamp(Math.Min(top, Math.Max(_scroll, top + CellHeight - Bounds.Height)), 0, MaxScroll);
        Invalidate(UiInvalidationKind.Render);
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearThumbnails();
            _thumbnailCache.Dispose();
            _cancellation.Dispose();
            _label.Dispose();
        }
        base.Dispose(disposing);
    }
}
