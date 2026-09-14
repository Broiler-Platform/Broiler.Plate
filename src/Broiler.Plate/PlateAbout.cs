using System;
using System.Reflection;
using Broiler.Graphics.Geometry;
using Broiler.UI.AboutDialog.Standard;
using Broiler.UI.Window;

namespace Broiler.Plate;

/// <summary>Composes the viewer's About dialog using the product assembly metadata.</summary>
internal static class PlateAbout
{
    public static void Show(UiWindow owner, BSize viewport, Assembly productAssembly)
    {
        if (owner.IsDisposed || owner.IsClosed)
            return;

        var dialog = new StandardAboutDialog();
        // Use Plate's assembly even when hosted by another application or a test runner.
        // Broiler.UI preserves the prerelease label and omits the SDK's commit hash.
        dialog.PopulateFromAssemblies(productAssembly);
        dialog.ProductName = "Broiler Plate";
        dialog.Title = "About Broiler Plate";

        double width = Math.Min(dialog.PreferredSize.Width, Math.Max(0, viewport.Width - 32));
        double height = Math.Min(dialog.PreferredSize.Height, Math.Max(0, viewport.Height - 32));
        _ = dialog.ShowModal(owner, new BRect(
            Math.Max(0, (viewport.Width - width) / 2),
            Math.Max(0, (viewport.Height - height) / 2), width, height));
    }
}
