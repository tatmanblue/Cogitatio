// Image upload for the post editor (GitHub issue #5).
//
// Uploads go through the "Upload image" dialog in Shared/PostEditor.razor, which asks for the
// image and its description (alt text) on one screen. The dialog opens from the editor's
// "Upload image" toolbar button, or when an image is pasted or dropped into the editor.
//
// The chosen file stays in the browser ("pending") until the dialog's Upload button is pressed.
// It is then resized with a <canvas> (re-encoding also drops EXIF metadata, including GPS
// location) and streamed to .NET through the Blazor circuit (PostEditor.UploadImage), which
// checks the admin session, validates the file and stores it. The returned /img/{id} is
// inserted at the cursor position saved when the dialog opened.
//
// TinyMCE picks up `cogitatioImages.conf` through the editor's JsConfSrc parameter.
window.cogitatioImages = (function () {
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];

    let dotNetRef = null;
    let options = { maxWidth: 1200, thumbWidth: 300, quality: 0.82 };
    let editor = null;
    let bookmark = null;
    let pending = null;   // { file, previewUrl }

    async function decode(blob) {
        try {
            // apply the camera's rotation flag, since EXIF is dropped when re-encoding
            return await createImageBitmap(blob, { imageOrientation: 'from-image' });
        } catch {
            return await createImageBitmap(blob);
        }
    }

    async function encode(bitmap, targetWidth, type, quality) {
        const scale = Math.min(1, targetWidth / bitmap.width);   // never scale up
        const width = Math.max(1, Math.round(bitmap.width * scale));
        const height = Math.max(1, Math.round(bitmap.height * scale));

        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        ctx.imageSmoothingEnabled = true;
        ctx.imageSmoothingQuality = 'high';
        ctx.drawImage(bitmap, 0, 0, width, height);

        const blob = await new Promise(resolve => canvas.toBlob(resolve, type, quality));
        return { blob, width, height };
    }

    // Returns { web: Blob, thumb: Blob|null, type }
    async function resize(file) {
        // Animated GIFs would lose their animation on a canvas; upload them unchanged.
        if (file.type === 'image/gif') {
            return { web: file, thumb: null, type: 'image/gif' };
        }

        const bitmap = await decode(file);
        try {
            // PNG stays PNG (screenshots, transparency); photos become WebP, or JPEG if the
            // browser can't encode WebP (toBlob silently falls back to PNG in that case).
            let type = file.type === 'image/png' ? 'image/png' : 'image/webp';
            let web = await encode(bitmap, options.maxWidth, type, options.quality);
            if (type === 'image/webp' && (!web.blob || web.blob.type !== 'image/webp')) {
                type = 'image/jpeg';
                web = await encode(bitmap, options.maxWidth, type, options.quality);
            }
            const thumb = await encode(bitmap, options.thumbWidth, type, options.quality);
            return { web: web.blob, thumb: thumb.blob, type };
        } finally {
            if (bitmap.close) bitmap.close();
        }
    }

    async function toStreamRef(blob) {
        return DotNet.createJSStreamReference(new Uint8Array(await blob.arrayBuffer()));
    }

    // Resizes and uploads. Returns { success, url, error, width, height }.
    async function upload(file, fileName, altText) {
        if (!dotNetRef) {
            return { success: false, url: '', error: 'Image uploads are not available on this page.' };
        }
        const resized = await resize(file);
        const webRef = await toStreamRef(resized.web);
        const thumbRef = resized.thumb ? await toStreamRef(resized.thumb) : null;
        return await dotNetRef.invokeMethodAsync('UploadImage', fileName || 'image', altText, webRef, thumbRef);
    }

    // first image file in a clipboard or drag-and-drop payload, or null
    function imageFileFrom(dataTransfer) {
        if (!dataTransfer || !dataTransfer.files) return null;
        for (const file of dataTransfer.files) {
            if (allowedTypes.includes(file.type)) return file;
        }
        return null;
    }

    function clearPending() {
        if (pending && pending.previewUrl) URL.revokeObjectURL(pending.previewUrl);
        pending = null;
    }

    // Holds the file in the browser and returns what the dialog shows, or null if it isn't
    // a supported image.
    function setPending(file) {
        clearPending();
        if (!file || !allowedTypes.includes(file.type)) return null;
        pending = { file, previewUrl: URL.createObjectURL(file) };
        return { name: file.name || 'image', previewUrl: pending.previewUrl, sizeBytes: file.size };
    }

    // Remembers the cursor position and asks PostEditor to show the dialog.
    function openDialog(file) {
        if (!dotNetRef || !editor) return;
        bookmark = editor.selection.getBookmark(2, true);
        const info = file ? setPending(file) : (clearPending(), null);
        dotNetRef.invokeMethodAsync('OpenImageDialog', info);
    }

    function insertImage(url, altText, width, height) {
        if (!editor) return;
        editor.focus();
        if (bookmark) editor.selection.moveToBookmark(bookmark);
        const attrs = { src: url, alt: altText, class: 'img-fluid' };
        if (width > 0 && height > 0) {
            attrs.width = width;
            attrs.height = height;
        }
        // createHTML encodes the attribute values, so the alt text can't break the markup
        editor.insertContent(editor.dom.createHTML('img', attrs));
        editor.undoManager.add();
        if (editor.dispatch) editor.dispatch('change'); else editor.fire('change');
    }

    // TinyMCE setup: the toolbar button, and paste/drop of image files opening the dialog
    // instead of TinyMCE inserting the image itself.
    function setup(ed) {
        editor = ed;

        ed.ui.registry.addButton('cogitatioimage', {
            icon: 'upload',
            tooltip: 'Upload image',
            onAction: () => openDialog(null)
        });

        ed.on('paste', e => {
            const file = imageFileFrom(e.clipboardData);
            if (!file || !dotNetRef) return;
            e.preventDefault();
            e.stopImmediatePropagation();
            openDialog(file);
        });

        ed.on('drop', e => {
            const file = imageFileFrom(e.dataTransfer);
            if (!file || !dotNetRef) return;
            e.preventDefault();
            e.stopImmediatePropagation();
            // insert where the image was dropped, not where the cursor was
            if (ed.selection.placeCaretAt) ed.selection.placeCaretAt(e.clientX, e.clientY);
            openDialog(file);
        });

        ed.on('remove', () => {
            if (editor === ed) editor = null;
        });
    }

    return {
        // called by PostEditor after it renders, when an image store is configured
        register: function (ref, opts) {
            dotNetRef = ref;
            options = Object.assign({}, options, opts || {});
        },
        unregister: function (ref) {
            if (dotNetRef === ref) dotNetRef = null;
            clearPending();
        },

        // called by the dialog's file input
        selectFile: function (input) {
            const file = input && input.files && input.files.length > 0 ? input.files[0] : null;
            const info = setPending(file);
            if (input) input.value = '';
            return info;
        },

        // called by the dialog's Upload button. Returns { success, url, error, width, height }.
        uploadPending: async function (altText) {
            if (!pending) {
                return { success: false, url: '', error: 'Choose an image first.' };
            }
            let result;
            try {
                result = await upload(pending.file, pending.file.name, altText);
            } catch (e) {
                console.error('Image upload failed', e);
                return { success: false, url: '', error: 'Image upload failed. See the browser console for details.' };
            }
            if (result && result.success) {
                insertImage(result.url, altText, result.width, result.height);
                clearPending();
                bookmark = null;
            }
            return result;
        },

        // called when the dialog closes without uploading
        cancel: function () {
            clearPending();
            bookmark = null;
            if (editor) editor.focus();
        },

        resize: resize,
        conf: {
            setup: setup
        }
    };
})();
