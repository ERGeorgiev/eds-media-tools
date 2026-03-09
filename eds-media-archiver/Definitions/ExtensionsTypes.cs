namespace EdsMediaArchiver.Definitions;

/// <summary>
/// File type classifications, extension mappings, and filename date patterns.
/// </summary>
public static partial class ExtensionsTypes
{
    public static readonly Dictionary<string, string> FileTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        // Image/Video Types
        [MediaType.Apng] = ".apng",
        [MediaType.Gif] = ".gif",
        [MediaType.Webp] = ".webp",

        // Image Types
        [MediaType.Avif] = ".avif",
        [MediaType.Bmp] = ".bmp",
        [MediaType.Heic] = ".heic",
        [MediaType.Heif] = ".heif",
        [MediaType.Jpeg] = ".jpg",
        [MediaType.Png] = ".png",
        [MediaType.Tiff] = ".tiff",

        // Video Types
        [MediaType.Amv] = ".amv",
        [MediaType.Asf] = ".asf",
        [MediaType.Avi] = ".avi",
        [MediaType.Dv] = ".dv",
        [MediaType.Dvr_ms] = ".dvr-ms",
        [MediaType.F4V] = ".f4v",
        [MediaType.Flv] = ".flv",
        [MediaType.Gxf] = ".gxf",
        [MediaType.Lrv] = ".lrv",
        [MediaType.M2Ts] = ".m2ts",
        [MediaType.M4V] = ".m4v",
        [MediaType.Mj2] = ".mj2",
        [MediaType.Mjpeg] = ".mjpeg",
        [MediaType.Mkv] = ".mkv",
        [MediaType.Mod] = ".mod",
        [MediaType.Mov] = ".mov",
        [MediaType.Mp4] = ".mp4",
        [MediaType.Mpeg] = ".mpeg",
        [MediaType.Mpegts] = ".ts",
        [MediaType.Mpg] = ".mpg",
        [MediaType.Mts] = ".mts",
        [MediaType.Mvi] = ".mvi",
        [MediaType.Mxf] = ".mxf",
        [MediaType.Ogv] = ".ogv",
        [MediaType.QuickTime] = ".mov",
        [MediaType.Rm] = ".rm",
        [MediaType.Rmvb] = ".rmvb",
        [MediaType.ThreeG2] = ".3g2",
        [MediaType.ThreeGp] = ".3gp",
        [MediaType.Tod] = ".tod",
        [MediaType.Ts] = ".ts",
        [MediaType.Vob] = ".vob",
        [MediaType.WebM] = ".webm",
        [MediaType.Wmv] = ".wmv",
        [MediaType.Wtv] = ".wtv",

        // Audio Types
        [MediaType.Aac] = ".aac",
        [MediaType.Ac3] = ".ac3",
        [MediaType.Aiff] = ".aiff",
        [MediaType.Amr] = ".amr",
        [MediaType.Dts] = ".dts",
        [MediaType.Flac] = ".flac",
        [MediaType.M4a] = ".m4a",
        [MediaType.M4b] = ".m4b",
        [MediaType.Mp2] = ".mp2",
        [MediaType.Mp3] = ".mp3",
        [MediaType.Ogg] = ".ogg",
        [MediaType.Opus] = ".opus",
        [MediaType.Pcm] = ".pcm",
        [MediaType.Wav] = ".wav",
        [MediaType.Wma] = ".wma"
    };

    public static readonly Dictionary<string, string> ExtensionToFileType = new(StringComparer.OrdinalIgnoreCase)
    {
        // Image/Video Types
        [".apng"] = MediaType.Apng,
        [".gif"] = MediaType.Gif,
        [".webp"] = MediaType.Webp,

        // Images
        [".avif"] = MediaType.Avif,
        [".bmp"] = MediaType.Bmp,
        [".heic"] = MediaType.Heic,
        [".heif"] = MediaType.Heif,
        [".jpg"] = MediaType.Jpeg,
        [".jpeg"] = MediaType.Jpeg,
        [".png"] = MediaType.Png,
        [".tiff"] = MediaType.Tiff,
        [".tif"] = MediaType.Tiff,

        // Videos
        [".amv"] = MediaType.Amv,
        [".asf"] = MediaType.Asf,
        [".avi"] = MediaType.Avi,
        [".dv"] = MediaType.Dv,
        [".dvr-ms"] = MediaType.Dvr_ms,
        [".f4v"] = MediaType.F4V,
        [".flv"] = MediaType.Flv,
        [".gxf"] = MediaType.Gxf,
        [".lrv"] = MediaType.Lrv,
        [".m2ts"] = MediaType.M2Ts,
        [".m4v"] = MediaType.M4V,
        [".mj2"] = MediaType.Mj2,
        [".mjpeg"] = MediaType.Mjpeg,
        [".mkv"] = MediaType.Mkv,
        [".mod"] = MediaType.Mod,
        [".mov"] = MediaType.Mov,
        [".mp4"] = MediaType.Mp4,
        [".mpeg"] = MediaType.Mpeg,
        [".mpg"] = MediaType.Mpg,
        [".m2t"] = MediaType.Mpegts,
        [".ts"] = MediaType.Ts,
        [".mts"] = MediaType.Mts,
        [".mvi"] = MediaType.Mvi,
        [".mxf"] = MediaType.Mxf,
        [".ogv"] = MediaType.Ogv,
        [".rm"] = MediaType.Rm,
        [".rmvb"] = MediaType.Rmvb,
        [".3g2"] = MediaType.ThreeG2,
        [".3gp"] = MediaType.ThreeGp,
        [".tod"] = MediaType.Tod,
        [".vob"] = MediaType.Vob,
        [".webm"] = MediaType.WebM,
        [".wmv"] = MediaType.Wmv,
        [".wtv"] = MediaType.Wtv,

        // Audio
        [".aac"] = MediaType.Aac,
        [".ac3"] = MediaType.Ac3,
        [".aif"] = MediaType.Aiff,
        [".aiff"] = MediaType.Aiff,
        [".amr"] = MediaType.Amr,
        [".dts"] = MediaType.Dts,
        [".flac"] = MediaType.Flac,
        [".m4a"] = MediaType.M4a,
        [".m4b"] = MediaType.M4b,
        [".mp2"] = MediaType.Mp2,
        [".mp3"] = MediaType.Mp3,
        [".ogg"] = MediaType.Ogg,
        [".opus"] = MediaType.Opus,
        [".pcm"] = MediaType.Pcm,
        [".wav"] = MediaType.Wav,
        [".wma"] = MediaType.Wma
    };
}
