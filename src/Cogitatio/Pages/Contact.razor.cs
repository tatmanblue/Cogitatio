using System.Text;
using System.Text.Json;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages;

public partial class Contact : ComponentBase
{
    [Inject]
    private ILogger<Contact> logger { get; set; }
    [Inject]
    private IDatabase database { get; set; }
    [Parameter] public string Slug { get; set; } = string.Empty;
    [SupplyParameterFromForm]
    private RequestContact contactData { get; set; } = new ();

    private bool showContactForm { get; set; } = true;
    
    public void SendContactRequest()
    {
        // TODO: can we merge RequestContact with this record?
        var record = new ContactRecord()
        {
            Name = contactData.Name,
            Email = contactData.Email,
            Message = contactData.Message,
            Slug = (string.IsNullOrEmpty(Slug) ? "" : Slug),
        };
        database.SaveContactRequest(record);
        showContactForm = false;
        StateHasChanged();
    }
}