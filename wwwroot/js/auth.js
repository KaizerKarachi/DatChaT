async function login(event) {
    event?.preventDefault();

    const nickname = document.getElementById("nicknameInput")?.value.trim();
    const password = document.getElementById("passwordInput")?.value.trim();
    const errorEl = document.getElementById("loginError");

    if (!nickname || !password) {
        showLoginError("Введите ник и пароль");
        return;
    }

    if (!isHubConnected()) {
        showLoginError("Соединение с сервером ещё не установлено");
        return;
    }

    try {
        const data = await connection.invoke("RegisterOrLogin", nickname, password);
        if (!pick(data, "success")) {
            showLoginError(pick(data, "error") || "Ошибка входа");
            return;
        }
        applySession(data);
        if (window.DatChat.lastUsers) renderUsers();
        showChat();
        if (pick(data, "isNew"))
            showToast?.("Аккаунт создан. Пароль сохранён в виде хеша.");
        document.getElementById("messageInput")?.focus();
    } catch (e) {
        console.error("Ошибка входа:", e);
        showLoginError(e.message || "Ошибка подключения");
    }
}

async function loginByToken() {
    const token = localStorage.getItem("sessionToken");
    const nickname = localStorage.getItem("nickname");
    if (!token || !nickname || !isHubConnected())
        return false;

    try {
        const data = await connection.invoke("JoinByToken", nickname, token);
        if (!pick(data, "success")) {
            clearSession();
            showLogin();
            return false;
        }
        applySession(data);
        if (window.DatChat.lastUsers) renderUsers();
        showChat();
        return true;
    } catch (e) {
        console.warn("Автовход не выполнен:", e);
        clearSession();
        showLogin();
        return false;
    }
}

function applySession(data) {
    window.DatChat.currentUser = pick(data, "nickname");
    window.DatChat.isAdmin = !!pick(data, "isAdmin");
    window.currentUser = window.DatChat.currentUser;
    window.isAdmin = window.DatChat.isAdmin;
    window.sessionToken = pick(data, "sessionToken");

    localStorage.setItem("sessionToken", window.sessionToken);
    localStorage.setItem("nickname", window.DatChat.currentUser);

    const errorEl = document.getElementById("loginError");
    if (errorEl) {
        errorEl.hidden = true;
        errorEl.textContent = "";
    }
}

function showLoginError(text) {
    const errorEl = document.getElementById("loginError");
    if (!errorEl) {
        alert(text);
        return;
    }
    errorEl.hidden = false;
    errorEl.textContent = text;
}

function clearSession() {
    localStorage.removeItem("sessionToken");
    localStorage.removeItem("nickname");
    window.sessionToken = null;
    window.currentUser = null;
    window.isAdmin = false;
    window.DatChat.currentUser = null;
    window.DatChat.isAdmin = false;
    window.DatChat.activeChat = "family";
    window.DatChat.unread = {};
}

async function logout() {
    try {
        if (isHubConnected())
            await window.connection.invoke("Logout");
    } catch (e) {
        console.warn(e);
    }
    clearSession();
    clearMessages?.();
    clearUsers?.();
    clearPinned?.();
    try { await window.connection?.stop(); } catch { /* ignore */ }
    showLogin();
    showToast("Вы вышли из чата");
    startConnection();
}

window.logout = logout;

