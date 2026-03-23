# Admin Portal

The purpose of this document is to describe the technical details behind the admin 
portal. The admin portal is used to administer the site.

While this document is intended for Claude code, it is human readable and should be 
informative to anyone.  

## Admin Portal Purpose

The purpose of the admin portal is to provide administrator functionality for the 
site. This includes creating an editing, post editing, post tags, maintaining the 
user database and maintaining site specific settings.

# Structure

The admin portal is built on blazer and C#. The admin portal does use the database 
connections which is described [here](DATABASE.md).  

[Code](../src/Cogitatio/Pages/Admin)  

## Site admin account

The site admin account is required to access the admin portal.  The site admin account 
is separate from user accounts. 

## Default Admin account
The default admin account is:
- username: admin
- password: Cogitatio2024!

**THIS ACCOUNT INFORMATION SHOULD BE CHANGED IMMEDIATELY AFTER FIRST LOGIN!**  Both the username 
and password can be changed after login via the settings page.

## Doc version

2026.03.21