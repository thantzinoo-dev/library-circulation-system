(function () {
    "use strict";

    var THEME_KEY = "school-library-theme";
    var transitionTimeout = null;

    function getStoredTheme() {
        try {
            return localStorage.getItem(THEME_KEY);
        } catch (_e) {
            return null;
        }
    }

    function getActiveTheme() {
        var stored = getStoredTheme();
        if (stored === "dark" || stored === "light") {
            return stored;
        }

        var configured = window.schoolLibraryThemePreference;
        if (configured === "dark" || configured === "light") {
            return configured;
        }

        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
    }

    function applyTheme(theme, animate) {
        var isDark = theme === "dark";
        var doc = document.documentElement;

        if (animate) {
            if (transitionTimeout) {
                clearTimeout(transitionTimeout);
            }
            doc.classList.add("theme-transitioning");
            transitionTimeout = setTimeout(function () {
                doc.classList.remove("theme-transitioning");
                transitionTimeout = null;
            }, 350);
        }

        doc.setAttribute("data-theme", theme);
        if (isDark) {
            doc.classList.add("dark");
        } else {
            doc.classList.remove("dark");
        }

        // Update all theme toggle buttons
        var toggles = document.querySelectorAll("[data-theme-toggle]");
        toggles.forEach(function (btn) {
            btn.setAttribute("aria-label", isDark ? "Switch to light mode" : "Switch to dark mode");
            btn.title = isDark ? "Switch to light mode" : "Switch to dark mode";

            var sunIcon = btn.querySelector(".theme-icon-sun");
            var moonIcon = btn.querySelector(".theme-icon-moon");
            var statusBadge = btn.querySelector(".theme-status-badge");

            if (sunIcon && moonIcon) {
                if (isDark) {
                    sunIcon.classList.remove("hidden");
                    sunIcon.classList.add("block");
                    moonIcon.classList.remove("block");
                    moonIcon.classList.add("hidden");
                } else {
                    sunIcon.classList.remove("block");
                    sunIcon.classList.add("hidden");
                    moonIcon.classList.remove("hidden");
                    moonIcon.classList.add("block");
                }
            }

            if (statusBadge) {
                statusBadge.textContent = isDark ? "On" : "Off";
            }
        });
    }

    function toggleTheme() {
        var current = document.documentElement.getAttribute("data-theme") || getActiveTheme();
        var next = current === "dark" ? "light" : "dark";

        try {
            localStorage.setItem(THEME_KEY, next);
        } catch (_e) {}

        applyTheme(next, true);
    }

    // Apply active theme immediately
    applyTheme(getActiveTheme(), false);

    function init() {
        applyTheme(getActiveTheme(), false);

        document.addEventListener("click", function (event) {
            var toggleBtn = event.target.closest("[data-theme-toggle]");
            if (toggleBtn) {
                event.preventDefault();
                toggleTheme();
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    if (window.matchMedia) {
        window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function (e) {
            if (!getStoredTheme() && window.schoolLibraryThemePreference !== "dark" && window.schoolLibraryThemePreference !== "light") {
                applyTheme(e.matches ? "dark" : "light", true);
            }
        });
    }
})();
