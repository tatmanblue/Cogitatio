using Microsoft.AspNetCore.Components;

namespace Cogitatio.Shared;

/// <summary>
/// TinyMCE editor for blog post content, shared by AddPost and EditPost so the editor
/// configuration (including image options) is defined once.
/// </summary>
public partial class PostEditor : ComponentBase
{
    [Inject] private IConfiguration configuration { get; set; } = null!;

    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    private string apiKey = string.Empty;

    // Image classes use Bootstrap utilities so they render the same on the public site.
    // The same rules are repeated in content_style so the editor preview matches.
    private readonly Dictionary<string, object> editorConfig = new Dictionary<string, object>
    {
        { "menubar", true },
        { "plugins", "link image code" },
        { "toolbar", "undo redo | styleselect | blockquote | bullist numlist | forecolor | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | link image | code" },

        // Pasted or dropped images would otherwise be stored as base64 data inside the post.
        // Turned back on when uploads go through the image store (issue #5, phase 2).
        { "paste_data_images", false },

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

    protected override void OnInitialized()
    {
        apiKey = configuration["CogitatioTinyMceKey"] ?? string.Empty;
    }

    private async Task OnValueChanged(string newValue)
    {
        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
    }
}
