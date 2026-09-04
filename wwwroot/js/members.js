(function () {
    "use strict";

    // Delete confirmation handler
    document.addEventListener("submit", function (event) {
        var form = event.target.closest("[data-delete-member-form]");
        if (!form) {
            return;
        }

        var memberName = form.dataset.memberName || "this member";
        var studentId = form.dataset.studentId || "";
        var message = studentId
            ? 'Are you sure you want to delete member "' + memberName + '" (' + studentId + ')?'
            : 'Are you sure you want to delete member "' + memberName + '"?';

        if (!window.confirm(message)) {
            event.preventDefault();
        }
    });

    // View Member Details Modal
    var modal = document.getElementById("member-details-modal");
    var modalContent = document.getElementById("member-details-card");
    var closeBtn = document.getElementById("close-modal-btn");
    var dismissBtn = document.getElementById("modal-dismiss-btn");

    if (!modal) {
        return;
    }

    function openModal(btn) {
        var name = btn.dataset.memberName || "";
        var studentId = btn.dataset.studentId || "";
        var membershipType = btn.dataset.membershipType || "Student";
        var email = btn.dataset.email || "—";
        var phone = btn.dataset.phone || "—";
        var department = btn.dataset.department || "—";
        var joinDate = btn.dataset.joinDate || "—";
        var status = btn.dataset.status || "Active";
        var borrows = btn.dataset.borrows || "0";
        var activeBorrows = btn.dataset.activeBorrows || "0";
        var memberId = btn.dataset.memberId || "";

        document.getElementById("modal-member-name").textContent = name;
        document.getElementById("modal-student-id").textContent = "ID: " + studentId;
        document.getElementById("modal-avatar").textContent = name.trim().charAt(0).toUpperCase() || "M";
        document.getElementById("modal-email").textContent = email;
        document.getElementById("modal-phone").textContent = phone;
        document.getElementById("modal-department").textContent = department;
        document.getElementById("modal-join-date").textContent = joinDate;
        document.getElementById("modal-borrows").textContent = borrows;
        document.getElementById("modal-active-borrows").textContent = activeBorrows;

        var typeEl = document.getElementById("modal-membership-type");
        typeEl.textContent = membershipType;
        typeEl.className = "mt-1 inline-flex rounded-md px-2.5 py-0.5 text-xs font-semibold ";
        if (membershipType === "Teacher") {
            typeEl.className += "border border-purple-200/60 bg-purple-50 text-purple-700";
        } else if (membershipType === "Staff") {
            typeEl.className += "border border-teal-200/60 bg-teal-50 text-teal-700";
        } else {
            typeEl.className += "border border-blue-200/60 bg-blue-50 text-brand-600";
        }

        var statusEl = document.getElementById("modal-status");
        statusEl.textContent = status;
        statusEl.className = "mt-1 inline-flex rounded-md px-2.5 py-0.5 text-xs font-semibold ";
        if (status === "Active") {
            statusEl.className += "border border-emerald-200/60 bg-emerald-50 text-emerald-700";
        } else {
            statusEl.className += "border border-rose-200/60 bg-rose-50 text-rose-700";
        }

        var editLink = document.getElementById("modal-edit-link");
        if (editLink && memberId) {
            editLink.href = "/Members/Edit/" + encodeURIComponent(memberId);
        }

        modal.classList.remove("opacity-0", "pointer-events-none");
        modal.classList.add("opacity-100");
        if (modalContent) {
            modalContent.classList.remove("scale-95");
            modalContent.classList.add("scale-100");
        }
        document.body.classList.add("overflow-hidden");
    }

    function closeModal() {
        modal.classList.add("opacity-0", "pointer-events-none");
        modal.classList.remove("opacity-100");
        if (modalContent) {
            modalContent.classList.add("scale-95");
            modalContent.classList.remove("scale-100");
        }
        document.body.classList.remove("overflow-hidden");
    }

    document.addEventListener("click", function (event) {
        var viewBtn = event.target.closest("[data-view-member]");
        if (viewBtn) {
            event.preventDefault();
            openModal(viewBtn);
            return;
        }

        if (event.target === modal) {
            closeModal();
        }
    });

    if (closeBtn) {
        closeBtn.addEventListener("click", closeModal);
    }
    if (dismissBtn) {
        dismissBtn.addEventListener("click", closeModal);
    }

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && !modal.classList.contains("opacity-0")) {
            closeModal();
        }
    });
})();
