using Cogitatio.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Pages;

/// <summary>
/// Keeping this here for now
/// https://www.w3schools.com/colors/colors_picker.asp
/// </summary>
public partial class Home
{
    [Inject]
    private ILogger<Home> logger { get; set; }
    
    [Inject]
    private IDatabase db { get; set; } = default!;    
}