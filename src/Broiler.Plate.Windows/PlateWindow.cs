using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Broiler.App;
using Broiler.Graphics;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Rendering;
using Broiler.Graphics.RenderList;
using Broiler.Graphics.Windowing;
using Broiler.Graphics.Windows;
using Broiler.Input.Keyboard;
using Broiler.UI;
using Broiler.UI.Standard;

namespace Broiler.Plate;

/// <summary>Win32/Direct2D host for the platform-neutral viewer.</summary>
[SupportedOSPlatform("windows7.0")]
internal sealed class PlateWindow : Direct2DWindow
{
    private readonly PlateWindowsUiHost _host;
    private readonly PlateApp _app;

    // Created in OnCreated: the Win32 clipboard is addressed by window handle,
    // and there is no handle until the window exists.
    private WindowsClipboard? _clipboard;

#pragma warning disable CS0618
    private readonly StandardLegacyGraphicsInputAdapter _legacyInput = new("broiler-plate");
#pragma warning restore CS0618

    public PlateWindow(PlateFileFormats formats)
        : base(new BWindowOptions
        {
            Title = "Broiler Plate",
            ClientWidth = 1120,
            ClientHeight = 780,
            ClearColor = PlatePalette.Canvas,
            RenderOptions = new BRenderOptions(Antialias: true, VSync: true, SubpixelText: true),
        })
    {
        // The Windows host, not the plain one: it offers IUiWindowHost, so the Open dialog
        // breaks out into its own OS window instead of rendering inside this one.
        _host = new PlateWindowsUiHost(
            () => ClientSize,
            () => DpiScale,
            Invalidate,
            static _ => { },
            () => this,
            ReadClipboardText,
            WriteClipboardText,
            getRenderer: () => Renderer);
        _app = new PlateApp(_host, CloseNativeWindow, formats);
    }

    protected override void OnCreated() => _clipboard = new WindowsClipboard(NativeHandle);

    /// <summary>
    /// Null rather than empty when there is no clipboard to read: the host
    /// treats that as "this machine offers none", which is what the window
    /// reports before it exists and if the OS refuses the clipboard.
    /// </summary>
    private string? ReadClipboardText() =>
        _clipboard is not null && _clipboard.TryGetText(out string text) ? text : null;

    private void WriteClipboardText(string text) => _clipboard?.SetText(text);

    protected override BRenderList? BuildRenderList(BSize clientSize) => _app.RenderFrame();

    protected override void OnResized(BSize clientSize, double dpiScale) => _app.Invalidate();

    protected override void OnPointerDown(BPointerEventArgs e) =>
        Dispatch(_legacyInput.FromPointerButton(e));

    protected override void OnPointerMove(BPointerEventArgs e) =>
        Dispatch(_legacyInput.FromPointerMove(e));

    protected override void OnPointerUp(BPointerEventArgs e) =>
        Dispatch(_legacyInput.FromPointerButton(e));

    protected override void OnMouseWheel(BMouseWheelEventArgs e) =>
        Dispatch(_legacyInput.FromMouseWheel(e));

    protected override void OnKeyDown(BKeyEventArgs e) =>
        Dispatch(_legacyInput.FromKey(e, KeyboardKeyTransition.Down));

    protected override void OnKeyUp(BKeyEventArgs e) =>
        Dispatch(_legacyInput.FromKey(e, KeyboardKeyTransition.Up));

    // Forwarded even though nothing here is editable: the Open dialog's name box is.
    protected override void OnTextInput(BTextInputEventArgs e) =>
        Dispatch(_legacyInput.FromText(e));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // The app first: disposing its session closes any dialog that is still open, which
            // tears down the host window it broke out into, and releases the image the viewer
            // had uploaded. The host then only has to sweep up whatever survived that.
            _app.Dispose();
            _host.Dispose();
        }

        base.Dispose(disposing);
    }

    private void CloseNativeWindow()
    {
        if (!PostToUiThread(CloseNativeWindowNow))
            CloseNativeWindowNow();
    }

    private void CloseNativeWindowNow()
    {
        if (NativeHandle != IntPtr.Zero)
            _ = DestroyWindow(NativeHandle);
    }

    private void Dispatch(UiInputEvent input) => _app.Dispatch(input);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hwnd);
}
