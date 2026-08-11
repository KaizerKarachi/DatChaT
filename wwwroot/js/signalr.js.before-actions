console.log("signalr.js: загрузка");

let connection = null;

function createConnection() {
    if (typeof signalR === "undefined") {
        console.error("SignalR library НЕ загружена");
        return false;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chat")
        .withAutomaticReconnect()
        .build();

    window.connection = connection;

    connection.onreconnecting(() => {
        console.log("SignalR: переподключение...");
    });

    connection.onreconnected(() => {
        console.log("SignalR: соединение восстановлено");
    });

    connection.onclose(() => {
        console.log("SignalR: соединение закрыто");
    });

    connection.on("LoadHistory", messages => {
        if (typeof clearMessages === "function")
            clearMessages();

        if (messages && typeof renderMessage === "function")
            messages.forEach(renderMessage);
    });

    connection.on("ReceiveMessage", message => {
        if (typeof renderMessage === "function")
            renderMessage(message);
    });

    connection.on("UpdateUsers", users => {
        if (typeof renderUsers === "function")
            renderUsers(users);
    });

    connection.on("UpdateOnlineUsers", users => {
        if (typeof renderUsers === "function")
            renderUsers(users);
    });

    connection.on("PinnedMessage", message => {
        if (typeof renderPinned === "function")
            renderPinned(message);
    });

    connection.on("MessageUnpinned", () => {
        if (typeof clearPinned === "function")
            clearPinned();
    });

    connection.on("MessageDeleted", message => {
        if (typeof removeMessage === "function") {
            const id = message?.Id ?? message?.id ?? message;
            removeMessage(id);
        }
    });

    connection.on("LoginSuccess", data => {
        console.log("SignalR: вход успешен", data);

        if (data) {
            window.currentUser = data.Nickname || window.currentUser;
        window.isAdmin = !!data.IsAdmin;
            window.sessionToken = data.SessionToken || window.sessionToken;

            if (window.sessionToken)
                localStorage.setItem("sessionToken", window.sessionToken);

            if (typeof showChat === "function")
                showChat();

            const currentUser = document.getElementById("currentUser");
            if (currentUser)
                currentUser.textContent = window.currentUser || "";
        }
    });

    connection.on("ForceLogout", message => {
        console.warn("ForceLogout:", message);

        localStorage.removeItem("sessionToken");
        window.sessionToken = null;
        window.currentUser = null;

        if (typeof showLogin === "function")
            showLogin();

        alert(message || "Вы вошли с другого устройства");
    });

    return true;
}

async function startConnection() {
    if (!connection) {
        if (!createConnection()) {
            console.error("SignalR: не удалось создать соединение");
            return;
        }
    }

    try {
        if (connection.state === signalR.HubConnectionState.Connected) {
            console.log("SignalR: уже подключён");
            return;
        }

        console.log("SignalR: подключение к /chat...");
        await connection.start();
        console.log("SignalR: ПОДКЛЮЧЁН");

    } catch (e) {
        console.error("SignalR: ошибка подключения", e);

        setTimeout(startConnection, 3000);
    }
}

window.startConnection = startConnection;

createConnection();

window.addEventListener("DOMContentLoaded", () => {
    startConnection();
});
