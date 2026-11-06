// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Broiler.Documents.Pdf.Writing;
using Broiler.Graphics;
using Broiler.Graphics.Color;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Imaging;
using Broiler.Graphics.Linux;
using Broiler.Graphics.Linux.OpenGL;
using Broiler.Graphics.Rendering;
using Broiler.Graphics.RenderList;
using Broiler.Hosting.Linux;
using Broiler.Input.Linux;
using Broiler.Input.Scanner.Linux;

namespace Broiler.Plate;

internal static class LinuxPlateRunner
{
    private static readonly TimeSpan InteractivePollInterval = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan OffscreenPollInterval = TimeSpan.FromMilliseconds(1);
    private static readonly BRenderOptions RenderOptions = new(Antialias: true, VSync: true, SubpixelText: true);

    public static async Task<int> RunAsync(
        LinuxPlateOptions options,
        PlateFileFormats fileFormats,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fileFormats);

        Console.WriteLine("Broiler Plate");
        Console.WriteLine();
        PrintRuntime(LinuxGraphicsRuntimeDiagnostics.Capture());
        Print("Windowing baseline", LinuxGraphicsDependencies.CheckWindowingBaseline());
        Print("OpenGL", LinuxOpenGlRenderer.CheckDependencies());

        bool closeRequested = false;
        long frameIndex = 0;
        long renderTicks = 0;
        int renderedFrames = 0;
        BFrameContext lastFrameContext = BFrameContext.Default;

        using LinuxOpenGlRenderer renderer = new();
        BSurfaceDescriptor descriptor = BSurfaceDescriptor.Default(new BSize(options.Width, options.Height));
        using IBroilerSurface surface = options.OpenWindow
            ? renderer.CreateX11WindowSurface(descriptor, "Broiler Plate")
            : renderer.CreateSurface(descriptor);

        LinuxOpenGlX11WindowSurface? x11Window = surface as LinuxOpenGlX11WindowSurface;
        bool canUseEvdev = options.EnableEvdevInput && x11Window is not null;
        if (options.EnableEvdevInput && x11Window is null)
            Console.WriteLine("evdev input requested, but no focus-capable X11 window exists; input is disabled.");

        using LinuxX11Clipboard? clipboard = x11Window is not null ? LinuxX11Clipboard.TryOpen() : null;
        if (x11Window is not null && clipboard is null)
            Console.WriteLine("No X11 clipboard is available; copy and paste are disabled.");

        using PlateUiHost host = new(
            () => surface.Size,
            () => surface.DpiScale,
            static () => { },
            renderList =>
            {
                lastFrameContext = new BFrameContext(PlatePalette.Canvas, frameIndex++, RenderOptions);
                renderer.Render(surface, renderList, lastFrameContext);
            },
            () => clipboard is not null && clipboard.TryGetText(out string text) ? text : null,
            text => clipboard?.SetText(text),
            getRenderer: () => renderer);

        using PlateApp app = new(
            host,
            () => closeRequested = true,
            formats: fileFormats,
            printHost: LinuxCupsPrintHost.IsAvailable ? new LinuxCupsPrintHost(new PdfRasterPageWriter()) : null,
            printPermission: PlatePdfPrintPermission.For,
            scanHost: LinuxScannerProvider.CheckDependencies().IsAvailable ? new PlateScanHost(new LinuxScannerProvider()) : null);

        if (!string.IsNullOrWhiteSpace(options.InitialPath))
        {
            if (Directory.Exists(options.InitialPath))
                app.OpenFolder(options.InitialPath);
            else if (File.Exists(options.InitialPath))
                app.OpenFile(options.InitialPath);
        }

        await using LinuxInputCoordinator input = new(canUseEvdev, Console.WriteLine, externalPointer: x11Window is not null, applicationName: "Broiler Plate");
        await input.InitializeAsync(cancellationToken).ConfigureAwait(false);

        DateTimeOffset start = DateTimeOffset.UtcNow;
        using PeriodicTimer timer = new(options.OpenWindow ? InteractivePollInterval : OffscreenPollInterval);
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool processedWindowEvents = false;

            clipboard?.ProcessPendingEvents();
            if (x11Window is not null)
            {
                processedWindowEvents = x11Window.ProcessPendingEvents();
                bool inputActive = options.IgnoreInputFocus || x11Window.IsFocused;
                await input.SetActiveAsync(inputActive, cancellationToken).ConfigureAwait(false);
            }

            input.SetViewport(host.ViewportSize);
            if (x11Window is not null && x11Window.TryGetPointerPosition(out int pointerX, out int pointerY, out _))
            {
                double scale = host.Scale <= 0 ? 1.0 : host.Scale;
                input.SetExternalPointer(pointerX / scale, pointerY / scale);
            }

            input.Drain(app.Dispatch);

            if (app.WantsTick)
                app.AnimationTick();

