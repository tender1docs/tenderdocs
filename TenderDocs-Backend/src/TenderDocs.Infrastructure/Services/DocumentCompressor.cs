using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Infrastructure.Services;

/// <summary>
/// Real size reduction for uploaded documents. Dispatches by file type:
///   • Images (jpg/png/webp/bmp/tiff/gif) -> downscale + re-encode via ImageSharp
///   • PDFs                               -> Ghostscript (if available) else PdfSharp re-save
///   • Office (docx/xlsx/pptx + macro)    -> repack the OOXML zip at max deflate, recompress media
/// Everything else is passed through untouched. The result is never larger than the input:
/// if a strategy fails or doesn't help, the original bytes are returned.
/// </summary>
public sealed class DocumentCompressor : IDocumentCompressor
{
    private readonly ILogger<DocumentCompressor> _log;
    private readonly CompressionOptions _opt;

    public DocumentCompressor(IConfiguration config, ILogger<DocumentCompressor> log)
    {
        _log = log;
        _opt = CompressionOptions.FromConfig(config);
    }

    public async Task<CompressionResult> CompressAsync(
        Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        // Buffer the upload to a temp file first. This guarantees we always hold the
        // original bytes to fall back to, and lets external tools (Ghostscript) read a path.
        var inPath = Path.Combine(Path.GetTempPath(), $"td_in_{Guid.NewGuid():N}{Path.GetExtension(fileName)}");
        await using (var fs = File.Create(inPath))
            await content.CopyToAsync(fs, ct);

        var originalSize = new FileInfo(inPath).Length;

        if (!_opt.Enabled || originalSize == 0)
            return PassThrough(inPath, fileName, contentType, originalSize);

        try
        {
            var ext = (Path.GetExtension(fileName) ?? string.Empty).TrimStart('.').ToLowerInvariant();

            (string outPath, string outFileName, string outContentType)? attempt = ext switch
            {
                "jpg" or "jpeg" or "png" or "webp" or "bmp" or "gif" or "tif" or "tiff"
                    => await TryCompressImageAsync(inPath, fileName, ct),
                "pdf"
                    => await TryCompressPdfAsync(inPath, fileName, contentType, ct),
                "docx" or "xlsx" or "pptx" or "docm" or "xlsm" or "pptm"
                    => TryRepackOoxml(inPath, fileName, contentType),
                _ => null,
            };

            if (attempt is { } a && File.Exists(a.outPath))
            {
                var newSize = new FileInfo(a.outPath).Length;
                // Only keep the result if it is meaningfully smaller (>2% saved).
                if (newSize > 0 && newSize < originalSize * 0.98)
                {
                    SafeDelete(inPath);
                    _log.LogInformation(
                        "Compressed {File}: {Old} -> {New} bytes ({Pct:0.#}% smaller)",
                        fileName, originalSize, newSize, (1 - (double)newSize / originalSize) * 100);
                    return new CompressionResult(
                        OpenTemp(a.outPath), a.outFileName, a.outContentType, originalSize, newSize);
                }
                SafeDelete(a.outPath);
            }
        }
        catch (OperationCanceledException) { SafeDelete(inPath); throw; }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Compression failed for {File}; storing the original.", fileName);
        }

