const usersContainer = document.getElementById("usersList");

function applyTdChats(chats) {
    window.DatChat.chats = chats || [];
    window.DatChat.unread = window.DatChat.unread || {};
    window.DatChat.previews = window.DatChat.previews || {};
    (chats || []).forEach(chat => {
        const id = pick(chat, "id") || "";
        const last = pick(chat, "lastMessage");
        const unread = Number(pick(chat, "unreadCount")) || 0;
        if (id === "family") {
            window.DatChat.unread.family = unread;
            setFamilyUnread(unread);
            if (last) setConvPreview("family", last);
            return;
        }
        const nick = pick(chat, "nickname") || id.replace(/^u:/, "");
        const key = displayName(nick);
        window.DatChat.unread[key] = unread;
        if (last) setConvPreview(nick, last);
    });
    renderUsers();
}

function currentNickname() {
    return window.DatChat.currentUser || localStorage.getItem("nickname");
}

function previewSnippet(msg) {
    if (!msg) return "";
    if (pick(msg, "isDeleted")) return "Сообщение удалено";
    const fileUrl = pick(msg, "fileUrl");
    const text = pick(msg, "text") || "";
    if (fileUrl) return "📎 " + (text || "Файл");
    return text.replace(/\s+/g, " ").trim();
}

function setConvPreview(chatId, msg) {
    window.DatChat.previews = window.DatChat.previews || {};
    const key = chatId === "family" ? "family" : displayName(chatId);
    const text = previewSnippet(msg);
    const time = formatMessageTime?.(pick(msg, "time", "timestamp")) || "";
    window.DatChat.previews[key] = { text, time };
    paintPreview(chatId, text, time);
}

function paintPreview(chatId, text, time) {
    if (chatId === "family") {
        const el = document.getElementById("familyPreview");
        if (el && text) el.textContent = text;
        const tm = document.getElementById("familyTime");
        if (tm) tm.textContent = time || "";
        return;
    }
    usersContainer?.querySelectorAll(".chat-item").forEach(item => {
        if (!sameUser(item.dataset.chat, chatId)) return;
        const sub = item.querySelector(".chat-item-status");
        const tm = item.querySelector(".chat-item-time");
        if (sub && text) {
            sub.textContent = text;
            sub.classList.remove("online");
        }
        if (tm) tm.textContent = time || "";
    });
}

function setFamilyUnread(count) {
    const badge = document.getElementById("familyUnread");
    if (!badge) return;
    if (count > 0) {
        badge.textContent = String(count);
        badge.classList.remove("hidden");
    } else {
        badge.classList.add("hidden");
    }
}

function updateMeChip() {
    const name = displayName(currentNickname());
    const meName = document.getElementById("meName");
    const meAv = document.getElementById("meAvatar");
    if (meName) meName.textContent = name ? "#" + name : "";
    if (meAv && name) {
        meAv.textContent = name.charAt(0).toUpperCase();
        meAv.className = "user-avatar " + avatarClass(name);
    }
}

function setNavConn(state) {
    const el = document.getElementById("navConn");
    if (!el) return;
    el.classList.remove("ok", "warn");
    if (state === "ok") {
        el.textContent = "в сети";
        el.classList.add("ok");
    } else if (state === "reconnect") {
        el.textContent = "переподключение…";
        el.classList.add("warn");
    } else {
        el.textContent = "нет связи";
    }
}

function setNavTab(tab) {
    const app = document.getElementById("app");
    if (app) app.dataset.navTab = tab;
    document.getElementById("tabConversations")?.classList.toggle("active", tab === "conversations");
    document.getElementById("tabContacts")?.classList.toggle("active", tab === "contacts");
    document.getElementById("tabConversations")?.setAttribute("aria-selected", tab === "conversations" ? "true" : "false");
    document.getElementById("tabContacts")?.setAttribute("aria-selected", tab === "contacts" ? "true" : "false");
    renderUsers();
}

function applyConvFilter() {
    const q = (document.getElementById("convFilter")?.value || "").trim().toLowerCase();
    document.querySelectorAll("#navigation .chat-item").forEach(item => {
        const name = (item.dataset.name || item.querySelector(".chat-item-name")?.textContent || "").toLowerCase();
        item.style.display = !q || name.includes(q) ? "" : "none";
    });
}

