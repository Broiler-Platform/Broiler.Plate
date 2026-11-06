// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using Broiler.Documents;
using Broiler.Documents.Pdf;

namespace Broiler.Plate;

/// <summary>
/// What an opened PDF's own permissions let a viewer print on Linux.
/// </summary>
internal static class PlatePdfPrintPermission
{
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