            if (host.IsInvalidated || processedWindowEvents)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                app.RenderFrame();
                stopwatch.Stop();
                renderTicks += stopwatch.ElapsedTicks;
                renderedFrames++;
            }

            if (!options.OpenWindow)
                break;

            if (!options.RunUntilClose &&
                (DateTimeOffset.UtcNow - start).TotalMilliseconds >= options.DurationMilliseconds)
            {
                break;
            }
        }
        while (!closeRequested &&
               (x11Window is null || !x11Window.IsCloseRequested) &&
               await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));

        await input.SetActiveAsync(false, cancellationToken).ConfigureAwait(false);
        using BBitmap bitmap = ReadSurface(surface);
        Console.WriteLine("Plate presentation:");
        Console.WriteLine("  presentation: " + PresentationState(surface));
        Console.WriteLine("  diagnostic: " + SurfaceDiagnostic(surface));
        Console.WriteLine("  bitmap: " + bitmap.Width.ToString(CultureInfo.InvariantCulture) + "x" + bitmap.Height.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("  render-time: " + FormatMilliseconds(renderTicks) + " ms total across " + renderedFrames.ToString(CultureInfo.InvariantCulture) + " frame(s)");
        Console.WriteLine("  input: " + InputSummary(input.Snapshot));
        SaveArtifacts(options, bitmap, host.LastRenderList, descriptor, lastFrameContext);
        Console.WriteLine();
        return 0;
    }

    private static BBitmap ReadSurface(IBroilerSurface surface) =>
        surface switch
        {
            LinuxOpenGlSurface openGlSurface => openGlSurface.ReadToBitmap(),
            LinuxOpenGlX11WindowSurface x11Surface => x11Surface.ReadToBitmap(),
            _ => throw new InvalidOperationException("Unexpected Linux plate surface."),
        };

    private static string PresentationState(IBroilerSurface surface) =>
        surface switch
        {
            LinuxOpenGlSurface openGlSurface => openGlSurface.IsGpuBacked ? "EGL/OpenGL pbuffer" : "CPU fallback",
            LinuxOpenGlX11WindowSurface x11Surface => x11Surface.IsGpuBacked ? "X11/EGL/OpenGL window" : "CPU fallback",
            _ => "unknown",
        };

    private static string SurfaceDiagnostic(IBroilerSurface surface) =>
        surface switch
        {
            LinuxOpenGlSurface openGlSurface => openGlSurface.Diagnostic,
            LinuxOpenGlX11WindowSurface x11Surface => x11Surface.Diagnostic,
            _ => string.Empty,
        };

    private static void SaveArtifacts(
        LinuxPlateOptions options,
        BBitmap backendBitmap,
        BRenderList? renderList,
        BSurfaceDescriptor descriptor,
        BFrameContext frameContext)
    {
        if (options.ArtifactDirectory is null)
            return;

        Directory.CreateDirectory(options.ArtifactDirectory);
        string backendPath = Path.Combine(options.ArtifactDirectory, "broiler-plate-linux-opengl.png");
        backendBitmap.Save(backendPath);

        if (renderList is not null)
        {
            using BImageRenderer cpu = new();
            using BBitmap cpuBitmap = cpu.RenderToImage(renderList, descriptor, frameContext);
            string cpuPath = Path.Combine(options.ArtifactDirectory, "broiler-plate-linux-cpu.png");
            cpuBitmap.Save(cpuPath);
            Console.WriteLine("  artifacts: " + cpuPath + "; " + backendPath);
            return;
        }

        Console.WriteLine("  artifact: " + backendPath);
    }

    private static string InputSummary(LinuxInputSnapshot input)
    {
        if (!input.Enabled)
            return "disabled";
        if (!input.Initialized)
            return "requested but not opened";

        string keyboard = input.KeyboardDevice ?? "none";
        string mouse = input.MouseDevice ?? "none";
        return "active=" + input.Active.ToString(CultureInfo.InvariantCulture) +
            ", keyboard=" + keyboard +
            ", mouse=" + mouse +
            ", keys=" + input.KeyEvents.ToString(CultureInfo.InvariantCulture) +
            ", text=" + input.TextEvents.ToString(CultureInfo.InvariantCulture) +
            ", moves=" + input.MouseMoveEvents.ToString(CultureInfo.InvariantCulture) +
            ", buttons=" + input.MouseButtonEvents.ToString(CultureInfo.InvariantCulture) +
            ", wheels=" + input.MouseWheelEvents.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatMilliseconds(long ticks) =>
        (ticks * 1000.0 / Stopwatch.Frequency).ToString("0.###", CultureInfo.InvariantCulture);

    private static void PrintRuntime(LinuxGraphicsRuntimeReport report)
    {
        Console.WriteLine("Runtime:");
        Console.WriteLine("  os: " + report.OperatingSystemDescription);
        Console.WriteLine("  framework: " + report.FrameworkDescription);
        Console.WriteLine("  arch: " + report.ProcessArchitecture);
        Console.WriteLine("  rid: " + report.RuntimeIdentifier);
        Console.WriteLine("  display: " + report.DisplayServer.Diagnostic);
        Console.WriteLine();
    }

    private static void Print(string title, System.Collections.Generic.IReadOnlyList<LinuxNativeLibraryStatus> statuses)
    {
        Console.WriteLine(title + ":");
        foreach (LinuxNativeLibraryStatus status in statuses)
        {
            string state = status.IsAvailable ? "available" : "missing";
            Console.WriteLine("  " + status.Id + ": " + state + " - " + status.Diagnostic);
        }

        Console.WriteLine();
    }
}
