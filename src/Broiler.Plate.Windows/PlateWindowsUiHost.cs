// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           6
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    High
// Criteria:         7/4
// Resource impact:  2/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Broiler.Graphics;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Rendering;
using Broiler.Graphics.RenderList;
using Broiler.Graphics.Windowing;
using Broiler.UI;

namespace Broiler.Plate;

/// <summary>
/// The Windows head's <see cref="PlateUiHost"/>. It adds the optional <see cref="IUiWindowHost"/>
/// capability, which is what lets the Open dialog break out of the main window into a real OS
/// window the user can move to another monitor and resize (Broiler.UI ADR 0025 and 0026).
/// </summary>
/// <remarks>
/// The capability is added *here* rather than on <see cref="PlateUiHost"/> because a host either
/// offers it or does not: Broiler.UI discovers it with <c>Host is IUiWindowHost</c>, so a host that
/// implemented it and then failed would turn the documented fallback into an exception. A head
/// that cannot open a second OS window keeps the plain host, does not answer the capability, and
/// its dialogs stay logical subwindows rendered inside the main viewport.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=0F2F4A
// Broiler-Falsified-If: ClientToScreen writes the owner's client origin into a NativePoint that is not two sequential 32-bit integers
// Broiler-Human:        PENDING
[SupportedOSPlatform("windows7.0")]
internal sealed class PlateWindowsUiHost : PlateUiHost, IUiWindowHost
{
    private readonly List<PlateHostWindow> _hostWindows = [];
    private readonly Func<BWindow?> _getOwnerWindow;
    private readonly Func<string?>? _getClipboardText;
    private readonly Action<string>? _setClipboardText;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=216D75
    // Broiler-Falsified-If: a null getOwnerWindow is accepted and fails only when the first dialog breaks out
    // Broiler-Human:        PENDING
    public PlateWindowsUiHost(
        Func<BSize> getViewportSize,
        Func<double> getScale,
        Action invalidate,
        Action<BRenderList> present,
        Func<BWindow?> getOwnerWindow,
        Func<string?>? getClipboardText = null,
        Action<string>? setClipboardText = null,
        Action<UiTextCaretInfo?>? caretChanged = null,
        Func<IBroilerRenderer?>? getRenderer = null)
        : base(getViewportSize, getScale, invalidate, present, getClipboardText, setClipboardText, caretChanged, getRenderer)
    {
        _getOwnerWindow = getOwnerWindow ?? throw new ArgumentNullException(nameof(getOwnerWindow));
        _getClipboardText = getClipboardText;
        _setClipboardText = setClipboardText;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=2; Fingerprint=B83259
    // Broiler-Falsified-If: a dialog window closed by the user is not removed from _hostWindows, so every Open dialog shown stays alive until the viewer exits
    // Broiler-Human:        PENDING
    public IUiHostWindow CreateHostWindow(UiHostWindowRequest request)
    {
        BWindow? owner = _getOwnerWindow();
        var window = new PlateHostWindow(ToScreenPlacement(owner, request), owner, _getClipboardText, _setClipboardText);
        _hostWindows.Add(window);
        window.Closed += (_, _) => _hostWindows.Remove(window);
        window.Show();
        return window;
    }

    /// <summary>
    /// Maps the requested placement from the owner's client coordinates onto the screen.
    ///
    /// Broiler.UI positions a dialog against the window it belongs to — <c>GetDialogPlacement</c>
    /// centers it in the main viewport — but a native window is placed on the desktop. Without the
    /// translation a dialog centered in a window that is itself halfway down a large monitor opens
    /// that far into the top-left corner of the screen instead. The client origin is in physical
    /// pixels and the placement is in device-independent ones, so it is scaled on the way through.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=B8790F
    // Broiler-Falsified-If: an owner client origin of (300, 200) physical pixels at Scale 1.5 moves the placement by anything other than (200, 133.3) device-independent pixels
    // Broiler-Human:        PENDING
    private UiHostWindowRequest ToScreenPlacement(BWindow? owner, UiHostWindowRequest request)
    {
        if (request.Placement.IsEmpty)
            return request;

        IntPtr ownerHandle = owner?.NativeHandle ?? IntPtr.Zero;
        if (ownerHandle == IntPtr.Zero)
            return request;

        var origin = default(NativePoint);
        if (!ClientToScreen(ownerHandle, ref origin))
            return request;

        double scale = Scale > 0 ? Scale : 1;
        BRect placement = request.Placement;
        return request with
        {
            Placement = new BRect(
                (origin.X / scale) + placement.X,
                (origin.Y / scale) + placement.Y,
                placement.Width,
                placement.Height),
        };
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=9C0F9A
    // Broiler-Falsified-If: disposing a host window raises Closed, which removes it from _hostWindows while the sweep is enumerating that list
    // Broiler-Human:        PENDING
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Disposing a host window destroys it, which raises Closed and removes it from the
            // list; iterate a copy so that does not mutate the collection underneath us.
            foreach (PlateHostWindow window in _hostWindows.ToArray())
                window.Dispose();

            _hostWindows.Clear();
        }

        base.Dispose(disposing);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=57F17C
    // Broiler-Falsified-If: the struct is not two sequential 32-bit signed fields, so ClientToScreen writes a POINT that does not fit it
    // Broiler-Human:        PENDING
    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=0F77BE
    // Broiler-Falsified-If: a failed ClientToScreen is marshalled as true because the result is not a 4-byte Win32 BOOL, so an unconverted origin offsets the dialog
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hwnd, ref NativePoint point);
}
