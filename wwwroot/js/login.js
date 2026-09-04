// Password visibility toggle for the login page.
(function () {
    "use strict";

    var toggle = document.getElementById("togglePassword");
    var input = document.getElementById("Password");

    if (!toggle || !input) {
        return;
    }

    var eye = toggle.querySelector('[data-icon="eye"]');
    var eyeOff = toggle.querySelector('[data-icon="eye-off"]');

    toggle.addEventListener("click", function () {
        var show = input.type === "password";

        input.type = show ? "text" : "password";
        toggle.setAttribute("aria-pressed", show ? "true" : "false");
        toggle.setAttribute("aria-label", show ? "Hide password" : "Show password");

        if (eye && eyeOff) {
            eye.classList.toggle("hidden", show);
            eyeOff.classList.toggle("hidden", !show);
        }

        input.focus({ preventScroll: true });
    });
})();
