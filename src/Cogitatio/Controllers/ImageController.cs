using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cogitatio.Controllers;

/// <summary>
/// Serves post images from the configured image store. Posts reference images as /img/{id},
/// so the storage location can change without editing posts, and the store stays private.
/// Images never change after upload (a replacement gets a new id), so they are cached for a year.
/// </summary>
[ApiController]
public class ImageController(IImageDatabase imageDb, IImageStore store, ILogger<ImageController> logger) : ControllerBase
{
    private const string CacheControl = "public, max-age=31536000, immutable";

    [HttpGet(Constants.ROUTE_IMAGE + "{id:int}")]
    public Task<IActionResult> GetImage(int id) => Serve(id, thumbnail: false);

    [HttpGet(Constants.ROUTE_IMAGE + "{id:int}/thumb")]
    public Task<IActionResult> GetThumbnail(int id) => Serve(id, thumbnail: true);

    private async Task<IActionResult> Serve(int id, bool thumbnail)
    {
        string etag = thumbnail ? $"\"img-{id}-thumb\"" : $"\"img-{id}\"";
        if (Request.Headers.IfNoneMatch.ToString() == etag)
            return StatusCode(StatusCodes.Status304NotModified);

        BlogImage? image = imageDb.Load(id);
        if (image == null)
            return NotFound();

        // GIFs have no thumbnail; fall back to the full image
        bool useThumb = thumbnail && !string.IsNullOrEmpty(image.ThumbPath);
        string path = useThumb ? image.ThumbPath! : image.StoragePath;
        string contentType = useThumb ? ContentTypeFromPath(path, image.ContentType) : image.ContentType;

        Stream? stream = await store.OpenReadAsync(path);
        if (stream == null)
        {
            logger.LogWarning("Image {ImageId} is in the database but its file is missing ({Path})", id, path);
            return NotFound();
        }

        Response.Headers.CacheControl = CacheControl;
        Response.Headers.ETag = etag;
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(stream, contentType);
    }

    private static string ContentTypeFromPath(string path, string fallback) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".webp" => "image/webp",
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => fallback
        };
}
