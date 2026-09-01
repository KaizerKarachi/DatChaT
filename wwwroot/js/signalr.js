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

const AVATAR_COLORS = ["av-green", "av-purple", "av-orange", "av-blue", "av-pink", "av-teal"];

function avatarClass(name) {
    const str = displayName(name || "");
    let hash = 0;
    for (let i = 0; i < str.length; i++) hash = str.charCodeAt(i) + ((hash << 5) - hash);
    return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length];
}

function formatMessageTime(value) {
    if (!value) return "";
    if (typeof value === "string" && /^\d{2}:\d{2}$/.test(value)) return value;
    const date = new Date(value);
    if (isNaN(date.getTime())) return String(value);
    return date.toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit" });
}

function privateChatId(nickname) {
    const nick = String(nickname || "");
    const withHash = nick.startsWith("#") ? nick : "#" + nick;
    return "u:" + withHash;
}

function activeChatId() {
    const active = window.DatChat.activeChat;
    if (!active) return null;
    if (active === "family") return "family";
    return privateChatId(active);
}

window.avatarClass = avatarClass;
window.formatMessageTime = formatMessageTime;

window.DatChat = window.DatChat || {
    currentUser: null,
    isAdmin: false,
    activeChat: null,
    unread: {},
    previews: {},
    quote: null
};

let connection = null;

function bindHubEvents(conn) {
    conn.on("updateChats", chats => applyTdChats?.(chats));
    conn.on("updateUsers", users => renderUsers?.(users));
    conn.on("updateChatHistory", payload => {
        const chatId = pick(payload, "chatId");
        const messages = pick(payload, "messages") || [];
        const last = messages.length ? messages[messages.length - 1] : null;
        if (last) setConvPreview?.(chatId === "family" ? "family" : String(chatId || "").replace(/^u:/, ""), last);
        const active = window.DatChat.activeChat;
        const matches = chatId === "family" ? active === "family"
            : active && sameUser(String(chatId).replace(/^u:/, ""), active);
        if (!matches) return;
        clearMessages();
        messages.forEach(msg => {
            if (chatId === "family") renderMessage(msg);
            else renderPrivateMessage({
                id: pick(msg, "id"),
                sender: pick(msg, "senderId", "nickname"),
                receiver: pick(msg, "receiverId"),
                text: pick(msg, "text"),
                fileUrl: pick(msg, "fileUrl"),
                fileType: pick(msg, "fileType"),
                time: pick(msg, "time"),
                timestamp: pick(msg, "timestamp")
            });
        });
        if (!messages.length)
            showEmptyChat(chatId === "family" ? "Напишите первое сообщение семье" : "Личная переписка ещё пустая");
        updateScrollJump?.();
    });
    conn.on("updateNewMessage", payload => {
        const chatId = pick(payload, "chatId");
        const message = pick(payload, "message");
        if (!message) return;
        if (chatId === "family") {
            setConvPreview?.("family", message);
            if (window.DatChat.activeChat === "family") {
                hideEmptyChat();
                renderMessage(message);
            } else {
                window.DatChat.unread.family = (window.DatChat.unread.family || 0) + 1;
                setFamilyUnread?.(window.DatChat.unread.family);
                refreshDocTitle?.();
            }
            return;
        }
        const sender = pick(message, "senderId", "nickname");
        const other = chatId && String(chatId).startsWith("u:") ? String(chatId).slice(2) : sender;
        setConvPreview?.(other, message);
        if (window.DatChat.activeChat && sameUser(window.DatChat.activeChat, other)) {
            hideEmptyChat();
            renderPrivateMessage({
                id: pick(message, "id"),
                sender,
                receiver: pick(message, "receiverId"),
                text: pick(message, "text"),
                fileUrl: pick(message, "fileUrl"),
                fileType: pick(message, "fileType"),
                time: pick(message, "time"),
                timestamp: pick(message, "timestamp")
            });
            return;
        }
        if (!sameUser(sender, window.DatChat.currentUser)) {
            const key = displayName(sender);
            window.DatChat.unread[key] = (window.DatChat.unread[key] || 0) + 1;
            markUnread?.(key);
            refreshDocTitle?.();
            showToast?.("Сообщение от " + displayName(sender));
        }
    });
    conn.on("updateChatLastMessage", payload => {
        const chatId = pick(payload, "chatId");
        const last = pick(payload, "lastMessage");
        if (chatId === "family") setConvPreview?.("family", last);
        else if (chatId) setConvPreview?.(String(chatId).replace(/^u:/, ""), last);
    });
    conn.on("updateChatReadInbox", payload => {
        const chatId = pick(payload, "chatId");
        if (chatId === "family") {
            window.DatChat.unread.family = 0;
            setFamilyUnread?.(0);
        } else if (chatId) {
            window.DatChat.unread[displayName(String(chatId).replace(/^u:/, ""))] = 0;
        }
        refreshDocTitle?.();
    });
    conn.on("updateChatAction", payload => {
        const chatId = pick(payload, "chatId");
        const action = pick(payload, "action");
        const userId = pick(payload, "userId");
        const active = window.DatChat.activeChat;
        const inThis = chatId === "family" ? active === "family"
            : active && (chatId === "u:" + active || sameUser(userId, active));
        const st = document.getElementById("chatStatus");
        if (!st) return;
        if (inThis && action === "typing")
            st.textContent = displayName(userId) + " печатает…";
        else if (inThis)
            st.textContent = active === "family" ? "общий чат" : st.dataset.base || st.textContent;
    });
    conn.on("updateDeleteMessages", payload => {
        (pick(payload, "messageIds") || []).forEach(id => removeMessage?.(id));
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

    connection.onreconnecting(() => {
        document.getElementById("connBar")?.classList.remove("hidden");
        setNavConn?.("reconnect");
        showToast?.("Переподключение...");
    });
    connection.onreconnected(async () => {
        document.getElementById("connBar")?.classList.add("hidden");
        setNavConn?.("ok");
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
        setNavConn?.("ok");
        await loginByToken?.();
    } catch (e) {
        console.error("SignalR: ошибка подключения", e);
        setNavConn?.("off");
        setTimeout(startConnection, 3000);
    }
}

window.startConnection = startConnection;
window.refreshDocTitle = function refreshDocTitle() {
    const n = Object.values(window.DatChat.unread || {}).reduce((sum, v) => sum + (Number(v) || 0), 0);
    document.title = n > 0 ? "(" + n + ") DatChaT" : "DatChaT";
};
window.isHubConnected = isHubConnected;
window.activeChatId = activeChatId;
window.privateChatId = privateChatId;
window.pick = pick;
window.displayName = displayName;
window.sameUser = sameUser;
