using System.Text;
using Broiler.Documents;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Imaging;
using Broiler.Graphics.Paging;
using Broiler.Graphics.RenderList;
using Broiler.Input;
using Broiler.Input.Keyboard;
using Broiler.Media;
using Broiler.Media.Image.Managed;
using Broiler.UI;
using Broiler.UI.PrintDialog.Standard;

namespace Broiler.Plate.Windows.Tests;

/// <summary>
/// File, Print in the viewer: a document laid out on the page it states, a picture fitted to the
/// page, and a file whose own rules refuse printing left unprinted.
/// </summary>
public sealed class PlatePrintTests
{
    [Fact]
    public void A_Picture_Fills_The_Content_Box_Without_Being_Cropped_Or_Stretched()
    {
        BRect fitted = PlatePicturePrintDocument.Fit(new BSize(400, 100), new BRect(10, 20, 200, 200));

        Assert.Equal(new BRect(10, 95, 200, 50), fitted);
        Assert.Equal(BRect.Empty, PlatePicturePrintDocument.Fit(BSize.Empty, new BRect(0, 0, 10, 10)));
    }

    [Fact]
    public void A_Wide_Picture_Asks_For_A_Landscape_Page_And_Carries_Its_Pixels()
    {
        BImageCodecs.Use(new MediaCodecCatalog(ManagedImageCodecs.CreateCodecs()));
        string path = Path.Combine(Path.GetTempPath(), "plate-print-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            using (var bitmap = new BBitmap(60, 20))
            {
                bitmap.Clear(Broiler.Graphics.Color.BColor.Red);
                bitmap.Save(path);
            }

            var document = new PlatePicturePrintDocument(path, new BSize(60, 20));
            Assert.Equal(BPageOrientation.Landscape, document.PreferredSetup.Orientation);

            BPage page = document.Paginate(document.PreferredSetup).GetPage(0);
            BRenderCommand.DrawImage draw = Assert.Single(page.Commands.OfType<BRenderCommand.DrawImage>());
            Assert.Equal(60, page.Images[draw.Image].Width);
            Assert.Equal(document.PreferredSetup.ContentBox.Width, draw.Destination.Width, 6);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_Document_That_Is_Not_An_Encrypted_PDF_May_Be_Printed()
    {
        DocumentReadResult plain = new Broiler.Documents.Rtf.RtfDocumentCodec().Read(
            new MemoryStream(Encoding.ASCII.GetBytes(@"{\rtf1\ansi Hello}")), new DocumentReadOptions());

        Assert.Equal(PlatePrintPermission.Allowed, PlatePdfPrintPermission.For(plain));
    }

    [Fact]
    public void Ctrl_P_Prints_The_Open_Document_Through_The_Dialog()
    {
        string path = Path.Combine(Path.GetTempPath(), "plate-print-" + Guid.NewGuid().ToString("N") + ".rtf");
        File.WriteAllText(path, @"{\rtf1\ansi A page to print.}");
        try
        {
            var printer = new FakePrintHost();
            using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
            using var app = new PlateApp(host, () => { }, printHost: printer);
            Assert.True(app.OpenFile(path));

            app.Dispatch(CtrlP());
            StandardPrintDialog dialog = Assert.IsType<StandardPrintDialog>(app.Session.ModalElement);
            Assert.True(dialog.AcceptPrint());
            WaitForPrint(app);

            UiPrintJob job = Assert.Single(printer.Jobs);
            Assert.Equal(Path.GetFileName(path), job.Title);
            Assert.Equal(1, job.ResolvePages().PageCount);
            Assert.Equal("Printed 1 page", app.LastAction);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_File_That_Refuses_Printing_Is_Not_Printed()
    {
        string path = Path.Combine(Path.GetTempPath(), "plate-print-" + Guid.NewGuid().ToString("N") + ".rtf");
        File.WriteAllText(path, @"{\rtf1\ansi Not for paper.}");
        try
        {
            var printer = new FakePrintHost();
            using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
            using var app = new PlateApp(host, () => { }, printHost: printer, printPermission: _ => PlatePrintPermission.Refused);
            Assert.True(app.OpenFile(path));

            Assert.False(app.CanPrint);
            app.Dispatch(CtrlP());

            Assert.Null(app.Session.ModalElement);
            Assert.Empty(printer.Jobs);
            Assert.EndsWith("does not permit printing", app.LastAction, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Nothing_Open_Means_Nothing_To_Print()
    {
        using var host = new PlateUiHost(() => new BSize(1200, 800), () => 1, () => { }, _ => { });
        using var app = new PlateApp(host, () => { }, printHost: new FakePrintHost());

        Assert.False(app.CanPrint);
        app.Dispatch(CtrlP());
        Assert.Null(app.Session.ModalElement);
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

    private static UiInputEvent CtrlP()
    {
        var header = new InputEventHeader(InputDeviceId.FromOpaqueValue("plate-print-test"), new InputTimestamp(1, 1000, "test"), 1);
        return UiInputEvent.FromKeyboardKey(new KeyboardKeyEvent(header, KeyboardKey.FromName("P"), KeyboardKeyTransition.Down,
            KeyboardModifierState.Control, 0, 0, 1, false, false));
    }

    private sealed class FakePrintHost : IUiPrintHost
    {
        public List<UiPrintJob> Jobs { get; } = [];

        public bool ShowsSystemPrintDialog => false;

        public IReadOnlyList<UiPrinterInfo> GetPrinters() => [new UiPrinterInfo("Office", IsDefault: true)];

        public UiPrinterCapabilities GetCapabilities(string printerName) => UiPrinterCapabilities.Unknown;

        public Task<UiPrintResult> PrintAsync(UiPrintJob job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return Task.FromResult(UiPrintResult.Printed(job.ResolvePages(cancellationToken).PageCount));
        }
    }
}
