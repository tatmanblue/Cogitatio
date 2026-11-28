namespace Cogitatio.Logic;

/// <summary>
/// Identifies an uploaded image from its first bytes (never from its name or the content type
/// the browser claims) and reads its dimensions from the header. Only JPEG, PNG, GIF and WebP
/// are recognized; anything else, including SVG, is rejected.
/// </summary>
public static class ImageFormat
{
    public record Info(string ContentType, string Extension, int Width, int Height);

    public static Info? Detect(byte[] data)
    {
        if (data.Length < 16) return null;

        if (IsPng(data)) return ReadPng(data);
        if (IsGif(data)) return ReadGif(data);
        if (IsJpeg(data)) return ReadJpeg(data);
        if (IsWebP(data)) return ReadWebP(data);
        return null;
    }

    private static bool IsPng(byte[] d) =>
        d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47 &&
        d[4] == 0x0D && d[5] == 0x0A && d[6] == 0x1A && d[7] == 0x0A;

    private static bool IsGif(byte[] d) =>
        d[0] == 'G' && d[1] == 'I' && d[2] == 'F' && d[3] == '8' && (d[4] == '7' || d[4] == '9') && d[5] == 'a';

    private static bool IsJpeg(byte[] d) => d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF;

    private static bool IsWebP(byte[] d) =>
        d[0] == 'R' && d[1] == 'I' && d[2] == 'F' && d[3] == 'F' &&
        d[8] == 'W' && d[9] == 'E' && d[10] == 'B' && d[11] == 'P';

    private static Info? ReadPng(byte[] d)
    {
        if (d.Length < 24) return null;
        int width = (d[16] << 24) | (d[17] << 16) | (d[18] << 8) | d[19];
        int height = (d[20] << 24) | (d[21] << 16) | (d[22] << 8) | d[23];
        return Valid("image/png", "png", width, height);
    }

    private static Info? ReadGif(byte[] d)
    {
        int width = d[6] | (d[7] << 8);
        int height = d[8] | (d[9] << 8);
        return Valid("image/gif", "gif", width, height);
    }

    private static Info? ReadJpeg(byte[] d)
    {
        int i = 2;
        while (i + 9 < d.Length)
        {
            if (d[i] != 0xFF) return null;
            byte marker = d[i + 1];
            if (marker == 0xFF) { i++; continue; }                          // fill byte
            if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD8)) { i += 2; continue; } // no length

            int segmentLength = (d[i + 2] << 8) | d[i + 3];
            bool isStartOfFrame = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
            if (isStartOfFrame)
            {
                int height = (d[i + 5] << 8) | d[i + 6];
                int width = (d[i + 7] << 8) | d[i + 8];
                return Valid("image/jpeg", "jpg", width, height);
            }
            if (segmentLength < 2) return null;
            i += 2 + segmentLength;
        }
        return null;
    }

    private static Info? ReadWebP(byte[] d)
    {
        if (d.Length < 30) return null;
        string chunk = System.Text.Encoding.ASCII.GetString(d, 12, 4);
        switch (chunk)
        {
            case "VP8 ":
            {
                int width = (d[26] | (d[27] << 8)) & 0x3FFF;
                int height = (d[28] | (d[29] << 8)) & 0x3FFF;
                return Valid("image/webp", "webp", width, height);
            }
            case "VP8L":
            {
                int b0 = d[21], b1 = d[22], b2 = d[23], b3 = d[24];
                int width = 1 + (((b1 & 0x3F) << 8) | b0);
                int height = 1 + (((b3 & 0x0F) << 10) | (b2 << 2) | ((b1 & 0xC0) >> 6));
                return Valid("image/webp", "webp", width, height);
            }
            case "VP8X":
            {
                int width = 1 + (d[24] | (d[25] << 8) | (d[26] << 16));
                int height = 1 + (d[27] | (d[28] << 8) | (d[29] << 16));
                return Valid("image/webp", "webp", width, height);
            }
            default:
                return null;
        }
    }

    private static Info? Valid(string contentType, string extension, int width, int height) =>
        width > 0 && height > 0 ? new Info(contentType, extension, width, height) : null;
}
