// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Broiler.Plate;

/// <summary>Linux entry point for Broiler Plate.</summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Composition root: without a codec catalog the renderer cannot decode
        // the images a document embeds or open graphics files.
        Broiler.Graphics.Imaging.BImageCodecs.Use(
            new Broiler.Media.MediaCodecCatalog(Broiler.Media.Image.Managed.ManagedImageCodecs.CreateCodecs()));

        // Font resolver for software rasterizer on Linux systems without DirectWrite.
        Broiler.Graphics.Text.BSystemFonts.InstallFontFileResolver();

        LinuxPlateOptions options = LinuxPlateOptions.Parse(args);
        if (options.ShowHelp)
        {
            LinuxPlateOptions.PrintHelp();
            return 0;
        }

        if (options.OpenWindow && !OperatingSystem.IsLinux())
        {
            Console.WriteLine("Windowed Broiler Plate uses the Linux X11/OpenGL backend; running offscreen on this OS.");
            options = options with { OpenWindow = false, EnableEvdevInput = false, RunUntilClose = false };
        }

        using CancellationTokenSource shutdown = new();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdown.Cancel();
        };

        try
        {
            return await LinuxPlateRunner
                .RunAsync(options, CreateFileFormats(), shutdown.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    /// <summary>
    /// The document formats this head offers: the shared four, plus PDF.
    /// </summary>
    private static PlateFileFormats CreateFileFormats() =>
        PlateFileFormats.CreateDefault().With(
            new PlateDocumentFormat(
                new Broiler.Documents.Pdf.PdfDocumentCodec(CreatePdfServices()), "PDF"));

    /// <summary>
    /// The PDF service graph this head composes: JPEG filter.
    /// </summary>
    internal static Broiler.Documents.Pdf.PdfCodecServices CreatePdfServices() =>
        Broiler.Documents.Pdf.PdfCodecServices.Base.WithStreamFilters(
            new Broiler.Documents.Pdf.Images.JpegStreamFilter());
}
