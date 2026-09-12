(function () {
    "use strict";

    var form = document.querySelector("[data-borrow-create-form]");
    if (!form) {
        return;
    }

    var memberInput = document.getElementById("member-search");
    var memberResults = document.getElementById("member-search-results");
    var memberHint = form.querySelector("[data-member-search-hint]");
    var memberIdInput = document.getElementById("selected-member-id");
    var selectedMemberCard = form.querySelector("[data-selected-member]");
    var changeMemberButton = form.querySelector("[data-change-member]");

    var bookInput = document.getElementById("book-search");
    var bookGrid = document.getElementById("available-book-grid");
    var bookStatus = document.getElementById("book-search-status");
    var noBooks = form.querySelector("[data-no-books]");
    var bookIdInput = document.getElementById("selected-book-id");
    var quantityInput = document.getElementById("borrow-quantity");

    var checkoutMember = form.querySelector("[data-checkout-member]");
    var checkoutBook = form.querySelector("[data-checkout-book]");
    var checkoutBookMeta = form.querySelector("[data-checkout-book-meta]");
    var checkoutCover = form.querySelector("[data-checkout-cover]");
    var checkoutHint = form.querySelector("[data-checkout-hint]");
    var submitButton = form.querySelector("[data-submit-borrow]");
    var borrowDate = document.getElementById("Input_BorrowDate");
    var dueDate = document.getElementById("Input_DueDate");

    var memberRequest = null;
    var bookRequest = null;
    var memberTimer = null;
    var bookTimer = null;
    var memberOptions = [];
    var activeMemberIndex = -1;
    var selectedMemberLabel = memberInput ? memberInput.value : "";

    function setText(selector, value) {
        var element = selectedMemberCard && selectedMemberCard.querySelector(selector);
        if (element) {
            element.textContent = value;
        }
    }

    function initials(value) {
        return (value || "M")
            .trim()
            .split(/\s+/)
            .slice(0, 2)
            .map(function (part) { return part.charAt(0).toUpperCase(); })
            .join("") || "M";
    }

    function createElement(tagName, className, textValue) {
        var element = document.createElement(tagName);
        if (className) {
            element.className = className;
        }
        if (textValue !== undefined) {
            element.textContent = textValue;
        }
        return element;
    }

    function setMemberResultsOpen(isOpen) {
        if (!memberInput || !memberResults) {
            return;
        }

        memberResults.classList.toggle("hidden", !isOpen);
        memberInput.setAttribute("aria-expanded", isOpen ? "true" : "false");
        if (!isOpen) {
            memberInput.removeAttribute("aria-activedescendant");
            activeMemberIndex = -1;
        }
    }

    function updateSubmitState() {
        var hasMember = memberIdInput && Number(memberIdInput.value) > 0;
        var hasBook = bookIdInput && Number(bookIdInput.value) > 0;

        if (submitButton) {
            submitButton.disabled = !hasMember || !hasBook;
        }
        if (checkoutHint) {
            checkoutHint.textContent = hasMember && hasBook
                ? "Ready to create the borrowing record."
                : "Select a member and a book to continue.";
        }
    }

    function clearMemberSelection() {
        if (memberIdInput) {
            memberIdInput.value = "0";
        }
        if (selectedMemberCard) {
            selectedMemberCard.classList.add("hidden");
        }
        if (checkoutMember) {
            checkoutMember.textContent = "No member selected";
        }
        selectedMemberLabel = "";
        updateSubmitState();
    }

    function selectMember(member) {
        if (!memberInput || !memberIdInput || !selectedMemberCard) {
            return;
        }

        memberIdInput.value = String(member.id);
        selectedMemberLabel = member.memberCode + " — " + member.name;
        memberInput.value = selectedMemberLabel;
        selectedMemberCard.classList.remove("hidden");
        setText("[data-member-initials]", initials(member.name));
        setText("[data-member-name]", member.name);
        setText("[data-member-code]", member.memberCode);
        setText("[data-member-type]", member.membershipType);
        setText("[data-member-contact]", member.email || member.phone || member.department || "No contact information");
        setText("[data-member-loans]", String(member.activeLoans));

        var overdue = selectedMemberCard.querySelector("[data-member-overdue]");
        if (overdue) {
            overdue.classList.toggle("hidden", !member.hasOverdueLoans);
        }
        if (checkoutMember) {
            checkoutMember.textContent = member.name + " (" + member.memberCode + ")";
        }

        setMemberResultsOpen(false);
        updateSubmitState();
    }

    function setActiveMemberOption(nextIndex) {
        if (!memberInput || memberOptions.length === 0) {
            return;
        }

        activeMemberIndex = (nextIndex + memberOptions.length) % memberOptions.length;
        memberOptions.forEach(function (option, index) {
            option.dataset.focused = index === activeMemberIndex ? "true" : "false";
        });
        var activeOption = memberOptions[activeMemberIndex];
        memberInput.setAttribute("aria-activedescendant", activeOption.id);
        activeOption.scrollIntoView({ block: "nearest" });
    }

    function renderMemberResults(members) {
        if (!memberResults) {
            return;
        }

        memberResults.replaceChildren();
        memberOptions = [];
        activeMemberIndex = -1;

        if (members.length === 0) {
            var empty = createElement("div", "px-3 py-3");
            empty.appendChild(createElement("p", "text-sm font-semibold text-ink-900", "No matching member found"));
            empty.appendChild(createElement("p", "mt-1 text-xs text-ink-500", "Check the Member ID or create a new member."));

            var addLink = createElement("a", "mt-3 inline-flex min-h-8 items-center rounded-md bg-brand-600 px-3 text-xs font-semibold text-white hover:bg-brand-700", "Add new member");
            addLink.href = "/Members/Create?returnUrl=%2FBorrow%2FCreate";
            empty.appendChild(addLink);
            memberResults.appendChild(empty);
            setMemberResultsOpen(true);
            return;
        }

        members.forEach(function (member, index) {
            var option = createElement("button", "borrow-member-result flex w-full items-start gap-3 rounded-lg px-3 py-2.5 text-left hover:bg-blue-50 focus-visible:outline-none");
            option.type = "button";
            option.id = "member-option-" + member.id;
            option.setAttribute("role", "option");
            option.setAttribute("aria-selected", memberIdInput && Number(memberIdInput.value) === member.id ? "true" : "false");
            option._memberData = member;

            option.appendChild(createElement("span", "flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-blue-50 text-xs font-bold text-brand-600", initials(member.name)));
            var details = createElement("span", "min-w-0 flex-1");

            var heading = createElement("span", "flex min-w-0 flex-wrap items-center gap-x-2 gap-y-1");
            heading.appendChild(createElement("span", "truncate text-sm font-semibold text-ink-900", member.name));
            heading.appendChild(createElement("span", "rounded bg-blue-50 px-1.5 py-0.5 text-[11px] font-bold text-brand-600", member.memberCode));
            details.appendChild(heading);

            var description = member.membershipType;
            if (member.department) {
                description += " · " + member.department;
            }
            if (member.email || member.phone) {
                description += " · " + (member.email || member.phone);
            }
            details.appendChild(createElement("span", "mt-1 block truncate text-xs text-ink-500", description));

            var loanText = member.activeLoans + " active loan" + (member.activeLoans === 1 ? "" : "s");
            if (member.hasOverdueLoans) {
                loanText += " · overdue items";
            }
            details.appendChild(createElement("span", "mt-1 block text-[11px] font-medium " + (member.hasOverdueLoans ? "text-red-600" : "text-ink-400"), loanText));
            option.appendChild(details);

            option.addEventListener("click", function () { selectMember(member); });
            memberResults.appendChild(option);
            memberOptions.push(option);

            if (index === 0) {
                option.dataset.focused = "false";
            }
        });

        setMemberResultsOpen(true);
    }

    async function searchMembers(searchTerm) {
        if (!memberResults || !memberHint) {
            return;
        }

        if (memberRequest) {
            memberRequest.abort();
        }
        memberRequest = new AbortController();
        memberHint.textContent = "Searching members...";

        try {
            var response = await fetch(form.dataset.memberSearchUrl + "&query=" + encodeURIComponent(searchTerm), {
                headers: { "Accept": "application/json" },
                signal: memberRequest.signal
            });
            if (!response.ok) {
                throw new Error("Member search failed.");
            }
            var members = await response.json();
            renderMemberResults(members);
            memberHint.textContent = members.length === 0
                ? "No matches found. You can add a new member without losing this workflow."
                : members.length + " matching member" + (members.length === 1 ? "" : "s") + ". Verify the Member ID before selecting.";
        } catch (error) {
            if (error.name !== "AbortError") {
                memberHint.textContent = "Member search is temporarily unavailable. Please try again.";
                setMemberResultsOpen(false);
            }
        }
    }

    function readBookCard(card) {
        return {
            id: Number(card.dataset.bookId),
            title: card.dataset.bookTitle || "Untitled",
            author: card.dataset.bookAuthor || "Unknown author",
            isbn: card.dataset.bookIsbn || "—",
            publishedYear: Number(card.dataset.bookYear),
            category: card.dataset.bookCategory || "Uncategorized",
            availableCopies: Number(card.dataset.bookAvailable),
            coverImagePath: card.dataset.bookCover || ""
        };
    }

    function setCheckoutCover(book) {
        if (!checkoutCover) {
            return;
        }

        checkoutCover.replaceChildren();
        if (book.coverImagePath) {
            var image = document.createElement("img");
            image.src = book.coverImagePath;
            image.alt = "";
            image.className = "h-full w-full object-cover";
            checkoutCover.appendChild(image);
        } else {
            checkoutCover.appendChild(createElement("span", "", initials(book.title)));
        }
    }

    function selectBook(book) {
        if (!bookIdInput) {
            return;
        }

        bookIdInput.value = String(book.id);
        bookGrid.querySelectorAll("[data-book-card]").forEach(function (card) {
            var selected = Number(card.dataset.bookId) === book.id;
            card.setAttribute("aria-selected", selected ? "true" : "false");
            var check = card.querySelector("[data-book-check]");
            if (check) {
                check.classList.toggle("hidden", !selected);
                check.classList.toggle("flex", selected);
            }
        });

        if (checkoutBook) {
            checkoutBook.textContent = book.title;
        }
        if (checkoutBookMeta) {
            checkoutBookMeta.textContent = book.author + " · " + book.isbn;
        }
        if (quantityInput) {
            quantityInput.max = String(book.availableCopies);
            if (Number(quantityInput.value) > book.availableCopies) {
                quantityInput.value = String(book.availableCopies);
            }
        }
        setCheckoutCover(book);
        updateSubmitState();
    }

    function createBookCard(book) {
        var card = createElement("button", "borrow-book-card relative flex min-w-0 gap-3 rounded-xl border border-line-200 bg-white p-3 text-left hover:border-brand-600/45 hover:bg-blue-50/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600/25");
        card.type = "button";
        card.dataset.bookCard = "";
        card.dataset.bookId = String(book.id);
        card.dataset.bookTitle = book.title;
        card.dataset.bookAuthor = book.author;
        card.dataset.bookIsbn = book.isbn;
        card.dataset.bookYear = String(book.publishedYear);
        card.dataset.bookCategory = book.category;
        card.dataset.bookAvailable = String(book.availableCopies);
        card.dataset.bookCover = book.coverImagePath || "";
        card.setAttribute("role", "option");
        card.setAttribute("aria-selected", bookIdInput && Number(bookIdInput.value) === book.id ? "true" : "false");

        if (book.coverImagePath) {
            var image = document.createElement("img");
            image.src = book.coverImagePath;
            image.alt = "";
            image.loading = "lazy";
            image.className = "h-20 w-14 shrink-0 rounded-md object-cover shadow-sm";
            card.appendChild(image);
        } else {
            card.appendChild(createElement("span", "flex h-20 w-14 shrink-0 items-center justify-center rounded-md bg-blue-50 text-xs font-bold text-brand-600", initials(book.title)));
        }

        var content = createElement("span", "min-w-0 flex-1");
        content.appendChild(createElement("span", "line-clamp-2 block text-sm font-semibold leading-5 text-ink-900", book.title));
        content.appendChild(createElement("span", "mt-1 block truncate text-xs text-ink-500", book.author));
        var metadata = createElement("span", "mt-2 flex flex-wrap items-center gap-1.5 text-[11px]");
        metadata.appendChild(createElement("span", "rounded bg-green-50 px-1.5 py-0.5 font-semibold text-green-700", book.availableCopies + " available"));
        metadata.appendChild(createElement("span", "text-ink-500", String(book.publishedYear)));
        content.appendChild(metadata);
        card.appendChild(content);

        var isSelected = bookIdInput && Number(bookIdInput.value) === book.id;
        var check = createElement("span", (isSelected ? "flex" : "hidden") + " absolute right-2 top-2 h-5 w-5 items-center justify-center rounded-full bg-brand-600 text-white");
        check.dataset.bookCheck = "";
        check.innerHTML = '<svg class="h-3 w-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m5 12 4 4L19 6"></path></svg>';
        card.appendChild(check);
        return card;
    }

    function renderBooks(books, searchTerm) {
        if (!bookGrid || !bookStatus || !noBooks) {
            return;
        }

        bookGrid.replaceChildren();
        books.forEach(function (book) {
            bookGrid.appendChild(createBookCard(book));
        });

        bookGrid.classList.toggle("hidden", books.length === 0);
        noBooks.classList.toggle("hidden", books.length !== 0);
        noBooks.classList.toggle("flex", books.length === 0);
        bookStatus.textContent = books.length === 0
            ? "No available books match “" + searchTerm + "”."
            : books.length + " available title" + (books.length === 1 ? "" : "s") + (searchTerm ? " matching “" + searchTerm + "”." : " shown.");
    }

    async function searchBooks(searchTerm) {
        if (!bookStatus) {
            return;
        }

        if (bookRequest) {
            bookRequest.abort();
        }
        bookRequest = new AbortController();
        bookStatus.textContent = "Searching available books...";

        try {
            var response = await fetch(form.dataset.bookSearchUrl + "&query=" + encodeURIComponent(searchTerm), {
                headers: { "Accept": "application/json" },
                signal: bookRequest.signal
            });
            if (!response.ok) {
                throw new Error("Book search failed.");
            }
            renderBooks(await response.json(), searchTerm);
        } catch (error) {
            if (error.name !== "AbortError") {
                bookStatus.textContent = "Book search is temporarily unavailable. Please try again.";
            }
        }
    }

    if (memberInput) {
        memberInput.addEventListener("input", function () {
            var term = memberInput.value.trim();
            if (memberIdInput && Number(memberIdInput.value) > 0 && memberInput.value !== selectedMemberLabel) {
                clearMemberSelection();
            }

            window.clearTimeout(memberTimer);
            if (term.length < 2) {
                setMemberResultsOpen(false);
                if (memberHint) {
                    memberHint.textContent = "Enter at least 2 characters to search. Results always include the unique Member ID.";
                }
                return;
            }
            memberTimer = window.setTimeout(function () { searchMembers(term); }, 250);
        });

        memberInput.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                setMemberResultsOpen(false);
                return;
            }
            if (event.key === "ArrowDown") {
                event.preventDefault();
                setActiveMemberOption(activeMemberIndex + 1);
            } else if (event.key === "ArrowUp") {
                event.preventDefault();
                setActiveMemberOption(activeMemberIndex - 1);
            } else if (event.key === "Enter" && activeMemberIndex >= 0) {
                event.preventDefault();
                selectMember(memberOptions[activeMemberIndex]._memberData);
            }
        });
    }

    if (changeMemberButton && memberInput) {
        changeMemberButton.addEventListener("click", function () {
            memberInput.focus();
            memberInput.select();
        });
    }

    if (bookInput) {
        bookInput.addEventListener("input", function () {
            var term = bookInput.value.trim();
            window.clearTimeout(bookTimer);
            bookTimer = window.setTimeout(function () { searchBooks(term); }, 250);
        });
    }

    if (bookGrid) {
        bookGrid.addEventListener("click", function (event) {
            var card = event.target.closest("[data-book-card]");
            if (card) {
                selectBook(readBookCard(card));
            }
        });
    }

    document.addEventListener("click", function (event) {
        if (memberInput && memberResults && !memberInput.contains(event.target) && !memberResults.contains(event.target)) {
            setMemberResultsOpen(false);
        }
    });

    if (borrowDate && dueDate) {
        dueDate.min = borrowDate.value;
        borrowDate.addEventListener("change", function () {
            dueDate.min = borrowDate.value;
            if (dueDate.value && dueDate.value < borrowDate.value) {
                dueDate.value = borrowDate.value;
            }
        });
    }

    updateSubmitState();
})();
