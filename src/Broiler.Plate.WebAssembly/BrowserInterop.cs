// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices.JavaScript;

namespace Broiler.Plate.WebAssembly;

/// <summary>
/// Managed-to-page bridge for the browser Plate application.
/// </summary>
internal static partial class BrowserInterop
{
    [JSImport("plate.scheduleFrame", "main.js")]
    internal static partial void ScheduleFrame();

    [JSImport("plate.setTitle", "main.js")]
    internal static partial void SetTitle(string title);

    [JSImport("plate.setCursor", "main.js")]
    internal static partial void SetCursor(string cursor);

    [JSImport("plate.openUrl", "main.js")]
    internal static partial void OpenUrl(string url);

    [JSImport("plate.webMenu", "main.js")]
    internal static partial void WebMenu(string action);

    [JSImport("plate.ready", "main.js")]
    internal static partial void Ready(double logicalWidth, double logicalHeight);

    [JSImport("plate.failed", "main.js")]
    internal static partial void Failed(string exceptionType, string details);

    [JSImport("plate.publishFrame", "main.js")]
    internal static partial void PublishFrame(
        double frameIndex,
        bool caretActive,
        double caretX,
        double caretY,
        double caretWidth,
        double caretHeight,
        int caretIndex,
        int selectionStart,
        int selectionLength,
        string statusText,
        bool darkTheme);

    [JSImport("plate.prefersDarkScheme", "main.js")]
    internal static partial bool PrefersDarkScheme();

    [JSImport("plate.prefersReducedMotion", "main.js")]
    internal static partial bool PrefersReducedMotion();

    [JSImport("plate.requestOpenFile", "main.js")]
    internal static partial void RequestOpenFile(string acceptExtensions);

    [JSImport("plate.requestOpenFolder", "main.js")]
    internal static partial void RequestOpenFolder();

    [JSImport("plate.reportOperation", "main.js")]
    internal static partial void ReportOperation(string outcome, string message);
}
