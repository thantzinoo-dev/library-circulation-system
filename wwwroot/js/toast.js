(() => {
    const toaster = document.querySelector("[data-sonner-toaster]");
    if (!toaster) return;

    const icons = {
        success: '<path d="m5 12 4 4L19 6" />',
        error: '<circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" />',
        info: '<circle cx="12" cy="12" r="9" /><path d="M12 11v5M12 8h.01" />'
    };

    const titles = {
        success: "Success",
        error: "Something went wrong",
        info: "Information"
    };

    const removeToast = (toast) => {
        if (!toast || toast.dataset.sonnerLeaving === "true") return;

        toast.dataset.sonnerLeaving = "true";
        toast.classList.remove("is-visible");
        toast.classList.add("is-leaving");

        const finish = () => toast.remove();
        toast.addEventListener("transitionend", finish, { once: true });
        window.setTimeout(finish, 300);
    };

    const initializeToast = (toast) => {
        if (toast.dataset.sonnerReady === "true") return;
        toast.dataset.sonnerReady = "true";

        const duration = Number.parseInt(toast.dataset.sonnerDuration || "5000", 10);
        let remaining = Number.isFinite(duration) ? duration : 5000;
        let startedAt = 0;
        let timerId = 0;

        const startTimer = () => {
            if (remaining <= 0) {
                removeToast(toast);
                return;
            }

            startedAt = Date.now();
            timerId = window.setTimeout(() => removeToast(toast), remaining);
        };

        const pauseTimer = () => {
            if (!timerId) return;
            window.clearTimeout(timerId);
            timerId = 0;
            remaining -= Date.now() - startedAt;
        };

        toast.querySelector("[data-sonner-close]")?.addEventListener("click", () => removeToast(toast));
        toast.addEventListener("mouseenter", pauseTimer);
        toast.addEventListener("mouseleave", startTimer);
        toast.addEventListener("focusin", pauseTimer);
        toast.addEventListener("focusout", (event) => {
            if (!toast.contains(event.relatedTarget)) startTimer();
        });

        window.requestAnimationFrame(() => {
            window.requestAnimationFrame(() => toast.classList.add("is-visible"));
        });
        startTimer();
    };

    const createToast = (message, options = {}) => {
        if (!message) return null;

        const type = ["success", "error", "info"].includes(options.type) ? options.type : "info";
        const toast = document.createElement("article");
        toast.className = `sonner-toast sonner-toast-${type}`;
        toast.dataset.sonnerToast = "";
        toast.dataset.sonnerType = type;
        toast.dataset.sonnerDuration = String(options.duration ?? (type === "error" ? 7000 : 5000));
        toast.setAttribute("role", type === "error" ? "alert" : "status");

        const icon = document.createElement("span");
        icon.className = "sonner-toast-icon";
        icon.setAttribute("aria-hidden", "true");
        icon.innerHTML = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.25" stroke-linecap="round" stroke-linejoin="round">${icons[type]}</svg>`;

        const content = document.createElement("span");
        content.className = "sonner-toast-content";

        const title = document.createElement("strong");
        title.className = "sonner-toast-title";
        title.textContent = options.title || titles[type];

        const description = document.createElement("span");
        description.className = "sonner-toast-description";
        description.textContent = message;
        content.append(title, description);

        const close = document.createElement("button");
        close.type = "button";
        close.className = "sonner-toast-close";
        close.dataset.sonnerClose = "";
        close.setAttribute("aria-label", "Dismiss notification");
        close.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><path d="m6 6 12 12M18 6 6 18" /></svg>';

        toast.append(icon, content, close);
        toaster.prepend(toast);
        initializeToast(toast);
        return toast;
    };

    toaster.querySelectorAll("[data-sonner-toast]").forEach(initializeToast);

    window.libraryToast = {
        show: (message, options) => createToast(message, options),
        success: (message, options = {}) => createToast(message, { ...options, type: "success" }),
        error: (message, options = {}) => createToast(message, { ...options, type: "error" }),
        info: (message, options = {}) => createToast(message, { ...options, type: "info" })
    };
})();
