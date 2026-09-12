(() => {
    "use strict";

    const dialog = document.querySelector("[data-return-dialog]");
    if (!dialog) return;

    const panel = dialog.querySelector("[data-return-form]");
    const loanIdInput = panel.querySelector("[name='Input.LoanId']");
    const returnDateInput = panel.querySelector("[data-return-date]");
    const fineInput = panel.querySelector("[name='Input.FineAmount']");
    const cover = panel.querySelector("[data-return-cover]");
    const coverFallback = panel.querySelector("[data-return-cover-fallback]");
    const overdueBadge = panel.querySelector("[data-return-overdue]");
    const dueDate = panel.querySelector("[data-return-due-date]");
    let previouslyFocused = null;
    let closeTimer = null;

    const setText = (selector, value) => {
        const element = panel.querySelector(selector);
        if (element) element.textContent = value || "—";
    };

    const populateFromButton = (button) => {
        panel.reset();
        loanIdInput.value = button.dataset.loanId || "0";
        returnDateInput.min = button.dataset.borrowDateIso || "";
        if (fineInput) fineInput.value = "0";

        setText("[data-return-loan-code]", button.dataset.loanCode);
        setText("[data-return-book-title]", button.dataset.bookTitle);
        setText("[data-return-isbn]", button.dataset.isbn);
        setText("[data-return-member-name]", button.dataset.memberName);
        setText("[data-return-member-code]", button.dataset.memberCode);
        setText("[data-return-member-type]", button.dataset.memberType);
        setText("[data-return-borrow-date]", button.dataset.borrowDate);
        setText("[data-return-due-date]", button.dataset.dueDate);
        setText("[data-return-quantity]", button.dataset.quantity);

        const isOverdue = button.dataset.isOverdue === "true";
        overdueBadge.textContent = button.dataset.overdueLabel || "Overdue";
        overdueBadge.classList.toggle("hidden", !isOverdue);
        dueDate.classList.toggle("text-red-600", isOverdue);
        dueDate.classList.toggle("text-ink-900", !isOverdue);

        const coverPath = button.dataset.cover || "";
        if (coverPath) {
            cover.src = coverPath;
            cover.classList.remove("hidden");
            coverFallback.classList.add("hidden");
        } else {
            cover.removeAttribute("src");
            cover.classList.add("hidden");
            coverFallback.classList.remove("hidden");
        }
    };

    const openDialog = (trigger, populate = true) => {
        if (closeTimer) {
            window.clearTimeout(closeTimer);
            closeTimer = null;
        }

        previouslyFocused = trigger || document.activeElement;
        if (populate && trigger) populateFromButton(trigger);

        dialog.hidden = false;
        dialog.inert = false;
        document.body.style.overflow = "hidden";

        window.requestAnimationFrame(() => {
            dialog.classList.remove("opacity-0", "pointer-events-none");
            dialog.classList.add("opacity-100");
            panel.classList.remove("translate-y-2", "scale-[0.98]");
            panel.classList.add("translate-y-0", "scale-100");
            returnDateInput.focus();
        });
    };

    const closeDialog = () => {
        dialog.classList.add("opacity-0", "pointer-events-none");
        dialog.classList.remove("opacity-100");
        panel.classList.add("translate-y-2", "scale-[0.98]");
        panel.classList.remove("translate-y-0", "scale-100");
        dialog.inert = true;
        document.body.style.overflow = "";

        const focusTarget = previouslyFocused;
        closeTimer = window.setTimeout(() => {
            dialog.hidden = true;
            closeTimer = null;
            if (focusTarget instanceof HTMLElement) focusTarget.focus();
        }, 150);
    };

    document.querySelectorAll("[data-return-open]").forEach((button) => {
        button.addEventListener("click", () => openDialog(button));
    });

    dialog.querySelectorAll("[data-return-dialog-close]").forEach((button) => {
        button.addEventListener("click", closeDialog);
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && !dialog.hidden) closeDialog();
    });

    if (dialog.dataset.initialOpen === "true") openDialog(null, false);
})();
