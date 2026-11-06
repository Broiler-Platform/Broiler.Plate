// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using Broiler.Graphics.Geometry;
using Broiler.UI.Dialog.Standard;

namespace Broiler.Plate.Windows.Tests;

public sealed class PlateFolderDialogTests : IDisposable
{
    private readonly string _testRoot;

    public PlateFolderDialogTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "plate-folder-dialog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
        Directory.CreateDirectory(Path.Combine(_testRoot, "SubA"));
        Directory.CreateDirectory(Path.Combine(_testRoot, "SubB"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_testRoot, recursive: true); } catch { }
    }

    [Fact]
    public void FolderDialog_Populates_Known_Places_And_Navigates()
    {
        var dialog = new StandardDialog();
        var picker = new PlateFolderDialog(dialog, _testRoot);

        // Verify known places are populated
        Assert.NotEmpty(picker.Places.Items);
        string[] labels = picker.Places.Items.Select(i => i.Text).ToArray();
        Assert.Contains("Home", labels);
        Assert.Contains("Desktop", labels);
        Assert.Contains("Documents", labels);
        Assert.Contains("Downloads", labels);
        Assert.Contains("Pictures", labels);
        Assert.Contains("Working folder", labels);

        // Folders list contains SubA and SubB
        Assert.Equal(2, picker.Folders.Items.Count);
        string[] folderNames = picker.Folders.Items.Select(i => i.Text).ToArray();
        Assert.Contains("SubA", folderNames);
        Assert.Contains("SubB", folderNames);

        // Navigate via Places selection to Desktop
        var desktopItem = picker.Places.Items.FirstOrDefault(i => i.Text == "Desktop");
        Assert.NotNull(desktopItem);
        picker.Places.SelectedItemId = desktopItem.Id;

        string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        Assert.Equal(Path.GetFullPath(desktopDir), Path.GetFullPath(picker.CurrentDirectory));
        Assert.Equal(Path.GetFullPath(desktopDir), Path.GetFullPath(picker.PathEdit.Text));
    }

    [Fact]
    public void FolderDialog_Selecting_Place_Highlights_Matching_Place()
    {
        var dialog = new StandardDialog();
        var picker = new PlateFolderDialog(dialog, _testRoot);

        var workItem = picker.Places.Items.FirstOrDefault(i => i.Text == "Working folder");
        Assert.NotNull(workItem);
        picker.Places.SelectedItemId = workItem.Id;

        Assert.Equal(workItem.Id, picker.Places.SelectedItemId);
        Assert.Equal(Path.GetFullPath(Environment.CurrentDirectory), Path.GetFullPath(picker.CurrentDirectory));
    }
}