function renderUsers(users) {
    if (!usersContainer) return;
    if (users) window.DatChat.lastUsers = users;
    const list = Array.isArray(window.DatChat.lastUsers) ? window.DatChat.lastUsers : [];
    const me = currentNickname();
    usersContainer.innerHTML = "";
    updateMeChip();

    const others = list.filter(user => {
        const nickname = typeof user === "string" ? user : (pick(user, "nickname") || "");
        const name = pick(user, "displayName") || displayName(nickname);
        return name && !sameUser(nickname, me);
    });

    if (!others.length) {
        const hint = document.createElement("p");
        hint.className = "users-empty";
        hint.textContent = "Пока никого нет. Когда близкий зарегистрируется своим ником — он появится здесь.";
        usersContainer.appendChild(hint);
        applyConvFilter();
        return;
    }

    others.forEach(user => {
        const nickname = typeof user === "string" ? user : (pick(user, "nickname") || "");
        const name = pick(user, "displayName") || displayName(nickname);
        const online = typeof user === "string" ? false : !!pick(user, "isOnline");
        const unread = window.DatChat.unread[displayName(nickname)] || 0;
        const preview = window.DatChat.previews?.[displayName(nickname)];
        const contacts = document.getElementById("app")?.dataset.navTab === "contacts";

        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "chat-item";
        if (window.DatChat.activeChat && window.DatChat.activeChat !== "family" && sameUser(window.DatChat.activeChat, nickname))
            btn.classList.add("active");
        btn.dataset.chat = nickname;
        btn.dataset.name = name;

        const av = document.createElement("div");
        av.className = "user-avatar " + avatarClass(name);
        av.textContent = name.charAt(0).toUpperCase();
        if (online) av.classList.add("is-online");

        const meta = document.createElement("div");
        meta.className = "chat-item-meta";
        const row = document.createElement("div");
        row.className = "chat-item-row";
        const title = document.createElement("div");
        title.className = "chat-item-name";
        title.textContent = name;
        const tm = document.createElement("div");
        tm.className = "chat-item-time";
        tm.textContent = preview?.time || "";
        row.append(title, tm);
        const sub = document.createElement("div");
        sub.className = "chat-item-status" + (online && (contacts || !preview?.text) ? " online" : "");
        sub.textContent = contacts || !preview?.text
            ? (pick(user, "status") || (online ? "онлайн" : "не в сети"))
            : preview.text;
        meta.append(row, sub);

        btn.append(av, meta);
        if (unread) {
            const badge = document.createElement("span");
            badge.className = "unread";
            badge.textContent = String(unread);
            btn.appendChild(badge);
        }
        btn.addEventListener("click", () => openPrivateChat(nickname, name, online));
        usersContainer.appendChild(btn);
    });
    applyConvFilter();
}

function clearUsers() {
    if (usersContainer) usersContainer.innerHTML = "";
}

function markUnread(name) {
    usersContainer?.querySelectorAll(".chat-item").forEach(item => {
        if (!sameUser(item.dataset.chat, name)) return;
        let badge = item.querySelector(".unread");
        if (!badge) {
            badge = document.createElement("span");
            badge.className = "unread";
            item.appendChild(badge);
        }
        badge.textContent = String(window.DatChat.unread[displayName(name)] || 1);
    });
    refreshDocTitle?.();
}

function setActiveChatButton(chat) {
    document.querySelectorAll("#navigation [data-chat]").forEach(el => {
        el.classList.toggle("active", !!chat && (sameUser(el.dataset.chat, chat) || (chat === "family" && el.dataset.chat === "family")));
    });
}

function setPeerHeader(title, status, chatId) {
    const el = document.getElementById("chatTitle");
    if (el) el.textContent = title;
    const st = document.getElementById("chatStatus");
    if (st) {
        st.dataset.base = status || "";
        st.textContent = status || "";
        st.classList.toggle("online", status === "онлайн");
    }
    const av = document.getElementById("peerAvatar");
    if (av) {
        av.textContent = (title || "?").charAt(0).toUpperCase();
        av.className = "user-avatar header-avatar " + avatarClass(title || chatId || "F");
        av.classList.toggle("is-online", status === "онлайн");
    }
}

function setConversationOpen(open) {
    document.getElementById("convIdle")?.classList.toggle("hidden", open);
    document.getElementById("convWork")?.classList.toggle("hidden", !open);
    if (open)
        window.setImPane?.("chat");
    else {
        window.DatChat.activeChat = null;
        window.setImPane?.("dialogs");
        setActiveChatButton("");
        clearQuote?.();
    }
}

async function openFamilyChat() {
    window.DatChat.activeChat = "family";
    window.DatChat.unread.family = 0;
    setFamilyUnread(0);
    refreshDocTitle?.();
    setActiveChatButton("family");
    setConversationOpen(true);
    setPeerHeader("Family", "общий чат", "family");
    if (window.DatChat.lastPinned) renderPinned(window.DatChat.lastPinned);
    try {
        await window.connection.invoke("GetChatHistory", "family");
        await window.connection.invoke("ViewMessages", "family");
    }
    catch (e) { console.error(e); }
}

async function openPrivateChat(nickname, name, online) {
    window.DatChat.activeChat = nickname;
    window.DatChat.unread[displayName(nickname)] = 0;
    refreshDocTitle?.();
    setActiveChatButton(nickname);
    setConversationOpen(true);
    setPeerHeader(name, online ? "онлайн" : "не в сети", nickname);
    hidePinned();
    const chatId = privateChatId(nickname);
    try {
        await window.connection.invoke("GetChatHistory", chatId);
        await window.connection.invoke("ViewMessages", chatId);
    }
    catch (e) {
        console.error(e);
        showToast?.("Не удалось открыть переписку");
    }
}

document.getElementById("familyChatBtn")?.addEventListener("click", openFamilyChat);

window.renderUsers = renderUsers;
window.applyTdChats = applyTdChats;
window.clearUsers = clearUsers;
window.markUnread = markUnread;
window.setConvPreview = setConvPreview;
window.setFamilyUnread = setFamilyUnread;
window.setConversationOpen = setConversationOpen;
window.setNavTab = setNavTab;
window.applyConvFilter = applyConvFilter;
window.updateMeChip = updateMeChip;
window.setNavConn = setNavConn;
window.openFamilyChat = openFamilyChat;
window.openPrivateChat = openPrivateChat;
