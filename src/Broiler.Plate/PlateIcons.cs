// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using Broiler.Graphics;
using Broiler.Graphics.Color;
using Broiler.Graphics.Geometry;
using Broiler.Graphics.Imaging;
using Broiler.Graphics.Rendering;
using Broiler.Graphics.RenderList;
using Broiler.Graphics.Text;

namespace Broiler.Plate;

/// <summary>
/// Resolution-independent toolbar icons and application mark for Broiler Plate, drawn as vector geometry.
/// </summary>
public static class PlateIcons
{
    /// <summary>The side of the square every icon is authored on.</summary>
    public const double DesignExtent = 16;

    /// <summary>The weight of a drawn line, in design units.</summary>
    private const double Stroke = 1.4;

    /// <summary>Draws a document sheet icon for opening files.</summary>
    public static void OpenFile(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Bar(3, 1.5, 6, Stroke);
        pen.Bar(3, 13.5, 9, Stroke);
        pen.Column(3, 1.5, 12, Stroke);
        pen.Column(11.3, 4.5, 9, Stroke);
        pen.Triangle(9, 1.5, 12, 4.5, 9, 4.5);
    }

    /// <summary>Draws a folder icon for opening directories.</summary>
    public static void OpenFolder(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Rect(1.5, 2.6, 6.2, 2.2);
        pen.Rect(1.5, 5.2, 13, 8.2);
    }

    /// <summary>Draws a close 'X' icon.</summary>
    public static void Close(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Line(3.5, 3.5, 12.5, 12.5, 1.8);
        pen.Line(12.5, 3.5, 3.5, 12.5, 1.8);
    }

    /// <summary>Draws a printer icon.</summary>
    public static void Print(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Outline(5, 1.5, 6, 4, Stroke);
        pen.RoundedRect(2, 4.5, 12, 6, 1.2);
        pen.Rect(4.5, 8.5, 7, 5);
        pen.Outline(4.5, 8.5, 7, 5, Stroke);
        pen.Bar(6, 11, 4, Stroke);
    }

    /// <summary>Draws a flatbed scanner icon.</summary>
    public static void Scan(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Line(2.5, 6, 12.5, 2.2, Stroke);
        pen.RoundedRect(2, 6, 12, 7.5, 1.2);
        pen.Bar(4, 9.75, 8, Stroke);
    }

    /// <summary>Draws a thumbnail grid icon for returning to gallery view.</summary>
    public static void Gallery(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Rect(2, 2, 5, 5);
        pen.Rect(9, 2, 5, 5);
        pen.Rect(2, 9, 5, 5);
        pen.Rect(9, 9, 5, 5);
    }

    /// <summary>Draws a folder with an up arrow for navigating to the parent folder.</summary>
    public static void UpFolder(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Rect(1.5, 6, 13, 7.5);
        pen.Triangle(8, 1.5, 4.5, 6, 11.5, 6);
        pen.Column(6.8, 5, 4.5, 2.4);
    }

    /// <summary>Draws a zoom-in magnifying glass with a plus sign.</summary>
    public static void ZoomIn(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Circle(2, 2, 8.5, Stroke);
        pen.Bar(4.25, 6.25, 4, Stroke);
        pen.Column(6.25, 4.25, 4, Stroke);
        pen.Line(8, 8, 13.5, 13.5, 2.0);
    }

    /// <summary>Draws a zoom-out magnifying glass with a minus sign.</summary>
    public static void ZoomOut(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Circle(2, 2, 8.5, Stroke);
        pen.Bar(4.25, 6.25, 4, Stroke);
        pen.Line(8, 8, 13.5, 13.5, 2.0);
    }

    /// <summary>Draws an actual size 1:1 icon.</summary>
    public static void ZoomReset(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Column(3.5, 3, 10, Stroke);
        pen.Triangle(1.8, 5.5, 3.8, 3, 3.8, 5.5);
        pen.Bar(2, 12.3, 4, Stroke);

        pen.Dot(7.5, 5.5, 1.8);
        pen.Dot(7.5, 9.5, 1.8);

        pen.Column(11.5, 3, 10, Stroke);
        pen.Triangle(9.8, 5.5, 11.8, 3, 11.8, 5.5);
        pen.Bar(10, 12.3, 4, Stroke);
    }

    /// <summary>Draws a fit-to-window framing icon.</summary>
    public static void ZoomFit(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Outline(2, 2, 12, 12, Stroke);
        pen.Rect(5, 5, 6, 6);
    }

