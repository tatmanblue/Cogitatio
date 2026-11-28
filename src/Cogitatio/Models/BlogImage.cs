namespace Cogitatio.Models;

/// <summary>
/// Metadata for an image uploaded for use in posts. Posts reference it as /img/{Id}.
/// </summary>
public class BlogImage
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ThumbPath { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long SizeBytes { get; set; }
    public string AltText { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    public static BlogImage Create(int tenantId, string fileName, string storagePath, string? thumbPath,
        string contentType, int width, int height, long sizeBytes, string altText)
    {
        return new BlogImage
        {
            TenantId = tenantId,
            FileName = fileName,
            StoragePath = storagePath,
            ThumbPath = thumbPath,
            ContentType = contentType,
            Width = width,
            Height = height,
            SizeBytes = sizeBytes,
            AltText = altText,
            CreatedDate = DateTime.UtcNow
        };
    }
}
