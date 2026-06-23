namespace TenderDocs.Application.Common.Interfaces;

/// <summary>
/// Result of running an uploaded file through the compression pipeline.
/// <see cref="Content"/> is positioned at the start and owned by the caller (dispose it).
/// When the file could not be made smaller (or its type is not compressible) the original
/// bytes are returned unchanged and <see cref="WasCompressed"/> is <c>false</c>.
/// </summary>
public sealed record CompressionResult(
    Stream Content,
    string FileName,
    string ContentType,
    long OriginalSizeBytes,
    long CompressedSizeBytes) : IAsyncDisposable
{
    /// <summary>True when the pipeline actually reduced the file size.</summary>
    public bool WasCompressed => CompressedSizeBytes < OriginalSizeBytes;

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

/// <summary>
/// Shrinks uploaded documents in-place before they are handed to a storage provider.
/// This is real size reduction (re-encoding images, recompressing PDFs and Office media),
/// not container-level zipping. Implementations must never increase the file size and must
/// always fall back to the original bytes if anything goes wrong.
/// </summary>
public interface IDocumentCompressor
{
    Task<CompressionResult> CompressAsync(
        Stream content, string fileName, string contentType, CancellationToken ct = default);
}
