namespace Broiler.Plate.Windows.Tests;

public sealed class PlateThumbnailCacheTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "plate-cache-" + Guid.NewGuid().ToString("N"));

    public PlateThumbnailCacheTests() => Directory.CreateDirectory(_folder);

    [Fact]
    public void Unchanged_Files_Reuse_Thumbnails_And_Changed_Or_Deleted_Files_Do_Not()
    {
        string path = WriteFile("image.png", "first");
        int decodes = 0;
        using var cache = new PlateThumbnailCache((file, _) => { decodes++; return File.ReadAllBytes(file); });
        byte[] first = cache.GetOrCreate(path, default)!;
        Assert.Same(first, cache.GetOrCreate(Path.Combine(_folder, ".", "image.png"), default));
        Assert.Equal(1, decodes);

        // A length change invalidates even if the timestamp has not advanced.
        DateTime time = File.GetLastWriteTimeUtc(path);
        File.WriteAllText(path, "longer image");
        File.SetLastWriteTimeUtc(path, time);
        byte[] second = cache.GetOrCreate(path, default)!;
        Assert.NotSame(first, second);
        Assert.Equal(2, decodes);

        // A timestamp change invalidates even if the length stays the same.
        File.SetLastWriteTimeUtc(path, time.AddMinutes(1));
        Assert.NotSame(second, cache.GetOrCreate(path, default));
        Assert.Equal(3, decodes);
        File.Delete(path);
        Assert.Null(cache.GetOrCreate(path, default));
        Assert.Equal(3, decodes);
    }

    [Theory]
    [InlineData(2, 100)]
    [InlineData(100, 8)]
    public void Capacity_And_Byte_Limits_Evict_The_Least_Recently_Used_Thumbnail(int capacity, long bytes)
    {
        string a = WriteFile("a.png", "aaaa"), b = WriteFile("b.png", "bbbb"), c = WriteFile("c.png", "cccc");
        var decodes = new Dictionary<string, int>();
        using var cache = new PlateThumbnailCache((path, _) =>
        {
            decodes[path] = decodes.GetValueOrDefault(path) + 1;
            return File.ReadAllBytes(path);
        }, capacity, bytes);
        byte[] first = cache.GetOrCreate(a, default)!;
        cache.GetOrCreate(b, default);
        Assert.Same(first, cache.GetOrCreate(a, default));
        cache.GetOrCreate(c, default);
        Assert.Same(first, cache.GetOrCreate(a, default));
        cache.GetOrCreate(b, default);
        Assert.Equal(1, decodes[a]);
        Assert.Equal(2, decodes[b]);
    }

    [Fact]
    public void Cancellation_And_Failed_Decoding_Do_Not_Poison_The_Cache()
    {
        string path = WriteFile("image.png", "image");
        using var cancellation = new CancellationTokenSource();
        int decodes = 0;
        using var cache = new PlateThumbnailCache((_, _) =>
        {
            decodes++;
            if (decodes == 1)
            {
                cancellation.Cancel();
                return [1];
            }
            return decodes == 2 ? null : [3];
        });
        Assert.Null(cache.GetOrCreate(path, cancellation.Token));
        Assert.Null(cache.GetOrCreate(path, default));
        byte[] result = Assert.IsType<byte[]>(cache.GetOrCreate(path, default));
        Assert.Equal(new byte[] { 3 }, result);
        Assert.Same(result, cache.GetOrCreate(path, default));
        Assert.Equal(3, decodes);
    }

    [Fact]
    public async Task Disposal_During_Decoding_Does_Not_Retain_The_Result()
    {
        string path = WriteFile("image.png", "image");
        using var started = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();
        using var cache = new PlateThumbnailCache((_, _) =>
        {
            started.Set();
            Assert.True(resume.Wait(TimeSpan.FromSeconds(5)));
            return [1];
        });
        Task<byte[]?> loading = Task.Run(() => cache.GetOrCreate(path, default));
        try
        {
            Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
            cache.Dispose();
        }
        finally { resume.Set(); }
        Assert.Null(await loading);
        Assert.Null(cache.GetOrCreate(path, default));
    }

    private string WriteFile(string name, string content)
    {
        string path = Path.Combine(_folder, name);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);
}
