using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.Input.Keyboard;

namespace Broiler.Plate;

/// <summary>What a zoom gesture asked for.</summary>
internal enum PlateZoomStep
{
    /// <summary>The gesture was not a zoom one.</summary>
    None = 0,

    /// <summary>The next level up.</summary>
    In,

    /// <summary>The next level down.</summary>
    Out,

    /// <summary>Back to the size the content states.</summary>
    Reset,
}

/// <summary>
/// The zoom levels the viewer offers and the policy around them.
/// </summary>
/// <remarks>
/// <para>
/// This is the Writer's <c>WriterZoom</c> ladder, restated here rather than
/// shared: the two applications agree that 150% means 150%, and both step along
/// the same rungs, but the Writer's copy lives in an assembly this repository
/// does not carry, and a viewer is not a reason to make one product depend on
/// another. The levels are a ladder rather than a free scale because the
/// toolbar, the menu and Ctrl+plus all step along it, and a free scale would
/// leave the three disagreeing about what one step means.
/// </para>
/// <para>
/// Fit is not on the ladder. It is not a scale at all - it is the instruction
/// "show all of it", which resolves to whatever scale the window happens to
/// allow - so it is carried as a null zoom rather than as a number, and only the
/// picture view offers it. Nothing here reads or writes a file: zoom is how
/// something is being looked at, not part of it.
/// </para>
/// </remarks>
internal static class PlateZoom
{
    /// <summary>Showing content at exactly the size it states.</summary>
    public const double Default = 1;

    /// <summary>How fit-to-window is written wherever the user sees it.</summary>
    public const string FitName = "Fit";

    /// <summary>
    /// How close two zooms must be to count as the same level. The ladder is
    /// written as decimal literals and stepped rather than accumulated, so this
    /// only has to absorb the last bits of a scale that was computed from a
    /// window size.
    /// </summary>
    private const double Tolerance = 1e-9;

    /// <summary>
    /// The levels offered, smallest first. A quarter size shows a whole page of
    /// an A4 document at a glance; four times it is enough to read a footnote set
    /// in six point.
    /// </summary>
    public static IReadOnlyList<double> Levels { get; } = [0.25, 0.5, 0.75, 1, 1.25, 1.5, 2, 3, 4];

    /// <summary>The smallest level, which is also the floor an arbitrary zoom is clamped to.</summary>
    public static double Minimum => Levels[0];

    /// <summary>The largest level, which is also the ceiling an arbitrary zoom is clamped to.</summary>
    public static double Maximum => Levels[^1];

    /// <summary>
    /// A zoom brought into range. Anything that is not a number reads as the
    /// stated size rather than as an error: there is no zoom to fall back to
    /// other than the one the content asked for.
    /// </summary>
    public static double Normalize(double zoom) =>
        double.IsFinite(zoom) ? Math.Clamp(zoom, Minimum, Maximum) : Default;

    /// <summary>Whether two zooms are the same level.</summary>
    public static bool Same(double left, double right) => Math.Abs(left - right) <= Tolerance;

    /// <summary>Whether two choices are the same, counting fit-to-window as a choice.</summary>
    public static bool SameChoice(double? left, double? right) =>
        left is double first ? right is double second && Same(first, second) : right is null;

    /// <summary>
    /// The first level above <paramref name="zoom"/>, or the largest when it is
    /// already there. A zoom that sits between two levels - which is what fit
    /// resolves to - steps to the one above it rather than snapping down first.
    /// </summary>
    public static double In(double zoom)
    {
        double current = Normalize(zoom);
        foreach (double level in Levels)
        {
            if (level > current + Tolerance)
                return level;
        }

        return Maximum;
    }

    /// <summary>The first level below <paramref name="zoom"/>, or the smallest when it is already there.</summary>
    public static double Out(double zoom)
    {
        double current = Normalize(zoom);
        for (int i = Levels.Count - 1; i >= 0; i--)
        {
            if (Levels[i] < current - Tolerance)
                return Levels[i];
        }

        return Minimum;
    }

    /// <summary>Applies a step to a zoom.</summary>
    public static double Apply(double zoom, PlateZoomStep step) => step switch
    {
        PlateZoomStep.In => In(zoom),
        PlateZoomStep.Out => Out(zoom),
        PlateZoomStep.Reset => Default,
        _ => Normalize(zoom),
    };

    /// <summary>
    /// How a zoom is written wherever the user sees it: a whole percentage, or
    /// the word for fit-to-window, which is not a percentage and is not written
    /// as one.
    /// </summary>
    public static string Describe(double? zoom) =>
        zoom is double scale
            ? Math.Round(Normalize(scale) * 100).ToString("0", CultureInfo.InvariantCulture) + "%"
            : FitName;

    /// <summary>
    /// The step a key press asks for, or <see cref="PlateZoomStep.None"/>. Ctrl
    /// with plus, minus and zero, matched by name and by native code alike: a
    /// head that names its keys and one that only numbers them both have to
    /// reach the same step.
    /// </summary>
    /// <remarks>
    /// A held key repeats on purpose - zooming several levels is one gesture, not
    /// one press per level - so this does not refuse a repeat.
    /// </remarks>
    public static PlateZoomStep StepFor(
        string? keyName,
        int nativeKeyCode,
        KeyboardModifierState modifiers,
        bool isDown)
    {
        if (!isDown ||
            (modifiers & KeyboardModifierState.Control) == KeyboardModifierState.None ||
            (modifiers & KeyboardModifierState.Alt) != KeyboardModifierState.None)
        {
            return PlateZoomStep.None;
        }

        if (IsKey(keyName, nativeKeyCode, OemPlus, "+", "=", "Equal", "Plus") ||
            IsKey(keyName, nativeKeyCode, NumpadAdd, "Add", "NumpadAdd"))
        {
            return PlateZoomStep.In;
        }

        if (IsKey(keyName, nativeKeyCode, OemMinus, "-", "_", "Minus") ||
            IsKey(keyName, nativeKeyCode, NumpadSubtract, "Subtract", "NumpadSubtract"))
        {
            return PlateZoomStep.Out;
        }

        if (IsKey(keyName, nativeKeyCode, Digit0, "0", "Digit0") ||
            IsKey(keyName, nativeKeyCode, Numpad0, "Numpad0"))
        {
            return PlateZoomStep.Reset;
        }

        return PlateZoomStep.None;
    }

    /// <summary>
    /// The step a wheel notch asks for while Ctrl is held, which is how a mouse
    /// zooms. <paramref name="controlHeld"/> is passed rather than read off the
    /// wheel event because not every head fills a pointer event's modifiers in.
    /// </summary>
    public static PlateZoomStep StepForWheel(bool controlHeld, double notches)
    {
        if (!controlHeld || !double.IsFinite(notches) || notches == 0)
            return PlateZoomStep.None;

        return notches > 0 ? PlateZoomStep.In : PlateZoomStep.Out;
    }

    // The Win32 virtual-key codes, which the browser reports as keyCode too.
    private const int OemPlus = 0xBB;
    private const int OemMinus = 0xBD;
    private const int NumpadAdd = 0x6B;
    private const int NumpadSubtract = 0x6D;
    private const int Digit0 = 0x30;
    private const int Numpad0 = 0x60;

    private static bool IsKey(string? keyName, int nativeKeyCode, int virtualKey, params string[] names)
    {
        if (nativeKeyCode == virtualKey)
            return true;

        if (string.IsNullOrEmpty(keyName))
            return false;

        if (string.Equals(
                keyName,
                "VirtualKey:" + virtualKey.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
        {
            return true;
        }

        foreach (string name in names)
        {
            if (string.Equals(keyName, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
