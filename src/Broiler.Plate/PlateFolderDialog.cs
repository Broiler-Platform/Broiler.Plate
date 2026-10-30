// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using Broiler.Graphics.Geometry;
using Broiler.Input.Keyboard;
using Broiler.UI;
using Broiler.UI.Button.Standard;
using Broiler.UI.Dialog.Standard;
using Broiler.UI.Edit.Standard;
using Broiler.UI.Label.Standard;
using Broiler.UI.ListView;
using Broiler.UI.ListView.Standard;

namespace Broiler.Plate;

/// <summary>A folder picker with an editable path and a browsable list of subfolders.</summary>
internal sealed class PlateFolderDialog : UiElement
{
    private readonly StandardDialog _dialog;
    internal StandardEdit PathEdit { get; } = new();
    internal StandardListView Folders { get; } = new() { ItemHeight = 28 };
    private readonly StandardButton _up = new() { Text = "Up" };
    private readonly StandardButton _go = new() { Text = "Go" };
    private readonly StandardButton _open = new() { Text = "Open folder", IsDefault = true };
    private readonly StandardButton _cancel = new() { Text = "Cancel", IsCancel = true };
    private readonly StandardLabel _message = new();
    private bool _navigating;
    internal string CurrentDirectory { get; private set; } = string.Empty;

    internal PlateFolderDialog(StandardDialog dialog, string directory)
    {
        _dialog = dialog;
        Focusable = true;
        foreach (UiElement child in new UiElement[] { PathEdit, _up, _go, Folders, _message, _open, _cancel })
            AddChild(child);
        _up.Clicked += (_, _) => Navigate(Directory.GetParent(CurrentDirectory)?.FullName ?? CurrentDirectory);
        _go.Clicked += (_, _) => Navigate(PathEdit.Text);
        PathEdit.Submitted += (_, _) => Navigate(PathEdit.Text);
        Folders.SelectionChanged += (_, e) =>
        {
            if (!_navigating && e.NewItemId is not null)
                Navigate(e.NewItemId);
        };
        _open.Clicked += (_, _) => AcceptFolder();
        _cancel.Clicked += (_, _) => _dialog.Cancel();
        Navigate(directory);
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
        Folders.Arrange(new BRect(rect.Left, rect.Top + 40, width, Math.Max(0, bottom - rect.Top - 74)));
        _message.Arrange(new BRect(rect.Left, bottom - 28, width, 24));
        _open.Arrange(new BRect(Math.Max(rect.Left, rect.Right - 228), bottom, 130, 30));
        _cancel.Arrange(new BRect(Math.Max(rect.Left, rect.Right - 90), bottom, 90, 30));
    }
}
