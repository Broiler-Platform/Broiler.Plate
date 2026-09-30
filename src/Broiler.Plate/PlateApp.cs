// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   68
// Annotated:        68/68
// Exempt:           51
// Human-reviewed:   0/68
// IP risk:          Low
// Security risk:    High
// Criteria:         52/9
// Resource impact:  8/10 max
// Unverified:       68
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Broiler.Documents;
using Broiler.Documents.Model;
using Broiler.Graphics;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.RenderList;
using Broiler.Graphics.Resources;
using Broiler.Graphics.Text;
using Broiler.Input.Keyboard;
using Broiler.UI;
using Broiler.UI.Button.Standard;
using Broiler.UI.ComboBox;
using Broiler.UI.ComboBox.Standard;
using Broiler.UI.Dialog;
using Broiler.UI.Dialog.Standard;
using Broiler.UI.FileDialog;
using Broiler.UI.FileDialog.Standard;
using Broiler.UI.ImageView;
using Broiler.UI.ImageView.Standard;
using Broiler.UI.Label;
using Broiler.UI.Label.Standard;
using Broiler.UI.Menu;
using Broiler.UI.Menu.Standard;
using Broiler.UI.RichEdit.Standard;
using Broiler.UI.Standard;
using Broiler.UI.Toolbar;
using Broiler.UI.Toolbar.Standard;
using Broiler.UI.Window.Standard;

namespace Broiler.Plate;

/// <summary>
/// Broiler Plate: a viewer. It opens one file at a time and shows it, and that
/// is the whole of it.
/// </summary>
/// <remarks>
/// <para>
/// The two things it can show are held as two views, one visible at a time,
/// rather than as one view that switches its own content. A rich document and a
/// bitmap have nothing in common at the point of drawing — one lays text out and
/// scrolls, the other scales a rectangle to fit — and the pair of them is easier
/// to reason about than a view with two modes. It is also where the roadmap
/// goes: audio and video are a third and fourth view, not a third mode.
/// </para>
/// <para>
/// The document view is a <see cref="StandardRichEdit"/> with
/// <see cref="StandardRichEdit.IsReadOnly"/> set. That is deliberately the same
/// control the Writer edits in, not a cut-down renderer: a viewer that draws
/// documents through a second implementation is a second implementation to keep
/// correct, and it would drift from the editor it is supposed to agree with.
/// Read-only still selects and copies, which is what a viewer is for.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=A2E9EA
// Broiler-Falsified-If: a hostile file opened through OpenFile ends the viewer with an exception instead of leaving a refused-open line in the status bar
// Broiler-Human:        PENDING
internal sealed class PlateApp : IDisposable
{
    private readonly PlateUiHost _host;
    private readonly Action _requestClose;
    private readonly UiSession _session;
    private readonly StandardWindow _rootWindow;
    private readonly StandardMenu _menu;
    private readonly StandardToolbar _toolbar;
    private readonly StandardLabel _title;
    private readonly StandardLabel _status;
    private readonly StandardRichEdit _documentView;
    private readonly StandardImageView _imageView;
    private readonly PlateContent _content;
    private readonly PlateFileFormats _formats;
    private readonly DocumentCodecCatalog _documentCatalog;
    private readonly UiFileDialogFilter[] _openFilters;

    /// <summary>
    /// The zoom levels the toolbar's picker is currently offering, in its own
    /// order. The list differs between the two views - only a picture can be
    /// fitted to the window - so what a chosen index means is read from here
    /// rather than from the ladder.
    /// </summary>
    private readonly List<double?> _zoomChoices = [];
    private readonly List<(UiMenuItem Item, double? Zoom)> _zoomMenuItems = [];
    private StandardComboBox? _zoomCombo;
    private UiMenuItem? _fitMenuItem;

    /// <summary>
    /// The zoom of each view, kept apart because they are not the same quantity.
    /// A document is read at a percentage of the size it states; a picture is
    /// shown at a percentage of its own pixels, or fitted to the window, which is
    /// no fixed percentage at all. Carrying one number across both would make
    /// opening a photograph change how the next document is read.
    /// </summary>
    private double _documentZoom = PlateZoom.Default;
    private double? _imageZoom;
    private bool _isControlHeld;
    private bool _isSyncingZoomCombo;

    private string? _currentPath;
    private string _lastDirectory = Environment.CurrentDirectory;
    private string _lastAction = "Ready";
    private IReadOnlyList<DocumentDiagnostic> _lastReadDiagnostics = Array.Empty<DocumentDiagnostic>();
    private string _lastReadFileName = "this file";
    private UiMenuItem? _notesMenuItem;
    private UiMenuItem? _closeMenuItem;

    /// <summary>
    /// The image currently on display, and the size it decoded to. The handle is
    /// renderer-owned, so it is released on the way to the next file and again on
    /// dispose; leaking one leaks a GPU texture for as long as the process lives.
    /// </summary>
    private BImageHandle _imageHandle = BImageHandle.Invalid;
    private BSize _imagePixelSize = BSize.Empty;

    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=B27EEA
    // Broiler-Human:        PENDING
    private static readonly BSize FileDialogPreferredSize = new(740, 430);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=473DBB
    // Broiler-Human:        PENDING
    private static readonly BSize NotesDialogPreferredSize = new(560, 320);

    /// <summary>
    /// How much of a file has to be in hand to tell a graphic from a document.
    /// Every signature this recognizes lives in the first twelve bytes; the rest
    /// is slack so the check does not become wrong the moment one is added.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=461D0D
    // Broiler-Falsified-If: a signature that ContentTypeForSignature recognizes needs more leading bytes than SignatureLength, so a graphic of that format is handed to the document codecs first
    // Broiler-Human:        PENDING
    private const int SignatureLength = 32;

