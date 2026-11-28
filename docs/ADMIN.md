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

Image uploads are switched on by choosing an image store on the **Images** settings tab
(GitHub issue #5).

**Images settings tab**

- **Image Storage:**
  - *None:* uploads off.
  - *Local:* a folder on the server, for development only. The default is
    `App_Data/images` under the content root. A deploy to Azure App Service can delete it.
  - *AzureBlob:* a private container. It is created automatically if it doesn't exist.
- **Resize Width, Thumbnail Width, Quality:** passed to the browser, which resizes images
  before upload.
- **Max Upload Size:** the limit after resizing. It mostly matters for GIFs, which are
  uploaded unresized so they keep their animation.

**How uploads work (when a store is configured)**

1. You click the **Upload image** toolbar button, or paste or drop an image into the editor.
   Either way, the *Upload image* dialog opens (pasted or dropped images are already loaded).
2. In the dialog you choose the image and enter its **Description** (the alt text, required).
   Nothing is uploaded until you press *Upload*. Cancel discards the image.
3. `wwwroot/js/imageUpload.js` resizes the image on a canvas to the resize width, plus a
   thumbnail:
   - Photos become WebP (or JPEG), and PNG stays PNG.
   - Re-encoding drops EXIF data, including GPS location.
4. The resized file is streamed to `PostEditor.UploadImage` over the Blazor connection. This
   checks the admin session, which is why there's no upload API endpoint.
5. `Logic/ImageService.cs`:
   - checks the real file type from its bytes (JPEG, PNG, GIF and WebP only; SVG is
     rejected), the size and the width;
   - saves the files to the store and a row to `Blog_Images`;
   - returns `/img/{id}` and the image size, and the browser inserts
     `<img src="/img/{id}" alt="..." width height class="img-fluid">` where the cursor was
     (or where the image was dropped).
6. `Controllers/ImageController.cs` serves `/img/{id}` and `/img/{id}/thumb` from the
   store, with a one-year browser cache. Images never change after upload.

TinyMCE's own image dialog (the *Insert/Edit Image* button) has no Upload tab. Use it to
insert an image by URL, or to change an existing image's description, size or class.

**Without a store:** images are inserted **by URL** only, and pasting or dragging images
is disabled so they aren't stored as base64 data inside the post content.

**In all cases:**

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