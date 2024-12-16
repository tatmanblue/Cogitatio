﻿using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages;

public partial class AdminContact : ComponentBase
{
    [Inject] private ILogger<AdminContact> logger { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] UserState userState { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    
    private List<ContactRecordModel> contacts;

    protected override void OnParametersSet()
    {
        if (!userState.IsAdmin)
            navigationManager.NavigateTo("/Admin");
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
    
    private void ShowMessage(int id)
    {
        var contact = contacts.FirstOrDefault(c => c.Id == id);
        if (contact != null)
        {
            contact.ShowMessage = true;
        }
    }

    private void HideMessage(int id)
    {
        var contact = contacts.FirstOrDefault(c => c.Id == id);
        if (contact != null)
        {
            contact.ShowMessage = false;
        }
    }

    private class ContactRecordModel : ContactRecord
    {
        public bool IsSelected { get; set; }
        public bool ShowMessage { get; set; }
    }
}