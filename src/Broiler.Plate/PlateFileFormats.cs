using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Broiler.Documents;
using Broiler.Documents.Docx;
using Broiler.Documents.Html;
using Broiler.Documents.Markdown;
using Broiler.Documents.Rtf;
using Broiler.UI.FileDialog;

namespace Broiler.Plate;

/// <summary>One document codec as a composition root offered it to the viewer.</summary>
/// <remarks>
/// There is no capability flag here as there is in the Writer's equivalent: a
/// viewer only ever reads, so the only question a format can answer is whether
/// it can be opened, and a codec that cannot read is refused outright rather
/// than registered and then never reached.
/// </remarks>
public sealed class PlateDocumentFormat
{
    public PlateDocumentFormat(DocumentCodec codec, string displayName)
    {
        ArgumentNullException.ThrowIfNull(codec);
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("A document format needs a display name.", nameof(displayName));
        if (!codec.CanRead)
            throw new ArgumentException(
                $"The {codec.Descriptor.Name} codec does not implement reading, so a viewer cannot offer it.",
                nameof(codec));
        if (codec.Descriptor.FileExtensions.Count == 0)
            throw new ArgumentException(
                $"The {codec.Descriptor.Name} codec declares no file extension, so the viewer cannot offer it in a file dialog.",
                nameof(codec));

        Codec = codec;
        DisplayName = displayName.Trim();
    }

    public DocumentCodec Codec { get; }

    /// <summary>The format name the Open dialog shows, without the pattern suffix.</summary>
    public string DisplayName { get; }

    public IReadOnlyList<string> FileExtensions => Codec.Descriptor.FileExtensions;

    public string DefaultExtension => FileExtensions[0];

    /// <summary>The dialog pattern, e.g. <c>*.html;*.htm</c>.</summary>
    public string FilterPattern => string.Join(";", Patterns());

    /// <summary>The dialog label, e.g. <c>HTML (*.html, *.htm)</c>.</summary>
    public string FilterName => DisplayName + " (" + string.Join(", ", Patterns()) + ")";

    public UiFileDialogFilter CreateFilter() => new(FilterName, FilterPattern, DefaultExtension);

    private IEnumerable<string> Patterns()
    {
        foreach (string extension in FileExtensions)
            yield return "*" + extension;
    }
}

/// <summary>
/// Everything one viewer instance will open: the document codecs a composition
/// root registered, and the graphics formats <see cref="PlateImageFormats"/>
/// recognizes.
/// </summary>
/// <remarks>
/// <para>
/// Composed by the head rather than discovered, for the reason the Writer
/// records: a codec that reaches a head by being someone else's transitive
/// reference has passed nobody's size, memory, trimming or AOT gate. The Windows
/// head adds PDF; a head that has not cleared PDF says nothing and gets nothing.
/// </para>
/// <para>
/// The graphics half is deliberately not composable in the same way. Images are
/// not decoded here at all - they go to the renderer's codec catalog, which the
/// head installs into <c>BImageCodecs</c> - so this type only decides which
/// extensions the Open dialog offers.
/// </para>
/// </remarks>
public sealed class PlateFileFormats
{
    private readonly ReadOnlyCollection<PlateDocumentFormat> _documents;

    public PlateFileFormats(IEnumerable<PlateDocumentFormat> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        PlateDocumentFormat[] array = documents.ToArray();
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (PlateDocumentFormat format in array)
        {
            if (format is null)
                throw new ArgumentException("The format collection contains a null entry.", nameof(documents));

            foreach (string extension in format.FileExtensions)
            {
                // Two formats claiming one extension make the Open filters
                // misleading, and an extension claimed by both a document codec
                // and the graphics table makes the open decision depend on which
                // check runs first. Both are refused at composition rather than
                // resolved by accident at runtime.
                if (!extensions.Add(extension))
                    throw new ArgumentException(
                        $"Two registered formats both claim '{extension}'; the second is {format.DisplayName}.",
                        nameof(documents));
                if (PlateImageFormats.HasImageExtension(extension))
                    throw new ArgumentException(
                        $"{format.DisplayName} claims '{extension}', which the viewer already opens as a graphic.",
                        nameof(documents));
            }
        }

        _documents = Array.AsReadOnly(array);
    }

    /// <summary>
    /// The document formats every head carries: RTF, DOCX, HTML and Markdown.
    /// Each call composes its own codec instances, so two viewers in one process
    /// never reach the same codec object.
    /// </summary>
    public static PlateFileFormats CreateDefault() => new(
    [
        new PlateDocumentFormat(new RtfDocumentCodec(), "Rich Text Format"),
        new PlateDocumentFormat(new DocxDocumentCodec(), "Word Document"),
        new PlateDocumentFormat(new HtmlDocumentCodec(), "HTML"),
        new PlateDocumentFormat(new MarkdownDocumentCodec(), "Markdown"),
    ]);

    public IReadOnlyList<PlateDocumentFormat> Documents => _documents;

    /// <summary>This set plus <paramref name="additional"/>, leaving this one unchanged.</summary>
    public PlateFileFormats With(params PlateDocumentFormat[] additional)
    {
        ArgumentNullException.ThrowIfNull(additional);
        return new PlateFileFormats(_documents.Concat(additional));
    }

    /// <summary>A catalog over the registered document codecs.</summary>
    public DocumentCodecCatalog CreateOpenCatalog() =>
        new(_documents.Select(static format => format.Codec));

    /// <summary>The pattern matching every document format, e.g. <c>*.rtf;*.docx</c>.</summary>
    public string DocumentFilterPattern =>
        string.Join(";", _documents.Select(static format => format.FilterPattern));

    /// <summary>
    /// The Open dialog filters, widest first: everything, then documents and
    /// graphics as groups, then one filter per format. Someone who knows what
    /// they are looking for can narrow all the way down; someone who does not
    /// never has to choose.
    /// </summary>
    public UiFileDialogFilter[] CreateOpenFilters()
    {
        string documents = DocumentFilterPattern;
        string graphics = PlateImageFormats.AllFilterPattern;
        string everything = documents.Length == 0 ? graphics : documents + ";" + graphics;

        var filters = new List<UiFileDialogFilter>(_documents.Count + PlateImageFormats.Formats.Length + 3)
        {
            new("All supported files", everything),
        };

        if (documents.Length > 0)
            filters.Add(new UiFileDialogFilter("Documents", documents, _documents[0].DefaultExtension));

        filters.Add(new UiFileDialogFilter("Graphics", graphics, ".png"));
        filters.AddRange(_documents.Select(static format => format.CreateFilter()));

        foreach ((string extension, string displayName) in PlateImageFormats.Formats)
        {
            string pattern = PlateImageFormats.FilterPattern(extension);
            filters.Add(new UiFileDialogFilter(
                displayName + " (" + pattern.Replace(";", ", ") + ")",
                pattern,
                extension));
        }

        return filters.ToArray();
    }
}