    /// <param name="formats">
    /// The formats this viewer offers. Null composes
    /// <see cref="PlateFileFormats.CreateDefault"/> - the RTF, DOCX, HTML and
    /// Markdown set every head carries. A head with more (the Windows head
    /// registers PDF) passes its own set, which is what keeps that codec out of
    /// heads that did not ask for it.
    /// </param>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=4B8E45
    // Broiler-Falsified-If: a PlateApp constructed without formats opens a PDF, although CreateDefault composes only the RTF, DOCX, HTML and Markdown codecs
    // Broiler-Human:        PENDING
    public PlateApp(PlateUiHost host, Action requestClose, PlateFileFormats? formats = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _requestClose = requestClose ?? throw new ArgumentNullException(nameof(requestClose));
        _formats = formats ?? PlateFileFormats.CreateDefault();
        _documentCatalog = _formats.CreateOpenCatalog();
        _openFilters = _formats.CreateOpenFilters();

        _session = new StandardUiSessionBuilder()
            .WithDispatcher(new ImmediateUiDispatcher())
            .Build(_host);

        _documentView = new StandardRichEdit
        {
            IsReadOnly = true,
            PreferredSize = new BSize(760, 520),
            Font = new BFontStyle("Segoe UI", 17),
            Background = PlatePalette.Page,
            BorderColor = PlatePalette.ViewBorder,
            FocusRing = PlatePalette.Accent,
            PaddingX = 18,
            PaddingY = 16,
        };
        _imageView = new StandardImageView
        {
            // The box the picture is drawn in is computed by PlateContent, which
            // is also the box the picture is cropped to fill, so the view scales
            // what it is given into exactly the box it was given. See
            // PlateContent.ArrangeImage for why the box is never the larger of
            // the two.
            Stretch = UiImageStretch.Fill,
            PreferredSize = new BSize(760, 520),
            PlaceholderBackground = PlatePalette.ImageMat,
            PlaceholderBorder = PlatePalette.ViewBorder,
            Visibility = UiVisibility.Collapsed,
        };

        _menu = CreateMenu();
        _toolbar = CreateToolbar();
        _title = new StandardLabel
        {
            Text = "No file open",
            Font = new BFontStyle("Segoe UI", 20, BFontWeight.SemiBold),
            Foreground = PlatePalette.Title,
        };
        _status = new StandardLabel
        {
            Text = "Ready",
            Font = new BFontStyle("Segoe UI", 13),
            Foreground = PlatePalette.Muted,
            Trimming = UiTextTrimming.CharacterEllipsis,
        };

        _rootWindow = new StandardWindow
        {
            Title = "Broiler Plate",
            Background = PlatePalette.Canvas,
            BorderColor = PlatePalette.WindowBorder,
            ActiveBorderColor = PlatePalette.Accent,
            BorderThickness = 1,
        };
        _content = new PlateContent(_menu, _toolbar, _title, _documentView, _imageView, _status);
        _rootWindow.AddChild(_content);

        SeedWelcome();
        SyncZoomChoices();
        _session.AddRoot(_rootWindow);
        _session.SetFocus(_documentView);

        _documentView.SelectionChanged += (_, _) => RefreshUi();
        _menu.ItemInvoked += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Item.CommandName))
                RefreshUi();
        };

        RefreshUi();
    }

    public UiSession Session => _session;

    internal StandardMenu Menu => _menu;

    /// <summary>The formats this viewer was composed with.</summary>
    internal PlateFileFormats Formats => _formats;

    /// <summary>The most recent status-bar line, which is where a refused open is reported.</summary>
    internal string LastAction => _lastAction;

    /// <summary>
    /// The diagnostics of the most recent document read, kept so a file that
    /// opens unexpectedly blank can be explained rather than guessed at.
    /// </summary>
    internal IReadOnlyList<DocumentDiagnostic> LastReadDiagnostics => _lastReadDiagnostics;

    /// <summary>What is on display, for tests and for the status line.</summary>
    internal PlateViewKind CurrentView => _content.CurrentView;

    /// <summary>The toolbar, so a test can check that nothing on it is drawn past its edge.</summary>
    internal StandardToolbar Toolbar => _toolbar;

    /// <summary>
    /// The zoom of the view on display, or null when a picture is being fitted to
    /// the window. This is what the toolbar's picker and the status line report.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=84B6FB
    // Broiler-Falsified-If: while a picture is on display the property reports the document's zoom, or a number while the picture is fitted to the window
    // Broiler-Human:        PENDING
    internal double? Zoom => _content.CurrentView == PlateViewKind.Image ? _imageZoom : _documentZoom;

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=7; Fingerprint=AFD3F6
    // Broiler-Human:        PENDING
    public BRenderList RenderFrame() => _session.RenderFrame();

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=8; Fingerprint=B69859
    // Broiler-Falsified-If: a key press answered as Ctrl+O or as a zoom shortcut is also delivered to the session, so the document view acts on the same key
    // Broiler-Human:        PENDING
    public void Dispatch(UiInputEvent input)
    {
        TrackModifiers(input);
        if (HandleOpenShortcut(input))
        {
            _host.RequestInvalidate();
            return;
        }

        if (HandleZoomShortcut(input))
        {
            _host.RequestInvalidate();
            return;
        }

        if (_session.DispatchInput(input))
            _host.RequestInvalidate();
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=2C3B2F
    // Broiler-Human:        PENDING
    public void Invalidate() => _host.RequestInvalidate();

    /// <summary>
    /// Opens <paramref name="path"/>, deciding for itself whether it is a
    /// document or a graphic. This is the whole of the first version's feature
    /// set, and the file dialog is one caller of it rather than the only way in -
    /// a command line argument and a drop target are the same call.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=C3CEFC
    // Broiler-Falsified-If: a file whose leading bytes carry a PNG, JPEG, GIF, BMP, TIFF or WebP signature is handed to a document codec before the image decoder sees it
    // Broiler-Human:        PENDING
    public bool OpenFile(string path)
    {
        try
        {
            string fullPath = Path.GetFullPath(path);

            // Read only the header first. Which of the two views a file belongs
            // in is answered by its leading bytes, and answering it before the
            // file is read means a 200MB photo is never fed to a document codec
            // and a document is never handed to the image decoder.
            byte[] signature = ReadSignature(fullPath, out bool isEmpty);
            if (isEmpty)
            {
                _lastAction = "Could not open " + Path.GetFileName(fullPath) + ": the file is empty.";
                RefreshUi();
                return false;
            }

            if (PlateImageFormats.ContentTypeForSignature(signature) is not null)
                return OpenImage(fullPath);

            if (OpenDocument(fullPath))
                return true;

            // The bytes identified nothing and no codec claimed it. A name is
            // weaker evidence than a magic number, which is why it is consulted
            // last, but a renderer may well decode a format this build has no
            // signature for - so a file named like a graphic still gets its try.
            if (PlateImageFormats.HasImageExtension(fullPath))
                return OpenImage(fullPath);

            return false;
        }
        catch (Exception ex) when (IsFileOperationException(ex))
        {
            _lastAction = "Open failed: " + ex.Message;
            RefreshUi();
            return false;
        }
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=5602D0
    // Broiler-Falsified-If: a picture's renderer handle is still held after Dispose returns
    // Broiler-Human:        PENDING
    public void Dispose()
    {
        ReleaseImage();
        _session.Dispose();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=2; Fingerprint=CD50D5
    // Broiler-Falsified-If: the Close command can run while no file is open, or the Notes command while the last read recorded no diagnostics
    // Broiler-Human:        PENDING
    private StandardMenu CreateMenu()
    {
        var dispatcher = new StandardCommandDispatcher();
        dispatcher.Add(new StandardCommand("file.open", ShowOpenDialog));
        dispatcher.Add(new StandardCommand("file.close", CloseFile, () => _currentPath is not null));
        dispatcher.Add(new StandardCommand("file.exit", _requestClose));
        dispatcher.Add(new StandardCommand("view.zoom.in", () => StepZoom(PlateZoomStep.In)));
        dispatcher.Add(new StandardCommand("view.zoom.out", () => StepZoom(PlateZoomStep.Out)));
        dispatcher.Add(new StandardCommand("view.zoom.reset", () => StepZoom(PlateZoomStep.Reset)));
        AddZoomLevelCommands(dispatcher);
        dispatcher.Add(new StandardCommand("help.notes", ShowNotes, () => _lastReadDiagnostics.Count > 0));
        dispatcher.Add(new StandardCommand("help.about", ShowAbout));

        var file = new UiMenuItem("file", "File") { AccessKey = 'F' };
        file.Children.Add(new UiMenuItem("open", "Open...") { CommandName = "file.open", AccessKey = 'O' });
        _closeMenuItem = new UiMenuItem("close", "Close") { CommandName = "file.close", AccessKey = 'C' };
        file.Children.Add(_closeMenuItem);
        file.Children.Add(new UiMenuItem("exit", "Exit") { CommandName = "file.exit", AccessKey = 'X' });

        var view = new UiMenuItem("view", "View") { AccessKey = 'V' };
        view.Children.Add(new UiMenuItem("zoom-in", "Zoom in") { CommandName = "view.zoom.in", AccessKey = 'I' });
        view.Children.Add(new UiMenuItem("zoom-out", "Zoom out") { CommandName = "view.zoom.out", AccessKey = 'O' });
        view.Children.Add(new UiMenuItem("zoom-reset", "Actual size") { CommandName = "view.zoom.reset", AccessKey = 'A' });

        // Only a picture can be fitted: a document's own size is stated in the
        // document, and a window is not a reason to restate it.
        _fitMenuItem = new UiMenuItem("zoom-fit", "Fit to window")
        {
            CommandName = ZoomCommandName(null),
            AccessKey = 'F',
            IsCheckable = true,
        };
        _zoomMenuItems.Add((_fitMenuItem, null));
        view.Children.Add(_fitMenuItem);
        view.Children.Add(CreateZoomMenu());

        var help = new UiMenuItem("help", "Help") { AccessKey = 'H' };
        _notesMenuItem = new UiMenuItem("notes", "Notes for this file...")
        {
            CommandName = "help.notes",
            AccessKey = 'N',
        };
        help.Children.Add(_notesMenuItem);
        help.Children.Add(new UiMenuItem("about", "About Broiler Plate") { CommandName = "help.about", AccessKey = 'A' });

        var menu = new StandardMenu
        {
            PresentationMode = UiMenuPresentationMode.MenuBar,
            PreferredSize = new BSize(360, 30),
            MenuBarHeight = 30,
            ItemHeight = 28,
            PopupWidth = 210,
            Font = new BFontStyle("Segoe UI", 14),
            Background = PlatePalette.MenuSurface,
            PopupBackground = PlatePalette.MenuPopup,
            Foreground = PlatePalette.Title,
            BorderColor = PlatePalette.ViewBorder,
            SelectedBackground = PlatePalette.MenuSelected,
            CommandDispatcher = dispatcher,
        };
        menu.SetItems([file, view, help]);
        return menu;
    }

    /// <summary>
    /// One command per level on the ladder, named after the percentage it
    /// selects, and one for fitting a picture to the window. The menu and the
    /// toolbar both go through these rather than setting the zoom themselves, so
    /// every way of choosing 150% is the same way.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=2E61BA
    // Broiler-Falsified-If: the fit command can run while a document is on display
    // Broiler-Human:        PENDING
    private void AddZoomLevelCommands(StandardCommandDispatcher dispatcher)
    {
        dispatcher.Add(new StandardCommand(
            ZoomCommandName(null),
            () => ApplyZoom(null),
            () => _content.CurrentView == PlateViewKind.Image));

        foreach (double level in PlateZoom.Levels)
        {
            double zoom = level;
            dispatcher.Add(new StandardCommand(ZoomCommandName(zoom), () => ApplyZoom(zoom)));
        }
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=3334B5
    // Broiler-Falsified-If: two ladder levels map to the same command name, so choosing one of them runs the other
    // Broiler-Human:        PENDING
    private static string ZoomCommandName(double? zoom) =>
        zoom is double scale
            ? "view.zoom." + Math.Round(scale * 100).ToString("0", CultureInfo.InvariantCulture)
            : "view.zoom.fit";

    /// <summary>
    /// The ladder as a checkable submenu. It offers the levels and says which one
    /// the file is being shown at, which is the half of it a menu can do that
    /// Zoom in and Zoom out cannot. Fit is not among them - it is not a level -
    /// and while a picture is fitted, none of these is checked, which is true.
    /// </summary>
    /// <remarks>
    /// Nothing here is checked as it is built. Which one is depends on the view
    /// on display, which does not exist yet while the menu is being composed, and
    /// <see cref="RefreshUi"/> - which the constructor ends with, and which every
    /// zoom change goes through - is the one place that answers it.
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=349A26
    // Broiler-Falsified-If: a ladder level's menu item names a command other than the one ZoomCommandName gives that level
    // Broiler-Human:        PENDING
    private UiMenuItem CreateZoomMenu()
    {
        var zoom = new UiMenuItem("zoom", "Zoom") { AccessKey = 'Z' };
        foreach (double level in PlateZoom.Levels)
        {
            var item = new UiMenuItem(
                "zoom-" + Math.Round(level * 100).ToString("0", CultureInfo.InvariantCulture),
                PlateZoom.Describe(level))
            {
                CommandName = ZoomCommandName(level),
                IsCheckable = true,
            };
            _zoomMenuItems.Add((item, level));
            zoom.Children.Add(item);
        }

        return zoom;
    }

    /// <summary>
    /// The toolbar: the one command a viewer has, and the controls for how big
    /// what it opened is drawn. Everything else the shell can do is a menu item,
    /// because a bar of buttons that are mostly unavailable is a bar that has to
    /// be read before it can be used.
    /// </summary>
    /// <remarks>
    /// The overflow mode is left at its default, so a window too narrow for these
    /// four moves what does not fit behind the chevron rather than off the edge.
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=8D70FD
    // Broiler-Falsified-If: the picker placed on the toolbar is not the one _zoomCombo holds, so zoom changes never reach it
    // Broiler-Human:        PENDING
    private StandardToolbar CreateToolbar()
    {
        var toolbar = new StandardToolbar
        {
            Title = "Viewer toolbar",
            PreferredSize = new BSize(0, 42),
            Orientation = UiToolbarOrientation.Horizontal,
            Padding = 5,
            Spacing = 4,
            Background = PlatePalette.ToolbarSurface,
            BorderColor = PlatePalette.MenuRule,
            SeparatorColor = PlatePalette.MenuRule,
            CornerRadius = 0,
            Foreground = PlatePalette.Title,
            PopupBackground = PlatePalette.MenuPopup,
            Font = new BFontStyle("Segoe UI", 15),
        };

        StandardButton openButton = ToolbarAction("Open", 56, ShowOpenDialog);
        StandardButton zoomOutButton = ToolbarAction("-", 30, () => StepZoom(PlateZoomStep.Out));
        StandardButton zoomInButton = ToolbarAction("+", 30, () => StepZoom(PlateZoomStep.In));
        StandardComboBox zoomCombo = CreateZoomCombo();
        _zoomCombo = zoomCombo;

        toolbar.AddChild(openButton);
        toolbar.AddChild(zoomOutButton);
        toolbar.AddChild(zoomCombo);
        toolbar.AddChild(zoomInButton);
        toolbar.SetSeparatorBefore(zoomOutButton, true);
        return toolbar;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=A2DD22
    // Broiler-Falsified-If: a toolbar button's action runs without the status line and zoom picker being refreshed afterwards
    // Broiler-Human:        PENDING
    private StandardButton ToolbarAction(string text, double width, Action action)
    {
        var button = new StandardButton
        {
            Text = text,
            PreferredSize = new BSize(width, 30),
            Font = new BFontStyle("Segoe UI", 13),
            PaddingX = 8,
            PaddingY = 5,
            Background = PlatePalette.ToolbarButton,
            Foreground = PlatePalette.Title,
            BorderColor = PlatePalette.ToolbarButtonBorder,
            DisabledForeground = PlatePalette.Muted,
            SecondaryHoverBackground = PlatePalette.ToolbarButtonHover,
            SecondaryPressedBackground = PlatePalette.ToolbarButtonPressed,
            FocusRing = PlatePalette.Accent,
            CornerRadius = 5,
        };
        button.Clicked += (_, _) =>
        {
            action();
            RefreshUi();
        };
        return button;
    }

    /// <summary>
    /// The toolbar's zoom picker. It shows the size the file is being shown at
    /// and drops down the whole ladder, which is the one control that answers
    /// "what am I looking at" without opening a menu.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=F9113F
    // Broiler-Falsified-If: a selection index of -1 or one past the offered list reads _zoomChoices out of range instead of being ignored
    // Broiler-Human:        PENDING
    private StandardComboBox CreateZoomCombo()
    {
        var combo = new StandardComboBox
        {
            PreferredSize = new BSize(74, 30),
            MaxDropDownItems = PlateZoom.Levels.Count + 1,
            ItemHeight = 26,
            Font = new BFontStyle("Segoe UI", 13),
            Background = PlatePalette.ToolbarButton,
            Foreground = PlatePalette.Title,
            BorderColor = PlatePalette.ToolbarButtonBorder,
            PopupBackground = PlatePalette.MenuPopup,
            SelectedBackground = PlatePalette.MenuSelected,
            FocusRing = PlatePalette.Accent,
            CornerRadius = 5,
        };

        // A selection this application made - refilling the list for the other
        // view, or driving the picker back from the zoom - is not a choice the
        // user made, and answering it would report a zoom nobody asked for.
        combo.SelectionChanged += (_, e) =>
        {
            if (_isSyncingZoomCombo || (uint)e.NewIndex >= (uint)_zoomChoices.Count)
                return;

            double? choice = _zoomChoices[e.NewIndex];
            if (!PlateZoom.SameChoice(choice, Zoom))
                ApplyZoom(choice);
        };
        return combo;
    }

    /// <summary>
    /// Fills the picker with the levels the view on display can be shown at. Only
    /// a picture is offered Fit, so the list is rebuilt when the view changes
    /// rather than carrying an entry that would do nothing.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=A0CB76
    // Broiler-Falsified-If: the picker offers Fit while a document is on display, or refilling it applies a zoom
    // Broiler-Human:        PENDING
    private void SyncZoomChoices()
    {
        if (_zoomCombo is null)
            return;

        bool offersFit = _content.CurrentView == PlateViewKind.Image;
        _zoomChoices.Clear();
        var items = new List<UiComboBoxItem>(PlateZoom.Levels.Count + 1);
        if (offersFit)
        {
            _zoomChoices.Add(null);
            items.Add(new UiComboBoxItem(ZoomCommandName(null), PlateZoom.FitName));
        }

        foreach (double level in PlateZoom.Levels)
        {
            _zoomChoices.Add(level);
            items.Add(new UiComboBoxItem(ZoomCommandName(level), PlateZoom.Describe(level)));
        }

        _isSyncingZoomCombo = true;
        try
        {
            _zoomCombo.SetItems(items);
        }
        finally
        {
            _isSyncingZoomCombo = false;
        }
    }

    /// <summary>
    /// Shows the file at <paramref name="zoom"/>, or fits it to the window when
    /// that is null. The view on display is where the number lives; the menu, the
    /// picker and the status line are told from here, so none of them can
    /// disagree with what is on screen.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=4; Fingerprint=30CDCB
    // Broiler-Falsified-If: a non-finite or off-ladder zoom reaches the document view or the picture without being clamped to the ladder
    // Broiler-Human:        PENDING
    private void ApplyZoom(double? zoom)
    {
        double? previous = Zoom;
        double? resolved;
        if (_content.CurrentView == PlateViewKind.Image)
        {
            resolved = zoom is double scale ? PlateZoom.Normalize(scale) : null;
            _imageZoom = resolved;
            _content.ImageZoom = resolved;
        }
        else
        {
            // A document has no fit: it states its own size, and the ladder is
            // read against that. Fit can only arrive here from a command that
            // outlived the view it was meant for, and it reads as actual size.
            double scale = PlateZoom.Normalize(zoom ?? PlateZoom.Default);
            resolved = scale;
            _documentZoom = scale;
            _documentView.Zoom = scale;
        }

        // A step that changed nothing - the top of the ladder, or the level
        // already chosen - says so rather than reporting a zoom that did not
        // happen.
        _lastAction = (PlateZoom.SameChoice(resolved, previous) ? "Zoom already " : "Zoom ") +
            PlateZoom.Describe(resolved);
        RefreshUi();
    }

    /// <summary>
    /// Steps the zoom of the view on display. A picture that is fitted steps from
    /// the size it is actually being shown at rather than from 100%: one step in
    /// from a photograph filling the window should be a little larger than the
    /// window, not four times the size of one.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=4; Fingerprint=D0CC0C
    // Broiler-Falsified-If: a step in from a fitted picture starts from 100% instead of the scale the picture is fitted at
    // Broiler-Human:        PENDING
    private void StepZoom(PlateZoomStep step)
    {
        if (step == PlateZoomStep.Reset)
        {
            ApplyZoom(PlateZoom.Default);
            return;
        }

        double from = Zoom ?? _content.FitScale;
        ApplyZoom(PlateZoom.Apply(from, step));
    }

    /// <summary>
    /// Remembers whether Ctrl is down. A wheel notch is where that has to be
    /// known and not every head fills a pointer event's modifiers in, but every
    /// head delivers the key events that raise and drop the flag.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=CB5AE1
    // Broiler-Falsified-If: after Ctrl is pressed and released in the window, a plain wheel notch still zooms because the release left the flag set
    // Broiler-Human:        PENDING
    private void TrackModifiers(UiInputEvent input)
    {
        if (input.Kind == UiInputEventKind.KeyboardKey)
            _isControlHeld = (input.KeyModifiers & KeyboardModifierState.Control) != KeyboardModifierState.None;
    }

    /// <summary>
    /// The zoom gestures: Ctrl with plus, minus or zero, and Ctrl with the wheel.
    /// They are answered before the session sees them, or Ctrl and the wheel
    /// would scroll the document it was asked to resize.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=6AC0B5
    // Broiler-Falsified-If: a wheel notch with Ctrl neither on the event nor tracked from key events changes the zoom
    // Broiler-Human:        PENDING
    private bool HandleZoomShortcut(UiInputEvent input)
    {
        PlateZoomStep step = input.Kind switch
        {
            UiInputEventKind.KeyboardKey => PlateZoom.StepFor(
                input.KeyName,
                input.NativeKeyCode,
                input.KeyModifiers,
                input.KeyTransition == KeyboardKeyTransition.Down),
            UiInputEventKind.PointerWheel => PlateZoom.StepForWheel(
                _isControlHeld || (input.KeyModifiers & KeyboardModifierState.Control) != KeyboardModifierState.None,
                input.WheelDeltaNotches),
            _ => PlateZoomStep.None,
        };

        if (step == PlateZoomStep.None)
            return false;

        StepZoom(step);
        return true;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=4; Fingerprint=AF9E20
    // Broiler-Falsified-If: a cancelled dialog, or one accepted with an empty or whitespace name, still calls OpenFile
    // Broiler-Human:        PENDING
    private void ShowOpenDialog()
    {
        var dialog = new StandardFileDialog
        {
            Mode = UiFileDialogMode.Open,
            CurrentDirectory = GetDialogDirectory(),
            FileName = _currentPath is null ? string.Empty : Path.GetFileName(_currentPath),
            PreferredSize = FileDialogPreferredSize,
        };
        dialog.SetFileTypeFilters(_openFilters);
        dialog.ResultCompleted += (_, e) =>
        {
            if (e.Result.Kind == UiDialogResultKind.Accepted && !string.IsNullOrWhiteSpace(e.Result.Value))
                OpenFile(e.Result.Value);
        };

        dialog.ShowOpenModal(_rootWindow, GetDialogPlacement());
        _lastAction = "Open file";
        RefreshUi();
    }

    /// <summary>
    /// Reads <paramref name="fullPath"/> as a document and shows it, or reports
    /// why it could not and leaves what is on display alone.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=91DBBF
    // Broiler-Falsified-If: a document the codecs' default read limits refuse is read whole into memory first, because it reaches SelectAndRead other than as the open file stream
    // Broiler-Human:        PENDING
    private bool OpenDocument(string fullPath)
    {
        // Streamed rather than File.ReadAllBytes: the read's own limits decide how
        // much of a file is allowed into memory, so an oversized document is
        // refused instead of being materialized and then measured.
        using FileStream file = File.OpenRead(fullPath);
        using DocumentInput input = DocumentInput.FromStream(file);
        DocumentCodecSelection selection = _documentCatalog.SelectAndRead(
            input,
            hints: new DocumentSourceHints(fileName: fullPath));

        _lastReadDiagnostics = selection.Result.Diagnostics;
        _lastReadFileName = Path.GetFileName(fullPath);

        if (!MayReplaceView(selection.Result))
        {
            _lastAction = DescribeRefusedOpen(Path.GetFileName(fullPath), selection);
            RefreshUi();
            return false;
        }

        ReleaseImage();
        _documentView.Document = selection.Result.Document;
        _documentView.Selection = RichTextRange.Caret(RichTextDocument.Start);
        ShowView(PlateViewKind.Document);
        AdoptPath(fullPath);
        _lastAction = DescribeOpen(Path.GetFileName(fullPath), selection.Result);
        _session.SetFocus(_documentView);
        RefreshUi();
        return true;
    }

    /// <summary>
    /// Uploads <paramref name="fullPath"/> to the renderer and shows it. What can
    /// actually be decoded is the head's codec catalog, not this method, so an
    /// upload that fails is reported as the decoder's refusal rather than as a
    /// missing file.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=889289
    // Broiler-Falsified-If: a file of several hundred megabytes that carries an image signature is read whole into memory by File.ReadAllBytes before any decoder limit applies
    // Broiler-Human:        PENDING
    private bool OpenImage(string fullPath)
    {
        byte[] bytes = File.ReadAllBytes(fullPath);
        BImageHandle handle = _host.CreateImage(bytes);
        if (!handle.IsValid)
        {
            _lastAction = "Could not open " + Path.GetFileName(fullPath) +
                ": no installed image codec decoded it. What is on display is unchanged.";
            RefreshUi();
            return false;
        }

        // Only now is the old one released. Releasing before the upload would
        // blank the view on the way to a failure that leaves nothing to show.
        ReleaseImage();
        _imageHandle = handle;
        _imagePixelSize = handle.PixelSize.IsEmpty
            ? PlateImageFormats.MeasureDisplaySize(bytes, 0)
            : handle.PixelSize;
        _imageView.Image = handle;
        _imageView.AltText = Path.GetFileNameWithoutExtension(fullPath);

        // Every picture arrives fitted. A document states the size it wants to be
        // read at and the ladder is read against that, so a zoom carries from one
        // document to the next; a picture states pixels, and the last picture's
        // 300% says nothing about whether this one fits on the screen.
        _imageZoom = null;
        _content.ImageZoom = null;
        _content.ImagePixelSize = _imagePixelSize;
        ShowView(PlateViewKind.Image);

        // A document left in the editor behind a picture is a document still held
        // in memory, and its notes would still answer the Help menu.
        _documentView.SetPlainText(string.Empty);
        _lastReadDiagnostics = Array.Empty<DocumentDiagnostic>();
        _lastReadFileName = Path.GetFileName(fullPath);

        AdoptPath(fullPath);
        _lastAction = "Opened " + Path.GetFileName(fullPath);
        RefreshUi();
        return true;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=2; Fingerprint=197A75
    // Broiler-Falsified-If: after Close, Help > Notes still offers the closed file's diagnostics
    // Broiler-Human:        PENDING
    private void CloseFile()
    {
        ReleaseImage();
        _imageView.Image = BImageHandle.Invalid;
        _imageView.AltText = string.Empty;
        _currentPath = null;
        _lastReadDiagnostics = Array.Empty<DocumentDiagnostic>();
        _lastReadFileName = "this file";
        _title.Text = "No file open";
        SeedWelcome();
        _imageZoom = null;
        _content.ImageZoom = null;
        ShowView(PlateViewKind.Document);
        _lastAction = "Closed";
        _session.SetFocus(_documentView);
        RefreshUi();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=38B249
    // Broiler-Falsified-If: the notes dialog shows one file's diagnostics under another file's name
    // Broiler-Human:        PENDING
    private void ShowNotes()
    {
        if (_lastReadDiagnostics.Count == 0)
        {
            _lastAction = "No notes for this file";
            RefreshUi();
            return;
        }

        StandardDialog dialog = PlateNotes.CreateDialog(_lastReadFileName, _lastReadDiagnostics);
        dialog.ShowModal(_rootWindow, GetNotesPlacement());
        _lastAction = "Notes for " + _lastReadFileName;
        RefreshUi();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=2; Fingerprint=E202DF
    // Broiler-Falsified-If: the About dialog reports the metadata of the hosting executable instead of the Broiler.Plate.Core assembly
    // Broiler-Human:        PENDING
    private void ShowAbout()
    {
        _session.SetFocus(_content.CurrentView == PlateViewKind.Image ? _imageView : _documentView);
        PlateAbout.Show(_rootWindow, _host.ViewportSize, typeof(PlateApp).Assembly);
    }

    /// <summary>
    /// What the viewer says before it has been given anything to show. It goes
    /// into the document view rather than a separate empty-state element so that
    /// there is one fewer thing on screen to lay out and keep in step.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=E10D4C
    // Broiler-Falsified-If: the caret is left anywhere other than the start of the welcome text
    // Broiler-Human:        PENDING
    private void SeedWelcome()
    {
        _documentView.SetPlainText(
            "Broiler Plate\n" +
            "A viewer for the formats the Broiler platform reads.\n" +
            "Choose File > Open, or press Ctrl+O, to open a document or a graphic. " +
            "Documents are drawn through Broiler.Documents and Broiler.UI; graphics are decoded by " +
            "the image codecs this build was composed with.");
        _documentView.Selection = RichTextRange.Caret(RichTextDocument.Start);
    }

    /// <summary>Ctrl+O, which no view owns, so nothing else will answer it.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=2CDB6B
    // Broiler-Falsified-If: AltGr+O, which the Windows head reports as Ctrl+Alt+O and the Polish programmer layout uses to type an accented o, opens the file dialog
    // Broiler-Human:        PENDING
    private bool HandleOpenShortcut(UiInputEvent input)
    {
        if (input.Kind != UiInputEventKind.KeyboardKey ||
            input.KeyTransition != KeyboardKeyTransition.Down ||
            (input.KeyModifiers & KeyboardModifierState.Control) == KeyboardModifierState.None)
        {
            return false;
        }

        if (!IsKey(input, VirtualKeyO, "O"))
            return false;

        ShowOpenDialog();
        return true;
    }

    /// <summary>The Win32 virtual-key code for O, which the browser reports as keyCode too.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=A53D55
    // Broiler-Falsified-If: VirtualKeyO is not the Win32 virtual-key code 0x4F for O, so Ctrl+O reported only by native code does not open the dialog
    // Broiler-Human:        PENDING
    private const int VirtualKeyO = 0x4F;

    /// <summary>
    /// Whether an event is the named key, asked the three ways the heads answer
    /// it. A head may fill in the native code, the key's name, or neither but a
    /// <c>VirtualKey:</c> name built from the code, and a shortcut that checks
    /// only one of them works on some heads and silently does nothing on the
    /// rest. This is the same test <c>StandardRichEdit</c> and the Writer's zoom
    /// ladder apply.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=85A462
    // Broiler-Falsified-If: an event whose native code and name both belong to another key, such as code 0x50 named P, is matched as O
    // Broiler-Human:        PENDING
    private static bool IsKey(UiInputEvent input, int virtualKey, string name) =>
        input.NativeKeyCode == virtualKey ||
        string.Equals(
            input.KeyName,
            "VirtualKey:" + virtualKey.ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal) ||
        string.Equals(input.KeyName, name, StringComparison.OrdinalIgnoreCase);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=C3570F
    // Broiler-Falsified-If: opening a file at a drive root clears the remembered directory instead of keeping the previous one
    // Broiler-Human:        PENDING
    private void AdoptPath(string fullPath)
    {
        _currentPath = fullPath;
        _lastDirectory = Path.GetDirectoryName(fullPath) ?? _lastDirectory;
        _title.Text = Path.GetFileName(fullPath);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=6E830D
    // Broiler-Falsified-If: a picture's handle is released while the image view still references it, or released twice
    // Broiler-Human:        PENDING
    private void ReleaseImage()
    {
        if (!_imageHandle.IsValid)
            return;

        _imageView.Image = BImageHandle.Invalid;
        _host.ReleaseImage(_imageHandle);
        _imageHandle = BImageHandle.Invalid;
        _imagePixelSize = BSize.Empty;
        _content.ImagePixelSize = BSize.Empty;
    }

    /// <summary>
    /// Switches views and refills the zoom picker, which is not the same list for
    /// both of them. Every switch goes through here, so the picker cannot be left
    /// offering Fit to a document.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=9B66D4
    // Broiler-Falsified-If: after a switch to the document view the zoom picker still offers Fit
    // Broiler-Human:        PENDING
    private void ShowView(PlateViewKind view)
    {
        _content.Show(view);
        SyncZoomChoices();
    }

    /// <summary>
    /// The first <see cref="SignatureLength"/> bytes of a file, for the format
    /// check. <paramref name="isEmpty"/> separates a zero-byte file - which no
    /// codec can be blamed for refusing - from a short one that is simply
    /// shorter than the buffer.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=59B600
    // Broiler-Falsified-If: a file shorter than SignatureLength yields bytes past those read, such as trailing zeros that complete a signature
    // Broiler-Human:        PENDING
    private static byte[] ReadSignature(string fullPath, out bool isEmpty)
    {
        using FileStream file = File.OpenRead(fullPath);
        var buffer = new byte[SignatureLength];
        int read = file.ReadAtLeast(buffer, SignatureLength, throwOnEndOfStream: false);
        isEmpty = read == 0;
        return read == SignatureLength ? buffer : buffer[..read];
    }

    /// <summary>
    /// Whether a read may replace what is on display.
    /// </summary>
    /// <remarks>
    /// A <see cref="DocumentResultStatus.Rejected"/> read carries a placeholder
    /// rather than content, and showing it would throw away what the user was
    /// looking at in exchange for nothing. So would a read that recovered no text
    /// at all and knows it is incomplete - a scanned PDF with no text layer is
    /// the shape that produces it - where "empty" is a report about the reader,
    /// not about the file. A read that produced text is shown even when it is
    /// <see cref="DocumentResultStatus.Partial"/>; the status line says so rather
    /// than passing it off as a clean open.
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=D03956
    // Broiler-Falsified-If: a Rejected read, or a Partial read that recovered no text, replaces what is on display
    // Broiler-Human:        PENDING
    internal static bool MayReplaceView(DocumentReadResult result) =>
        result.IsUsable &&
        (result.Document.PlainText.Length > 0 || result.Status == DocumentResultStatus.Success);

    /// <summary>Why a read was refused, for the status bar.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=27F65B
    // Broiler-Falsified-If: a refused read that carried a warning or error diagnostic is reported without that diagnostic's message
    // Broiler-Human:        PENDING
    internal static string DescribeRefusedOpen(string fileName, DocumentCodecSelection selection)
    {
        string reason = FirstProblem(selection.Result) ?? (selection.Codec is null
            ? "no registered format recognized it"
            : "the " + selection.Codec.Name + " reader recovered no content from it");

        return "Could not open " + fileName + ": " + reason.TrimEnd('.') +
            ". What is on display is unchanged.";
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=E58F0A
    // Broiler-Falsified-If: a Partial read is reported as a plain Opened line with no mention that parts were skipped or approximated
    // Broiler-Human:        PENDING
    internal static string DescribeOpen(string fileName, DocumentReadResult result)
    {
        string text = "Opened " + fileName;
        if (result.Document.PlainText.Length == 0)
            text += " (no readable content)";

        int notes = 0;
        foreach (DocumentDiagnostic diagnostic in result.Diagnostics)
        {
            if (diagnostic.Severity != DocumentDiagnosticSeverity.Info)
                notes++;
        }

        if (notes > 0)
            text += " with " + notes.ToString(CultureInfo.InvariantCulture) + " note(s)";

        // A partial read is a different outcome from a clean one, not a clean one
        // with footnotes, so it is never reported as undifferentiated success.
        return result.Status == DocumentResultStatus.Partial
            ? text + "; parts of it were skipped or approximated"
            : text;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=5AC7DD
    // Broiler-Falsified-If: an Info diagnostic is returned as the problem ahead of a later warning or error
    // Broiler-Human:        PENDING
    private static string? FirstProblem(DocumentReadResult result)
    {
        foreach (DocumentDiagnostic diagnostic in result.Diagnostics)
        {
            if (diagnostic.Severity != DocumentDiagnosticSeverity.Info)
                return diagnostic.Message;
        }

        return null;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=58C699
    // Broiler-Falsified-If: an exception a hostile file provokes while opening, such as OverflowException from a header field, is not matched and escapes OpenFile
    // Broiler-Human:        PENDING
    private static bool IsFileOperationException(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=5DE95E
    // Broiler-Human:        PENDING
    private BRect GetDialogPlacement() => CenterInViewport(FileDialogPreferredSize, minTop: 42);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=ACD293
    // Broiler-Human:        PENDING
    private BRect GetNotesPlacement() => CenterInViewport(NotesDialogPreferredSize, minTop: 72);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=957C37
    // Broiler-Falsified-If: a viewport smaller than the preferred size gives the dialog a negative origin or a size larger than the preferred one
    // Broiler-Human:        PENDING
    private BRect CenterInViewport(BSize preferred, double minTop)
    {
        BSize viewport = _host.ViewportSize;
        double x = Math.Max(12, (viewport.Width - preferred.Width) / 2);
        double y = Math.Max(minTop, (viewport.Height - preferred.Height) / 2);
        return new BRect(
            x,
            y,
            Math.Min(preferred.Width, Math.Max(280, viewport.Width - 24)),
            Math.Min(preferred.Height, Math.Max(180, viewport.Height - (minTop + 22))));
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=698B87
    // Broiler-Falsified-If: a directory that no longer exists is handed to the file dialog as its starting directory
    // Broiler-Human:        PENDING
    private string GetDialogDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_currentPath))
        {
            string? directory = Path.GetDirectoryName(_currentPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                return directory;
        }

        return Directory.Exists(_lastDirectory) ? _lastDirectory : Environment.CurrentDirectory;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=4; Fingerprint=F21A7A
    // Broiler-Falsified-If: selecting the picker entry for the current zoom here is answered by the picker's handler as a user choice and applies a zoom
    // Broiler-Human:        PENDING
    private void RefreshUi()
    {
        if (_closeMenuItem is not null)
            _closeMenuItem.IsEnabled = _currentPath is not null;
        if (_notesMenuItem is not null)
            _notesMenuItem.IsEnabled = _lastReadDiagnostics.Count > 0;

        double? zoom = Zoom;
        foreach ((UiMenuItem item, double? level) in _zoomMenuItems)
            item.IsChecked = PlateZoom.SameChoice(level, zoom);
        if (_fitMenuItem is not null)
            _fitMenuItem.IsEnabled = _content.CurrentView == PlateViewKind.Image;

        int choice = IndexOfZoomChoice(zoom);
        if (_zoomCombo is not null && choice >= 0)
        {
            _isSyncingZoomCombo = true;
            try
            {
                _zoomCombo.SelectIndex(choice);
            }
            finally
            {
                _isSyncingZoomCombo = false;
            }
        }

        _status.Text = BuildStatus();
        _host.RequestInvalidate();
    }

    /// <summary>
    /// Where <paramref name="zoom"/> sits in the picker's current list, or -1 for
    /// a zoom that is not on it - which a picture's fit scale never is, and which
    /// nothing else reaches.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=EC41F6
    // Broiler-Falsified-If: a zoom between two ladder levels returns a level's index instead of -1
    // Broiler-Human:        PENDING
    private int IndexOfZoomChoice(double? zoom)
    {
        for (int i = 0; i < _zoomChoices.Count; i++)
        {
            if (PlateZoom.SameChoice(_zoomChoices[i], zoom))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// The status line: what is on display, measured in the terms that view is
    /// measured in, then the last thing that happened.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=4; Fingerprint=6469F5
    // Broiler-Falsified-If: with a picture on display the status line describes the document view instead
    // Broiler-Human:        PENDING
    private string BuildStatus()
    {
        if (_currentPath is null)
            return "No file open | " + _lastAction;

        string what = _content.CurrentView == PlateViewKind.Image
            ? DescribeImage()
            : DescribeDocument();

        return what + " | " + _lastAction;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=BAF9F6
    // Broiler-Human:        PENDING
    private string DescribeImage()
    {
        if (_imagePixelSize.IsEmpty)
            return "Graphic | " + PlateZoom.Describe(_imageZoom);

        return "Graphic | " +
            ((int)Math.Round(_imagePixelSize.Width)).ToString(CultureInfo.InvariantCulture) + " x " +
            ((int)Math.Round(_imagePixelSize.Height)).ToString(CultureInfo.InvariantCulture) + " pixels | " +
            PlateZoom.Describe(_imageZoom);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=4; Fingerprint=91CEF2
    // Broiler-Falsified-If: the character count reported differs from the length of the document view's plain text
    // Broiler-Human:        PENDING
    private string DescribeDocument()
    {
        int paragraphs = _documentView.Document.ParagraphCount;
        int chars = _documentView.GetPlainText().Length;

        return "Document | " +
            paragraphs.ToString(CultureInfo.InvariantCulture) + (paragraphs == 1 ? " paragraph" : " paragraphs") + " | " +
            chars.ToString(CultureInfo.InvariantCulture) + (chars == 1 ? " character" : " characters") + " | " +
            PlateZoom.Describe(_documentZoom) + " | " +
            DescribeNotes();
    }

    /// <summary>
    /// The note count for the document on display, and nothing when the last
    /// read was of some other file.
    /// </summary>
    /// <remarks>
    /// A refused open still records its diagnostics, because they are the reason
    /// it was refused and Help &gt; Notes is where that reason is read. But the
    /// file on display is then not the file those notes belong to, and counting
    /// them here would attribute one file's problems to another — after a refused
    /// open, the document that survived it would appear to have grown notes.
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=5FBB84
    // Broiler-Falsified-If: after a refused open, the document still on display is reported with the refused file's note count
    // Broiler-Human:        PENDING
    private string DescribeNotes()
    {
        if (_lastReadDiagnostics.Count == 0)
            return "no notes";

        if (!string.Equals(_lastReadFileName, Path.GetFileName(_currentPath), StringComparison.Ordinal))
            return "no notes for it";

        return _lastReadDiagnostics.Count.ToString(CultureInfo.InvariantCulture) + " note(s)";
    }

    /// <summary>
    /// The window's body: a menu bar and a toolbar across the top, a title, the
    /// view, and a status line. Only one of the two views is ever visible; the
    /// other is collapsed, so it costs nothing to measure or arrange.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=56B73D
    // Broiler-Falsified-If: a zoomed-in picture is arranged with a box larger than the view area, so it covers the toolbar and menu and takes their clicks
    // Broiler-Human:        PENDING
    private sealed class PlateContent : UiElement
    {
        // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=805B86
        // Broiler-Human:        PENDING
        private const double Margin = 24;
        // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=A72D39
        // Broiler-Human:        PENDING
        private const double TitleTop = 18;
        // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=49F65A
        // Broiler-Human:        PENDING
        private const double StatusHeight = 24;
        // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=7A09F9
        // Broiler-Human:        PENDING
        private const double MinWidth = 900;
        // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=EE9AE6
        // Broiler-Human:        PENDING
        private const double MinHeight = 620;

        private readonly StandardMenu _menu;
        private readonly StandardToolbar _toolbar;
        private readonly StandardLabel _title;
        private readonly StandardRichEdit _documentView;
        private readonly StandardImageView _imageView;
        private readonly StandardLabel _status;

        /// <summary>
        /// The ground a view is given, which for a picture is not the same as the
        /// box the picture is drawn in: a zoomed-in picture is larger than this
        /// and a zoomed-out one smaller, and this is what the mat covers and what
        /// either is clipped to.
        /// </summary>
        private BRect _viewArea;
        private double? _imageZoom;
        private BSize _imagePixelSize;

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=E291C2
        // Broiler-Human:        PENDING
        public PlateContent(
            StandardMenu menu,
            StandardToolbar toolbar,
            StandardLabel title,
            StandardRichEdit documentView,
            StandardImageView imageView,
            StandardLabel status)
        {
            _menu = menu;
            _toolbar = toolbar;
            _title = title;
            _documentView = documentView;
            _imageView = imageView;
            _status = status;

            AddChild(_menu);
            AddChild(_toolbar);
            AddChild(_title);
            AddChild(_documentView);
            AddChild(_imageView);
            AddChild(_status);
        }

        public PlateViewKind CurrentView { get; private set; } = PlateViewKind.Document;

        /// <summary>
        /// How large the picture is drawn, as a multiple of its own pixels, or
        /// null to fit it to the window.
        /// </summary>
        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=7E6129
        // Broiler-Human:        PENDING
        public double? ImageZoom
        {
            get => _imageZoom;
            set
            {
                if (PlateZoom.SameChoice(_imageZoom, value))
                    return;

                _imageZoom = value;
                Invalidate(UiInvalidationKind.Arrange | UiInvalidationKind.Render);
            }
        }

        /// <summary>What the picture on display decoded to, which a zoom is a multiple of.</summary>
        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=688AD3
        // Broiler-Human:        PENDING
        public BSize ImagePixelSize
        {
            get => _imagePixelSize;
            set
            {
                if (_imagePixelSize == value)
                    return;

                _imagePixelSize = value;
                Invalidate(UiInvalidationKind.Arrange | UiInvalidationKind.Render);
            }
        }

        /// <summary>
        /// The scale a fitted picture was last drawn at, which is what a step in
        /// or out from Fit steps away from. One until a picture has been arranged,
        /// so a step taken before the first frame reads as actual size rather than
        /// as a division by an empty window.
        /// </summary>
        public double FitScale { get; private set; } = PlateZoom.Default;

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=78B92C
        // Broiler-Falsified-If: both views are left visible, or neither, after a switch
        // Broiler-Human:        PENDING
        public void Show(PlateViewKind view)
        {
            if (CurrentView == view)
                return;

            CurrentView = view;
            _documentView.Visibility = view == PlateViewKind.Document ? UiVisibility.Visible : UiVisibility.Collapsed;
            _imageView.Visibility = view == PlateViewKind.Image ? UiVisibility.Visible : UiVisibility.Collapsed;
            Invalidate(UiInvalidationKind.Measure | UiInvalidationKind.Arrange | UiInvalidationKind.Render);
        }

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=579592
        // Broiler-Falsified-If: an unbounded available size is returned as infinite instead of MinWidth by MinHeight
        // Broiler-Human:        PENDING
        protected override BSize MeasureCore(BSize availableSize)
        {
            double width = double.IsInfinity(availableSize.Width) ? MinWidth : Math.Max(0, availableSize.Width);
            double height = double.IsInfinity(availableSize.Height) ? MinHeight : Math.Max(0, availableSize.Height);
            double contentWidth = Math.Max(0, width - (Margin * 2));
            double viewHeight = Math.Max(240, height - 194);

            _menu.Measure(new BSize(width, _menu.MenuBarHeight));
            _toolbar.Measure(new BSize(width, _toolbar.PreferredSize.Height));
            _title.Measure(new BSize(contentWidth, double.PositiveInfinity));
            if (CurrentView == PlateViewKind.Document)
                _documentView.Measure(new BSize(contentWidth, viewHeight));
            else
                _imageView.Measure(new BSize(contentWidth, viewHeight));
            _status.Measure(new BSize(contentWidth, StatusHeight));

            return new BSize(width, height);
        }

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=8FD5F9
        // Broiler-Falsified-If: a window shorter than the menu, toolbar, title and status line gives the view a negative height
        // Broiler-Human:        PENDING
        protected override void ArrangeCore(BRect finalRect)
        {
            double toolbarHeight = _toolbar.PreferredSize.Height;
            _menu.Arrange(new BRect(finalRect.Left, finalRect.Top, finalRect.Width, _menu.MenuBarHeight));
            _toolbar.Arrange(new BRect(
                finalRect.Left, finalRect.Top + _menu.MenuBarHeight, finalRect.Width, toolbarHeight));

            double margin = finalRect.Width < 600 ? 12 : Margin;
            double x = finalRect.Left + margin;
            double y = finalRect.Top + _menu.MenuBarHeight + toolbarHeight + TitleTop;
            double width = Math.Max(0, finalRect.Width - (margin * 2));

            _title.Arrange(new BRect(x, y, width, _title.DesiredSize.Height));
            y += _title.DesiredSize.Height + 14;

            double statusTop = finalRect.Bottom - margin - StatusHeight;
            double viewHeight = Math.Max(0, statusTop - y - 14);
            _viewArea = new BRect(x, y, width, viewHeight);
            if (CurrentView == PlateViewKind.Document)
                _documentView.Arrange(_viewArea);
            else
                ArrangeImage();

            _status.Arrange(new BRect(x, statusTop, width, StatusHeight));
        }

        /// <summary>
        /// Puts the picture in the area, at the size asked for.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A picture larger than the area is cropped rather than oversized: the
        /// view is asked for the middle of it - the part that would be on screen
        /// - and given a box that is exactly the area. It is never handed a box
        /// bigger than the ground it was given, because an element's box is not
        /// only where it draws. Input is routed by hit-testing those boxes, and a
        /// picture whose box reached over the toolbar and the menu would take
        /// every click on them, leaving a zoomed-in window with nothing to press.
        /// </para>
        /// <para>
        /// Fit falls out of the same arithmetic rather than being a case of its
        /// own: at the scale that fits, the whole picture is what fits, so the
        /// crop is the whole picture and the box is the letterboxed middle of the
        /// area.
        /// </para>
        /// </remarks>
        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=3D3A61
        // Broiler-Falsified-If: a picture zoomed past the fit scale is given a box wider or taller than the view area
        // Broiler-Human:        PENDING
        private void ArrangeImage()
        {
            if (_imagePixelSize.IsEmpty || _viewArea.IsEmpty)
            {
                // Nothing to measure against - no picture, or no room for one.
                FitScale = PlateZoom.Default;
                _imageView.SourceRect = null;
                _imageView.Arrange(_viewArea);
                return;
            }

            FitScale = Math.Min(
                _viewArea.Width / _imagePixelSize.Width,
                _viewArea.Height / _imagePixelSize.Height);

            double scale = _imageZoom ?? FitScale;
            double sourceWidth = Math.Min(_imagePixelSize.Width, _viewArea.Width / scale);
            double sourceHeight = Math.Min(_imagePixelSize.Height, _viewArea.Height / scale);
            _imageView.SourceRect = new BRect(
                (_imagePixelSize.Width - sourceWidth) / 2,
                (_imagePixelSize.Height - sourceHeight) / 2,
                sourceWidth,
                sourceHeight);

            double drawnWidth = sourceWidth * scale;
            double drawnHeight = sourceHeight * scale;
            _imageView.Arrange(new BRect(
                _viewArea.Left + ((_viewArea.Width - drawnWidth) / 2),
                _viewArea.Top + ((_viewArea.Height - drawnHeight) / 2),
                drawnWidth,
                drawnHeight));
        }

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=1FA218
        // Broiler-Falsified-If: the collapsed view is rendered alongside the visible one
        // Broiler-Human:        PENDING
        protected override void RenderCore(UiRenderContext context)
        {
            context.RenderList.FillRect(Bounds, PlatePalette.Canvas);
            context.RenderList.FillRect(
                new BRect(Bounds.Left, Bounds.Top, Bounds.Width, _menu.MenuBarHeight),
                PlatePalette.MenuSurface);
            context.RenderList.FillRect(
                new BRect(Bounds.Left, Bounds.Top + _menu.MenuBarHeight, Bounds.Width, 1),
                PlatePalette.MenuRule);
            context.RenderList.FillRect(
                new BRect(
                    Bounds.Left,
                    Bounds.Top + _menu.MenuBarHeight + _toolbar.PreferredSize.Height,
                    Bounds.Width,
                    1),
                PlatePalette.MenuRule);

            _menu.Render(context);
            _toolbar.Render(context);
            _title.Render(context);
            if (CurrentView == PlateViewKind.Image)
                RenderImage(context);
            else
                _documentView.Render(context);
            _status.Render(context);
        }

        /// <summary>
        /// The picture, on its mat. The mat covers the whole area rather than the
        /// picture's own box, because a picture with transparency or a white
        /// border has to be distinguishable from the surface under it and a
        /// zoomed-out one leaves most of the area bare. Nothing is clipped here:
        /// <see cref="ArrangeImage"/> has already made the box fit the area, and
        /// a clip would only paper over a box that did not.
        /// </summary>
        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=A3EC64
        // Broiler-Falsified-If: the mat is painted over the picture instead of under it
        // Broiler-Human:        PENDING
        private void RenderImage(UiRenderContext context)
        {
            context.RenderList.FillRect(_viewArea, PlatePalette.ImageMat);
            _imageView.Render(context);
        }
    }
}

/// <summary>Which of the viewer's two views is on display.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=371D33
// Broiler-Human:        PENDING
internal enum PlateViewKind
{
    Document,
    Image,
}
