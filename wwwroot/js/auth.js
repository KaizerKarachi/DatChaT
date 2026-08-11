window.currentUser = null;
window.sessionToken = localStorage.getItem("sessionToken") || null;

function waitForLoginSuccess(timeout = 5000) {
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => {
            connection.off("LoginSuccess", handler);
            reject(new Error("Сервер не подтвердил вход"));
        }, timeout);

        function handler(data) {
            clearTimeout(timer);
            connection.off("LoginSuccess", handler);
            resolve(data);
        }

        connection.on("LoginSuccess", handler);
    });
}

async function login() {
    const nicknameInput = document.getElementById("nicknameInput");
    const passwordInput = document.getElementById("passwordInput");

    const nickname = nicknameInput.value.trim();
    const password = passwordInput.value.trim();

    if (!nickname || !password) {
        alert("Введите ник и пароль");
        return;
    }

    if (!connection || connection.state !== "Connected") {
        alert("Соединение с сервером ещё не установлено");
        return;
    }

    try {
        const loginPromise = waitForLoginSuccess();

        await connection.invoke(
            "RegisterOrLogin",
            nickname,
            password
        );

        const data = await loginPromise;

        window.currentUser = data.Nickname;
        window.sessionToken = data.SessionToken;

        localStorage.setItem("sessionToken", data.SessionToken);
        localStorage.setItem("nickname", data.Nickname);

        window.isAdmin = !!data.IsAdmin;

        showChat();

        const currentUser = document.getElementById("currentUser");
        if (currentUser) {
            currentUser.textContent = data.Nickname;
        }

        messageInput?.focus();

    } catch (e) {
        console.error("Ошибка входа:", e);
        alert(e.message || "Ошибка подключения");
    }
}

async function loginByToken() {
    const token = localStorage.getItem("sessionToken");
    const nickname = localStorage.getItem("nickname");

    if (!token || !nickname)
        return false;

    if (!connection || connection.state !== "Connected")
        return false;

    try {
        const loginPromise = waitForLoginSuccess();

        await connection.invoke(
            "JoinByToken",
            nickname,
            token
        );

        const data = await loginPromise;

        window.currentUser = data.Nickname;
        window.sessionToken = data.SessionToken || token;
        window.isAdmin = !!data.IsAdmin;

        localStorage.setItem("sessionToken", window.sessionToken);
        localStorage.setItem("nickname", data.Nickname);

        showChat();

        const currentUser = document.getElementById("currentUser");
        if (currentUser) {
            currentUser.textContent = data.Nickname;
        }

        return true;

    } catch (e) {
        console.warn("Автовход не выполнен:", e);

        localStorage.removeItem("sessionToken");
        localStorage.removeItem("nickname");

        window.sessionToken = null;
        window.currentUser = null;

        return false;
    }
}

function logout() {
    localStorage.removeItem("sessionToken");
    localStorage.removeItem("nickname");

    window.sessionToken = null;
    window.currentUser = null;
    window.isAdmin = false;

    if (typeof showLogin === "function") {
        showLogin();
    }
}

document.getElementById("loginBtn")?.addEventListener("click", login);

document.getElementById("logoutBtn")?.addEventListener("click", logout);

document.getElementById("nicknameInput")?.addEventListener("keydown", e => {
    if (e.key === "Enter") {
        e.preventDefault();
        document.getElementById("passwordInput")?.focus();
    }
});

document.getElementById("passwordInput")?.addEventListener("keydown", e => {
    if (e.key === "Enter") {
        e.preventDefault();
        login();
    }
});
