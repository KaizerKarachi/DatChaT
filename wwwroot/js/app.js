window.addEventListener("DOMContentLoaded", () => {

    const loginBtn = document.getElementById("loginBtn");
    const logoutBtn = document.getElementById("logoutBtn");
    const sendBtn = document.getElementById("sendBtn");
    const messageInput = document.getElementById("messageInput");
    const themeBtn = document.getElementById("themeBtn");

    loginBtn?.addEventListener("click", login);
    logoutBtn?.addEventListener("click", logout);
    sendBtn?.addEventListener("click", sendMessage);

    messageInput?.addEventListener("keydown", e => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    themeBtn?.addEventListener("click", toggleTheme);

    const savedTheme = localStorage.getItem("theme");

    if (savedTheme === "dark") {
        document.body.classList.add("dark-theme");
        if (themeBtn) themeBtn.textContent = "☀️";
    }

    startConnection();
});
