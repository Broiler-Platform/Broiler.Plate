// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Broiler.Plate;

/// <summary>Session-local LRU cache of encoded thumbnails, independent of renderer handles.</summary>
internal sealed class PlateThumbnailCache : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedListNode<Entry>> _entries = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly LinkedList<Entry> _recent = new();
    private readonly Func<string, CancellationToken, byte[]?> _create;
    private readonly int _maxEntries;
    private readonly long _maxBytes;
    private long _bytes;
    private bool _disposed;

    internal PlateThumbnailCache(Func<string, CancellationToken, byte[]?> create,
        int maxEntries = 256, long maxBytes = 32 * 1024 * 1024)
    {
        ArgumentNullException.ThrowIfNull(create);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEntries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        _create = create;
        _maxEntries = maxEntries;
        _maxBytes = maxBytes;
    }

    /// <summary>Called by the thumbnail worker; source files are decoded outside the cache lock.</summary>
    internal byte[]? GetOrCreate(string path, CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return null;
        string fullPath = Path.GetFullPath(path);
        Stamp? stamp = ReadStamp(fullPath);
        lock (_gate)
        {
            if (_disposed)
                return null;
            if (_entries.TryGetValue(fullPath, out var existing))
            {
                if (stamp is not null && existing.Value.Stamp == stamp.Value)
                {
                    _recent.Remove(existing);
                    _recent.AddFirst(existing);
                    return existing.Value.Bytes;
                }
                Remove(existing);
            }
        }
        if (stamp is null)
            return null;

        byte[]? thumbnail = _create(fullPath, cancellation);
        // Never remember cancelled/failed work, or bytes generated while the source was changing.
        if (cancellation.IsCancellationRequested)
            return null;
        if (thumbnail is null || ReadStamp(fullPath) != stamp)
            return null;
        lock (_gate)
        {
            if (_disposed || cancellation.IsCancellationRequested)
                return null;
            if (thumbnail.LongLength > _maxBytes)
                return thumbnail;
            if (_entries.TryGetValue(fullPath, out var existing))
                Remove(existing);
            var node = _recent.AddFirst(new Entry(fullPath, stamp.Value, thumbnail));
            _entries.Add(fullPath, node);
            _bytes += thumbnail.LongLength;
            while (_entries.Count > _maxEntries || _bytes > _maxBytes)
                Remove(_recent.Last!);
        }
        return thumbnail;
    }

    private static Stamp? ReadStamp(string path)
    {
        try
        {
            var file = new FileInfo(path);
            return file.Exists ? new Stamp(file.Length, file.LastWriteTimeUtc.Ticks) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void Remove(LinkedListNode<Entry> node)
    {
        _entries.Remove(node.Value.Path);
        _recent.Remove(node);
        _bytes -= node.Value.Bytes.LongLength;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _entries.Clear();
            _recent.Clear();
            _bytes = 0;
        }
    }

    private readonly record struct Stamp(long Length, long LastWriteTicks);
    private sealed record Entry(string Path, Stamp Stamp, byte[] Bytes);
}
