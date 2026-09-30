// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   19
// Annotated:        19/19
// Exempt:           4
// Human-reviewed:   0/19
// IP risk:          Low
// Security risk:    Critical
// Criteria:         17/7
// Resource impact:  7/10 max
// Unverified:       19
//
// GENERATED - DO NOT EDIT MANUALLY

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
// Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=7; Fingerprint=1FD40B
// Broiler-Falsified-If: a paste requested before OnCreated has bound the clipboard reaches WindowsClipboard instead of answering null
// Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=2; Fingerprint=6BD93A
    // Broiler-Falsified-If: the viewer's host is built without the clipboard accessors, so Ctrl+V in the Open dialog finds nothing while the system clipboard holds text
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=3B311F
    // Broiler-Falsified-If: the clipboard is bound to a zero window handle, so SetClipboardData fails after EmptyClipboard has already cleared the system clipboard
    // Broiler-Human:        PENDING
    protected override void OnCreated() => _clipboard = new WindowsClipboard(NativeHandle);

    /// <summary>
    /// Null rather than empty when there is no clipboard to read: the host
    /// treats that as "this machine offers none", which is what the window
    /// reports before it exists and if the OS refuses the clipboard.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Critical; Resources=4; Fingerprint=ABF7C4
    // Broiler-Falsified-If: a paste requested before OnCreated has bound the clipboard reaches WindowsClipboard instead of answering null
    // Broiler-Human:        PENDING
    private string? ReadClipboardText() =>
        _clipboard is not null && _clipboard.TryGetText(out string text) ? text : null;

    // Broiler-AI:           Origin=AI; IP=None; Security=Critical; Resources=4; Fingerprint=53E442
    // Broiler-Falsified-If: a copy requested before OnCreated has bound the clipboard throws NullReferenceException instead of being dropped
    // Broiler-Human:        PENDING
    private void WriteClipboardText(string text) => _clipboard?.SetText(text);

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=322E78
    // Broiler-Human:        PENDING
    protected override BRenderList? BuildRenderList(BSize clientSize) => _app.RenderFrame();

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=C778FE
    // Broiler-Falsified-If: a resize is not followed by an invalidation, so the viewer keeps drawing the layout of the previous client size
    // Broiler-Human:        PENDING
    protected override void OnResized(BSize clientSize, double dpiScale) => _app.Invalidate();

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=591E77
    // Broiler-Falsified-If: a mouse button press in the main window reaches PlateApp.Dispatch as anything other than a pointer-button event, so the menu and toolbar never respond
    // Broiler-Human:        PENDING
    protected override void OnPointerDown(BPointerEventArgs e) =>
        Dispatch(_legacyInput.FromPointerButton(e));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=7448EF
    // Broiler-Falsified-If: pointer motion in the main window is delivered as a button event, so moving over the toolbar presses its buttons
    // Broiler-Human:        PENDING
    protected override void OnPointerMove(BPointerEventArgs e) =>
        Dispatch(_legacyInput.FromPointerMove(e));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=C0302C
    // Broiler-Falsified-If: a button release in the main window is delivered as a pointer move, so a pressed toolbar button never completes its click
    // Broiler-Human:        PENDING
    protected override void OnPointerUp(BPointerEventArgs e) =>
        Dispatch(_legacyInput.FromPointerButton(e));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=4D9946
    // Broiler-Falsified-If: a Ctrl+wheel notch reaches PlateApp.Dispatch without its notch count, so it neither scrolls the document nor steps the zoom
    // Broiler-Human:        PENDING
    protected override void OnMouseWheel(BMouseWheelEventArgs e) =>
        Dispatch(_legacyInput.FromMouseWheel(e));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=EBC72C
    // Broiler-Falsified-If: a key press is converted with KeyboardKeyTransition.Up, so Ctrl+O and the Ctrl+plus zoom shortcut never fire
    // Broiler-Human:        PENDING
    protected override void OnKeyDown(BKeyEventArgs e) =>
        Dispatch(_legacyInput.FromKey(e, KeyboardKeyTransition.Down));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=E0727F
    // Broiler-Falsified-If: a key release is converted with KeyboardKeyTransition.Down, so one Ctrl+plus keystroke steps the zoom twice
    // Broiler-Human:        PENDING
    protected override void OnKeyUp(BKeyEventArgs e) =>
        Dispatch(_legacyInput.FromKey(e, KeyboardKeyTransition.Up));

    // Forwarded even though nothing here is editable: the Open dialog's name box is.
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=499FE8
    // Broiler-Falsified-If: a character typed in the main window is dropped instead of reaching PlateApp.Dispatch as text input
    // Broiler-Human:        PENDING
    protected override void OnTextInput(BTextInputEventArgs e) =>
        Dispatch(_legacyInput.FromText(e));

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=2; Fingerprint=AE072C
    // Broiler-Falsified-If: the host is disposed before the app, so a dialog still open at shutdown is torn down by the host's sweep rather than by the session that presented it
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=CE27BE
    // Broiler-Falsified-If: a close requested while the window is open destroys it inside the command that asked, instead of after that input has been dispatched
    // Broiler-Human:        PENDING
    private void CloseNativeWindow()
    {
        if (!PostToUiThread(CloseNativeWindowNow))
            CloseNativeWindowNow();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=698F16
    // Broiler-Falsified-If: DestroyWindow is called with a zero or already-destroyed window handle
    // Broiler-Human:        PENDING
    private void CloseNativeWindowNow()
    {
        if (NativeHandle != IntPtr.Zero)
            _ = DestroyWindow(NativeHandle);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=6A9571
    // Broiler-Human:        PENDING
    private void Dispatch(UiInputEvent input) => _app.Dispatch(input);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=B255F0
    // Broiler-Falsified-If: the HWND is marshalled narrower than pointer size, so a 64-bit process destroys a truncated window handle
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hwnd);
}
