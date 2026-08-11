// ===============================
// DatChaT v2
// ui.js
// ===============================

const chatScreen =
    document.getElementById("app");

const currentUserLabel = null;

const onlineCountLabel =
    document.getElementById("userCount");

// ---------------------
// Показать окно входа
// ---------------------

function showLogin() {

    if (loginScreen)
        loginScreen.classList.remove("hidden");

    if (chatScreen)
        chatScreen.classList.add("hidden");

}

// ---------------------
// Показать чат
// ---------------------

function showChat() {

    if (loginScreen)
        loginScreen.classList.add("hidden");

    if (chatScreen)
        chatScreen.classList.remove("hidden");

}

// ---------------------
// Имя пользователя
// ---------------------

function setCurrentUser(name) {

    window.currentUser = name;

    if (currentUserLabel)
        currentUserLabel.textContent = name;

}

// ---------------------
// Онлайн
// ---------------------

function setOnlineCount(count) {

    if (onlineCountLabel)
        onlineCountLabel.textContent = count;

}

// ---------------------
// Toast
// ---------------------

function showToast(text) {

    let toast =
        document.getElementById("toast");

    if (!toast) {

        toast =
            document.createElement("div");

        toast.id = "toast";

        toast.style.position = "fixed";
        toast.style.right = "25px";
        toast.style.bottom = "25px";
        toast.style.background = "#355E3B";
        toast.style.color = "#fff";
        toast.style.padding = "12px 18px";
        toast.style.borderRadius = "12px";
        toast.style.boxShadow =
            "0 10px 25px rgba(0,0,0,.25)";
        toast.style.zIndex = "9999";

        document.body.appendChild(toast);

    }

    toast.textContent = text;

    toast.style.opacity = "1";

    clearTimeout(window.toastTimer);

    window.toastTimer =
        setTimeout(() => {

            toast.style.opacity = "0";

        }, 2500);

}

// ---------------------
// Выход
// ---------------------

function logout() {

    localStorage.removeItem("sessionToken");

    window.sessionToken = null;

    window.currentUser = null;

    clearMessages();

    clearUsers();

    clearPinned();

    showLogin();

}

// ---------------------
// Кнопка выхода
// ---------------------

document
.getElementById("logoutBtn")
?.addEventListener(
    "click",
    logout
);

// ---------------------
// Начальный экран
// ---------------------

if (window.sessionToken)
    showChat();
else
    showLogin();


/* ==================================================
   DatChaT — Theme
   ================================================== */

function toggleTheme() {
    document.body.classList.toggle("dark-theme");

    const dark =
        document.body.classList.contains("dark-theme");

    localStorage.setItem(
        "theme",
        dark ? "dark" : "light"
    );

    const btn = document.getElementById("themeBtn");

    if (btn) {
        btn.textContent = dark ? "☀️" : "🌙";
    }
}
