using System.Diagnostics;

namespace EdsMediaArchiver.Services.Compressors;

/// <summary>Chroma subsampling for JPEG output.</summary>
public enum ChromaSubsampling
{
    /// <summary>4:4:4, full-resolution colour. Largest; best for text, screenshots, max-fidelity archival.</summary>
    Chroma444,
    /// <summary>4:2:2, colour halved horizontally only. Middle ground.</summary>
    Chroma422,
    /// <summary>4:2:0, colour halved on both axes. Smallest; fine for photos.</summary>
    Chroma420
}

public sealed record JpegEncodeOptions
{
    private readonly int _quality = 85;

    /// <summary>mozjpeg quality, 0-100.</summary>
    public int Quality
    {
        get => _quality;
        init => _quality = value is >= 0 and <= 100
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Quality must be 0-100.");
    }

    public ChromaSubsampling Subsampling { get; init; } = ChromaSubsampling.Chroma420;

    /// <summary>Progressive is mozjpeg's default and a real size win; baseline only for ancient decoders.</summary>
    public bool Progressive { get; init; } = true;
}

public interface IJpegEncoder
{
    /// <summary>
    /// Encodes a lossless intermediate (e.g. an 8-bit P6 PPM byte stream) to a JPEG file using mozjpeg.
    /// The intermediate carries its own dimensions, so only encode settings are passed here.
    /// </summary>
    Task EncodeAsync(ReadOnlyMemory<byte> losslessInput, JpegEncodeOptions options,
        string outputPath, CancellationToken ct = default);
}

/// <summary>
/// Encodes via the mozjpeg <c>cjpeg</c> CLI. Going through the CLI guarantees the quality-per-byte
/// features (trellis quantization, progressive coding, the tuned default quant table) are actually
/// applied. The simplified in-process TurboJPEG path does not reliably expose them.
/// Swap this out for an in-process implementation by re-implementing <see cref="IJpegEncoder"/>.
/// </summary>
public sealed class MozJpegCliEncoder(string cjpegPath) : IJpegEncoder
{
    public async Task EncodeAsync(ReadOnlyMemory<byte> losslessInput, JpegEncodeOptions options,
        string outputPath, CancellationToken ct = default)
    {
        var sample = options.Subsampling switch
        {
            ChromaSubsampling.Chroma444 => "1x1",
            ChromaSubsampling.Chroma422 => "2x1",
            _ => "2x2"                              // Chroma420
        };

        var psi = new ProcessStartInfo(cjpegPath)
        {
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-quality");
        psi.ArgumentList.Add(options.Quality.ToString());
        // -quant-table MUST come after -quality, otherwise mozjpeg resets it. 3 = Robidoux/ImageMagick table.
        psi.ArgumentList.Add("-quant-table");
        psi.ArgumentList.Add("3");
        psi.ArgumentList.Add("-sample");
        psi.ArgumentList.Add(sample);
        if (!options.Progressive)
            psi.ArgumentList.Add("-baseline");
        psi.ArgumentList.Add("-outfile");
        psi.ArgumentList.Add(outputPath);

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Could not start cjpeg at '{cjpegPath}'.");
        try
        {
            // Drain stderr concurrently so a chatty/erroring process can't deadlock the stdin write.
            var stderrTask = proc.StandardError.ReadToEndAsync(ct);

            var stdin = proc.StandardInput.BaseStream;
            try
            {
                await stdin.WriteAsync(losslessInput, ct);
                await stdin.FlushAsync(ct);
            }
            catch (IOException)
            {
                // Broken pipe: cjpeg rejected the input and exited early. Swallow so we fall through
                // to WaitForExitAsync and surface cjpeg's actual stderr instead of "broken pipe".
                // Narrow on purpose: OperationCanceledException still propagates.
            }
            finally
            {
                stdin.Close();
            }

            await proc.WaitForExitAsync(ct);
            var stderr = await stderrTask;
            if (proc.ExitCode != 0)
                throw new InvalidOperationException($"cjpeg exited {proc.ExitCode}: {stderr}");
        }
        finally
        {
            if (!proc.HasExited)
            {
                try { proc.Kill(entireProcessTree: true); }
                catch { /* already gone or unkillable; nothing useful to do */ }
            }
        }
    }
}