using System.ComponentModel.DataAnnotations;

namespace Cogitatio.Models;

public class RequestContact
{
    [StringLength(maximumLength:75)] public string Name { get; set; }
    [EmailAddress] public string Email { get; set; }
    [StringLength(maximumLength:250)] public string Message { get; set; }
}