using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.Admin;

/// <summary>
/// Lists contact entries submitted via the contact form and saved in the database
/// </summary>
public partial class ReviewContacts : ComponentBase
{
    [Inject] private ILogger<ReviewContacts> logger { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] AdminUserState AdminUserState { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    
    private List<ContactRecordModel> contacts;

    protected override void OnParametersSet()
    {
        if (!AdminUserState.IsAdmin)
            navigationManager.NavigateTo(General.Constants.ROUTE_ADMIN_HOME);
    }
    
    protected override void OnInitialized()
    {
        contacts = database.GetContacts().Select(c => new ContactRecordModel
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Message = c.Message,
            Slug = c.Slug,
            DateAdded = c.DateAdded
        }).ToList();
    }

    private void DeleteSelected()
    {
        var selectedIds = contacts.Where(c => c.IsSelected).Select(c => c).ToList();
        foreach (var rec in selectedIds)
        {
            database.DeleteContact(rec);
        }
        contacts.RemoveAll(c => selectedIds.Contains(c));
    }

    private void ToggleSelectAll(ChangeEventArgs e)
    {
        bool isChecked = (bool)e.Value;
        foreach (var contact in contacts)
        {
            contact.IsSelected = isChecked;
        }
    }
    
    private void ShowDetails(int id)
    {
        var contact = contacts.FirstOrDefault(c => c.Id == id);
        if (contact != null)
        {
            contact.CanShowContacts = true;
        }
    }

    private void HideDetails(int id)
    {
        var contact = contacts.FirstOrDefault(c => c.Id == id);
        if (contact != null)
        {
            contact.CanShowContacts = false;
        }
    }

    private class ContactRecordModel : ContactRecord
    {
        public bool IsSelected { get; set; }
        public bool CanShowContacts { get; set; }
    }
}