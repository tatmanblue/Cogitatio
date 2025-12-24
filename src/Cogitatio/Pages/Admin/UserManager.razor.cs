using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.Admin;

/// <summary>
/// Use to manage users.  Currently, the only function is changing the account state,
/// which will affect users ability to comment
/// </summary>
public partial class UserManager : ComponentBase
{
    [Inject] private ILogger<TagEditor> logger { get; set; }
    [Inject] private IUserDatabase userDB { get; set; }
    [Inject] AdminUserState AdminUserState { get; set; }
    [Inject] private SiteSettings site { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }

    private List<AdminUserRecord> records = new();
    private int selectedUser = 1;
    private bool hasChanges = false;

    protected override void OnParametersSet()
    {
        if (!HasRights())
            navigationManager.NavigateTo("/a/Admin");
    }

    protected override void OnInitialized()
    {
        records = userDB.LoadAll().Select(u =>
        {
            AdminUserRecord record = new()
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                AccountState = u.AccountState 
            };
            return record;
        }).ToList();
    }
    
    private bool HasRights()
    {
        return AdminUserState.IsAdmin;
    }
    
    private void ShowDetails(int id)
    {
        if (selectedUser != -1 && hasChanges)
            return;
        
        if (selectedUser != -1)
            HideDetails(selectedUser);
        
        var user = records.FirstOrDefault(c => c.Id == id);
        if (user != null)
        {
            user.ShowDetails = true;
            selectedUser = id;
            hasChanges = false;
        }
        
        StateHasChanged();
    }

    private void HideDetails(int id)
    {
        var user = records.FirstOrDefault(c => c.Id == id);
        if (user != null)
        {
            user.ShowDetails = false;
            selectedUser = -1;
            hasChanges = false;
        }
    }

    private void Save(int id)
    {
        if (selectedUser == -1 || false == hasChanges)
            return;
        
        var user = records.FirstOrDefault(c => c.Id == id);
        if (user != null)
        {
            userDB.UpdateStatus(user);
            logger.LogInformation($"User {id} status has been changed");
            if (!string.IsNullOrEmpty(user.UpdatedPassword))
            {
                user.Password = Password.HashPassword(site.PasswordSalt + user.UpdatedPassword);
                userDB.UpdatePassword(user);
                logger.LogInformation($"User {id} password has been changed");
            }
        }

        HideDetails(selectedUser);
        selectedUser = -1;
        hasChanges = false;
        StateHasChanged();
    }

    private void MarkAsChanged(int id, UserAccountStates state)
    {
        selectedUser = id;
        hasChanges = true;
        var user = records.FirstOrDefault(c => c.Id == id);
        if (user != null)
        {
            user.AccountState = state;
        }
    }

    class AdminUserRecord : BlogUserRecord
    {
        public bool ShowDetails { get; set; } = false;
        public string UpdatedPassword { get; set; } = string.Empty;
    }
    
}