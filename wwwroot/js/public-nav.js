(function () {
    "use strict";

    var navigation = document.querySelector("[data-public-navigation]");
    if (!navigation) {
        return;
    }

    var links = Array.from(navigation.querySelectorAll("[data-public-nav-link]"));
    var currentPage = (navigation.dataset.publicPage || "").toLowerCase();

    function updateActiveLink() {
        var activeKey = null;

        if (currentPage === "/index") {
            activeKey = window.location.hash.toLowerCase() === "#books" ? "books" : "home";
        } else if (currentPage === "/public/borrow") {
            activeKey = "books";
        } else if (currentPage === "/public/myborrowings") {
            activeKey = "borrowings";
        }

        links.forEach(function (link) {
            if (link.dataset.publicNavLink === activeKey) {
                link.setAttribute("aria-current", "page");
            } else {
                link.removeAttribute("aria-current");
            }
        });
    }

    window.addEventListener("hashchange", updateActiveLink);
    updateActiveLink();
})();
