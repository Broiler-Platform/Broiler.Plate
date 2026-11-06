// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Broiler.Plate.WebAssembly;

[SupportedOSPlatform("browser")]
internal static class Program
{
    public static async Task Main()
    {
        try
        {
            await BrowserPlateApp.StartAsync();
        }
        catch (Exception exception)
        {
            Environment.ExitCode = 1;
            BrowserInterop.Failed(exception.GetType().Name, exception.ToString());
        }

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}
