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

## Post editor

AddPost and EditPost share one TinyMCE editor component,
[Shared/PostEditor.razor](../src/Cogitatio/Shared/PostEditor.razor.cs). Editor options,
including image options, are defined there once.

### Images in posts

Image upload and storage are planned (GitHub issue #5). Until then:

- Images are inserted **by URL** with the toolbar image button.
- **Pasting or dragging images into the editor is disabled** (`paste_data_images: false`).
  Otherwise they would be stored as base64 data inside the post content.
- **Alt text is required.** A post can't be published or updated while any image has
  missing or empty alt text, and the page shows how many images need it.
- **Layout:** pick a *Class* in the image dialog:
  - Responsive (default)
  - Left, text wraps
  - Right, text wraps
  - Centered
  - Full width

  These use Bootstrap classes. Captions are available with the dialog's *Show caption* option.
- Post images scale down to fit the column (`site.css`), and floated images are cleared
  at the end of the post.
- **Home page previews never show images.** `BlogPostShort` removes images
  (`StripImages()` in `General/GeneralExtensions.cs`) before shortening the post. A post
  with only images shows "(image post)". This doesn't change the stored post.

## Doc version

2026.11.27