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
// Security risk:    High
// Criteria:         2/2
// Resource impact:  0/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Documents;
using Broiler.Documents.Pdf;

namespace Broiler.Plate;

/// <summary>
/// What an opened PDF's own permissions let a viewer print. ADR 0015 leaves honouring the print
/// bits to the host, and this head is the one that composes the PDF codec, so it is the one that
/// can read them.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=B821B6
// Broiler-Falsified-If: an encrypted PDF opened without the owner password whose permissions lack Print is answered anything but Refused, or one with Print but not PrintHighQuality anything but LowResolution
// Broiler-Human:        PENDING
internal static class PlatePdfPrintPermission
{
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=D0DC40
    // Broiler-Falsified-If: a document that is not an encrypted PDF, or one opened with the owner password, is answered anything but Allowed
    // Broiler-Human:        PENDING
    public static PlatePrintPermission For(DocumentReadResult result)
    {
        if (result is not PdfReadResult { Encryption: { } encryption } || encryption.Access == PdfAccessLevel.Owner)
            return PlatePrintPermission.Allowed;

        if (!encryption.Permissions.HasFlag(PdfPermissions.Print))
            return PlatePrintPermission.Refused;

        return encryption.Permissions.HasFlag(PdfPermissions.PrintHighQuality)
            ? PlatePrintPermission.Allowed
            : PlatePrintPermission.LowResolution;
    }
}
