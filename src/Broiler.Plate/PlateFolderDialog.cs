// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Text;
using Broiler.Input.Keyboard;
using Broiler.UI;
using Broiler.UI.Button.Standard;
using Broiler.UI.Dialog.Standard;
using Broiler.UI.Edit.Standard;
using Broiler.UI.Label.Standard;
using Broiler.UI.ListView;
using Broiler.UI.ListView.Standard;

namespace Broiler.Plate;

/// <summary>A folder picker with an editable path, known places shortcuts, and a browsable list of subfolders.</summary>
internal sealed class PlateFolderDialog : UiElement
{
    private readonly StandardDialog _dialog;
    internal StandardEdit PathEdit { get; } = new();
    internal StandardListView Places { get; } = new()
    {
        ItemHeight = 28,
        Background = PlatePalette.ToolbarButton,
        Foreground = PlatePalette.Title,
        BorderColor = PlatePalette.ViewBorder,
        SelectedBackground = PlatePalette.MenuSelected,
    };
    internal StandardListView Folders { get; } = new()
    {
        ItemHeight = 28,
        Background = PlatePalette.ToolbarButton,
        Foreground = PlatePalette.Title,
        BorderColor = PlatePalette.ViewBorder,
        SelectedBackground = PlatePalette.MenuSelected,
    };
    private readonly StandardLabel _placesHeader = new()
    {
        Text = "Known places",
        Font = new BFontStyle("Segoe UI", 12, BFontWeight.SemiBold),
        Foreground = PlatePalette.Muted,
    };
    private readonly StandardLabel _foldersHeader = new()
    {
        Text = "Folders",
        Font = new BFontStyle("Segoe UI", 12, BFontWeight.SemiBold),
        Foreground = PlatePalette.Muted,
    };
    private readonly StandardButton _up = new() { Text = "Up" };
    private readonly StandardButton _go = new() { Text = "Go" };
    private readonly StandardButton _open = new() { Text = "Open folder", IsDefault = true };
    private readonly StandardButton _cancel = new() { Text = "Cancel", IsCancel = true };
    private readonly StandardLabel _message = new();
    private readonly Dictionary<string, string> _placePaths = [];
    private bool _navigating;
    internal string CurrentDirectory { get; private set; } = string.Empty;

    internal PlateFolderDialog(StandardDialog dialog, string directory)
    {
        _dialog = dialog;
        Focusable = true;
        foreach (UiElement child in new UiElement[] { PathEdit, _up, _go, _placesHeader, Places, _foldersHeader, Folders, _message, _open, _cancel })
            AddChild(child);

        PopulatePlaces();

        _up.Clicked += (_, _) => Navigate(Directory.GetParent(CurrentDirectory)?.FullName ?? CurrentDirectory);
        _go.Clicked += (_, _) => Navigate(PathEdit.Text);
        PathEdit.Submitted += (_, _) => Navigate(PathEdit.Text);
        Places.SelectionChanged += (_, e) =>
        {
            if (!_navigating && e.NewItemId is not null && _placePaths.TryGetValue(e.NewItemId, out string? placePath))
                Navigate(placePath);
        };
        Folders.SelectionChanged += (_, e) =>
        {
            if (!_navigating && e.NewItemId is not null)
                Navigate(e.NewItemId);
        };
        _open.Clicked += (_, _) => AcceptFolder();
        _cancel.Clicked += (_, _) => _dialog.Cancel();
        Navigate(directory);
    }

    private void PopulatePlaces()
    {
        var items = new List<UiListItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _placePaths.Clear();

        AddPlace(items, seen, "Home", GetKnownFolder(Environment.SpecialFolder.UserProfile));
        AddPlace(items, seen, "Desktop", GetKnownFolder(Environment.SpecialFolder.DesktopDirectory));
        AddPlace(items, seen, "Documents", GetKnownFolder(Environment.SpecialFolder.MyDocuments));
        AddPlace(items, seen, "Downloads", GetDownloadsFolder());
        AddPlace(items, seen, "Pictures", GetKnownFolder(Environment.SpecialFolder.MyPictures));
        AddPlace(items, seen, "Working folder", Environment.CurrentDirectory);

        foreach (string drive in EnumerateDriveRoots())
            AddPlace(items, seen, DescribeDriveRoot(drive), drive);

        Places.SetItems(items);
    }

    private void AddPlace(List<UiListItem> items, HashSet<string> seen, string label, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        string normalized;
        try
        {
            normalized = Path.GetFullPath(path);
        }
        catch (Exception ex) when (IsFileSystemReadException(ex) || ex is ArgumentException or NotSupportedException)
        {
            return;
        }

        if (!Directory.Exists(normalized) || !seen.Add(NormalizeForComparison(normalized)))
            return;

        string id = "place:" + items.Count.ToString(CultureInfo.InvariantCulture);
        _placePaths[id] = normalized;
        items.Add(new UiListItem(id, label));
    }

