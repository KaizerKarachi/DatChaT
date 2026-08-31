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

    list.forEach(user => {
        const nickname = typeof user === "string" ? user : (pick(user, "nickname") || "");
        const name = pick(user, "displayName") || displayName(nickname);
        if (!name || sameUser(nickname, me)) return;

        const online = typeof user === "string" ? true : !!pick(user, "isOnline");
        const unread = window.DatChat.unread[displayName(nickname)] || 0;

        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "nav-link";
        if (window.DatChat.activeChat !== "family" && sameUser(window.DatChat.activeChat, nickname))
            btn.classList.add("active");
        btn.dataset.chat = nickname;
        btn.append(name);
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
    usersContainer?.querySelectorAll(".nav-link").forEach(item => {
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
    document.querySelectorAll(".nav-link[data-chat]").forEach(el => {
        el.classList.toggle("active", sameUser(el.dataset.chat, chat) || (chat === "family" && el.id === "familyChatBtn"));
    });
}

function setPeerHeader(title) {
    const el = document.getElementById("chatTitle");
    if (el) el.textContent = title;
}

async function openFamilyChat() {
    window.DatChat.activeChat = "family";
    setActiveChatButton("family");
    setPeerHeader("Family Chat");
    document.getElementById("sidebar")?.classList.remove("open");
    if (window.DatChat.lastPinned) renderPinned(window.DatChat.lastPinned);
    try { await window.connection.invoke("LoadFamilyHistory"); }
    catch (e) { console.error(e); }
}

async function openPrivateChat(nickname, name) {
    window.DatChat.activeChat = nickname;
    window.DatChat.unread[displayName(nickname)] = 0;
    setActiveChatButton(nickname);
    setPeerHeader(name);
    document.getElementById("sidebar")?.classList.remove("open");
    hidePinned();
    try { await window.connection.invoke("LoadPrivateHistory", nickname); }
    catch (e) {
        console.error(e);
        showToast?.("Не удалось открыть переписку");
    }
}

document.getElementById("familyChatBtn")?.addEventListener("click", openFamilyChat);
