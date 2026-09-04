(() => {
    "use strict";

    document.querySelectorAll("[data-delete-form]").forEach((form) => {
        form.addEventListener("submit", (event) => {
            const title = form.dataset.bookTitle || "this book";
            if (!window.confirm(`Delete “${title}”? This action cannot be undone.`)) {
                event.preventDefault();
            }
        });
    });
})();
