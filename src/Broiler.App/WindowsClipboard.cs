// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   15
// Annotated:        15/15
// Exempt:           0
// Human-reviewed:   0/15
// IP risk:          Low
// Security risk:    Critical
// Criteria:         15/15
// Resource impact:  4/10 max
// Unverified:       15
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Broiler.UI;

namespace Broiler.App;

/// <summary>
/// The Win32 clipboard, shared by the Browser, Writer and Code heads.
///
/// There is deliberately no in-memory fallback. A private string standing in
/// for the clipboard makes copy and paste appear to work while silently not
/// interoperating with anything else on the machine — a user copies from the
/// editor, pastes into a browser, and gets their previous clipboard contents.
/// The Browser and Writer hosts did exactly that until they were wired to this;
/// it reports failure instead, and the caller shows the command as unavailable.
///
/// The bindings are <c>DllImport</c> rather than <c>LibraryImport</c> on
/// purpose: this file is compiled into the Browser, Writer and Code heads
/// alike, and the generated marshalling stubs would require
/// <c>AllowUnsafeBlocks</c> in every one of them. The two application heads use
/// <c>DllImport</c> for their own interop for the same reason.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=4; Fingerprint=F39BF4
// Broiler-Falsified-If: CF_UNICODETEXT data whose global block holds no NUL character is read past GlobalSize(handle) by Marshal.PtrToStringUni
// Broiler-Human:        PENDING
[SupportedOSPlatform("windows5.0")]
internal sealed class WindowsClipboard(IntPtr ownerWindow) : IUiClipboardHost
{
    // Broiler-AI:           Origin=AI; IP=None; Security=Critical; Resources=0; Fingerprint=82BB56
    // Broiler-Falsified-If: the value is not Win32 CF_UNICODETEXT (13), so TryGetText reads a block of another clipboard format as NUL-terminated UTF-16
    // Broiler-Human:        PENDING
    private const uint CfUnicodeText = 13;
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=6C1EE9
    // Broiler-Falsified-If: the value is not Win32 GMEM_MOVEABLE (0x0002), so SetClipboardData is handed a block that was not allocated moveable as its contract requires
    // Broiler-Human:        PENDING
    private const uint GmemMoveable = 0x0002;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=4; Fingerprint=23A8D0
    // Broiler-Falsified-If: CF_UNICODETEXT data whose global block holds no NUL character is read past GlobalSize(handle) by Marshal.PtrToStringUni
    // Broiler-Human:        PENDING
    public bool TryGetText(out string text)
    {
        text = string.Empty;
        if (!IsClipboardFormatAvailable(CfUnicodeText))
            return false;

        // The clipboard is a shared, single-owner resource: another process can
        // hold it, so opening is allowed to fail and the caller is told rather
        // than being handed something stale.
        if (!OpenClipboard(ownerWindow))
            return false;

        try
        {
            IntPtr handle = GetClipboardData(CfUnicodeText);
            if (handle == IntPtr.Zero)
                return false;

            IntPtr pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
                return false;

            try
            {
                text = Marshal.PtrToStringUni(pointer) ?? string.Empty;
                return text.Length > 0;
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=4; Fingerprint=33578F
    // Broiler-Falsified-If: Marshal.Copy or the terminating WriteInt16 for a text of n chars writes outside the (n + 1) * 2 bytes requested from GlobalAlloc
    // Broiler-Human:        PENDING
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!OpenClipboard(ownerWindow))
            return;

        try
        {
            EmptyClipboard();

            // The clipboard takes ownership of the moveable block on success,
            // so it must not be freed here — and must be freed if
            // SetClipboardData fails, or the process leaks it on every copy.
            int bytes = (text.Length + 1) * sizeof(char);
            IntPtr block = GlobalAlloc(GmemMoveable, (UIntPtr)bytes);
            if (block == IntPtr.Zero)
                return;

            IntPtr pointer = GlobalLock(block);
            if (pointer == IntPtr.Zero)
            {
                GlobalFree(block);
                return;
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                Marshal.WriteInt16(pointer, text.Length * sizeof(char), 0);
            }
            finally
            {
                GlobalUnlock(block);
            }

            if (SetClipboardData(CfUnicodeText, block) == IntPtr.Zero)
                GlobalFree(block);
        }
        finally
        {
            CloseClipboard();
        }
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=0B1567
    // Broiler-Falsified-If: the result is not marshalled as a 4-byte Win32 BOOL, so a clipboard held by another process is reported as opened
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr owner);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=819D6E
    // Broiler-Falsified-If: the declaration is not user32 CloseClipboard taking no arguments, so the clipboard opened by TryGetText or SetText is never released to other processes
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=82E0CE
    // Broiler-Falsified-If: the declaration is not user32 EmptyClipboard taking no arguments, so SetText adds its block beside the previous owner's formats instead of replacing them
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=AAAC79
    // Broiler-Falsified-If: the format is not passed as the 32-bit UINT user32 expects, so availability is answered for a format other than CF_UNICODETEXT
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=3; Fingerprint=317039
    // Broiler-Falsified-If: the returned HANDLE is marshalled narrower than pointer size, so a 64-bit process locks a truncated handle
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint format);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=BDB3DF
    // Broiler-Falsified-If: the returned HANDLE is not marshalled pointer-sized, so a block the clipboard refused reads as accepted and is never freed
    // Broiler-Human:        PENDING
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr data);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=3; Fingerprint=B864A6
    // Broiler-Falsified-If: the byte count is marshalled narrower than SIZE_T, so a 64-bit process allocates fewer bytes than SetText then writes
    // Broiler-Human:        PENDING
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=80E1CC
    // Broiler-Falsified-If: the handle is marshalled narrower than pointer size, so SetText frees a truncated handle instead of the block it allocated
    // Broiler-Human:        PENDING
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr handle);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=D52183
    // Broiler-Falsified-If: the returned pointer is marshalled narrower than pointer size, so the Marshal reads and writes go to a truncated address
    // Broiler-Human:        PENDING
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr handle);

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=BDEB39
    // Broiler-Falsified-If: the declaration is not kernel32 GlobalUnlock taking the HGLOBAL, so a block stays locked after SetText hands it to the clipboard
    // Broiler-Human:        PENDING
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr handle);
}
