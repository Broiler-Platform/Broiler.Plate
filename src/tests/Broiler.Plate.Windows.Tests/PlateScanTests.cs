// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using Broiler.Graphics.Color;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Imaging;
using Broiler.Graphics.Paging;
using Broiler.Graphics.Rendering;
using Broiler.Input;
using Broiler.Input.Keyboard;
using Broiler.Input.Scanner;
using Broiler.UI;
using Broiler.UI.PrintDialog.Standard;
using Broiler.UI.ScanDialog.Standard;

namespace Broiler.Plate.Windows.Tests;

/// <summary>
/// File, Scan in the viewer: acquiring a page through <see cref="StandardScanDialog"/>,
/// displaying it in the image view, printing it without a backing file on disk, and
/// adapting <see cref="IScannerInputProvider"/> via <see cref="PlateScanHost"/>.
/// </summary>
public sealed class PlateScanTests
{
    [Fact]
    public void Ctrl_Shift_S_Scans_A_Page_Into_The_Viewer_And_Allows_Printing_It()
    {
        using var renderer = new BImageRenderer();
        var scanner = new FakeScanHost();
        var printer = new FakePrintHost();
        using var host = new PlateUiHost(
            () => new BSize(1200, 800),
            () => 1,
            () => { },
            _ => { },
            getRenderer: () => renderer);
        using var app = new PlateApp(host, () => { }, printHost: printer, scanHost: scanner);

        Assert.True(app.CanScan);
        Assert.True(app.ScanButton!.IsEnabled);

        app.Dispatch(CtrlShiftS());
        StandardScanDialog scanDialog = Assert.IsType<StandardScanDialog>(app.Session.ModalElement);
        Assert.True(scanDialog.AcceptScan());
        WaitForScan(app);

        Assert.Single(scanner.Jobs);
        Assert.Equal(PlateViewKind.Image, app.CurrentView);
        Assert.Equal("Scanned 1 page", app.LastAction);
        Assert.True(app.CanPrint);

        app.Dispatch(CtrlP());
        StandardPrintDialog printDialog = Assert.IsType<StandardPrintDialog>(app.Session.ModalElement);
        Assert.True(printDialog.AcceptPrint());
        WaitForPrint(app);

        UiPrintJob printJob = Assert.Single(printer.Jobs);
        Assert.Equal(1, printJob.ResolvePages().PageCount);
        Assert.Equal("Printed 1 page", app.LastAction);
    }

    [Fact]
    public async Task PlateScanHost_Enumerates_Devices_And_Converts_Scanned_Pages()
    {
        var provider = new FakeScannerProvider();
        var scanHost = new PlateScanHost(provider);

        IReadOnlyList<UiScannerInfo> scanners = scanHost.GetScanners();
        UiScannerInfo info = Assert.Single(scanners);
        Assert.Equal("Broiler Test Flatbed", info.Name);
        Assert.True(info.IsDefault);

        UiScannerCapabilities capabilities = scanHost.GetCapabilities(info.Name);
        Assert.Contains(UiScanSource.Flatbed, capabilities.Sources);
        Assert.Contains(UiScanColorMode.Color, capabilities.ColorModes);
        Assert.Contains(300, capabilities.Resolutions);

        var settings = new UiScanSettings
        {
            ScannerName = info.Name,
            Source = UiScanSource.Flatbed,
            ColorMode = UiScanColorMode.Color,
            Dpi = 300,
            PageSetup = new BPageSetup(BPaperSize.A4, BPageOrientation.Portrait, BPageMargins.Uniform(0)),
        };
        var reportedProgress = new List<UiScanProgress>();
        UiScanResult result = await scanHost.ScanAsync(new UiScanJob(settings, isPreview: false, reportedProgress.Add));

        Assert.Equal(UiScanOutcome.Scanned, result.Outcome);
        Assert.Equal(1, result.PagesScanned);
        Assert.NotEmpty(reportedProgress);
        Assert.Contains(reportedProgress, static p => p.Fraction >= 0.5);
        Assert.Equal(1.0, reportedProgress[^1].Fraction, precision: 3);
        Assert.True(PlateScanHost.TryExtractFirstPagePixels(result.Pages, out BPixelBuffer pixels));
        Assert.Equal(4, pixels.Width);
        Assert.Equal(3, pixels.Height);
        ReadOnlySpan<byte> bytes = pixels.Rgba;
        Assert.Equal(20, bytes[0]);
        Assert.Equal(40, bytes[1]);
        Assert.Equal(200, bytes[2]);
        Assert.Equal(255, bytes[3]);
    }

    [Fact]
    public async Task Preview_In_ScanDialog_Fills_Preview_And_Accepting_Scan_Reuses_It_In_Viewer()
    {
        using var renderer = new BImageRenderer();
        var scanner = new FakeScanHost();
        using var host = new PlateUiHost(
            () => new BSize(1200, 800),
            () => 1,
            () => { },
            _ => { },
            getRenderer: () => renderer);
        using var app = new PlateApp(host, () => { }, scanHost: scanner);

        app.Dispatch(CtrlShiftS());
        StandardScanDialog scanDialog = Assert.IsType<StandardScanDialog>(app.Session.ModalElement);

        UiScanResult previewResult = await scanDialog.PreviewAsync();
        Assert.Equal(UiScanOutcome.Scanned, previewResult.Outcome);
        Assert.Equal(1, scanDialog.Preview.PageCount);
        Assert.True(scanDialog.HasMatchingPreview);
        Assert.Single(scanner.Jobs);
        Assert.True(scanner.Jobs[0].IsPreview);

        Assert.True(scanDialog.AcceptScan());
        WaitForScan(app);

        // Because the preview already acquired the single requested page for the exact same
        // settings, accepting the dialog reuses that page instead of scanning the flatbed twice.
        Assert.Single(scanner.Jobs);
        Assert.Equal(PlateViewKind.Image, app.CurrentView);
        Assert.Equal("Scanned 1 page", app.LastAction);
    }

