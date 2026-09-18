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
    const loansRegion = document.querySelector("[data-active-loans-region]");
    const loansBody = loansRegion?.querySelector("[data-active-loans-body]");
    const loansCount = loansRegion?.querySelector("[data-active-loans-count]");
    const pagination = loansRegion?.querySelector("[data-return-pagination]");
    const summaryStart = loansRegion?.querySelector("[data-summary-start]");
    const summaryEnd = loansRegion?.querySelector("[data-summary-end]");
    const summaryTotal = loansRegion?.querySelector("[data-summary-total]");
    const rowTemplate = document.querySelector("[data-return-loan-row-template]");
    const searchInput = document.getElementById("Search");
    const dueStatusInput = document.getElementById("DueStatus");
    const memberTypeInput = document.getElementById("MembershipType");
    const filterForm = document.querySelector("[data-return-filter-form]");
    const modalPageInput = panel.querySelector("[name='PageNumber']");
    let previouslyFocused = null;
    let closeTimer = null;
    let pageRequest = null;

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
            if (focusTarget instanceof HTMLElement && focusTarget.isConnected) focusTarget.focus();
        }, 150);
    };

    const setRowText = (row, selector, value) => {
        const element = row.querySelector(selector);
        if (element) element.textContent = value ?? "—";
    };

    const renderLoans = (loans) => {
        if (!loansBody || !rowTemplate) return;

        if (!loans.length) {
            const row = document.createElement("tr");
            const cell = document.createElement("td");
            cell.colSpan = 8;
            cell.className = "px-5 py-14 text-center";
            cell.innerHTML = '<span class="mx-auto flex h-11 w-11 items-center justify-center rounded-full bg-blue-50 text-brand-600"><svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="11" cy="11" r="7"></circle><path d="m20 20-3.8-3.8"></path></svg></span><p class="mt-3 font-semibold text-ink-900">No active loans found</p><p class="mt-1 text-sm text-ink-500">Try changing the search term or filters.</p>';
            row.append(cell);
            loansBody.replaceChildren(row);
            return;
        }

        const rows = document.createDocumentFragment();
        loans.forEach((loan) => {
            const fragment = rowTemplate.content.cloneNode(true);
            const row = fragment.querySelector("tr");
            const coverElement = row.querySelector("[data-row-cover]");
            const coverFallback = row.querySelector("[data-row-cover-fallback]");
            const bookTitle = row.querySelector("[data-row-book-title]");
            const rowDueDate = row.querySelector("[data-row-due-date]");
            const status = row.querySelector("[data-row-status]");
            const returnButton = row.querySelector("[data-return-open]");

            setRowText(row, "[data-row-loan-code]", loan.loanId);
            setRowText(row, "[data-row-member-name]", loan.memberName);
            setRowText(row, "[data-row-member-code]", loan.memberCode);
            setRowText(row, "[data-row-book-title]", loan.bookTitle);
            setRowText(row, "[data-row-isbn]", loan.isbn);
            setRowText(row, "[data-row-borrow-date]", loan.borrowDate);
            setRowText(row, "[data-row-due-date]", loan.dueDate);
            setRowText(row, "[data-row-quantity]", String(loan.quantity));

            if (bookTitle) bookTitle.title = loan.bookTitle || "";

            if (loan.coverImagePath && coverElement && coverFallback) {
                coverElement.src = loan.coverImagePath;
                coverElement.classList.remove("hidden");
                coverFallback.classList.add("hidden");
            }

            if (rowDueDate) {
                rowDueDate.classList.add(loan.isOverdue ? "text-red-600" : "text-ink-700");
            }

            if (status) {
                if (loan.isOverdue) {
                    status.textContent = `${loan.daysOverdue} ${loan.daysOverdue === 1 ? "day" : "days"} overdue`;
                    status.classList.add("border-red-200", "bg-red-50", "text-red-700");
                } else {
                    status.textContent = "Active";
                    status.classList.add("return-active-badge", "border-emerald-200", "bg-emerald-50", "text-emerald-700");
                }
            }

            if (returnButton) {
                returnButton.dataset.loanId = String(loan.id);
                returnButton.dataset.loanCode = loan.loanId || "";
                returnButton.dataset.memberName = loan.memberName || "";
                returnButton.dataset.memberCode = loan.memberCode || "";
                returnButton.dataset.memberType = loan.membershipType || "";
                returnButton.dataset.bookTitle = loan.bookTitle || "";
                returnButton.dataset.isbn = loan.isbn || "";
                returnButton.dataset.cover = loan.coverImagePath || "";
                returnButton.dataset.borrowDate = loan.borrowDate || "";
                returnButton.dataset.borrowDateIso = loan.borrowDateIso || "";
                returnButton.dataset.dueDate = loan.dueDate || "";
                returnButton.dataset.quantity = String(loan.quantity);
                returnButton.dataset.isOverdue = String(Boolean(loan.isOverdue));
                returnButton.dataset.overdueLabel = `${loan.daysOverdue} ${loan.daysOverdue === 1 ? "day" : "days"} overdue`;
                returnButton.setAttribute("aria-label", `Return ${loan.loanId} for ${loan.memberName}`);
            }

            rows.append(fragment);
        });

        loansBody.replaceChildren(rows);
        window.LibraryTableMotion?.animateRows(loansBody);
    };

    const createPageButton = (label, pageNumber, options = {}) => {
        const button = document.createElement("button");
        button.type = "button";
        button.dataset.returnPage = String(pageNumber);
        button.textContent = label;
        button.disabled = Boolean(options.disabled);
        button.className = options.current
            ? "inline-flex h-9 min-w-9 items-center justify-center rounded-lg bg-brand-600 px-2 text-sm font-semibold text-white transition-colors"
            : "inline-flex h-9 min-w-9 items-center justify-center rounded-lg border border-line-300 px-2 text-sm font-semibold text-ink-600 transition-colors hover:bg-slate-50 disabled:pointer-events-none disabled:opacity-40";
        if (options.label) button.setAttribute("aria-label", options.label);
        if (options.current) button.setAttribute("aria-current", "page");
        return button;
    };

    const renderPagination = ({ pageNumber, totalPages }) => {
        if (!pagination) return;

        pagination.classList.toggle("hidden", totalPages <= 1);
        if (totalPages <= 1) {
            pagination.replaceChildren();
            return;
        }

        let firstPage = Math.max(1, pageNumber - 2);
        const lastPage = Math.min(totalPages, firstPage + 4);
        firstPage = Math.max(1, lastPage - 4);

        const controls = document.createDocumentFragment();
        controls.append(createPageButton("«", 1, { disabled: pageNumber === 1, label: "First page" }));
        controls.append(createPageButton("‹", pageNumber - 1, { disabled: pageNumber === 1, label: "Previous page" }));

        for (let number = firstPage; number <= lastPage; number += 1) {
            controls.append(createPageButton(String(number), number, { current: number === pageNumber }));
        }

        controls.append(createPageButton("›", pageNumber + 1, { disabled: pageNumber === totalPages, label: "Next page" }));
        controls.append(createPageButton("»", totalPages, { disabled: pageNumber === totalPages, label: "Last page" }));
        pagination.replaceChildren(controls);
    };

    const buildPageUrl = (pageNumber) => {
        const url = new URL(loansRegion.dataset.loanPageUrl, window.location.href);
        url.searchParams.set("PageNumber", String(pageNumber));
        url.searchParams.set("DueStatus", dueStatusInput?.value || "All");
        url.searchParams.set("MembershipType", memberTypeInput?.value || "All");

        const search = searchInput?.value.trim();
        if (search) url.searchParams.set("Search", search);
        else url.searchParams.delete("Search");

        return url;
    };

    const updateBrowserUrl = (pageNumber) => {
        const url = new URL(window.location.href);
        url.searchParams.set("PageNumber", String(pageNumber));
        url.searchParams.set("DueStatus", dueStatusInput?.value || "All");
        url.searchParams.set("MembershipType", memberTypeInput?.value || "All");

        const search = searchInput?.value.trim();
        if (search) url.searchParams.set("Search", search);
        else url.searchParams.delete("Search");

        window.history.replaceState({}, "", url);
    };

    const loadLoanPage = async (pageNumber) => {
        if (!loansRegion || !loansBody || !pagination || !loansRegion.dataset.loanPageUrl) return;

        if (pageRequest) pageRequest.abort();
        const request = new AbortController();
        pageRequest = request;
        const paginationButtons = Array.from(pagination.querySelectorAll("button"));
        const disabledStates = paginationButtons.map((button) => button.disabled);
        loansRegion.setAttribute("aria-busy", "true");
        loansBody.classList.add("opacity-60");
        paginationButtons.forEach((button) => { button.disabled = true; });

        try {
            const response = await fetch(buildPageUrl(pageNumber), {
                headers: { "X-Requested-With": "XMLHttpRequest" },
                cache: "no-store",
                signal: request.signal
            });
            if (!response.ok) throw new Error(`Loan page request failed with ${response.status}.`);

            const data = await response.json();
            renderLoans(data.loans || []);
            renderPagination(data);

            if (loansCount) {
                loansCount.textContent = `${data.totalActiveLoans} ${data.totalActiveLoans === 1 ? "record" : "records"}`;
            }
            if (summaryStart) summaryStart.textContent = String(data.startItem);
            if (summaryEnd) summaryEnd.textContent = String(data.endItem);
            if (summaryTotal) summaryTotal.textContent = String(data.totalActiveLoans);
            if (modalPageInput) modalPageInput.value = String(data.pageNumber);
            updateBrowserUrl(data.pageNumber);
        } catch (error) {
            if (error.name !== "AbortError") {
                console.error(error);
                paginationButtons.forEach((button, index) => { button.disabled = disabledStates[index]; });
            }
        } finally {
            if (pageRequest === request) {
                loansRegion.removeAttribute("aria-busy");
                loansBody.classList.remove("opacity-60");
                pageRequest = null;
            }
        }
    };

    document.addEventListener("click", (event) => {
        if (!(event.target instanceof Element)) return;

        const returnButton = event.target.closest("[data-return-open]");
        if (returnButton) {
            openDialog(returnButton);
            return;
        }

        const pageButton = event.target.closest("[data-return-page]");
        if (pageButton && loansRegion?.contains(pageButton) && !pageButton.disabled) {
            const pageNumber = Number.parseInt(pageButton.dataset.returnPage || "", 10);
            if (Number.isInteger(pageNumber) && pageNumber > 0) loadLoanPage(pageNumber);
        }
    });

    filterForm?.addEventListener("submit", (event) => {
        event.preventDefault();
        loadLoanPage(1);
    });

    dialog.querySelectorAll("[data-return-dialog-close]").forEach((button) => {
        button.addEventListener("click", closeDialog);
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && !dialog.hidden) closeDialog();
    });

    if (dialog.dataset.initialOpen === "true") openDialog(null, false);
})();
