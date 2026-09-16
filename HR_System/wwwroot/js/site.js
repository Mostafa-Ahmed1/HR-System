(() => {
  const menu = document.getElementById("layout-menu");
  const toggles = document.querySelectorAll("[data-sneat-menu-toggle]");
  if (!menu || !toggles.length) return;
  const setOpen = (open) => {
    menu.classList.toggle("is-open", open);
    document.querySelectorAll(".layout-overlay").forEach((overlay) => overlay.classList.toggle("is-open", open));
    document.body.classList.toggle("menu-open", open);
    toggles.forEach((toggle) => toggle.setAttribute("aria-expanded", open ? "true" : "false"));
  };
  toggles.forEach((toggle) => toggle.addEventListener("click", () => setOpen(!menu.classList.contains("is-open"))));
  document.addEventListener("keydown", (event) => { if (event.key === "Escape") setOpen(false); });
  window.addEventListener("resize", () => { if (window.innerWidth >= 1200) setOpen(false); });
})();

