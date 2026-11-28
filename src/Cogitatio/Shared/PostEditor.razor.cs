using Cogitatio.Logic;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cogitatio.Shared;

/// <summary>
/// TinyMCE editor for blog post content, shared by AddPost and EditPost so the editor
/// configuration (including image options) is defined once.
///
/// When an image store is configured, the editor gets an "Upload image" toolbar button. It and
/// pasted or dropped images open this component's upload dialog, which takes the image and its
/// description (alt text) on one screen. wwwroot/js/imageUpload.js keeps the file in the
/// browser, resizes it on Upload, streams it to <see cref="UploadImage"/> over the Blazor
/// circuit (where the admin session is checked) and inserts the result at the cursor.
/// </summary>
public partial class PostEditor : ComponentBase, IAsyncDisposable
{
    [Inject] private IConfiguration configuration { get; set; } = null!;
    [Inject] private IJSRuntime js { get; set; } = null!;
    [Inject] private ImageService imageService { get; set; } = null!;
    [Inject] private AdminUserState adminUserState { get; set; } = null!;
    [Inject] private ILogger<PostEditor> logger { get; set; } = null!;

    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    private string apiKey = string.Empty;
    private bool uploadsEnabled = false;
    private string? jsConfSrc = null;
    private Dictionary<string, object> editorConfig = new Dictionary<string, object>();
    private DotNetObjectReference<PostEditor>? dotNetRef;

    // upload dialog state
    private bool showImageDialog = false;
    private PendingImage? pendingImage = null;
    private string altText = string.Empty;
    private string dialogError = string.Empty;
    private bool uploading = false;
    private ElementReference fileInput;

    /// <summary>The image chosen in the browser, waiting for Upload. Set by imageUpload.js.</summary>
    public record PendingImage(string Name, string PreviewUrl, long SizeBytes);

    private bool CanUpload => pendingImage != null && !string.IsNullOrWhiteSpace(altText) && !uploading;

    protected override void OnInitialized()
    {
        apiKey = configuration["CogitatioTinyMceKey"] ?? string.Empty;
        uploadsEnabled = imageService.IsEnabled;
        jsConfSrc = uploadsEnabled ? "cogitatioImages.conf" : null;
        editorConfig = BuildConfig(uploadsEnabled);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !uploadsEnabled) return;

