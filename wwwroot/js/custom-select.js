(function () {
    "use strict";

    function initCustomSelects() {
        var selects = document.querySelectorAll("select[data-custom-select]");
        selects.forEach(function (select) {
            if (select.dataset.shadcnInitialized === "true") {
                return;
            }

            var id = select.id || "select-" + Math.random().toString(36).substring(2, 9);
            select.dataset.shadcnInitialized = "true";
            select.style.display = "none";

            var wrapper = document.createElement("div");
            wrapper.className = "shadcn-select-wrapper " + (select.className.indexOf("w-full") !== -1 ? "w-full" : "");

            var trigger = document.createElement("button");
            trigger.type = "button";
            trigger.className = "shadcn-select-trigger";
            trigger.id = id + "-trigger";
            trigger.setAttribute("aria-haspopup", "listbox");
            trigger.setAttribute("aria-expanded", "false");
            trigger.setAttribute("aria-controls", id + "-menu");

            var textSpan = document.createElement("span");
            textSpan.className = "truncate";

            var chevron = document.createElementNS("http://www.w3.org/2000/svg", "svg");
            chevron.setAttribute("class", "h-4 w-4 shrink-0 text-ink-400 transition-transform duration-200");
            chevron.setAttribute("viewBox", "0 0 24 24");
            chevron.setAttribute("fill", "none");
            chevron.setAttribute("stroke", "currentColor");
            chevron.setAttribute("stroke-width", "2");
            chevron.setAttribute("stroke-linecap", "round");
            chevron.setAttribute("stroke-linejoin", "round");
            chevron.setAttribute("aria-hidden", "true");
            chevron.innerHTML = '<path d="m6 9 6 6 6-6" />';

            trigger.appendChild(textSpan);
            trigger.appendChild(chevron);

            var menu = document.createElement("div");
            menu.className = "shadcn-select-menu";
            menu.id = id + "-menu";
            menu.setAttribute("role", "listbox");
            menu.setAttribute("data-state", "closed");
            menu.setAttribute("aria-labelledby", id + "-trigger");
            menu.style.display = "none";
            // If the element is near right edge or flex justify-between, align right
            if (select.closest(".justify-between") || select.className.indexOf("min-w-[130px]") !== -1) {
                menu.style.right = "0";
                menu.style.left = "auto";
            }

            function updateDisplay() {
                var selectedOption = select.options[select.selectedIndex] || select.options[0];
                textSpan.textContent = selectedOption ? selectedOption.textContent : "";
                
                var items = menu.querySelectorAll(".shadcn-select-option");
                items.forEach(function (item) {
                    var val = item.getAttribute("data-value");
                    var isSelected = val === select.value;
                    item.setAttribute("aria-selected", isSelected ? "true" : "false");
                    var check = item.querySelector(".shadcn-select-check");
                    if (check) {
                        if (isSelected) {
                            check.classList.remove("hidden");
                        } else {
                            check.classList.add("hidden");
                        }
                    }
                });
            }

            Array.from(select.options).forEach(function (option) {
                var item = document.createElement("div");
                item.className = "shadcn-select-option";
                item.setAttribute("role", "option");
                item.setAttribute("data-value", option.value);

                var itemText = document.createElement("span");
                itemText.textContent = option.textContent;

                var check = document.createElementNS("http://www.w3.org/2000/svg", "svg");
                check.setAttribute("class", "shadcn-select-check h-4 w-4 text-brand-600");
                check.setAttribute("viewBox", "0 0 24 24");
                check.setAttribute("fill", "none");
                check.setAttribute("stroke", "currentColor");
                check.setAttribute("stroke-width", "2.5");
                check.setAttribute("stroke-linecap", "round");
                check.setAttribute("stroke-linejoin", "round");
                check.innerHTML = '<path d="M20 6 9 17l-5-5" />';

                item.appendChild(itemText);
                item.appendChild(check);

                item.addEventListener("click", function (e) {
                    e.stopPropagation();
                    select.value = option.value;
                    updateDisplay();
                    closeMenu();
                    select.dispatchEvent(new Event("change", { bubbles: true }));
                });

                menu.appendChild(item);
            });

            function openMenu() {
                document.querySelectorAll(".shadcn-select-menu").forEach(function (otherMenu) {
                    if (otherMenu !== menu) {
                        otherMenu.setAttribute("data-state", "closed");
                        otherMenu.style.display = "none";
                        var otherTrigger = document.querySelector('[aria-controls="' + otherMenu.id + '"]');
                        if (otherTrigger) {
                            otherTrigger.setAttribute("aria-expanded", "false");
                            var icon = otherTrigger.querySelector("svg");
                            if (icon) icon.style.transform = "rotate(0deg)";
                        }
                    }
                });

                menu.style.display = "block";
                menu.setAttribute("data-state", "open");
                trigger.setAttribute("aria-expanded", "true");
                chevron.style.transform = "rotate(180deg)";
            }

            function closeMenu() {
                menu.style.display = "none";
                menu.setAttribute("data-state", "closed");
                trigger.setAttribute("aria-expanded", "false");
                chevron.style.transform = "rotate(0deg)";
            }

            trigger.addEventListener("click", function (e) {
                e.stopPropagation();
                if (menu.getAttribute("data-state") === "open") {
                    closeMenu();
                } else {
                    openMenu();
                }
            });

            trigger.addEventListener("keydown", function (e) {
                if (e.key === "ArrowDown" || e.key === "ArrowUp" || e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    if (menu.getAttribute("data-state") !== "open") {
                        openMenu();
                    }
                } else if (e.key === "Escape") {
                    closeMenu();
                }
            });

            // Handle outside click
            document.addEventListener("click", function (e) {
                if (!wrapper.contains(e.target)) {
                    closeMenu();
                }
            });

            // Sync with form resets
            if (select.form) {
                select.form.addEventListener("reset", function () {
                    setTimeout(updateDisplay, 10);
                });
            }

            select.parentNode.insertBefore(wrapper, select);
            wrapper.appendChild(select);
            wrapper.appendChild(trigger);
            wrapper.appendChild(menu);

            updateDisplay();
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initCustomSelects);
    } else {
        initCustomSelects();
    }

    window.initCustomSelects = initCustomSelects;
})();
