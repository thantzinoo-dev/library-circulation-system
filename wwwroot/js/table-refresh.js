(() => {
    "use strict";

    const requests = new Map();
    const page = document.querySelector("[data-ajax-table-page]");

    const liveRegion = document.createElement("div");
    liveRegion.className = "sr-only";
    liveRegion.setAttribute("aria-live", "polite");
    liveRegion.setAttribute("aria-atomic", "true");
    document.body.append(liveRegion);

    const animateRows = (container) => {
        if (!container || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

        const rows = Array.from(container.querySelectorAll("tbody tr"));
        rows.forEach((row, index) => {
            row.classList.remove("table-row-enter");
            row.style.setProperty("--table-row-delay", `${Math.min(index, 9) * 24}ms`);
            void row.offsetWidth;
            row.classList.add("table-row-enter");
        });

        window.setTimeout(() => {
            rows.forEach((row) => {
                row.classList.remove("table-row-enter");
                row.style.removeProperty("--table-row-delay");
            });
        }, 500);
    };

    window.LibraryTableMotion = { animateRows };

    const tableSelector = (key) => `[data-ajax-table-region="${CSS.escape(key)}"]`;

    const updateTable = async (key, requestedUrl, updateHistory = true) => {
        const currentRegion = document.querySelector(tableSelector(key));
        if (!currentRegion) return;

        requests.get(key)?.abort();
        const request = new AbortController();
        requests.set(key, request);
        currentRegion.setAttribute("aria-busy", "true");

        try {
            const response = await fetch(requestedUrl, {
                headers: { "X-Requested-With": "XMLHttpRequest" },
                cache: "no-store",
                signal: request.signal
            });
            if (!response.ok) throw new Error(`Table request failed with ${response.status}.`);

            const html = await response.text();
            const documentResult = new DOMParser().parseFromString(html, "text/html");
            const incomingRegion = documentResult.querySelector(tableSelector(key));

            if (!incomingRegion) {
                window.location.assign(response.url || requestedUrl);
                return;
            }

            currentRegion.innerHTML = incomingRegion.innerHTML;
            currentRegion.removeAttribute("aria-busy");
            animateRows(currentRegion);

            if (updateHistory) {
                window.history.replaceState({ ajaxTable: key }, "", requestedUrl);
            }

            liveRegion.textContent = "Table data updated.";
        } catch (error) {
            if (error.name !== "AbortError") {
                console.error(error);
                window.location.assign(requestedUrl);
            }
        } finally {
            if (requests.get(key) === request) {
                currentRegion.removeAttribute("aria-busy");
                requests.delete(key);
            }
        }
    };

    const formUrl = (form) => {
        const url = new URL(form.action || window.location.href, window.location.href);
        url.search = "";

        for (const [name, value] of new FormData(form)) {
            if (typeof value === "string" && value.trim()) {
                url.searchParams.append(name, value.trim());
            }
        }

        return url;
    };

    document.addEventListener("submit", (event) => {
        if (!(event.target instanceof HTMLFormElement)) return;
        const form = event.target.closest("[data-ajax-table-form]");
        if (!form) return;

        const key = form.dataset.ajaxTableForm;
        if (!key || !document.querySelector(tableSelector(key))) return;

        event.preventDefault();
        updateTable(key, formUrl(form));
    });

    document.addEventListener("click", (event) => {
        if (!(event.target instanceof Element)) return;

        const link = event.target.closest("[data-ajax-pagination] a, a[data-ajax-table-link]");
        if (!link || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        if (link.classList.contains("pointer-events-none")) return;

        const owner = link.closest("[data-ajax-table-page]") || page;
        const key = link.dataset.ajaxTableLink || owner?.dataset.ajaxTablePage;
        if (!key || !document.querySelector(tableSelector(key))) return;

        event.preventDefault();
        updateTable(key, new URL(link.href, window.location.href));
    });

})();
