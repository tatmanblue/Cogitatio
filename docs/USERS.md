# User accounts, logins and security

The purpose of this document is to document how logins, user account creation and 
management is implemented for this site.  

While this document is intended for Claude code, it is human readable and should be 
helpful for anyone wanting to understand this part of this system better.  

This document does not discuss the database nor the database access for users. That 
is discussed in the [database](DATABASE.md) document.  

## Site admin account

The site admin account is separate from user accounts. At this time, no user account can be used to administer the site.

## User administration

The site admin can use the [user administration page](../src/Cogitatio/Pages/Admin/UserManager.razor.cs) to manage user accounts.  It is implemented as a blazer page. The link includes only the C# code behind.

## Components

There are several components related to user accounts.  These components are implemented as blazer components. There are blazer files, C# code 
behind files and CSS style sheets.  The links below referenced strictly the C# code.  

[Proof of work](../src/Cogitatio/Shared/PasswordEditor.razor.cs)  
[Password Editor](../src/Cogitatio/Shared/PasswordEditor.razor.cs)  

### Proof of work
The proof of work component is intended to slow down, automated processes that may attempt to login or create accounts. It relies on the browser, computing a value and sending that back to the site for verification.

### Password editor
The password editor is a blazer component for the user to enter a password. This field is used both for login and for account creation.

## Pages

The use pages are implemented as blazer pages. There are blazer files, c# code behind 
files and CSS style sheets. The links below reference strictly the c# code.   

[Code](../src/Cogitatio/Pages/User) - all pages for the user to create accounts, login, reset passwords and verify accounts is contained in this directory.

## User accounts

User account state is represented by the [UserAccountStates](../src/Cogitatio/Models/enums.cs) enum.   The meaning of each of the enum values are fairly well commented in the code.  



## Doc version

2026.03.21