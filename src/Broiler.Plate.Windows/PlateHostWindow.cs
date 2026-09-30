// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   34
// Annotated:        34/34
// Exempt:           9
// Human-reviewed:   0/34
// IP risk:          Low
// Security risk:    Critical
// Criteria:         25/6
// Resource impact:  8/10 max
// Unverified:       34
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Broiler.Graphics;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Imaging;
using Broiler.Graphics.Rendering;
using Broiler.Graphics.RenderList;
using Broiler.Graphics.Resources;
using Broiler.Graphics.Windowing;
using Broiler.Graphics.Windows;
using Broiler.Input.Keyboard;
using Broiler.UI;
using Broiler.UI.Standard;

namespace Broiler.Plate;

/// <summary>
/// A second native window hosting a broken-out viewer dialog (Broiler.UI ADR 0025 and 0026). It is
/// a <see cref="Direct2DWindow"/> that does not own the thread's message loop — the main window's
/// loop services it, so closing a dialog does not quit the viewer — and it exposes the neutral
/// <see cref="IUiHostWindow"/> and <see cref="IUiWindowChromeHost"/> contracts so a
/// <see cref="UiSession"/> can render into it, take its input, and drive its title bar.
/// </summary>
/// <remarks>
/// Created with <see cref="BWindowChrome.Owner"/>, so Windows draws no caption and the dialog keeps
/// the single title bar it already draws for itself.
///
/// Everything a host has to answer beyond the window — render lists, the clipboard, the caret,
/// image upload — is delegated to a <see cref="PlateUiHost"/> built over this window, so a dialog
/// behaves exactly as it did while it rendered inside the main window. In particular the clipboard
/// accessors are the main window's, which is what keeps Ctrl+V in the Open dialog's name box
/// talking to the real Windows clipboard rather than to a private string.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=8; Fingerprint=9479CF
// Broiler-Falsified-If: Ctrl+V in a broken-out dialog pastes text that did not come from the main window's WindowsClipboard read
// Broiler-Human:        PENDING
[SupportedOSPlatform("windows7.0")]
internal sealed class PlateHostWindow : Direct2DWindow, IUiHostWindow, IUiWindowChromeHost,
    IUiClipboardHost, IUiTextInputHost, IUiImageHost
{
    private readonly PlateUiHost _host;
    private UiSession? _session;

#pragma warning disable CS0618
    private readonly StandardLegacyGraphicsInputAdapter _legacyInput = new("broiler-plate-dialog");
#pragma warning restore CS0618

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=58B33F
    // Broiler-Falsified-If: a modal request creates the dialog without a native owner, so the main window can be raised above a dialog that blocks its input
    // Broiler-Human:        PENDING
    public PlateHostWindow(
        UiHostWindowRequest request,
        BWindow? owner,
        Func<string?>? getClipboardText,
        Action<string>? setClipboardText)
        : base(new BWindowOptions
        {
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Broiler Plate" : request.Title,
            ClientWidth = ToClientExtent(request.Placement.Width, 640),
            ClientHeight = ToClientExtent(request.Placement.Height, 420),
            Left = request.Placement.IsEmpty ? null : request.Placement.X,
            Top = request.Placement.IsEmpty ? null : request.Placement.Y,
            ClearColor = PlatePalette.Canvas,
            RenderOptions = new BRenderOptions(Antialias: true, VSync: true, SubpixelText: true),
            OwnsMessageLoop = false,
            Chrome = request.Chrome == UiHostWindowChrome.Owner ? BWindowChrome.Owner : BWindowChrome.System,
            Resizable = request.Resizable,

            // A modal dialog is owned by the window it blocks. Broiler.UI already refuses that
            // window's input, but refusing input does not keep the dialog in front of it: without
            // native ownership the user can raise the main window over a dialog it cannot respond
            // to, which reads as a hang. Ownership is what locks the z-order.
            Owner = request.IsModal ? owner : null,
        })
    {
        _host = new PlateUiHost(
            () => ClientSize,
            () => DpiScale,
            InvalidateIfAlive,
            static _ => { },
            getClipboardText,
            setClipboardText,
            getRenderer: () => Renderer);

        StateChanged += (_, _) => WindowStateChanged?.Invoke(this, EventArgs.Empty);
    }

    // IUiHost - forwarded so this window answers exactly as the main window's host does.
    BSize IUiHost.ViewportSize => _host.ViewportSize;

    double IUiHost.Scale => _host.Scale;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=F74B47
    // Broiler-Human:        PENDING
    BRenderList IUiHost.CreateRenderList(int capacity) => _host.CreateRenderList(capacity);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=03E2EF
    // Broiler-Human:        PENDING
    void IUiHost.Invalidate(UiInvalidation invalidation) => _host.Invalidate(invalidation);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=AD35F0
    // Broiler-Human:        PENDING
    void IUiHost.Present(BRenderList renderList) => _host.Present(renderList);

    // IUiHostWindow. SetTitle and CloseRequested come from BWindow, which already reports a
    // secondary window's WM_CLOSE as a request instead of destroying the window itself.
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=B9E321
    // Broiler-Falsified-If: a null session is accepted, so the next frame or input dereferences it
    // Broiler-Human:        PENDING
    public void Bind(UiSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        InvalidateIfAlive();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=99FA51
    // Broiler-Falsified-If: SetForegroundWindow is called with the zero handle of a dialog whose native window was already destroyed
    // Broiler-Human:        PENDING
    public void Activate()
    {
        if (IsDisposed || NativeHandle == IntPtr.Zero)
            return;

        SetForegroundWindow(NativeHandle);
        InvalidateIfAlive();
    }

    // IUiWindowChromeHost
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=94A32F
    // Broiler-Human:        PENDING
    public event EventHandler? WindowStateChanged;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=332E8F
    // Broiler-Falsified-If: a window created with BWindowChrome.Owner reports anything but UiHostWindowChrome.Owner
    // Broiler-Human:        PENDING
    UiHostWindowChrome IUiWindowChromeHost.Chrome =>
        Options.Chrome == BWindowChrome.Owner ? UiHostWindowChrome.Owner : UiHostWindowChrome.System;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=FF8537
    // Broiler-Human:        PENDING
    bool IUiWindowChromeHost.IsResizable => Options.Resizable;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=672D79
    // Broiler-Human:        PENDING
    UiHostWindowState IUiWindowChromeHost.WindowState => ToHostState(WindowState);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=D89695
    // Broiler-Falsified-If: a Minimized request from the dialog's title bar is applied as Normal or Maximized
    // Broiler-Human:        PENDING
    void IUiWindowChromeHost.SetWindowState(UiHostWindowState state) => SetWindowState(state switch
    {
        UiHostWindowState.Minimized => BWindowState.Minimized,
        UiHostWindowState.Maximized => BWindowState.Maximized,
        _ => BWindowState.Normal,
    });

    void IUiWindowChromeHost.SetIcon(BPixelBuffer? icon) => SetIcon(icon);

    void IUiWindowChromeHost.RequestClose() => Close();

    void IUiWindowChromeHost.BeginMoveDrag() => BeginMoveDrag();

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=777A55
    // Broiler-Human:        PENDING
    void IUiWindowChromeHost.BeginResizeDrag(UiWindowEdge edge) => BeginResizeDrag(ToWindowEdge(edge));

    // IUiClipboardHost / IUiTextInputHost / IUiImageHost
    // Broiler-AI:           Origin=AI; IP=None; Security=Critical; Resources=4; Fingerprint=3E75CC
    // Broiler-Falsified-If: Ctrl+V in a broken-out dialog pastes text other than what the main window's WindowsClipboard read returned
    // Broiler-Human:        PENDING
    public bool TryGetText(out string text) => _host.TryGetText(out text);

    // Broiler-AI:           Origin=AI; IP=None; Security=Critical; Resources=4; Fingerprint=DFC83A
    // Broiler-Falsified-If: text copied in a broken-out dialog does not reach the main window's WindowsClipboard.SetText, so the system clipboard keeps its previous contents
    // Broiler-Human:        PENDING
    public void SetText(string text) => _host.SetText(text);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=14297D
    // Broiler-Human:        PENDING
    public void PublishCaret(UiTextCaretInfo caret) => _host.PublishCaret(caret);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=C31E89
    // Broiler-Human:        PENDING
    public void ClearCaret(UiElement owner) => _host.ClearCaret(owner);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=8; Fingerprint=086BBA
    // Broiler-Falsified-If: an encoded image the renderer's decoder rejects with an exception escapes CreateImage instead of returning BImageHandle.Invalid
    // Broiler-Human:        PENDING
    public BImageHandle CreateImage(ReadOnlySpan<byte> encodedImage) => _host.CreateImage(encodedImage);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=49004E
    // Broiler-Falsified-If: an invalid image handle is handed on to the renderer's ReleaseImage
    // Broiler-Human:        PENDING
    public void ReleaseImage(BImageHandle image) => _host.ReleaseImage(image);

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=4; Fingerprint=667EE7
    // Broiler-Falsified-If: a frame requested after the dialog's session was disposed asks that disposed session to render
    // Broiler-Human:        PENDING
    protected override BRenderList? BuildRenderList(BSize clientSize) =>
        _session is { IsDisposed: false } session ? session.RenderFrame() : null;

    protected override void OnResized(BSize clientSize, double dpiScale) => InvalidateIfAlive();

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=8; Fingerprint=591E77
    // Broiler-Falsified-If: a mouse button press in the dialog is delivered to its session as anything other than a pointer-button event
    // Broiler-Human:        PENDING
    protected override void OnPointerDown(BPointerEventArgs e) => Dispatch(_legacyInput.FromPointerButton(e));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=8; Fingerprint=7448EF
    // Broiler-Falsified-If: pointer motion in the dialog is delivered to its session as a button event, so hovering a list entry selects or opens it
    // Broiler-Human:        PENDING
    protected override void OnPointerMove(BPointerEventArgs e) => Dispatch(_legacyInput.FromPointerMove(e));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=8; Fingerprint=C0302C
    // Broiler-Falsified-If: a button release in the dialog is delivered as a pointer move, so its Open and Cancel buttons never complete a click
    // Broiler-Human:        PENDING
    protected override void OnPointerUp(BPointerEventArgs e) => Dispatch(_legacyInput.FromPointerButton(e));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=8; Fingerprint=4D9946
    // Broiler-Falsified-If: a wheel notch over the dialog's file list reaches its session without its notch count, so the list does not scroll
    // Broiler-Human:        PENDING
    protected override void OnMouseWheel(BMouseWheelEventArgs e) => Dispatch(_legacyInput.FromMouseWheel(e));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=8; Fingerprint=EBC72C
    // Broiler-Falsified-If: a key press in the dialog is converted with KeyboardKeyTransition.Up, so Enter in the name box does not accept the dialog
    // Broiler-Human:        PENDING
    protected override void OnKeyDown(BKeyEventArgs e) => Dispatch(_legacyInput.FromKey(e, KeyboardKeyTransition.Down));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=8; Fingerprint=E0727F
    // Broiler-Falsified-If: a key release in the dialog is converted with KeyboardKeyTransition.Down, so one Enter accepts the dialog twice
    // Broiler-Human:        PENDING
    protected override void OnKeyUp(BKeyEventArgs e) => Dispatch(_legacyInput.FromKey(e, KeyboardKeyTransition.Up));

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=8; Fingerprint=499FE8
    // Broiler-Falsified-If: characters typed into the Open dialog's name box never reach the dialog's session
    // Broiler-Human:        PENDING
    protected override void OnTextInput(BTextInputEventArgs e) => Dispatch(_legacyInput.FromText(e));

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=3D9A68
    // Broiler-Falsified-If: disposing a dialog leaves its native window alive, so Closed never fires and PlateWindowsUiHost keeps it in its list
    // Broiler-Human:        PENDING
    protected override void Dispose(bool disposing)
    {
        // Destroy the native window when the framework disposes this host window.
        if (disposing && !IsDisposed)
            Close();

        base.Dispose(disposing);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=9BBFBE
    // Broiler-Falsified-If: a maximized dialog reports UiHostWindowState.Normal, so its title bar offers Maximize instead of Restore
    // Broiler-Human:        PENDING
    private static UiHostWindowState ToHostState(BWindowState state) => state switch
    {
        BWindowState.Minimized => UiHostWindowState.Minimized,
        BWindowState.Maximized => UiHostWindowState.Maximized,
        _ => UiHostWindowState.Normal,
    };

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=870E51
    // Broiler-Falsified-If: a drag on UiWindowEdge.TopRight resizes any edge other than the top-right corner
    // Broiler-Human:        PENDING
    private static BWindowEdge ToWindowEdge(UiWindowEdge edge) => edge switch
    {
        UiWindowEdge.Left => BWindowEdge.Left,
        UiWindowEdge.Top => BWindowEdge.Top,
        UiWindowEdge.Right => BWindowEdge.Right,
        UiWindowEdge.Bottom => BWindowEdge.Bottom,
        UiWindowEdge.TopLeft => BWindowEdge.TopLeft,
        UiWindowEdge.TopRight => BWindowEdge.TopRight,
        UiWindowEdge.BottomLeft => BWindowEdge.BottomLeft,
        UiWindowEdge.BottomRight => BWindowEdge.BottomRight,
        _ => BWindowEdge.None,
    };

    /// <summary>
    /// Routes native input into the hosted session. Dispatching can close the dialog — its Cancel
    /// button, or the owner-drawn close button — which disposes the session *and* this window while
    /// the call is still on the stack, so nothing here may assume it is still alive once
    /// <see cref="UiSession.DispatchInput"/> returns.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=8; Fingerprint=9921F7
    // Broiler-Falsified-If: an input that closes the dialog disposes this window inside DispatchInput, and the repaint that follows throws ObjectDisposedException
    // Broiler-Human:        PENDING
    private void Dispatch(UiInputEvent input)
    {
        if (_session is null || _session.IsDisposed)
            return;

        if (_session.DispatchInput(input))
            InvalidateIfAlive();
    }

    /// <summary>Repaints, unless this window is already gone. See <see cref="Dispatch"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=58A4DD
    // Broiler-Falsified-If: a repaint requested after Close destroyed the native window, but before Dispose, reaches Invalidate with a zero handle
    // Broiler-Human:        PENDING
    private void InvalidateIfAlive()
    {
        if (!IsDisposed && NativeHandle != IntPtr.Zero)
            Invalidate();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=F5B14B
    // Broiler-Falsified-If: a NaN or sub-pixel requested extent is used as the client size instead of the fallback
    // Broiler-Human:        PENDING
    private static int ToClientExtent(double requested, int fallback) =>
        requested > 1 ? (int)Math.Round(requested) : fallback;

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=5B5789
    // Broiler-Falsified-If: the HWND is marshalled narrower than pointer size, so a 64-bit process brings a truncated window handle to the foreground
    // Broiler-Human:        PENDING
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hwnd);
}
