// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using Broiler.Graphics.Geometry;
using Broiler.UI;

namespace Broiler.Plate.Windows.Tests;

public sealed class PlateTooltipTests
{
    [Fact]
    public void Controls_And_Toolbar_Have_Tooltips()
    {
        using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
        bool ticking = false;
        using var app = new PlateApp(host, () => { }, setTicking: t => ticking = t);
        app.RenderFrame();

        Assert.Equal("Document actions toolbar", app.Toolbar.ToolTipText);
        Assert.False(app.WantsTick);
        Assert.False(ticking);
    }
}