    internal bool AcceptFolder()
    {
        if (!Navigate(PathEdit.Text))
            return false;
        return _dialog.Accept(CurrentDirectory);
    }

    protected override bool OnInput(UiInputEvent input)
    {
        if (input.Kind == UiInputEventKind.KeyboardKey && input.KeyTransition == KeyboardKeyTransition.Down &&
            (input.KeyName == "Enter" || input.NativeKeyCode == 0x0D))
        {
            AcceptFolder();
            return true;
        }
        return false;
    }

    internal bool Navigate(string path)
    {
        try
        {
            string fullPath = Path.GetFullPath(path);
            var items = Directory.GetDirectories(fullPath)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .Select(p => new UiListItem(p, Path.GetFileName(p))).ToArray();
            _navigating = true;
            try
            {
                Folders.SetItems(items);
                CurrentDirectory = fullPath;
                PathEdit.Text = fullPath;
                _up.IsEnabled = Directory.GetParent(fullPath) is not null;
                _message.Text = items.Length == 0 ? "No subfolders. Open this folder to view its contents." : "Choose a subfolder, or open the current folder.";
                SyncSelectedPlace();
            }
            finally { _navigating = false; }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _message.Text = "Could not open folder: " + ex.Message;
            return false;
        }
    }

    private void SyncSelectedPlace()
    {
        string current = NormalizeForComparison(CurrentDirectory);
        string? selected = _placePaths
            .Where(pair => current.StartsWith(AddTrailingSeparator(NormalizeForComparison(pair.Value)), StringComparison.OrdinalIgnoreCase) ||
                           StringComparer.OrdinalIgnoreCase.Equals(current, NormalizeForComparison(pair.Value)))
            .OrderByDescending(pair => NormalizeForComparison(pair.Value).Length)
            .Select(static pair => pair.Key)
            .FirstOrDefault();

        Places.SelectedItemId = selected;
    }

    protected override BSize MeasureCore(BSize availableSize)
    {
        foreach (UiElement child in Children)
            child.Measure(availableSize);
        return availableSize;
    }

    protected override void ArrangeCore(BRect rect)
    {
        double width = Math.Max(0, rect.Width);
        double bottom = Math.Max(rect.Top + 40, rect.Bottom - 34);

        _up.Arrange(new BRect(rect.Left, rect.Top, 48, 30));
        PathEdit.Arrange(new BRect(rect.Left + 56, rect.Top, Math.Max(0, width - 112), 30));
        _go.Arrange(new BRect(rect.Right - 48, rect.Top, 48, 30));

        double listTop = rect.Top + 38;
        double headerHeight = 20;
        double placesWidth = Math.Min(190, Math.Max(140, width * 0.28));
        double gap = 8;
        double foldersLeft = rect.Left + placesWidth + gap;
        double foldersWidth = Math.Max(0, width - placesWidth - gap);
        double listHeight = Math.Max(0, bottom - listTop - 36 - headerHeight);

        _placesHeader.Arrange(new BRect(rect.Left, listTop, placesWidth, headerHeight));
        Places.Arrange(new BRect(rect.Left, listTop + headerHeight, placesWidth, listHeight));

        _foldersHeader.Arrange(new BRect(foldersLeft, listTop, foldersWidth, headerHeight));
        Folders.Arrange(new BRect(foldersLeft, listTop + headerHeight, foldersWidth, listHeight));

        _message.Arrange(new BRect(rect.Left, bottom - 28, width, 24));
        _open.Arrange(new BRect(Math.Max(rect.Left, rect.Right - 228), bottom, 130, 30));
        _cancel.Arrange(new BRect(Math.Max(rect.Left, rect.Right - 90), bottom, 90, 30));
    }

    private static bool IsFileSystemReadException(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException;

    private static string? GetKnownFolder(Environment.SpecialFolder folder)
    {
        try
        {
            string path = Environment.GetFolderPath(folder);
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static string? GetDownloadsFolder()
    {
        string? home = GetKnownFolder(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(home) ? null : Path.Combine(home, "Downloads");
    }

    private static string[] EnumerateDriveRoots()
    {
        try
        {
            return Directory.GetLogicalDrives();
        }
        catch (Exception ex) when (IsFileSystemReadException(ex) || ex is NotSupportedException)
        {
            return [];
        }
    }

    private static string DescribeDriveRoot(string path)
    {
        string root = Path.GetPathRoot(path) ?? path;
        if (StringComparer.Ordinal.Equals(root, Path.DirectorySeparatorChar.ToString()))
            return "File system";

        return "Drive " + root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string NormalizeForComparison(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string AddTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