        return PassThrough(inPath, fileName, contentType, originalSize);
    }

    // ---- Images -------------------------------------------------------------

    private async Task<(string, string, string)?> TryCompressImageAsync(
        string inPath, string fileName, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(inPath, ct);

        // Downscale anything larger than the configured cap, preserving aspect ratio.
        if (image.Width > _opt.MaxImageDimension || image.Height > _opt.MaxImageDimension)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(_opt.MaxImageDimension, _opt.MaxImageDimension),
            }));

        var ext = (Path.GetExtension(fileName) ?? string.Empty).TrimStart('.').ToLowerInvariant();
        var outPath = Path.Combine(Path.GetTempPath(), $"td_out_{Guid.NewGuid():N}");

        // PNGs may carry transparency we must not flatten, so keep them as optimized PNG.
        // Everything else re-encodes to JPEG, which is dramatically smaller for scans/photos.
        if (ext == "png")
        {
            await image.SaveAsync(outPath, new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = PngColorType.RgbWithAlpha,
            }, ct);
            return (outPath, fileName, "image/png");
        }

        await image.SaveAsync(outPath, new JpegEncoder { Quality = _opt.JpegQuality }, ct);
        var newName = Path.ChangeExtension(fileName, ".jpg");
        return (outPath, newName, "image/jpeg");
    }

    // ---- PDFs ---------------------------------------------------------------

    private async Task<(string, string, string)?> TryCompressPdfAsync(
        string inPath, string fileName, string contentType, CancellationToken ct)
    {
        // Two complementary stages:
        //   • Ghostscript downsamples embedded raster images  -> wins on scanned PDFs.
        //   • qpdf packs objects into compressed object streams (lossless)
        //     -> wins on object-heavy vector PDFs (ReportLab / Office exports).
        // Ghostscript is the slow stage (seconds), so we skip it for PDFs that have
        // no images — qpdf alone already roughly halves those, in a fraction of the time.
        var current = inPath;
        var intermediates = new List<string>();

        if (_opt.GhostscriptEnabled && PdfHasImages(inPath))
        {
            var gs = await TryGhostscriptAsync(current, ct);
            if (gs is not null) { intermediates.Add(gs); current = gs; }
        }

        var q = await TryQpdfAsync(current, ct);
        if (q is not null) { intermediates.Add(q); current = q; }

        if (current != inPath)
        {
            foreach (var f in intermediates.Where(f => f != current)) SafeDelete(f);
            return (current, fileName, "application/pdf");
        }

        // No external tool was available or helped — fall back to a managed re-save.
        var viaManaged = TryPdfSharp(inPath);
        if (viaManaged is not null) return (viaManaged, fileName, "application/pdf");

        return null;
    }

    /// <summary>
    /// Cheap heuristic: does the PDF contain raster images worth downsampling?
    /// Scans the raw bytes for image XObject / image-filter markers so vector-only
    /// PDFs can skip the expensive Ghostscript pass. On any doubt, returns true.
    /// </summary>
    private static bool PdfHasImages(string path)
    {
        // Real image XObjects are tagged "/Subtype/Image" (with or without a space). We must NOT
        // match a bare "/Image", which also appears inside every page's "/ProcSet [.. /ImageB
        // /ImageC /ImageI]" array — that false-positive defeats the skip-Ghostscript optimization
        // for vector PDFs. The DCT/JPX/CCITT/JBIG2 filters are unambiguous image signals too.
        ReadOnlySpan<byte> img1 = "/Subtype/Image"u8, img2 = "/Subtype /Image"u8,
                           dct = "/DCTDecode"u8, jpx = "/JPXDecode"u8, ccitt = "/CCITTFax"u8, jbig2 = "/JBIG2"u8;
        try
        {
            const int carry = 32; // overlap so a marker split across two reads is still found
            using var fs = File.OpenRead(path);
            var buffer = new byte[256 * 1024];
            var window = new byte[buffer.Length + carry];
            var tail = new byte[carry];
            var tailLen = 0;
            int read;
            while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                Array.Copy(tail, 0, window, 0, tailLen);
                Array.Copy(buffer, 0, window, tailLen, read);
                var span = window.AsSpan(0, tailLen + read);
                if (span.IndexOf(img1) >= 0 || span.IndexOf(img2) >= 0 || span.IndexOf(dct) >= 0
                    || span.IndexOf(jpx) >= 0 || span.IndexOf(ccitt) >= 0 || span.IndexOf(jbig2) >= 0)
                    return true;
                tailLen = Math.Min(carry, read);
                Array.Copy(buffer, read - tailLen, tail, 0, tailLen);
            }
            return false;
        }
        catch
        {
            return true; // can't tell -> don't skip a potentially useful pass
        }
    }

    private async Task<string?> TryGhostscriptAsync(string inPath, CancellationToken ct)
    {
        var exe = ResolveExecutable(_opt.GhostscriptPath,
            OperatingSystem.IsWindows() ? new[] { "gswin64c", "gswin32c", "gs" } : new[] { "gs" });
        if (exe is null) return null;

        var outPath = Path.Combine(Path.GetTempPath(), $"td_out_{Guid.NewGuid():N}.pdf");
        var args = new List<string>
        {
            "-sDEVICE=pdfwrite",
            "-dCompatibilityLevel=1.5",
            $"-dPDFSETTINGS={_opt.GhostscriptPreset}",
            "-dNOPAUSE", "-dBATCH", "-dQUIET",
            "-dDetectDuplicateImages=true",
            "-dCompressFonts=true",
            "-dSubsetFonts=true",
            "-dPassThroughJPEGImages=false",  // re-encode existing JPEGs so downsampling actually applies
            "-dDownsampleColorImages=true",
            "-dDownsampleGrayImages=true",
            "-dColorImageDownsampleThreshold=1.0",
            "-dGrayImageDownsampleThreshold=1.0",
            $"-dColorImageResolution={_opt.PdfImageDpi}",
            $"-dGrayImageResolution={_opt.PdfImageDpi}",
            "-dMonoImageResolution=300",
            $"-sOutputFile={outPath}",
            inPath,
        };

        if (await RunExternalAsync(exe, args, ct) && File.Exists(outPath) && new FileInfo(outPath).Length > 0)
            return outPath;

        SafeDelete(outPath);
        return null;
    }

    private async Task<string?> TryQpdfAsync(string inPath, CancellationToken ct)
    {
        var exe = ResolveExecutable(_opt.QpdfPath, new[] { "qpdf" });
        if (exe is null) return null;

        var outPath = Path.Combine(Path.GetTempPath(), $"td_out_{Guid.NewGuid():N}.pdf");
        var args = new List<string>
        {
            "--object-streams=generate",
            "--compress-streams=y",
            "--recompress-flate",
            "--compression-level=9",
            "--no-warn",
            inPath,
            outPath,
        };

        // qpdf exit code 3 == "completed with warnings" but still writes valid output.
        await RunExternalAsync(exe, args, ct, acceptExitCodes: new[] { 0, 3 });
        if (File.Exists(outPath) && new FileInfo(outPath).Length > 0) return outPath;

        SafeDelete(outPath);
        return null;
    }

    /// <summary>Runs an external converter with a timeout, killing it if it overruns.</summary>
    private async Task<bool> RunExternalAsync(string exe, IEnumerable<string> args,
        CancellationToken ct, int[]? acceptExitCodes = null)
    {
        acceptExitCodes ??= new[] { 0 };
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        Process? proc = null;
        try
        {
            proc = Process.Start(psi);
            if (proc is null) return false;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(_opt.GhostscriptTimeoutSeconds));
            await proc.WaitForExitAsync(timeout.Token);
            return Array.IndexOf(acceptExitCodes, proc.ExitCode) >= 0;
        }
        catch (Exception ex)
        {
            try { if (proc is { HasExited: false }) proc.Kill(entireProcessTree: true); } catch { }
            _log.LogWarning(ex, "External compressor '{Exe}' failed.", exe);
            return false;
        }
        finally { proc?.Dispose(); }
    }

    /// <summary>Resolves a configured path, or probes candidate executable names on PATH.</summary>
    private static string? ResolveExecutable(string? configuredPath, string[] candidates)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        foreach (var name in candidates)
        {
            try
            {
                using var probe = Process.Start(new ProcessStartInfo
                {
                    FileName = name,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (probe is null) continue;
                probe.WaitForExit(3000);
                if (probe.HasExited && probe.ExitCode == 0) return name;
            }
            catch { /* not installed under this name */ }
        }
        return null;
    }

    private string? TryPdfSharp(string inPath)
    {
        try
        {
            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(inPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);
            doc.Options.CompressContentStreams = true;
            doc.Options.NoCompression = false;
            doc.Options.FlateEncodeMode = PdfSharp.Pdf.PdfFlateEncodeMode.BestCompression;
            doc.Options.EnableCcittCompressionForBilevelImages = true;

            var outPath = Path.Combine(Path.GetTempPath(), $"td_out_{Guid.NewGuid():N}.pdf");
            doc.Save(outPath);
            return outPath;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Managed PDF (PdfSharp) compression failed.");
            return null;
        }
    }

    // ---- OOXML (docx / xlsx / pptx) ----------------------------------------

    private (string, string, string)? TryRepackOoxml(string inPath, string fileName, string contentType)
    {
        var outPath = Path.Combine(Path.GetTempPath(), $"td_out_{Guid.NewGuid():N}{Path.GetExtension(fileName)}");
        try
        {
            using var src = ZipFile.OpenRead(inPath);
            using var outFs = File.Create(outPath);
            using var dest = new ZipArchive(outFs, ZipArchiveMode.Create);

            foreach (var entry in src.Entries)
            {
                if (entry.FullName.EndsWith('/')) continue; // directory marker
                var newEntry = dest.CreateEntry(entry.FullName, CompressionLevel.SmallestSize);

                var lower = entry.FullName.ToLowerInvariant();
                var isMediaImage = lower.Contains("/media/")
                    && (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".png"));

                using var outStream = newEntry.Open();
                if (isMediaImage && TryRecompressEmbeddedImage(entry, outStream))
                    continue;

                using var inStream = entry.Open();
                inStream.CopyTo(outStream);
            }
            return (outPath, fileName, contentType);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "OOXML repack failed for {File}.", fileName);
            SafeDelete(outPath);
            return null;
        }
    }

    private bool TryRecompressEmbeddedImage(ZipArchiveEntry entry, Stream outStream)
    {
        try
        {
            using var raw = new MemoryStream();
            using (var es = entry.Open()) es.CopyTo(raw);
            raw.Position = 0;

            using var image = Image.Load(raw);
            if (image.Width > _opt.MaxImageDimension || image.Height > _opt.MaxImageDimension)
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(_opt.MaxImageDimension, _opt.MaxImageDimension),
                }));

            using var encoded = new MemoryStream();
            var isPng = entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            if (isPng)
                image.SaveAsPng(encoded, new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression });
            else
                image.SaveAsJpeg(encoded, new JpegEncoder { Quality = _opt.JpegQuality });

            // Keep the recompressed version only if it actually helped.
            if (encoded.Length > 0 && encoded.Length < raw.Length)
            {
                encoded.Position = 0;
                encoded.CopyTo(outStream);
                return true;
            }

            raw.Position = 0;
            raw.CopyTo(outStream);
            return true;
        }
        catch
        {
            return false; // caller will fall back to a byte copy of the original entry
        }
    }

    // ---- helpers ------------------------------------------------------------

    private static CompressionResult PassThrough(string inPath, string fileName, string contentType, long size)
        => new(OpenTemp(inPath), fileName, contentType, size, size);

    /// <summary>Opens a temp file as a read stream that deletes itself when disposed.</summary>
    private static FileStream OpenTemp(string path)
        => new(path, FileMode.Open, FileAccess.Read, FileShare.None, 81920, FileOptions.DeleteOnClose);

    private static void SafeDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }

    // ---- options ------------------------------------------------------------

    private sealed record CompressionOptions(
        bool Enabled,
        int JpegQuality,
        int MaxImageDimension,
        bool GhostscriptEnabled,
        string? GhostscriptPath,
        string GhostscriptPreset,
        int PdfImageDpi,
        int GhostscriptTimeoutSeconds,
        string? QpdfPath)
    {
        public static CompressionOptions FromConfig(IConfiguration config)
        {
            var s = config.GetSection("Compression");
            var enabled = s.GetValue("Enabled", true);
            var level = (s["Level"] ?? "Balanced").Trim().ToLowerInvariant();

            // Level presets — quality / max dimension / Ghostscript preset / target DPI.
            var (quality, maxDim, preset, dpi) = level switch
            {
                "light"   => (85, 2400, "/printer", 200),
                "maximum" => (60, 1600, "/screen", 110),
                _         => (75, 2000, "/ebook", 150), // balanced
            };

            var gs = s.GetSection("Ghostscript");
            return new CompressionOptions(
                Enabled: enabled,
                JpegQuality: s.GetValue("JpegQuality", quality),
                MaxImageDimension: s.GetValue("MaxImageDimension", maxDim),
                GhostscriptEnabled: gs.GetValue("Enabled", true),
                GhostscriptPath: gs["ExecutablePath"],
                GhostscriptPreset: gs["Preset"] ?? preset,
                PdfImageDpi: gs.GetValue("ImageDpi", dpi),
                GhostscriptTimeoutSeconds: gs.GetValue("TimeoutSeconds", 120),
                QpdfPath: s["Qpdf:ExecutablePath"]);
        }
    }
}
