(() => {
    "use strict";

    const input = document.querySelector("[data-cover-input]");
    const uploader = document.querySelector("[data-cover-uploader]");
    const dropzone = uploader?.querySelector("[data-cover-dropzone]");
    const emptyState = uploader?.querySelector("[data-cover-empty]");
    const preview = uploader?.querySelector("[data-cover-preview]");
    const previewImage = uploader?.querySelector("[data-cover-preview-image]");
    const fileName = uploader?.querySelector("[data-cover-file-name]");
    const fileSize = uploader?.querySelector("[data-cover-file-size]");
    const error = uploader?.querySelector("[data-cover-error]");
    const removeButton = uploader?.querySelector("[data-cover-remove]");

    if (!input || !uploader || !dropzone || !emptyState || !preview || !previewImage || !fileName || !fileSize || !error) return;

    const allowedTypes = new Set(["image/jpeg", "image/png", "image/webp"]);
    const allowedExtension = /\.(jpe?g|png|webp)$/i;
    const maximumSize = 5 * 1024 * 1024;
    let previewUrl = null;

    input.classList.add("hidden");
    uploader.classList.remove("hidden");

    const clearError = () => {
        error.textContent = "";
        error.classList.add("hidden");
        dropzone.classList.remove("border-red-400", "bg-red-50/40");
    };

    const showError = (message) => {
        error.textContent = message;
        error.classList.remove("hidden");
        dropzone.classList.add("border-red-400", "bg-red-50/40");
    };

    const releasePreview = () => {
        if (previewUrl) URL.revokeObjectURL(previewUrl);
        previewUrl = null;
        previewImage.removeAttribute("src");
    };

    const resetSelection = () => {
        input.value = "";
        releasePreview();
        preview.classList.add("hidden");
        preview.classList.remove("flex");
        emptyState.classList.remove("hidden");
        clearError();
    };

    const validate = (file) => {
        if (!allowedTypes.has(file.type) && !allowedExtension.test(file.name)) {
            return "Choose a JPG, PNG, or WebP image.";
        }
        if (file.size > maximumSize) {
            return "The cover image must be 5 MB or smaller.";
        }
        return null;
    };

    const showFile = (file) => {
        const validationMessage = validate(file);
        if (validationMessage) {
            resetSelection();
            showError(validationMessage);
            return;
        }

        clearError();
        releasePreview();
        previewUrl = URL.createObjectURL(file);
        previewImage.src = previewUrl;
        fileName.textContent = file.name;
        fileSize.textContent = `${(file.size / 1024 / 1024).toFixed(2)} MB`;
        emptyState.classList.add("hidden");
        preview.classList.remove("hidden");
        preview.classList.add("flex");
    };

    const useDroppedFile = (file) => {
        const transfer = new DataTransfer();
        transfer.items.add(file);
        input.files = transfer.files;
        showFile(file);
    };

    dropzone.addEventListener("click", (event) => {
        if (event.target.closest("[data-cover-remove]")) return;
        input.click();
    });

    dropzone.addEventListener("keydown", (event) => {
        if (event.target.closest("[data-cover-remove]")) return;
        if (event.key !== "Enter" && event.key !== " ") return;
        event.preventDefault();
        input.click();
    });

    input.addEventListener("change", () => {
        const file = input.files?.[0];
        if (file) showFile(file);
        else resetSelection();
    });

    ["dragenter", "dragover"].forEach((eventName) => {
        dropzone.addEventListener(eventName, (event) => {
            event.preventDefault();
            dropzone.classList.add("border-brand-600", "bg-blue-50", "ring-2", "ring-brand-600/15");
        });
    });

    ["dragleave", "drop"].forEach((eventName) => {
        dropzone.addEventListener(eventName, (event) => {
            event.preventDefault();
            dropzone.classList.remove("border-brand-600", "bg-blue-50", "ring-2", "ring-brand-600/15");
        });
    });

    dropzone.addEventListener("drop", (event) => {
        const file = event.dataTransfer?.files?.[0];
        if (file) useDroppedFile(file);
    });

    removeButton?.addEventListener("click", (event) => {
        event.stopPropagation();
        resetSelection();
        dropzone.focus();
    });

    window.addEventListener("beforeunload", releasePreview);
})();
