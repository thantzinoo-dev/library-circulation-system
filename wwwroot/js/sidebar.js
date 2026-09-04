(function () {
    "use strict";

    var sidebar = document.querySelector("[data-app-sidebar]");
    var sidebarToggle = document.querySelector("[data-sidebar-toggle]");
    var accountRoot = document.querySelector("[data-account-root]");
    var accountTrigger = document.querySelector("[data-account-trigger]");
    var accountMenu = document.querySelector("[data-account-menu]");

    if (!sidebar || !sidebarToggle || !accountRoot || !accountTrigger || !accountMenu) {
        return;
    }

    var storageKey = "school-library-sidebar-collapsed";

    function isSidebarCollapsed() {
        return sidebar.dataset.collapsed === "true";
    }

    function setSidebarCollapsed(collapsed, persist) {
        sidebar.dataset.collapsed = collapsed ? "true" : "false";
        sidebarToggle.setAttribute("aria-expanded", collapsed ? "false" : "true");
        sidebarToggle.setAttribute("aria-label", collapsed ? "Expand sidebar" : "Collapse sidebar");
        sidebarToggle.title = collapsed ? "Expand sidebar" : "Collapse sidebar";

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
        accountMenu.hidden = false;
        accountTrigger.setAttribute("aria-expanded", "true");
        positionAccountMenu();

        if (focusFirstItem) {
            var firstItem = accountMenu.querySelector('[role="menuitem"]');
            if (firstItem) {
                firstItem.focus({ preventScroll: true });
            }
        }
    }

    function closeAccountMenu(restoreFocus) {
        if (accountMenu.hidden) {
            return;
        }

        accountMenu.hidden = true;
        accountMenu.style.removeProperty("visibility");
        accountTrigger.setAttribute("aria-expanded", "false");

        if (restoreFocus) {
            accountTrigger.focus({ preventScroll: true });
        }
    }

    function toggleAccountMenu() {
        if (accountMenu.hidden) {
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

    var initiallyCollapsed = savedCollapsedState === null
        ? window.matchMedia("(max-width: 900px)").matches
        : savedCollapsedState === "true";

    setSidebarCollapsed(initiallyCollapsed, false);

    sidebarToggle.addEventListener("click", function () {
        closeAccountMenu(false);
        setSidebarCollapsed(!isSidebarCollapsed(), true);
    });

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
        }
    });

    window.addEventListener("resize", positionAccountMenu);
    window.addEventListener("scroll", positionAccountMenu, true);
})();
