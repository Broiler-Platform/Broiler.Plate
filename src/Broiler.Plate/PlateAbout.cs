// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Imaging;
using Broiler.UI.AboutDialog.Standard;
using Broiler.UI.Window;

namespace Broiler.Plate;

/// <summary>Composes the viewer's About dialog using the product assembly metadata and embedded artwork.</summary>
internal static class PlateAbout
{
    private static readonly Lazy<BImageResource> Artwork = new(() =>
    {
        using Stream stream = typeof(PlateAbout).Assembly.GetManifestResourceStream("Broiler.Plate.About.png")
            ?? throw new InvalidOperationException("The embedded About picture is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return BImageResource.FromEncoded(bytes.ToArray(), "image/png");
    });

    public static void Show(UiWindow owner, BSize viewport, Assembly productAssembly, Action<Uri>? openUrl = null)
    {
        if (owner.IsDisposed || owner.IsClosed)
            return;

        var dialog = new StandardAboutDialog();
        // Use Plate's assembly even when hosted by another application or a test runner.
        // Broiler.UI preserves the prerelease label and omits the SDK's commit hash.
        var components = new[] { productAssembly, typeof(PlateAbout).Assembly, typeof(StandardAboutDialog).Assembly };
        dialog.PopulateFromAssemblies(productAssembly, components);
        dialog.ProductName = "Broiler Plate";
        dialog.Title = "About Broiler Plate";
        if (OperatingSystem.IsBrowser())
        {
            dialog.CanClose = false;
        }
        dialog.Picture = Artwork.Value;
        dialog.PreferredSize = new BSize(820, 420);
        dialog.PictureAltText = "Broiler Plate";
        dialog.ProductUrl = new Uri("https://broiler-platform.github.io/");
        dialog.ProductUrlRequested += (_, url) =>
        {
            try
            {
                (openUrl ?? OpenDesktopUrl)(url);
            }
            catch (Exception error) when (error is Win32Exception or InvalidOperationException or NotSupportedException)
            {
                dialog.WebsiteButton.Text = "Unable to open website";
            }
        };

        double width = Math.Min(dialog.PreferredSize.Width, Math.Max(0, viewport.Width - 32));
        double height = Math.Min(dialog.PreferredSize.Height, Math.Max(0, viewport.Height - 32));
        _ = dialog.ShowModal(owner, new BRect(
            Math.Max(0, (viewport.Width - width) / 2),
            Math.Max(0, (viewport.Height - height) / 2), width, height));
    }

    private static void OpenDesktopUrl(Uri url)
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            throw new NotSupportedException("This host must supply website navigation.");
        using Process? process = Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }
}
