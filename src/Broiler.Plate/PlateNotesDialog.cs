// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   11
// Annotated:        11/11
// Exempt:           2
// Human-reviewed:   0/11
// IP risk:          Low
// Security risk:    Medium
// Criteria:         6/0
// Resource impact:  3/10 max
// Unverified:       11
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.Documents;
using Broiler.Graphics;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Text;
using Broiler.UI;
using Broiler.UI.Button.Standard;
using Broiler.UI.Dialog.Standard;
using Broiler.UI.RichEdit.Standard;

namespace Broiler.Plate;

/// <summary>
/// Shows what a read reported. The status bar can say a document opened with
/// notes, but not what they were, and a viewer has even more need of the detail
/// than an editor does: someone who cannot change the file can only decide
/// whether to trust what they are looking at, and that decision is exactly what
/// the notes answer.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=B61E02
// Broiler-Falsified-If: a note's code or message reported for an opened document is missing from the text the Notes dialog shows
// Broiler-Human:        PENDING
internal static class PlateNotes
{
    /// <summary>
    /// A dialog listing <paramref name="diagnostics"/>. StandardDialog is sealed
    /// and arranges every child across its whole client area, so the content is
    /// one element that lays itself out rather than a subclass.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=9E9C66
    // Broiler-Falsified-If: the note list in the dialog accepts typing, so a keystroke changes the text of a note the document reported
    // Broiler-Human:        PENDING
    public static StandardDialog CreateDialog(string fileName, IReadOnlyList<DocumentDiagnostic> diagnostics)
    {
        var dialog = new StandardDialog
        {
            Title = "Notes for " + fileName,
            PreferredSize = new BSize(560, 320),
            TitleFont = new BFontStyle("Segoe UI", 14, BFontWeight.SemiBold),
        };

        // Read-only rather than a label so the list scrolls when a document
        // reports more notes than fit, and so a code can be selected and copied
        // into a bug report.
        var text = new StandardRichEdit
        {
            IsReadOnly = true,
            Font = new BFontStyle("Segoe UI", 13),
        };
        text.SetPlainText(Describe(diagnostics));

        var close = new StandardButton { Text = "Close" };
        close.Clicked += (_, _) => dialog.Accept();

        dialog.AddChild(new NotesContent(text, close));
        return dialog;
    }

    /// <summary>
    /// One entry per note: severity, code, then the message. The code is included
    /// because it is the part worth searching for or quoting - the message says
    /// what happened, the code says which rule produced it.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=5782C9
    // Broiler-Falsified-If: the text for a list of N diagnostics states a count other than N or lists fewer than N code entries
    // Broiler-Human:        PENDING
    internal static string Describe(IReadOnlyList<DocumentDiagnostic> diagnostics)
    {
        if (diagnostics is null || diagnostics.Count == 0)
            return "This document opened with nothing to report.";

        var builder = new StringBuilder();
        builder.Append(diagnostics.Count.ToString(CultureInfo.InvariantCulture))
            .Append(diagnostics.Count == 1 ? " note:" : " notes:");

        foreach (DocumentDiagnostic diagnostic in diagnostics)
        {
            builder.Append(NL).Append(NL)
                .Append(diagnostic.Severity.ToString().ToLowerInvariant())
                .Append("  ")
                .Append(diagnostic.Code)
                .Append(NL)
                .Append("    ")
                .Append(diagnostic.Message);
        }

        return builder.ToString();
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=521C42
    // Broiler-Human:        PENDING
    private const string NL = "\n";

    /// <summary>The dialog's body: the note list above, a Close button below it.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=3; Fingerprint=8DE26B
    // Broiler-Falsified-If: the Close button is arranged over the note list at some client size
    // Broiler-Human:        PENDING
    private sealed class NotesContent : UiElement
    {
        // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=ADC1E4
        // Broiler-Human:        PENDING
        private const double ButtonHeight = 30;
        // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=2B7B68
        // Broiler-Human:        PENDING
        private const double ButtonWidth = 88;
        // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=B41882
        // Broiler-Human:        PENDING
        private const double Gap = 10;

        private readonly StandardRichEdit _text;
        private readonly StandardButton _close;

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=E83E4B
        // Broiler-Human:        PENDING
        public NotesContent(StandardRichEdit text, StandardButton close)
        {
            _text = text;
            _close = close;
            AddChild(_text);
            AddChild(_close);
        }

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=3; Fingerprint=8FF623
        // Broiler-Falsified-If: an available height below ButtonHeight plus Gap measures the note list with a negative height
        // Broiler-Human:        PENDING
        protected override BSize MeasureCore(BSize availableSize)
        {
            _text.Measure(new BSize(availableSize.Width, Math.Max(0, availableSize.Height - ButtonHeight - Gap)));
            _close.Measure(new BSize(ButtonWidth, ButtonHeight));
            return availableSize;
        }

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=3; Fingerprint=D31548
        // Broiler-Falsified-If: a final rect narrower than ButtonWidth places the Close button left of the rect's left edge
        // Broiler-Human:        PENDING
        protected override void ArrangeCore(BRect finalRect)
        {
            double textHeight = Math.Max(0, finalRect.Height - ButtonHeight - Gap);
            _text.Arrange(new BRect(finalRect.Left, finalRect.Top, finalRect.Width, textHeight));
            _close.Arrange(new BRect(
                finalRect.Left + Math.Max(0, finalRect.Width - ButtonWidth),
                finalRect.Top + textHeight + Gap,
                ButtonWidth,
                ButtonHeight));
        }
    }
}
