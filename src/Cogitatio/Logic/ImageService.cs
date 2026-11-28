using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;

namespace Cogitatio.Logic;

/// <summary>
/// Validates and stores uploaded post images. Images are resized in the browser before upload;
/// this class only checks them (type from file bytes, size, width) and saves file + metadata.
/// Callers must have checked that the current user is the admin.
/// </summary>
public class ImageService
{
    public const int DefaultMaxUploadBytes = 5 * 1024 * 1024;
    public const int DefaultMaxWidth = 1200;
    public const int DefaultThumbWidth = 300;
    public const int DefaultQuality = 82;
    public const int MaxAltTextLength = 500;

    // allowance for rounding in the browser resize before an image counts as "not resized"
    private const int WidthAllowance = 16;

    private readonly ILogger<ImageService> logger;
    private readonly IImageStore store;
    private readonly IImageDatabase imageDb;
    private readonly IDatabase database;
    private readonly int tenantId;

    public ImageService(ILogger<ImageService> logger, IImageStore store, IImageDatabase imageDb,
        IDatabase database, IConfiguration configuration)
    {
        this.logger = logger;
        this.store = store;
        this.imageDb = imageDb;
        this.database = database;
        tenantId = Convert.ToInt32(configuration["CogitatioTenantId"] ?? "0");
    }

    public bool IsEnabled => store.IsConfigured;

    public int MaxUploadBytes => database.GetIntSetting(BlogSettings.ImageMaxUploadBytes, DefaultMaxUploadBytes);
    public int MaxWidth => database.GetIntSetting(BlogSettings.ImageMaxWidth, DefaultMaxWidth);
    public int ThumbWidth => database.GetIntSetting(BlogSettings.ImageThumbWidth, DefaultThumbWidth);
    public int Quality => Math.Clamp(database.GetIntSetting(BlogSettings.ImageQuality, DefaultQuality), 1, 100);

    /// <summary>
    /// Result of an upload. Width and Height are the stored image's size, used for the
    /// width/height attributes of the inserted &lt;img&gt; so the page doesn't jump while loading.
    /// </summary>
    public record UploadResult(bool Success, string Url, string Error, int Width = 0, int Height = 0)
    {
        public static UploadResult Fail(string error) => new UploadResult(false, string.Empty, error);
    }

    /// <summary>
    /// Validates and saves an image. <paramref name="thumbData"/> is optional (GIFs have none).
    /// </summary>
    public async Task<UploadResult> SaveAsync(string fileName, string altText, byte[] imageData, byte[]? thumbData)
    {
        if (!store.IsConfigured)
            return UploadResult.Fail("Image uploads are not configured. Set an image store in Admin > Settings > Images.");

        altText = (altText ?? string.Empty).Trim();
        if (altText.Length == 0)
            return UploadResult.Fail("Alt text is required.");
        if (altText.Length > MaxAltTextLength)
            return UploadResult.Fail($"Alt text must be {MaxAltTextLength} characters or fewer.");

        if (imageData.Length > MaxUploadBytes)
            return UploadResult.Fail($"Image is too large ({imageData.Length / 1024} KB). The limit is {MaxUploadBytes / 1024} KB.");

        ImageFormat.Info? info = ImageFormat.Detect(imageData);
        if (info == null)
            return UploadResult.Fail("Unsupported file. Only JPEG, PNG, GIF and WebP images are allowed.");

        // GIFs are uploaded without resizing (resizing would lose animation); everything else
        // must have been resized in the browser.
        bool isGif = info.ContentType == "image/gif";
        if (!isGif && info.Width > MaxWidth + WidthAllowance)
            return UploadResult.Fail($"Image is {info.Width}px wide but should have been resized to {MaxWidth}px. Try uploading again.");

        ImageFormat.Info? thumbInfo = null;
        if (thumbData != null && thumbData.Length > 0)
        {
            thumbInfo = ImageFormat.Detect(thumbData);
            if (thumbInfo == null || thumbData.Length > MaxUploadBytes)
                return UploadResult.Fail("The thumbnail generated for this image is invalid.");
        }

        string safeName = Path.GetFileName(fileName ?? string.Empty);
        if (safeName.Length == 0) safeName = $"image.{info.Extension}";
        if (safeName.Length > 255) safeName = safeName[^255..];

        DateTime now = DateTime.UtcNow;
        string baseName = $"{tenantId}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";
        string storagePath = $"{baseName}.{info.Extension}";
        string? thumbPath = thumbInfo != null ? $"{baseName}_thumb.{thumbInfo.Extension}" : null;

        try
        {
            using (MemoryStream imageStream = new MemoryStream(imageData, writable: false))
                await store.SaveAsync(storagePath, imageStream, info.ContentType);

            if (thumbInfo != null && thumbPath != null)
            {
                using MemoryStream thumbStream = new MemoryStream(thumbData!, writable: false);
                await store.SaveAsync(thumbPath, thumbStream, thumbInfo.ContentType);
            }

            BlogImage image = BlogImage.Create(tenantId, safeName, storagePath, thumbPath,
                info.ContentType, info.Width, info.Height, imageData.Length, altText);
            imageDb.Save(image);

            return new UploadResult(true, $"{Constants.ROUTE_IMAGE}{image.Id}", string.Empty, info.Width, info.Height);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save uploaded image {FileName}", safeName);
            // don't leave files behind without a database row
            await TryDeleteAsync(storagePath);
            if (thumbPath != null) await TryDeleteAsync(thumbPath);
            return UploadResult.Fail("The image could not be saved. Check the image store settings and the log.");
        }
    }

    private async Task TryDeleteAsync(string storagePath)
    {
        try { await store.DeleteAsync(storagePath); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not clean up image file {Path}", storagePath); }
    }
}