        dotNetRef = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("cogitatioImages.register", dotNetRef, new
        {
            maxWidth = imageService.MaxWidth,
            thumbWidth = imageService.ThumbWidth,
            quality = imageService.Quality / 100.0
        });
    }

    /// <summary>
    /// Called from imageUpload.js with the browser-resized image (and thumbnail, except for GIFs).
    /// </summary>
    [JSInvokable]
    public async Task<ImageService.UploadResult> UploadImage(string fileName, string altText,
        IJSStreamReference image, IJSStreamReference? thumbnail)
    {
        if (!adminUserState.IsAdmin)
        {
            logger.LogWarning("Image upload rejected: not signed in as admin");
            return ImageService.UploadResult.Fail("You must be signed in as the admin to upload images.");
        }

        int maxBytes = imageService.MaxUploadBytes;
        try
        {
            byte[] imageData = await ReadAllAsync(image, maxBytes);
            byte[]? thumbData = thumbnail != null ? await ReadAllAsync(thumbnail, maxBytes) : null;
            return await imageService.SaveAsync(fileName, altText, imageData, thumbData);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("maxAllowedSize", StringComparison.OrdinalIgnoreCase)
                                                   || ex.Message.Contains("exceeds", StringComparison.OrdinalIgnoreCase))
        {
            return ImageService.UploadResult.Fail($"Image is too large. The limit is {maxBytes / 1024} KB.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image upload failed for {FileName}", fileName);
            return ImageService.UploadResult.Fail("The image could not be uploaded.");
        }
        finally
        {
            await image.DisposeAsync();
            if (thumbnail != null) await thumbnail.DisposeAsync();
        }
    }

    /// <summary>
    /// Called from imageUpload.js by the toolbar button (image is null) or when an image is
    /// pasted or dropped into the editor (image is that file).
    /// </summary>
    [JSInvokable]
    public async Task OpenImageDialog(PendingImage? image)
    {
        await InvokeAsync(() =>
        {
            pendingImage = image;
            altText = string.Empty;
            dialogError = string.Empty;
            uploading = false;
            showImageDialog = true;
            StateHasChanged();
        });
    }

    private async Task OnFileChosen(ChangeEventArgs e)
    {
        dialogError = string.Empty;
        pendingImage = await js.InvokeAsync<PendingImage?>("cogitatioImages.selectFile", fileInput);
        if (pendingImage == null)
            dialogError = "Choose a JPEG, PNG, GIF or WebP image.";
    }

    private async Task UploadPendingImage()
    {
        if (!CanUpload) return;

        string description = altText.Trim();
        if (description.Length > ImageService.MaxAltTextLength)
        {
            dialogError = $"The description must be {ImageService.MaxAltTextLength} characters or fewer.";
            return;
        }

        uploading = true;
        dialogError = string.Empty;
        try
        {
            ImageService.UploadResult? result =
                await js.InvokeAsync<ImageService.UploadResult?>("cogitatioImages.uploadPending", description);
            if (result != null && result.Success)
            {
                CloseImageDialog();
                return;
            }
            dialogError = result?.Error ?? "The image could not be uploaded.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image upload from the dialog failed");
            dialogError = "The image could not be uploaded.";
        }
        finally
        {
            uploading = false;
        }
    }

    private async Task CancelImageDialog()
    {
        if (uploading) return;
        CloseImageDialog();
        await js.InvokeVoidAsync("cogitatioImages.cancel");
    }

    private void CloseImageDialog()
    {
        showImageDialog = false;
        pendingImage = null;
        altText = string.Empty;
        dialogError = string.Empty;
    }

    private static string FormatSize(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0):0.0} MB" : $"{Math.Max(1, bytes / 1024)} KB";

    private static async Task<byte[]> ReadAllAsync(IJSStreamReference streamRef, int maxBytes)
    {
        if (streamRef.Length > maxBytes)
            throw new InvalidOperationException($"Stream length {streamRef.Length} exceeds the limit of {maxBytes} bytes.");

        await using Stream stream = await streamRef.OpenReadStreamAsync(maxAllowedSize: maxBytes);
        using MemoryStream buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private async Task OnValueChanged(string newValue)
    {
        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
    }

    // Image classes use Bootstrap utilities so they render the same on the public site.
    // The same rules are repeated in content_style so the editor preview matches.
    private static Dictionary<string, object> BuildConfig(bool uploadsEnabled)
    {
        Dictionary<string, object> config = new Dictionary<string, object>
        {
            { "menubar", true },
            { "plugins", "link image code" },
            // cogitatioimage is the "Upload image" button, added by cogitatioImages.conf when
            // an image store is configured
            { "toolbar", "undo redo | styleselect | blockquote | bullist numlist | forecolor | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | link image" +
                         (uploadsEnabled ? " cogitatioimage" : string.Empty) + " | code" },

            // Keep /img/{id} as a root-relative URL; TinyMCE would otherwise rewrite it
            // relative to the admin page (e.g. ../img/42), which breaks on other pages.
            { "relative_urls", false },
            { "remove_script_host", true },

            // TinyMCE never stores or uploads images itself: base64 images pasted into the post
            // are blocked, and its image dialog has no Upload tab. With an image store, pasted
            // or dropped image files open the upload dialog instead (cogitatioImages.conf).
            { "paste_data_images", false },
            { "automatic_uploads", false },
            { "image_uploadtab", false },

            // Always show the alt text field; posts can't be saved with images missing alt text.
            { "a11y_advanced_options", false },
            { "image_advtab", true },
            { "image_caption", true },
            { "image_class_list", new object[]
                {
                    new { title = "Responsive (default)", value = "img-fluid" },
                    new { title = "Left, text wraps", value = "img-fluid float-start me-3 mb-2" },
                    new { title = "Right, text wraps", value = "img-fluid float-end ms-3 mb-2" },
                    new { title = "Centered", value = "img-fluid d-block mx-auto" },
                    new { title = "Full width", value = "img-fluid w-100" }
                }
            },
            { "content_style",
                "img { max-width: 100%; height: auto; } " +
                ".float-start { float: left; } .float-end { float: right; } " +
                ".me-3 { margin-right: 1rem; } .ms-3 { margin-left: 1rem; } .mb-2 { margin-bottom: .5rem; } " +
                ".d-block { display: block; } .mx-auto { margin-left: auto; margin-right: auto; } .w-100 { width: 100%; } " +
                "figure.image { display: table; margin: 1rem auto; } figure.image figcaption { text-align: center; font-size: .9em; color: #666; }"
            }
        };
        return config;
    }

    public async ValueTask DisposeAsync()
    {
        if (dotNetRef == null) return;
        try
        {
            await js.InvokeVoidAsync("cogitatioImages.unregister", dotNetRef);
        }
        catch (JSDisconnectedException)
        {
            // circuit already gone; nothing to clean up in the browser
        }
        dotNetRef.Dispose();
    }
}
