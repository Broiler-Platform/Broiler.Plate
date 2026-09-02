using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Broiler.Documents;
using Broiler.Documents.Model;
using Broiler.Graphics;
using Broiler.Input.Keyboard;
using Broiler.UI;
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
internal sealed class PlateApp : IDisposable
{
    private readonly PlateUiHost _host;
    private readonly Action _requestClose;
    private readonly UiSession _session;
    private readonly StandardWindow _rootWindow;
    private readonly StandardMenu _menu;
    private readonly StandardLabel _title;
    private readonly StandardLabel _status;
    private readonly StandardRichEdit _documentView;
    private readonly StandardImageView _imageView;
    private readonly PlateContent _content;
    private readonly PlateFileFormats _formats;
    private readonly DocumentCodecCatalog _documentCatalog;
    private readonly UiFileDialogFilter[] _openFilters;

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

    private static readonly BSize FileDialogPreferredSize = new(740, 430);
    private static readonly BSize NotesDialogPreferredSize = new(560, 320);

    /// <summary>
    /// How much of a file has to be in hand to tell a graphic from a document.
    /// Every signature this recognizes lives in the first twelve bytes; the rest
    /// is slack so the check does not become wrong the moment one is added.
    /// </summary>
    private const int SignatureLength = 32;

    /// <param name="formats">
    /// The formats this viewer offers. Null composes
    /// <see cref="PlateFileFormats.CreateDefault"/> - the RTF, DOCX, HTML and
    /// Markdown set every head carries. A head with more (the Windows head
    /// registers PDF) passes its own set, which is what keeps that codec out of
    /// heads that did not ask for it.
    /// </param>
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
            Stretch = UiImageStretch.Uniform,
            PreferredSize = new BSize(760, 520),
            PlaceholderBackground = PlatePalette.ImageMat,
            PlaceholderBorder = PlatePalette.ViewBorder,
            Visibility = UiVisibility.Collapsed,
        };

        _menu = CreateMenu();
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
        _content = new PlateContent(_menu, _title, _documentView, _imageView, _status);
        _rootWindow.AddChild(_content);

        SeedWelcome();
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

    public BRenderList RenderFrame() => _session.RenderFrame();

    public void Dispatch(UiInputEvent input)
    {
        if (HandleOpenShortcut(input))
        {
            _host.RequestInvalidate();
            return;
        }

        if (_session.DispatchInput(input))
            _host.RequestInvalidate();
    }

    public void Invalidate() => _host.RequestInvalidate();

    /// <summary>
    /// Opens <paramref name="path"/>, deciding for itself whether it is a
    /// document or a graphic. This is the whole of the first version's feature
    /// set, and the file dialog is one caller of it rather than the only way in -
    /// a command line argument and a drop target are the same call.
    /// </summary>
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

    public void Dispose()
    {
        ReleaseImage();
        _session.Dispose();
    }

    private StandardMenu CreateMenu()
    {
        var dispatcher = new StandardCommandDispatcher();
        dispatcher.Add(new StandardCommand("file.open", ShowOpenDialog));
        dispatcher.Add(new StandardCommand("file.close", CloseFile, () => _currentPath is not null));
        dispatcher.Add(new StandardCommand("file.exit", _requestClose));
        dispatcher.Add(new StandardCommand("help.notes", ShowNotes, () => _lastReadDiagnostics.Count > 0));
        dispatcher.Add(new StandardCommand("help.about", ShowAbout));

        var file = new UiMenuItem("file", "File") { AccessKey = 'F' };
        file.Children.Add(new UiMenuItem("open", "Open...") { CommandName = "file.open", AccessKey = 'O' });
        _closeMenuItem = new UiMenuItem("close", "Close") { CommandName = "file.close", AccessKey = 'C' };
        file.Children.Add(_closeMenuItem);
        file.Children.Add(new UiMenuItem("exit", "Exit") { CommandName = "file.exit", AccessKey = 'X' });

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
        menu.SetItems([file, help]);
        return menu;
    }

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
        _documentView.Selection = RichTextRange.Caret(_documentView.Document.Start);
        _content.Show(PlateViewKind.Document);
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
        _content.Show(PlateViewKind.Image);

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
        _content.Show(PlateViewKind.Document);
        _lastAction = "Closed";
        _session.SetFocus(_documentView);
        RefreshUi();
    }

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

    private void ShowAbout()
    {
        _lastAction = "Broiler Plate: a Broiler.UI window over the platform's document codecs and image decoders";
        RefreshUi();
    }

    /// <summary>
    /// What the viewer says before it has been given anything to show. It goes
    /// into the document view rather than a separate empty-state element so that
    /// there is one fewer thing on screen to lay out and keep in step.
    /// </summary>
    private void SeedWelcome()
    {
        _documentView.SetPlainText(
            "Broiler Plate\n" +
            "A viewer for the formats the Broiler platform reads.\n" +
            "Choose File > Open, or press Ctrl+O, to open a document or a graphic. " +
            "Documents are drawn through Broiler.Documents and Broiler.UI; graphics are decoded by " +
            "the image codecs this build was composed with.");
        _documentView.Selection = RichTextRange.Caret(_documentView.Document.Start);
    }

    /// <summary>Ctrl+O, which no view owns, so nothing else will answer it.</summary>
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
    private const int VirtualKeyO = 0x4F;

    /// <summary>
    /// Whether an event is the named key, asked the three ways the heads answer
    /// it. A head may fill in the native code, the key's name, or neither but a
    /// <c>VirtualKey:</c> name built from the code, and a shortcut that checks
    /// only one of them works on some heads and silently does nothing on the
    /// rest. This is the same test <c>StandardRichEdit</c> and the Writer's zoom
    /// ladder apply.
    /// </summary>
    private static bool IsKey(UiInputEvent input, int virtualKey, string name) =>
        input.NativeKeyCode == virtualKey ||
        string.Equals(
            input.KeyName,
            "VirtualKey:" + virtualKey.ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal) ||
        string.Equals(input.KeyName, name, StringComparison.OrdinalIgnoreCase);

    private void AdoptPath(string fullPath)
    {
        _currentPath = fullPath;
        _lastDirectory = Path.GetDirectoryName(fullPath) ?? _lastDirectory;
        _title.Text = Path.GetFileName(fullPath);
    }

    private void ReleaseImage()
    {
        if (!_imageHandle.IsValid)
            return;

        _imageView.Image = BImageHandle.Invalid;
        _host.ReleaseImage(_imageHandle);
        _imageHandle = BImageHandle.Invalid;
        _imagePixelSize = BSize.Empty;
    }

    /// <summary>
    /// The first <see cref="SignatureLength"/> bytes of a file, for the format
    /// check. <paramref name="isEmpty"/> separates a zero-byte file - which no
    /// codec can be blamed for refusing - from a short one that is simply
    /// shorter than the buffer.
    /// </summary>
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
    internal static bool MayReplaceView(DocumentReadResult result) =>
        result.IsUsable &&
        (result.Document.PlainText.Length > 0 || result.Status == DocumentResultStatus.Success);

    /// <summary>Why a read was refused, for the status bar.</summary>
    internal static string DescribeRefusedOpen(string fileName, DocumentCodecSelection selection)
    {
        string reason = FirstProblem(selection.Result) ?? (selection.Codec is null
            ? "no registered format recognized it"
            : "the " + selection.Codec.Name + " reader recovered no content from it");

        return "Could not open " + fileName + ": " + reason.TrimEnd('.') +
            ". What is on display is unchanged.";
    }

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

    private static string? FirstProblem(DocumentReadResult result)
    {
        foreach (DocumentDiagnostic diagnostic in result.Diagnostics)
        {
            if (diagnostic.Severity != DocumentDiagnosticSeverity.Info)
                return diagnostic.Message;
        }

        return null;
    }

    private static bool IsFileOperationException(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException;

    private BRect GetDialogPlacement() => CenterInViewport(FileDialogPreferredSize, minTop: 42);

    private BRect GetNotesPlacement() => CenterInViewport(NotesDialogPreferredSize, minTop: 72);

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

    private void RefreshUi()
    {
        if (_closeMenuItem is not null)
            _closeMenuItem.IsEnabled = _currentPath is not null;
        if (_notesMenuItem is not null)
            _notesMenuItem.IsEnabled = _lastReadDiagnostics.Count > 0;

        _status.Text = BuildStatus();
        _host.RequestInvalidate();
    }

    /// <summary>
    /// The status line: what is on display, measured in the terms that view is
    /// measured in, then the last thing that happened.
    /// </summary>
    private string BuildStatus()
    {
        if (_currentPath is null)
            return "No file open | " + _lastAction;

        string what = _content.CurrentView == PlateViewKind.Image
            ? DescribeImage()
            : DescribeDocument();

        return what + " | " + _lastAction;
    }

    private string DescribeImage()
    {
        if (_imagePixelSize.IsEmpty)
            return "Graphic";

        return "Graphic | " +
            ((int)Math.Round(_imagePixelSize.Width)).ToString(CultureInfo.InvariantCulture) + " x " +
            ((int)Math.Round(_imagePixelSize.Height)).ToString(CultureInfo.InvariantCulture) + " pixels";
    }

    private string DescribeDocument()
    {
        int paragraphs = _documentView.Document.ParagraphCount;
        int chars = _documentView.GetPlainText().Length;

        return "Document | " +
            paragraphs.ToString(CultureInfo.InvariantCulture) + (paragraphs == 1 ? " paragraph" : " paragraphs") + " | " +
            chars.ToString(CultureInfo.InvariantCulture) + (chars == 1 ? " character" : " characters") + " | " +
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
    private string DescribeNotes()
    {
        if (_lastReadDiagnostics.Count == 0)
            return "no notes";

        if (!string.Equals(_lastReadFileName, Path.GetFileName(_currentPath), StringComparison.Ordinal))
            return "no notes for it";

        return _lastReadDiagnostics.Count.ToString(CultureInfo.InvariantCulture) + " note(s)";
    }

    /// <summary>
    /// The window's body: a menu bar across the top, a title, the view, and a
    /// status line. Only one of the two views is ever visible; the other is
    /// collapsed, so it costs nothing to measure or arrange.
    /// </summary>
    private sealed class PlateContent : UiElement
    {
        private const double Margin = 24;
        private const double TitleTop = 18;
        private const double StatusHeight = 24;
        private const double MinWidth = 900;
        private const double MinHeight = 620;

        private readonly StandardMenu _menu;
        private readonly StandardLabel _title;
        private readonly StandardRichEdit _documentView;
        private readonly StandardImageView _imageView;
        private readonly StandardLabel _status;

        public PlateContent(
            StandardMenu menu,
            StandardLabel title,
            StandardRichEdit documentView,
            StandardImageView imageView,
            StandardLabel status)
        {
            _menu = menu;
            _title = title;
            _documentView = documentView;
            _imageView = imageView;
            _status = status;

            AddChild(_menu);
            AddChild(_title);
            AddChild(_documentView);
            AddChild(_imageView);
            AddChild(_status);
        }

        public PlateViewKind CurrentView { get; private set; } = PlateViewKind.Document;

        public void Show(PlateViewKind view)
        {
            if (CurrentView == view)
                return;

            CurrentView = view;
            _documentView.Visibility = view == PlateViewKind.Document ? UiVisibility.Visible : UiVisibility.Collapsed;
            _imageView.Visibility = view == PlateViewKind.Image ? UiVisibility.Visible : UiVisibility.Collapsed;
            Invalidate(UiInvalidationKind.Measure | UiInvalidationKind.Arrange | UiInvalidationKind.Render);
        }

        protected override BSize MeasureCore(BSize availableSize)
        {
            double width = double.IsInfinity(availableSize.Width) ? MinWidth : Math.Max(0, availableSize.Width);
            double height = double.IsInfinity(availableSize.Height) ? MinHeight : Math.Max(0, availableSize.Height);
            double contentWidth = Math.Max(0, width - (Margin * 2));
            double viewHeight = Math.Max(240, height - 152);

            _menu.Measure(new BSize(width, _menu.MenuBarHeight));
            _title.Measure(new BSize(contentWidth, double.PositiveInfinity));
            if (CurrentView == PlateViewKind.Document)
                _documentView.Measure(new BSize(contentWidth, viewHeight));
            else
                _imageView.Measure(new BSize(contentWidth, viewHeight));
            _status.Measure(new BSize(contentWidth, StatusHeight));

            return new BSize(width, height);
        }

        protected override void ArrangeCore(BRect finalRect)
        {
            _menu.Arrange(new BRect(finalRect.Left, finalRect.Top, finalRect.Width, _menu.MenuBarHeight));

            double margin = finalRect.Width < 600 ? 12 : Margin;
            double x = finalRect.Left + margin;
            double y = finalRect.Top + _menu.MenuBarHeight + TitleTop;
            double width = Math.Max(0, finalRect.Width - (margin * 2));

            _title.Arrange(new BRect(x, y, width, _title.DesiredSize.Height));
            y += _title.DesiredSize.Height + 14;

            double statusTop = finalRect.Bottom - margin - StatusHeight;
            double viewHeight = Math.Max(0, statusTop - y - 14);
            var view = new BRect(x, y, width, viewHeight);
            if (CurrentView == PlateViewKind.Document)
                _documentView.Arrange(view);
            else
                _imageView.Arrange(view);

            _status.Arrange(new BRect(x, statusTop, width, StatusHeight));
        }

        protected override void RenderCore(UiRenderContext context)
        {
            context.RenderList.FillRect(Bounds, PlatePalette.Canvas);
            context.RenderList.FillRect(
                new BRect(Bounds.Left, Bounds.Top, Bounds.Width, _menu.MenuBarHeight),
                PlatePalette.MenuSurface);
            context.RenderList.FillRect(
                new BRect(Bounds.Left, Bounds.Top + _menu.MenuBarHeight, Bounds.Width, 1),
                PlatePalette.MenuRule);

            // The mat behind a picture. A Uniform-stretched image leaves the rest
            // of its box unpainted, and an image with transparency or a white
            // border has to be distinguishable from the surface under it.
            if (CurrentView == PlateViewKind.Image)
                context.RenderList.FillRect(_imageView.Bounds, PlatePalette.ImageMat);

            base.RenderCore(context);
        }
    }
}

/// <summary>Which of the viewer's two views is on display.</summary>
internal enum PlateViewKind
{
    Document,
    Image,
}
