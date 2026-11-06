// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using Broiler.Graphics.Color;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.RenderList;
using Broiler.UI;
using Broiler.UI.Button.Standard;
using Broiler.UI.Toolbar;
using Xunit;

namespace Broiler.Plate.Windows.Tests;

public sealed class PlateToolbarTests
{
    [Fact]
    public void Toolbar_Contains_All_Actions_With_Icons_And_Tooltips()
    {
        using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
        using var app = new PlateApp(host, () => { });

        Assert.NotNull(app.OpenButton);
        Assert.NotNull(app.OpenFolderButton);
        Assert.NotNull(app.CloseButton);
        Assert.NotNull(app.PrintButton);
        Assert.NotNull(app.ScanButton);
        Assert.NotNull(app.GalleryButton);
        Assert.NotNull(app.UpFolderButton);
        Assert.NotNull(app.ZoomOutButton);
        Assert.NotNull(app.ZoomCombo);
        Assert.NotNull(app.ZoomInButton);
        Assert.NotNull(app.ZoomResetButton);
        Assert.NotNull(app.ZoomFitButton);
        Assert.NotNull(app.NotesButton);
        Assert.NotNull(app.AboutButton);

        StandardButton[] iconButtons =
        [
            app.OpenButton!,
            app.OpenFolderButton!,
            app.CloseButton!,
            app.PrintButton!,
            app.ScanButton!,
            app.GalleryButton!,
            app.UpFolderButton!,
            app.ZoomOutButton!,
            app.ZoomInButton!,
            app.ZoomResetButton!,
            app.ZoomFitButton!,
            app.NotesButton!,
            app.AboutButton!,
        ];

        foreach (StandardButton button in iconButtons)
        {
            Assert.Contains(button, app.Toolbar.Children);
            Assert.NotNull(button.IconPainter);
            Assert.False(string.IsNullOrWhiteSpace(button.ToolTipText));
            Assert.Equal(16, button.IconExtent);
            Assert.Equal(30, button.PreferredSize.Width);
            Assert.Equal(30, button.PreferredSize.Height);
        }

        Assert.Contains(app.ZoomCombo!, app.Toolbar.Children);
        Assert.False(string.IsNullOrWhiteSpace(app.ZoomCombo!.ToolTipText));

        Assert.Equal(UiToolbarBreak.Gap, app.Toolbar.GetBreakBefore(app.GalleryButton!));
        Assert.Equal(UiToolbarBreak.Gap, app.Toolbar.GetBreakBefore(app.ZoomOutButton!));
        Assert.Equal(UiToolbarBreak.Gap, app.Toolbar.GetBreakBefore(app.NotesButton!));
    }

    [Fact]
    public void Toolbar_Icons_Draw_Geometry()
    {
        using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
        using var app = new PlateApp(host, () => { });

        StandardButton[] iconButtons =
        [
            app.OpenButton!,
            app.OpenFolderButton!,
            app.CloseButton!,
            app.PrintButton!,
            app.ScanButton!,
            app.GalleryButton!,
            app.UpFolderButton!,
            app.ZoomOutButton!,
            app.ZoomInButton!,
            app.ZoomResetButton!,
            app.ZoomFitButton!,
            app.NotesButton!,
            app.AboutButton!,
        ];

        foreach (StandardButton button in iconButtons)
        {
            var list = new BRenderList();
            button.IconPainter!(list, new BRect(0, 0, 16, 16), BColor.Black);
            Assert.NotEmpty(list.Commands);
        }
    }

    [Fact]
    public void Toolbar_Initial_Button_States()
    {
        using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
        using var app = new PlateApp(host, () => { });

        Assert.True(app.OpenButton!.IsEnabled);
        Assert.True(app.OpenFolderButton!.IsEnabled);
        Assert.False(app.CloseButton!.IsEnabled);
        Assert.False(app.PrintButton!.IsEnabled);
        Assert.False(app.ScanButton!.IsEnabled);
        Assert.Equal(UiVisibility.Collapsed, app.GalleryButton!.Visibility);
        Assert.Equal(UiVisibility.Collapsed, app.UpFolderButton!.Visibility);
        Assert.True(app.ZoomOutButton!.IsEnabled);
        Assert.True(app.ZoomCombo!.IsEnabled);
        Assert.True(app.ZoomInButton!.IsEnabled);
        Assert.True(app.ZoomResetButton!.IsEnabled);
        Assert.False(app.ZoomFitButton!.IsEnabled); // Fit is only enabled for image view
        Assert.False(app.NotesButton!.IsEnabled); // No diagnostics initially
        Assert.True(app.AboutButton!.IsEnabled);

        // Verify frame rendering succeeds with toolbar items and vector icons
        BRenderList frame = app.RenderFrame();
        Assert.NotEmpty(frame.Commands);
    }

    [Fact]
    public void OpenButton_Invokes_RequestOpenFile_WhenProvided()
    {
        bool openFileRequested = false;
        using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
        using var app = new PlateApp(host, () => { }, requestOpenFile: () => openFileRequested = true);

        app.OpenButton!.Click();

        Assert.True(openFileRequested);
    }

    [Fact]
    public void OpenFolderButton_Invokes_RequestOpenFolder_WhenProvided()
    {
        bool openFolderRequested = false;
        using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
        using var app = new PlateApp(host, () => { }, requestOpenFolder: () => openFolderRequested = true);

        app.OpenFolderButton!.Click();

        Assert.True(openFolderRequested);
    }

    [Fact]
    public void PlateFileFormats_CreateAcceptExtensions_ProducesCommaSeparatedExtensions()
    {
        var formats = PlateFileFormats.CreateDefault();
        string accept = formats.CreateAcceptExtensions();

        Assert.Contains(".rtf", accept);
        Assert.Contains(".docx", accept);
        Assert.Contains(".html", accept);
        Assert.Contains(".md", accept);
        Assert.Contains(".png", accept);
        Assert.Contains(".jpg", accept);
        Assert.DoesNotContain("*", accept);
        Assert.DoesNotContain(";", accept);
    }

    [Fact]
    public void OpenFile_WithJpeg_Decodes_And_Renders()
    {
        Broiler.Graphics.Imaging.BImageCodecs.Use(new Broiler.Media.MediaCodecCatalog(
            Broiler.Media.Image.Managed.ManagedImageCodecs.CreateCodecs()));

        using var renderer = new Broiler.Graphics.Rendering.BImageRenderer();
        using var host = new PlateUiHost(
            () => new BSize(1200, 800),
            () => 1,
            () => { },
            _ => { },
            getRenderer: () => renderer);
        using var app = new PlateApp(host, () => { });

        using var bmp = new Broiler.Graphics.Imaging.BBitmap(200, 150);
        bmp.Clear(Broiler.Graphics.Color.BColor.Red);
        byte[] jpegBytes = bmp.Encode(Broiler.Media.Image.ImageEncodeFormat.Jpeg);

        bool opened = app.Open("test_image.jpg", jpegBytes);
        Assert.True(opened);

        var list = app.RenderFrame();
        Assert.NotNull(list);
    }
}
