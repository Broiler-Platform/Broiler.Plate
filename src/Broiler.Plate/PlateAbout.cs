// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           0
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    Low
// Criteria:         2/0
// Resource impact:  2/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Reflection;
using Broiler.Graphics.Geometry;
using Broiler.UI.AboutDialog.Standard;
using Broiler.UI.Window;

namespace Broiler.Plate;

/// <summary>Composes the viewer's About dialog using the product assembly metadata.</summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=2; Fingerprint=1EC027
// Broiler-Falsified-If: a viewport smaller than 32 units in either dimension gives the About dialog a negative width, height or origin
// Broiler-Human:        PENDING
internal static class PlateAbout
{
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=2; Fingerprint=F53C29
    // Broiler-Falsified-If: a viewport smaller than 32 units in either dimension gives the About dialog a negative width, height or origin instead of zero
    // Broiler-Human:        PENDING
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
