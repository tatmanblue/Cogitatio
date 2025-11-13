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
    [Description("Whether to require two factor authentication for admin users")]
    Use2FA
}