const usersContainer = document.getElementById("usersList");

function currentNickname() {
    return window.DatChat.currentUser || localStorage.getItem("nickname");
}

function renderUsers(users) {
    if (!usersContainer) return;
    if (users) window.DatChat.lastUsers = users;
    const list = Array.isArray(window.DatChat.lastUsers) ? window.DatChat.lastUsers : [];
    const me = currentNickname();
    usersContainer.innerHTML = "";

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
        return;
    }

    others.forEach(user => {
        const nickname = typeof user === "string" ? user : (pick(user, "nickname") || "");
        const name = pick(user, "displayName") || displayName(nickname);

        const online = typeof user === "string" ? false : !!pick(user, "isOnline");
        const status = pick(user, "status") || (online ? "онлайн" : "не в сети");
        const unread = window.DatChat.unread[displayName(nickname)] || 0;

        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "chat-item";
        if (window.DatChat.activeChat !== "family" && sameUser(window.DatChat.activeChat, nickname))
            btn.classList.add("active");
        btn.dataset.chat = nickname;

        const av = document.createElement("div");
        av.className = "user-avatar " + avatarClass(name);
        av.textContent = name.charAt(0).toUpperCase();
        if (online) av.classList.add("is-online");

        const meta = document.createElement("div");
        meta.className = "chat-item-meta";
        const title = document.createElement("div");
        title.className = "chat-item-name";
        title.textContent = name;
        const sub = document.createElement("div");
        sub.className = "chat-item-status" + (online ? " online" : "");
        sub.textContent = status;
        meta.append(title, sub);

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
}

function setActiveChatButton(chat) {
    document.querySelectorAll("[data-chat]").forEach(el => {
        el.classList.toggle("active", sameUser(el.dataset.chat, chat) || (chat === "family" && el.dataset.chat === "family"));
    });
}

function setPeerHeader(title, status) {
    const el = document.getElementById("chatTitle");
    if (el) el.textContent = title;
    const st = document.getElementById("chatStatus");
    if (st) {
        st.textContent = status || "";
        st.classList.toggle("online", status === "онлайн");
    }
}

async function openFamilyChat() {
    window.DatChat.activeChat = "family";
    setActiveChatButton("family");
    setPeerHeader("Family", "общий чат");
    window.setSidebarOpen?.(false);
    if (window.DatChat.lastPinned) renderPinned(window.DatChat.lastPinned);
    try { await window.connection.invoke("LoadFamilyHistory"); }
    catch (e) { console.error(e); }
}

async function openPrivateChat(nickname, name, online) {
    window.DatChat.activeChat = nickname;
    window.DatChat.unread[displayName(nickname)] = 0;
    setActiveChatButton(nickname);
    setPeerHeader(name, online ? "онлайн" : "не в сети");
    window.setSidebarOpen?.(false);
    hidePinned();
    try { await window.connection.invoke("LoadPrivateHistory", nickname); }
    catch (e) {
        console.error(e);
        showToast?.("Не удалось открыть переписку");
    }
}

document.getElementById("familyChatBtn")?.addEventListener("click", openFamilyChat);
