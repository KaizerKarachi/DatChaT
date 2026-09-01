window.addEventListener("DOMContentLoaded", () => {
    if ("ontouchstart" in window || navigator.maxTouchPoints > 0)
        document.documentElement.classList.add("is-touch");
    bindUi();
    showLogin();
    startConnection();
});
