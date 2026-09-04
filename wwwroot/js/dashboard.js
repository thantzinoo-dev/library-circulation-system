(function () {
    "use strict";

    var container = document.getElementById("borrowings-chart-container");
    var tooltip = document.getElementById("chart-tooltip");
    var tooltipDate = document.getElementById("chart-tooltip-date");
    var tooltipCount = document.getElementById("chart-tooltip-count");

    if (!container || !tooltip || !tooltipDate || !tooltipCount) {
        return;
    }

    var dots = Array.from(container.querySelectorAll(".chart-dot"));
    var activeDot = null;
    var hideTimer = null;

    function showTooltip(dot) {
        if (hideTimer !== null) {
            clearTimeout(hideTimer);
            hideTimer = null;
        }

        if (activeDot && activeDot !== dot) {
            activeDot.classList.remove("is-active");
        }

        activeDot = dot;
        dot.classList.add("is-active");

        var date = dot.getAttribute("data-date");
        var count = dot.getAttribute("data-count");
        tooltipDate.textContent = date;
        tooltipCount.textContent = count;

        var dotRect = dot.getBoundingClientRect();
        var containerRect = container.getBoundingClientRect();

        var x = dotRect.left - containerRect.left + dotRect.width / 2;
        var y = dotRect.top - containerRect.top - 8;

        tooltip.style.display = "block";
        tooltip.style.left = x + "px";
        tooltip.style.top = y + "px";

        window.requestAnimationFrame(function () {
            tooltip.style.opacity = "1";
            tooltip.style.transform = "translate(-50%, -100%) scale(1)";
        });
    }

    function hideTooltip() {
        if (activeDot) {
            activeDot.classList.remove("is-active");
            activeDot = null;
        }

        tooltip.style.opacity = "0";
        tooltip.style.transform = "translate(-50%, -100%) scale(0.95)";
        hideTimer = window.setTimeout(function () {
            tooltip.style.display = "none";
            hideTimer = null;
        }, 150);
    }

    // Direct hover and keyboard focus on dots
    dots.forEach(function (dot) {
        dot.addEventListener("mouseenter", function () {
            showTooltip(dot);
        });
        dot.addEventListener("focus", function () {
            showTooltip(dot);
        });
        dot.addEventListener("mouseleave", hideTooltip);
        dot.addEventListener("blur", hideTooltip);
    });

    // Proximity hover when moving mouse over chart area
    container.addEventListener("mousemove", function (event) {
        if (dots.length === 0) return;

        var mouseX = event.clientX;
        var mouseY = event.clientY;

        var closestDot = null;
        var closestDistSq = Infinity;

        dots.forEach(function (dot) {
            var rect = dot.getBoundingClientRect();
            var centerX = rect.left + rect.width / 2;
            var centerY = rect.top + rect.height / 2;
            var dx = mouseX - centerX;
            var dy = mouseY - centerY;
            var distSq = dx * dx + dy * dy;

            if (distSq < closestDistSq) {
                closestDistSq = distSq;
                closestDot = dot;
            }
        });

        if (closestDot && Math.sqrt(closestDistSq) <= 48) {
            showTooltip(closestDot);
        } else if (activeDot) {
            hideTooltip();
        }
    });

    container.addEventListener("mouseleave", hideTooltip);
})();
