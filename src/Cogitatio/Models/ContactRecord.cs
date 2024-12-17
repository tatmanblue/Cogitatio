using System.ComponentModel.DataAnnotations;

namespace Cogitatio.Models;

/// <summary>
/// The fields with DataAnnotations as user editable, everything else is updated by
/// the Cogitatio
/// </summary>
public class ContactRecord
{
    public int Id { get; set; }
    [StringLength(maximumLength:75)] public string Name { get; set; } = string.Empty;
    [EmailAddress] public string Email { get; set; } = string.Empty;
    [StringLength(maximumLength:500)] public string Message { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; }
}