    /// <summary>Draws a document notes icon.</summary>
    public static void Notes(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Outline(2.5, 1.5, 11, 13, Stroke);
        pen.Bar(5, 4.5, 6, Stroke);
        pen.Bar(5, 7.5, 6, Stroke);
        pen.Bar(5, 10.5, 4, Stroke);
    }

    /// <summary>Draws an information 'i' circle icon for the About dialog.</summary>
    public static void About(BRenderList list, BRect box, BColor color)
    {
        var pen = new Pen(list, box, color);
        pen.Circle(2, 2, 12, Stroke);
        pen.Dot(7.2, 4.5, 1.8);
        pen.Column(7.2, 7.5, 4.5, Stroke);
    }

    /// <summary>Draws the application mark: a plate tile with concentric circles.</summary>
    public static void AppMark(BRenderList list, BRect box, BColor page, BColor tile, BColor ink)
    {
        var backdrop = new Pen(list, box, tile);
        backdrop.RoundedRect(0, 0, 16, 16, 3.4);

        var plate = new Pen(list, box, page);
        plate.Circle(2.5, 2.5, 11, 1.4);
        plate.Circle(4.5, 4.5, 7, 1.0);

        var dot = new Pen(list, box, ink);
        dot.Dot(7, 7, 2);
    }

    /// <summary>Rasterises <see cref="AppMark"/> for a window icon or thumbnail.</summary>
    public static BPixelBuffer RenderAppIcon(int size, BColor page, BColor tile, BColor ink)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "An icon needs a positive size.");

        var list = new BRenderList();
        AppMark(list, new BRect(0, 0, size, size), page, tile, ink);

        using var renderer = new BImageRenderer();
        using BBitmap bitmap = renderer.RenderToImage(
            list,
            BSurfaceDescriptor.Default(new BSize(size, size)),
            new BFrameContext(BColor.FromArgb(0, 0, 0, 0)));

        return bitmap.ToPixelBuffer();
    }

    private readonly struct Pen(BRenderList list, BRect box, BColor color)
    {
        private readonly double _scale = box.Width / DesignExtent;

        private double X(double x) => box.Left + (x * _scale);
        private double Y(double y) => box.Top + (y * _scale);
        private double S(double v) => v * _scale;

        public void Bar(double x, double y, double length, double thickness) =>
            Rect(x, y - (thickness / 2), length, thickness);

        public void Column(double x, double y, double length, double thickness) =>
            Rect(x, y, thickness, length);

        public void Rect(double x, double y, double width, double height) =>
            list.FillRect(new BRect(X(x), Y(y), S(width), S(height)), color);

        public void RoundedRect(double x, double y, double width, double height, double radius) =>
            list.FillRoundedRect(new BRect(X(x), Y(y), S(width), S(height)), color, S(radius), S(radius));

        public void Dot(double x, double y, double diameter) =>
            list.FillRoundedRect(
                new BRect(X(x), Y(y), S(diameter), S(diameter)),
                color,
                S(diameter / 2),
                S(diameter / 2));

        public void Circle(double x, double y, double diameter, double thickness) =>
            list.StrokeRoundedRect(
                new BRect(X(x), Y(y), S(diameter), S(diameter)),
                color,
                S(diameter / 2),
                S(diameter / 2),
                S(thickness));

        public void Outline(double x, double y, double width, double height, double thickness)
        {
            Rect(x, y, width, thickness);
            Rect(x, y + height - thickness, width, thickness);
            Rect(x, y, thickness, height);
            Rect(x + width - thickness, y, thickness, height);
        }

        public void Triangle(double ax, double ay, double bx, double by, double cx, double cy) =>
            list.FillTriangle(new BPoint(X(ax), Y(ay)), new BPoint(X(bx), Y(by)), new BPoint(X(cx), Y(cy)), color);

        public void Line(double ax, double ay, double bx, double by, double thickness)
        {
            double dx = bx - ax;
            double dy = by - ay;
            double length = Math.Sqrt((dx * dx) + (dy * dy));
            if (length <= 0)
                return;

            double nx = -dy / length * (thickness / 2);
            double ny = dx / length * (thickness / 2);

            Triangle(ax + nx, ay + ny, bx + nx, by + ny, bx - nx, by - ny);
            Triangle(ax + nx, ay + ny, bx - nx, by - ny, ax - nx, ay - ny);
        }
    }
}
