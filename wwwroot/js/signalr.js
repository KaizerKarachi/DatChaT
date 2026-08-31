function pick(obj, ...keys) {
    if (obj == null) return undefined;
    for (const key of keys) {
        if (obj[key] !== undefined && obj[key] !== null) return obj[key];
        const found = Object.keys(obj).find(k => k.toLowerCase() === String(key).toLowerCase());
        if (found && obj[found] !== undefined) return obj[found];
    }
    return undefined;
}

function displayName(nickname) {
    if (!nickname) return "";
    return String(nickname).startsWith("#") ? String(nickname).slice(1) : String(nickname);
}

function sameUser(a, b) {
    return displayName(a).toLowerCase() === displayName(b).toLowerCase();
}

window.DatChat = window.DatChat || {
    currentUser: null,
    isAdmin: false,
    activeChat: "family",
    unread: {}
};

let connection = null;

function createConnection() {
    if (typeof signalR === "undefined") {
        console.error("Библиотека SignalR не загружена");
        return false;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chat")
        .withAutomaticReconnect()
        .build();

    window.connection = connection;

    connection.onreconnecting(() => showToast?.("Переподключение..."));
    connection.onreconnected(async () => {
        const nickname = localStorage.getItem("nickname");
        const token = localStorage.getItem("sessionToken");
        if (nickname && token) {
            try { await connection.invoke("JoinByToken", nickname, token); } catch (e) { console.warn(e); }
        }
        showToast?.("Соединение восстановлено");
    });

    connection.on("LoadHistory", messages => {
        if (window.DatChat.activeChat !== "family") return;
        clearMessages();
        (messages || []).forEach(renderMessage);
    });

    connection.on("LoadPrivateHistory", messages => {
        if (window.DatChat.activeChat === "family") return;
        clearMessages();
        (messages || []).forEach(msg => renderPrivateMessage(msg));
    });

    connection.on("ReceiveMessage", message => {
        if (window.DatChat.activeChat === "family") renderMessage(message);
    });

    connection.on("ReceivePrivateMessage", message => {
        const sender = pick(message, "sender");
        const receiver = pick(message, "receiver");
        const other = sameUser(sender, window.DatChat.currentUser) ? receiver : sender;
        if (window.DatChat.activeChat !== "family" && sameUser(window.DatChat.activeChat, other)) {
            renderPrivateMessage(message);
            return;
        }
        if (!sameUser(sender, window.DatChat.currentUser)) {
            const key = displayName(sender);
            window.DatChat.unread[key] = (window.DatChat.unread[key] || 0) + 1;
            if (typeof markUnread === "function") markUnread(key);
            showToast?.("Личное сообщение от " + displayName(sender));
        }
    });

    connection.on("UpdateOnlineUsers", users => renderUsers?.(users));
    connection.on("UpdateUsers", users => renderUsers?.(users));
    connection.on("PinnedMessage", message => renderPinned?.(message));
    connection.on("MessageUnpinned", () => clearPinned?.(true));
    connection.on("MessageDeleted", id => removeMessage?.(id));
    connection.on("SearchResults", results => renderSearchResults?.(results));
    connection.on("SystemMessage", text => showToast?.(text));

    return true;
}

async function startConnection() {
    if (!connection && !createConnection()) return;
    try {
        if (connection.state === signalR.HubConnectionState.Connected) return;
        await connection.start();
        if (typeof loginByToken === "function")
            await loginByToken();
    } catch (e) {
        console.error("SignalR: ошибка подключения", e);
        setTimeout(startConnection, 3000);
    }
}

window.startConnection = startConnection;
window.pick = pick;
window.displayName = displayName;
window.sameUser = sameUser;
