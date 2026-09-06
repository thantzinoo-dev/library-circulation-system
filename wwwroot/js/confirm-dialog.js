(() => {
    "use strict";

    const dialog = document.querySelector("[data-confirm-dialog]");
    if (!dialog) {
        return;
    }

    const panel = dialog.querySelector("[data-confirm-dialog-panel]");
    const title = dialog.querySelector("[data-confirm-dialog-title]");
    const description = dialog.querySelector("[data-confirm-dialog-description]");
    const cancelButton = dialog.querySelector("[data-confirm-dialog-cancel]");
    const actionButton = dialog.querySelector("[data-confirm-dialog-action]");
    let activeForm = null;
    let activeSubmitter = null;
    let previouslyFocused = null;
    let closeTimer = null;

    const focusableSelector = [
        "button:not([disabled])",
        "a[href]",
        "input:not([disabled])",
        "select:not([disabled])",
        "textarea:not([disabled])",
        "[tabindex]:not([tabindex='-1'])"
    ].join(",");

    function openDialog(form, submitter) {
        if (closeTimer) {
            window.clearTimeout(closeTimer);
            closeTimer = null;
        }

        activeForm = form;
        activeSubmitter = submitter;
        previouslyFocused = document.activeElement;
        title.textContent = form.dataset.confirmTitle || "Are you sure?";
        description.textContent = form.dataset.confirmDescription || "This action cannot be undone.";
        actionButton.textContent = form.dataset.confirmAction || "Continue";
        actionButton.disabled = false;

        dialog.hidden = false;
        dialog.inert = false;
        window.requestAnimationFrame(() => {
            dialog.classList.remove("opacity-0", "pointer-events-none");
            dialog.classList.add("opacity-100");
            panel.classList.remove("translate-y-2", "scale-[0.98]");
            panel.classList.add("translate-y-0", "scale-100");
            cancelButton.focus();
        });
    }

    function closeDialog(restoreFocus = true) {
        dialog.classList.add("opacity-0", "pointer-events-none");
        dialog.classList.remove("opacity-100");
        panel.classList.add("translate-y-2", "scale-[0.98]");
        panel.classList.remove("translate-y-0", "scale-100");
        dialog.inert = true;

        const focusTarget = previouslyFocused;
        closeTimer = window.setTimeout(() => {
            dialog.hidden = true;
            closeTimer = null;
            if (restoreFocus && focusTarget instanceof HTMLElement) {
                focusTarget.focus();
            }
        }, 150);

        activeForm = null;
        activeSubmitter = null;
        previouslyFocused = null;
    }

    document.addEventListener("submit", (event) => {
        const form = event.target.closest("[data-confirm-form]");
        if (!form) {
            return;
        }

        if (form.dataset.confirmed === "true") {
            delete form.dataset.confirmed;
            return;
        }

        event.preventDefault();
        openDialog(form, event.submitter);
    });

    cancelButton.addEventListener("click", () => closeDialog());

    actionButton.addEventListener("click", () => {
        const form = activeForm;
        const submitter = activeSubmitter;
        if (!form) {
            return;
        }

        actionButton.disabled = true;
        form.dataset.confirmed = "true";
        closeDialog(false);
        if (submitter instanceof HTMLElement) {
            form.requestSubmit(submitter);
        } else {
            form.requestSubmit();
        }
    });

    document.addEventListener("keydown", (event) => {
        if (dialog.hidden) {
            return;
        }

        if (event.key === "Escape") {
            event.preventDefault();
            closeDialog();
            return;
        }

        if (event.key !== "Tab") {
            return;
        }

        const focusableItems = Array.from(panel.querySelectorAll(focusableSelector));
        if (focusableItems.length === 0) {
            event.preventDefault();
            return;
        }

        const first = focusableItems[0];
        const last = focusableItems[focusableItems.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    });
})();
