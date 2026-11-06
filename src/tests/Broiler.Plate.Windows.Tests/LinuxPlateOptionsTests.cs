// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace Broiler.Plate.Windows.Tests;

public sealed class LinuxPlateOptionsTests
{
    [Fact]
    public void Default_Options_Are_Sensible()
    {
        var options = LinuxPlateOptions.Parse([]);

        Assert.Equal(1120, options.Width);
        Assert.Equal(780, options.Height);
        Assert.Null(options.ArtifactDirectory);
        Assert.Null(options.InitialPath);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void Flag_Options_Are_Parsed_Correctly()
    {
        var options = LinuxPlateOptions.Parse([
            "--offscreen",
            "--no-input",
            "--duration-ms=2500",
            "--size=1920x1080",
            "--artifact-dir=/tmp/test-output",
            "--input-ignore-focus",
            "/path/to/sample.pdf",
        ]);

        Assert.False(options.OpenWindow);
        Assert.False(options.EnableEvdevInput);
        Assert.Equal(2500, options.DurationMilliseconds);
        Assert.Equal(1920, options.Width);
        Assert.Equal(1080, options.Height);
        Assert.Equal("/tmp/test-output", options.ArtifactDirectory);
        Assert.True(options.IgnoreInputFocus);
        Assert.Equal("/path/to/sample.pdf", options.InitialPath);
    }

    [Fact]
    public void Size_Options_Are_Clamped_To_Limits()
    {
        var optionsTooSmall = LinuxPlateOptions.Parse(["--width=100", "--height=50"]);
        Assert.Equal(640, optionsTooSmall.Width);
        Assert.Equal(480, optionsTooSmall.Height);

        var optionsTooLarge = LinuxPlateOptions.Parse(["--size=5000x3000"]);
        Assert.Equal(3840, optionsTooLarge.Width);
        Assert.Equal(2160, optionsTooLarge.Height);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public void Help_Flag_Sets_ShowHelp(string flag)
    {
        var options = LinuxPlateOptions.Parse([flag]);
        Assert.True(options.ShowHelp);
    }
}
