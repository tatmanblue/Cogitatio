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

Highly recommended, even though its not required, you put the users table in a separate db.


## Users

Account creation is a toggle in admin settings.  Turning this one allows 
viewers to create an account.   Users must provide an email.  A verification
link is sent to the email.  Emails are sent through only [SendGrid](sendgrid.com/).

Yes, support for azure/AWS, STMP servers will be created a some point.

### After the users verify their email
Admins have to manually set the user account to one of CommentWithApproval, CommentWithoutApproval statuses.

Yes, admin screens will be created at some point.

## Login

Account login is a toggle in admin settings.  Turning this on allows users to login. 

Once a viewer is logged in, they can comment on posts.

## Comments
Behaviors for comments are configured in admin settings.  Settings allow for max # comments per post, max length of a comment, etc...

## Doc version

2025.12.15