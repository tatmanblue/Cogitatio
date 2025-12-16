# Users and Comments in Cogitatio

Here's some terse documentation on allowing authenticated users to comment on blog posts. 

## Setup 

### Comment Table Set up
_Currently only available for MSSQL dbs._  You will need to create the blog table.  See the `create_blob_tables.sql`.

### User Table setup
_Currently only available for MSSQL dbs._

There's a bit more setup here:
1. Create the user tables.  See `create_blog_users_table.sql`
2. Provide a connection string to the user table in admin settings.

Highly recommended, even though it's not required, you put the users table in a separate db.

Yes, postgres sql will be updated to allow for commenting/users.

## Users

Account creation is a turned on via a toggle in admin settings.  Turning this on allows 
users to create an account.   Users must provide an email.  A verification
link is sent to the email.  Emails are sent through only [SendGrid](sendgrid.com/).

Yes, support for azure/AWS, STMP servers will be created a some point.

### After the users verify their email
Admins have to manually set the user account to one of CommentWithApproval, CommentWithoutApproval statuses to allow users to create comments.

There is a tool, available in the admin portal, for updating user statuses.

## Login

Account login is a toggle in admin settings.  Turning this on allows users to login. 

Once a users is logged in, they can comment on posts.

## Comments
Behaviors for comments are configured in admin settings.  Settings allow for max # comments per post, max length of a comment, etc...

Currently comments can only be created.  There is no edit functionality--which is probably something that needs to be implemented sooner than later.

User permissions are tied to commenting.  Comments made by users with the `CommentWithApproval` account state will have to approved before they will appear on the blog.

There is a tool, available in the admin portal, to help with moderation.  I'm sure this could be extended to be more functional.

## Doc version

2025.12.15