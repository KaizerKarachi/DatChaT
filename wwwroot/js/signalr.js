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

function isHubConnected() {
    return window.connection
        && window.connection.state === signalR.HubConnectionState.Connected;
}

window.DatChat = window.DatChat || {
    currentUser: null,
    isAdmin: false,
    activeChat: "family",
    unread: {}
};

let connection = null;

function bindHubEvents(conn) {
    conn.on("LoadHistory", messages => {
        if (window.DatChat.activeChat !== "family") return;
        clearMessages();
        (messages || []).forEach(renderMessage);
        if (!messages?.length) showEmptyChat("Напишите первое сообщение семье");
    });

    conn.on("LoadPrivateHistory", messages => {
        if (window.DatChat.activeChat === "family") return;
        clearMessages();
        (messages || []).forEach(msg => renderPrivateMessage(msg));
        if (!messages?.length) showEmptyChat("Личная переписка ещё пустая");
    });

    conn.on("ReceiveMessage", message => {
        if (window.DatChat.activeChat === "family") {
            hideEmptyChat();
            renderMessage(message);
        }
    });

    conn.on("ReceivePrivateMessage", message => {
        const sender = pick(message, "sender");
        const receiver = pick(message, "receiver");
        const other = sameUser(sender, window.DatChat.currentUser) ? receiver : sender;
        if (window.DatChat.activeChat !== "family" && sameUser(window.DatChat.activeChat, other)) {
            hideEmptyChat();
            renderPrivateMessage(message);
            return;
        }
        if (!sameUser(sender, window.DatChat.currentUser)) {
            const key = displayName(sender);
            window.DatChat.unread[key] = (window.DatChat.unread[key] || 0) + 1;
            markUnread?.(key);
            showToast?.("Сообщение от " + displayName(sender));
        }
    });

    conn.on("UpdateOnlineUsers", users => renderUsers?.(users));
    conn.on("PinnedMessage", message => renderPinned?.(message));
    conn.on("MessageUnpinned", () => clearPinned?.(true));
    conn.on("MessageDeleted", id => removeMessage?.(id));
    conn.on("SearchResults", results => renderSearchResults?.(results));
    conn.on("SystemMessage", text => showToast?.(text));
}

function createConnection() {
    if (typeof signalR === "undefined") {
        console.error("Библиотека SignalR не загружена");
        return false;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chat")
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

    window.connection = connection;
    bindHubEvents(connection);

    connection.onreconnecting(() => showToast?.("Переподключение..."));
    connection.onreconnected(async () => {
        const nickname = localStorage.getItem("nickname");
        const token = localStorage.getItem("sessionToken");
        if (nickname && token) {
            try { await connection.invoke("JoinByToken", nickname, token); }
            catch (e) { console.warn(e); }
        }
        showToast?.("Снова в чате");
    });

    return true;
}

async function startConnection() {
    if (!connection && !createConnection()) return;
    try {
        if (connection.state === signalR.HubConnectionState.Connected) return;
        if (connection.state === signalR.HubConnectionState.Connecting) return;
        await connection.start();
        await loginByToken?.();
    } catch (e) {
        console.error("SignalR: ошибка подключения", e);
        setTimeout(startConnection, 3000);
    }
}

window.startConnection = startConnection;
window.isHubConnected = isHubConnected;
window.pick = pick;
window.displayName = displayName;
window.sameUser = sameUser;