    [Fact]
    public void Without_ScanHost_Scan_Is_Disabled()
    {
        using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
        using var app = new PlateApp(host, () => { });

        Assert.False(app.CanScan);
        Assert.False(app.ScanButton!.IsEnabled);
        app.Dispatch(CtrlShiftS());
        Assert.Null(app.Session.ModalElement);
    }

    private static void WaitForScan(PlateApp app)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        do
        {
            app.RenderFrame();
            if (!app.IsScanning)
                return;

            Thread.Sleep(5);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Fail("The scan job never finished.");
    }

    private static void WaitForPrint(PlateApp app)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        do
        {
            app.RenderFrame();
            if (!app.IsPrinting)
                return;

            Thread.Sleep(5);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Fail("The print job never finished.");
    }

    private static UiInputEvent CtrlShiftS()
    {
        var header = new InputEventHeader(InputDeviceId.FromOpaqueValue("plate-scan-test"), new InputTimestamp(1, 1000, "test"), 1);
        return UiInputEvent.FromKeyboardKey(new KeyboardKeyEvent(
            header,
            KeyboardKey.FromName("S"),
            KeyboardKeyTransition.Down,
            KeyboardModifierState.Control | KeyboardModifierState.Shift,
            0,
            0,
            1,
            false,
            false));
    }

    private static UiInputEvent CtrlP()
    {
        var header = new InputEventHeader(InputDeviceId.FromOpaqueValue("plate-scan-test"), new InputTimestamp(2, 1000, "test"), 2);
        return UiInputEvent.FromKeyboardKey(new KeyboardKeyEvent(
            header,
            KeyboardKey.FromName("P"),
            KeyboardKeyTransition.Down,
            KeyboardModifierState.Control,
            0,
            0,
            1,
            false,
            false));
    }

    private sealed class FakeScanHost : IUiScanHost
    {
        public List<UiScanJob> Jobs { get; } = [];

        public bool ShowsSystemScanDialog => false;

        public IReadOnlyList<UiScannerInfo> GetScanners() =>
            [new UiScannerInfo("Office Flatbed", IsDefault: true)];

        public UiScannerCapabilities GetCapabilities(string scannerName) => UiScannerCapabilities.Unknown;

        public Task<UiScanResult> ScanAsync(UiScanJob job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            using var bitmap = new BBitmap(32, 24);
            bitmap.Clear(new BColor(40, 120, 220));
            return Task.FromResult(UiScanResult.Scanned([bitmap.ToPixelBuffer()], job.Settings.Dpi, job.Settings.PageSetup.PageSize));
        }
    }

    private sealed class FakePrintHost : IUiPrintHost
    {
        public List<UiPrintJob> Jobs { get; } = [];

        public bool ShowsSystemPrintDialog => false;

        public IReadOnlyList<UiPrinterInfo> GetPrinters() =>
            [new UiPrinterInfo("Office Printer", IsDefault: true)];

        public UiPrinterCapabilities GetCapabilities(string printerName) => UiPrinterCapabilities.Unknown;

        public Task<UiPrintResult> PrintAsync(UiPrintJob job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return Task.FromResult(UiPrintResult.Printed(job.ResolvePages(cancellationToken).PageCount));
        }
    }

    private sealed class FakeScannerProvider : IScannerInputProvider
    {
        private static readonly InputDeviceDescriptor Descriptor = new(
            InputDeviceId.FromOpaqueValue("fake:scanner:0"),
            InputKind.Scanner,
            "Broiler Test Flatbed",
            InputDeviceAvailability.Available,
            [new InputCapability("scanner.capture.source", "fake")]);

        public ValueTask<IReadOnlyList<InputDeviceDescriptor>> GetDevicesAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<InputDeviceDescriptor>>([Descriptor]);

        public ValueTask<ScannerInputDevice> OpenAsync(
            InputDeviceDescriptor descriptor,
            ScannerOpenOptions options,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScannerInputDevice>(new FakeScannerDevice(descriptor, options));
    }

    private sealed class FakeScannerDevice(InputDeviceDescriptor descriptor, ScannerOpenOptions options)
        : ScannerInputDevice(descriptor)
    {
        public override async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            TransitionCaptureTo(ScannerCaptureState.Starting);
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
            TransitionCaptureTo(ScannerCaptureState.Scanning);

            int width = 4;
            int height = 3;
            byte[] rgba = new byte[width * height * 4];
            for (int i = 0; i < rgba.Length; i += 4)
            {
                rgba[i] = 20;
                rgba[i + 1] = 40;
                rgba[i + 2] = 200;
                rgba[i + 3] = 255;
            }

            ScannerFormat format = options.PreferredFormat
                ?? new ScannerFormat(ScannerSourceKind.Flatbed, 300, 300, ScannerPixelFormat.Rgba32);
            SetNegotiatedFormat(format);
            RaiseProgress(0, 0.5, rgba.Length / 2);
            var lease = new ScannerPageLease(
                rgba,
                width,
                height,
                format,
                [new ScannerImagePlane(0, rgba.Length, width * 4, width, height)],
                Clock.GetTimestamp(),
                pageNumber: 0,
                flags: ScannerPageFlags.EndOfBatch,
                rotation: ScannerRotation.None,
                colorSpace: ScannerColorSpace.SRgb);
            RaisePageReady(lease);
            MarkCaptureCompleted();
        }
    }
}
