(function () {
    "use strict";

    var sidebar = document.querySelector("[data-app-sidebar]");
    var sidebarToggle = document.querySelector("[data-sidebar-toggle]");
    var sidebarBackdrop = document.querySelector("[data-sidebar-backdrop]");
    var accountRoot = document.querySelector("[data-account-root]");
    var accountTrigger = document.querySelector("[data-account-trigger]");
    var accountMenu = document.querySelector("[data-account-menu]");

    if (!sidebar || !sidebarToggle || !accountRoot || !accountTrigger || !accountMenu) {
        return;
    }

    var storageKey = "school-library-sidebar-collapsed";
    var compactViewport = window.matchMedia("(max-width: 900px)");
    var reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    var accountMenuCloseTimer = null;
    var accountMenuOpenFrame = null;

    function isSidebarCollapsed() {
        return sidebar.dataset.collapsed === "true";
    }

    function setSidebarCollapsed(collapsed, persist) {
        sidebar.dataset.collapsed = collapsed ? "true" : "false";
        document.documentElement.dataset.sidebarCollapsed = collapsed ? "true" : "false";
        sidebarToggle.setAttribute("aria-expanded", collapsed ? "false" : "true");
        sidebarToggle.setAttribute("aria-label", collapsed ? "Expand sidebar" : "Collapse sidebar");
        sidebarToggle.title = collapsed ? "Expand sidebar" : "Collapse sidebar";

        var toggleIcon = sidebarToggle.querySelector(".sidebar-toggle-icon") || sidebarToggle.querySelector("svg");
        if (toggleIcon) {
            toggleIcon.style.transform = collapsed ? "rotate(180deg)" : "rotate(0deg)";
        }

        if (sidebarBackdrop) {
            sidebarBackdrop.hidden = !compactViewport.matches || collapsed;
        }

        if (persist) {
            try {
                window.localStorage.setItem(storageKey, collapsed ? "true" : "false");
            } catch (_error) {
                // The sidebar remains usable when browser storage is unavailable.
            }
        }

        if (!accountMenu.hidden) {
            positionAccountMenu();
        }
    }

    function positionAccountMenu() {
        if (accountMenu.hidden) {
            return;
        }

        var viewportPadding = 8;
        var menuGap = 8;
        var triggerRect = accountTrigger.getBoundingClientRect();
        var availableWidth = Math.max(0, window.innerWidth - viewportPadding * 2);
        var preferredWidth = isSidebarCollapsed() ? 240 : triggerRect.width;
        var menuWidth = Math.min(Math.max(preferredWidth, 224), availableWidth);

        accountMenu.style.width = menuWidth + "px";
        accountMenu.style.left = viewportPadding + "px";
        accountMenu.style.top = viewportPadding + "px";
        accountMenu.style.visibility = "hidden";

        var menuHeight = accountMenu.offsetHeight;
        var left = Math.min(
            Math.max(triggerRect.left, viewportPadding),
            Math.max(viewportPadding, window.innerWidth - menuWidth - viewportPadding)
        );
        var top = Math.max(viewportPadding, triggerRect.top - menuHeight - menuGap);

        accountMenu.style.left = left + "px";
        accountMenu.style.top = top + "px";
        accountMenu.style.visibility = "visible";
    }

    function openAccountMenu(focusFirstItem) {
        if (accountMenuCloseTimer !== null) {
            window.clearTimeout(accountMenuCloseTimer);
            accountMenuCloseTimer = null;
        }

        if (accountMenuOpenFrame !== null) {
            window.cancelAnimationFrame(accountMenuOpenFrame);
        }

        accountMenu.hidden = false;
        accountMenu.inert = false;
        accountMenu.dataset.state = "closed";
        accountTrigger.setAttribute("aria-expanded", "true");
        positionAccountMenu();

        accountMenuOpenFrame = window.requestAnimationFrame(function () {
            accountMenuOpenFrame = null;
            accountMenu.dataset.state = "open";

            if (focusFirstItem) {
                var firstItem = accountMenu.querySelector('[role="menuitem"]');
                if (firstItem) {
                    firstItem.focus({ preventScroll: true });
                }
            }
        });
    }

    function closeAccountMenu(restoreFocus) {
        if (accountMenu.hidden || accountTrigger.getAttribute("aria-expanded") !== "true") {
            return;
        }

        if (accountMenuOpenFrame !== null) {
            window.cancelAnimationFrame(accountMenuOpenFrame);
            accountMenuOpenFrame = null;
        }

        accountMenu.dataset.state = "closed";
        accountMenu.inert = true;
        accountTrigger.setAttribute("aria-expanded", "false");

        if (restoreFocus) {
            accountTrigger.focus({ preventScroll: true });
        }

        function finishClose() {
            if (accountTrigger.getAttribute("aria-expanded") === "false") {
                accountMenu.hidden = true;
                accountMenu.style.removeProperty("visibility");
            }
            accountMenuCloseTimer = null;
        }

        if (reducedMotion.matches) {
            finishClose();
        } else {
            accountMenuCloseTimer = window.setTimeout(finishClose, 180);
        }
    }

    function toggleAccountMenu() {
        if (accountTrigger.getAttribute("aria-expanded") !== "true") {
            openAccountMenu(false);
        } else {
            closeAccountMenu(false);
        }
    }

    function focusAdjacentMenuItem(direction) {
        var items = Array.from(accountMenu.querySelectorAll('[role="menuitem"]'));
        var currentIndex = items.indexOf(document.activeElement);

        if (items.length === 0) {
            return;
        }

        var nextIndex = currentIndex < 0
            ? 0
            : (currentIndex + direction + items.length) % items.length;

        items[nextIndex].focus({ preventScroll: true });
    }

    var savedCollapsedState = null;
    try {
        savedCollapsedState = window.localStorage.getItem(storageKey);
    } catch (_error) {
        // Use the viewport default when browser storage is unavailable.
    }

    var initiallyCollapsed = compactViewport.matches || savedCollapsedState === "true";

    setSidebarCollapsed(initiallyCollapsed, false);
    window.requestAnimationFrame(function () {
        sidebar.dataset.ready = "true";
    });

    sidebarToggle.addEventListener("click", function () {
        closeAccountMenu(false);
        setSidebarCollapsed(!isSidebarCollapsed(), true);
    });

    if (sidebarBackdrop) {
        sidebarBackdrop.addEventListener("click", function () {
            setSidebarCollapsed(true, false);
            sidebarToggle.focus({ preventScroll: true });
        });
    }

    accountTrigger.addEventListener("click", toggleAccountMenu);

    accountTrigger.addEventListener("keydown", function (event) {
        if (event.key === "ArrowUp" || event.key === "ArrowDown") {
            event.preventDefault();
            openAccountMenu(true);
        }
    });

    accountMenu.addEventListener("keydown", function (event) {
        if (event.key === "ArrowDown") {
            event.preventDefault();
            focusAdjacentMenuItem(1);
        } else if (event.key === "ArrowUp") {
            event.preventDefault();
            focusAdjacentMenuItem(-1);
        } else if (event.key === "Home" || event.key === "End") {
            event.preventDefault();
            var items = accountMenu.querySelectorAll('[role="menuitem"]');
            var item = event.key === "Home" ? items[0] : items[items.length - 1];
            if (item) {
                item.focus({ preventScroll: true });
            }
        }
    });

    accountMenu.addEventListener("click", function (event) {
        if (event.target.closest('[role="menuitem"]')) {
            closeAccountMenu(false);
        }
    });

    document.addEventListener("pointerdown", function (event) {
        if (!accountMenu.hidden && !accountRoot.contains(event.target)) {
            closeAccountMenu(false);
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && !accountMenu.hidden) {
            event.preventDefault();
            closeAccountMenu(true);
        } else if (event.key === "Escape" && compactViewport.matches && !isSidebarCollapsed()) {
            event.preventDefault();
            setSidebarCollapsed(true, false);
            sidebarToggle.focus({ preventScroll: true });
        }
    });

    function handleViewportChange(event) {
        if (event.matches) {
            setSidebarCollapsed(true, false);
            return;
        }

        var savedState = null;
        try {
            savedState = window.localStorage.getItem(storageKey);
        } catch (_error) {
            // Default to expanded on desktop when storage is unavailable.
        }

        setSidebarCollapsed(savedState === "true", false);
    }

    if (typeof compactViewport.addEventListener === "function") {
        compactViewport.addEventListener("change", handleViewportChange);
    } else {
        compactViewport.addListener(handleViewportChange);
    }

    window.addEventListener("resize", positionAccountMenu);
    window.addEventListener("scroll", positionAccountMenu, true);
})();
