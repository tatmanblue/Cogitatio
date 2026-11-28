using Cogitatio.Models;

namespace Cogitatio.Interfaces;

/// <summary>
/// Metadata for uploaded post images (Blog_Images). Scoped to the current tenant.
/// </summary>
public interface IImageDatabase
{
    BlogImage Save(BlogImage image);
    BlogImage? Load(int id);
}
