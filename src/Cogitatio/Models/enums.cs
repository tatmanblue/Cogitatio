using System.ComponentModel;

namespace Cogitatio.Models;

public enum BlogPostStatuses
{
    NA = 0,
    Visible = 1,
    NotVisible = 2,
    Deleted = 3
}

// Settings keys for blog settings
public enum BlogSettings
{
    [Description("The title of the blog which appears in the browser tab")]
    SiteTitle,
    [Description("The title left cornor of the blog")]
    ShortTitle,
    [Description("The title in the header of the blog")]
    LongTitle,
    [Description("The description of the blog which appears on the home page")]
    Introduction,
    [Description("The about page content")]
    About,
    [Description("The copyright information in the footer")]
    Copyright,
    [Description("Whether to require authenticator authentication for admin users, false means use password only")]
    UseTOTP,
    [Description("The secret key used for two factor authentication")]
    TwoFactorSecret,
    [Description("The admin user ID, default is 'admin'")]
    AdminId,
    [Description("The admin user password in plain text, default is 'Cogitatio2024!'")]
    AdminPassword,
    [Description("Turns on or off comments on blog posts")]
    AllowComments,
    [Description("Salt value used when hashing user passwords")]
    PasswordSalt,
    [Description("The connection string for the user database")]
    UserDBConnectionString
}

public enum UserAccountStates
{
    Unknown = 0,
    Created = 1,                        // user entered account information, need to verify email
    CommentWithApproval = 2,            // level one commenting, each comment must be approved
    CommentWithoutApproval = 3,         // level two commenting, can comment without approval
    Moderator = 4,                      // level three commenting, can approve comments
    Blocked = 5                         // user is blocked from commenting,
}