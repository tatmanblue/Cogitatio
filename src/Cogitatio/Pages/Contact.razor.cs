using System.Text;
using System.Text.Json;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages;

public partial class Contact : ComponentBase
{
    [Inject] private ILogger<Contact> logger { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Parameter] public string Slug { get; set; } = string.Empty;
    [SupplyParameterFromForm]
    private ContactRecord contactData { get; set; } = new ();

    private bool showContactForm { get; set; } = true;
    
    public void SendContactRequest()
    {
        contactData.Slug = (string.IsNullOrEmpty(Slug) ? "" : Slug);
        database.SaveContactRequest(contactData);
        showContactForm = false;
        StateHasChanged();
    }
}