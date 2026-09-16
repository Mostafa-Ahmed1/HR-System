// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(() => {
    const sidebar = document.getElementById("hr-sidebar");
    const toggle = document.querySelector("[data-hr-sidebar-toggle]");
    const closeButtons = document.querySelectorAll("[data-hr-sidebar-close]");

    if (!sidebar || !toggle) {
        return;
    }

    const setOpen = (open) => {
        sidebar.classList.toggle("is-open", open);
        document.body.classList.toggle("hr-nav-open", open);
        toggle.setAttribute("aria-expanded", open ? "true" : "false");
    };

    toggle.addEventListener("click", () => setOpen(!sidebar.classList.contains("is-open")));
    closeButtons.forEach((button) => button.addEventListener("click", () => setOpen(false)));
    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            setOpen(false);
        }
    });
    window.addEventListener("resize", () => {
        if (window.innerWidth > 1199) {
            setOpen(false);
        }
    });
})();

