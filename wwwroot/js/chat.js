const messagesBox = document.getElementById("messages");
const messageInput = document.getElementById("messageInput");
window.messageInput = messageInput;
const TOUCH_MENU_MS = 600;
let lastDateKey = "";

function clearMessages() {
    if (messagesBox) messagesBox.innerHTML = "";
    lastDateKey = "";
}

function scrollBottom() {
    if (!messagesBox) return;
    messagesBox.scrollTop = messagesBox.scrollHeight;
    updateScrollJump();
}

function isNearBottom() {
    if (!messagesBox) return true;
    return messagesBox.scrollHeight - messagesBox.scrollTop - messagesBox.clientHeight < 96;
}

function lastMessageRow() {
    if (!messagesBox) return null;
    const rows = messagesBox.querySelectorAll(".message");
    return rows.length ? rows[rows.length - 1] : null;
}

function dateKeyFromMessage(msg) {
    const raw = pick(msg, "timestamp", "Timestamp");
    const date = raw ? new Date(raw) : new Date();
    if (isNaN(date.getTime())) return "";
    return localDateKey(date);
}

function localDateKey(date) {
    return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0");
}

function formatDateChip(key) {
    const date = new Date(key + "T00:00:00");
    const todayKey = localDateKey(new Date());
    const label = date.toLocaleDateString("ru-RU", { day: "numeric", month: "long" });
    return key === todayKey ? "Сегодня, " + label : label;
}

function maybeDateChip(msg) {
    const key = dateKeyFromMessage(msg);
    if (!key || key === lastDateKey || !messagesBox) return;
    lastDateKey = key;
    const chip = document.createElement("div");
    chip.className = "date-chip";
    chip.textContent = formatDateChip(key);
    messagesBox.appendChild(chip);
}

function familyNumericId(id) {
    const s = String(id ?? "");
    if (s.startsWith("f-")) return Number(s.slice(2));
    return Number(s);
}

function parseQuotedText(text) {
    const raw = String(text || "");
    const m = raw.match(/^«([^»]+)»: ([^\n]+)\n\n([\s\S]*)$/);
    if (!m) return { quote: null, body: raw };
    return { quote: { nick: m[1], text: m[2] }, body: m[3] };
}

function setQuote(nick, text) {
    window.DatChat.quote = { nick: displayName(nick), text: String(text || "").replace(/\s+/g, " ").slice(0, 120) };
    const bar = document.getElementById("quoteBar");
    const qn = document.getElementById("quoteNick");
    const qt = document.getElementById("quoteText");
    if (qn) qn.textContent = window.DatChat.quote.nick;
    if (qt) qt.textContent = window.DatChat.quote.text;
    bar?.classList.remove("hidden");
    focusComposer?.();
}

function clearQuote() {
    window.DatChat.quote = null;
    document.getElementById("quoteBar")?.classList.add("hidden");
}

function updateScrollJump() {
    const btn = document.getElementById("scrollJump");
    if (!btn || !messagesBox) return;
    btn.classList.toggle("hidden", isNearBottom());
}

function closeMessageMenus() {
    document.querySelectorAll(".message-menu").forEach(menu => menu.remove());
}

function createMessageMenu(div, id) {
    const menu = document.createElement("div");
    menu.className = "message-menu";

    const replyBtn = document.createElement("button");
    replyBtn.type = "button";
    replyBtn.textContent = "↩ Ответить";
    replyBtn.addEventListener("click", e => {
        e.stopPropagation();
        const nick = div.dataset.nick || "";
        const body = div.querySelector(".msg-text")?.innerText || "";
        setQuote(nick, body);
        closeMessageMenus();
    });
    menu.appendChild(replyBtn);

    if (window.DatChat.activeChat === "family" && window.DatChat.isAdmin) {
        const pinBtn = document.createElement("button");
        pinBtn.type = "button";
        pinBtn.textContent = "📌 Закрепить";
        pinBtn.addEventListener("click", async e => {
            e.stopPropagation();
            try {
                await window.connection.invoke("PinMessage", familyNumericId(id));
                closeMessageMenus();
            } catch {
                showToast?.("Не удалось закрепить сообщение");
            }
        });
        menu.appendChild(pinBtn);
    }

    const deleteBtn = document.createElement("button");
    deleteBtn.type = "button";
    deleteBtn.textContent = "🗑 Удалить";
    deleteBtn.addEventListener("click", async e => {
        e.stopPropagation();
        try {
            if (window.DatChat.activeChat === "family")
                await window.connection.invoke("DeleteMessage", familyNumericId(id));
            else
                div.remove();
            closeMessageMenus();
        } catch {
            showToast?.("Не удалось удалить сообщение");
        }
    });
    menu.appendChild(deleteBtn);
    div.appendChild(menu);
}

function attachMenu(div, id) {
    div.addEventListener("contextmenu", e => {
        e.preventDefault();
        closeMessageMenus();
        createMessageMenu(div, id);
    });

    let pressTimer = null;
    div.addEventListener("touchstart", () => {
        pressTimer = setTimeout(() => {
            closeMessageMenus();
            createMessageMenu(div, id);
        }, TOUCH_MENU_MS);
    }, { passive: true });
    div.addEventListener("touchend", () => clearTimeout(pressTimer));
    div.addEventListener("touchmove", () => clearTimeout(pressTimer));
}

function appendFile(body, msg, text) {
    const fileUrl = pick(msg, "fileUrl");
    const fileType = pick(msg, "fileType");
    if (!fileUrl) {
        if (text) body.textContent = text;
        return;
    }

    if (fileType === "image") {
        const img = document.createElement("img");
        img.src = fileUrl;
        img.alt = text || "изображение";
        const wrap = document.createElement("div");
        wrap.className = "msg-file";
        wrap.appendChild(img);
        body.appendChild(wrap);
        if (text) {
            const cap = document.createElement("div");
            cap.textContent = text;
            body.appendChild(cap);
        }
        return;
    }

    const link = document.createElement("a");
    link.href = fileUrl;
    link.target = "_blank";
    link.rel = "noopener";
    link.textContent = "📎 " + (text || "Файл");
    body.appendChild(link);
}

function buildBubble(msg, nickname, text, mine) {
    const bubble = document.createElement("div");
    bubble.className = "bubble";

    const name = document.createElement("div");
    name.className = "msg-name";
    name.textContent = displayName(nickname);

    const body = document.createElement("div");
    body.className = "msg-text";

    const parsed = pick(msg, "isDeleted") ? { quote: null, body: "" } : parseQuotedText(text);
    if (pick(msg, "isDeleted")) {
        body.textContent = "Сообщение удалено";
        body.classList.add("deleted-message");
    } else {
        if (parsed.quote) {
            const q = document.createElement("div");
            q.className = "msg-quote";
            q.textContent = parsed.quote.nick + ": " + parsed.quote.text;
            body.appendChild(q);
        }
        appendFile(body, msg, parsed.body);
    }

    const meta = document.createElement("div");
    meta.className = "msg-meta";
    const time = document.createElement("span");
    time.textContent = formatMessageTime(pick(msg, "time", "timestamp"));
    meta.appendChild(time);
    if (mine) {
        const checks = document.createElement("span");
        checks.className = "checks";
        checks.textContent = "✓✓";
        meta.appendChild(checks);
    }

    bubble.appendChild(name);
    bubble.appendChild(body);
    bubble.appendChild(meta);
    return bubble;
}

function renderMessage(msg) {
    if (!messagesBox) return;
    const id = pick(msg, "id");
    if (id == null) return;

    const old = document.getElementById("msg-" + id);
    if (old) old.remove();

    maybeDateChip(msg);

    const nickname = pick(msg, "nickname", "senderId", "user") || "";
    const text = pick(msg, "text") || "";
    const mine = sameUser(nickname, window.DatChat.currentUser);

    const row = document.createElement("div");
    row.className = mine ? "message mine" : "message";
    row.id = "msg-" + id;
    row.dataset.nick = displayName(nickname);
    const prev = lastMessageRow();
    if (prev && sameUser(prev.dataset.nick, nickname))
        row.classList.add("grouped");

    if (!mine) {
        const av = document.createElement("div");
        av.className = "user-avatar " + avatarClass(nickname);
        av.textContent = displayName(nickname).charAt(0).toUpperCase();
        row.appendChild(av);
    }

    row.appendChild(buildBubble(msg, nickname, text, mine));
    attachMenu(row, id);
    const stick = isNearBottom() || mine;
    messagesBox.appendChild(row);
    if (stick) scrollBottom();
}

function renderPrivateMessage(msg) {
    if (!messagesBox) return;
    const sender = pick(msg, "sender");
    const mapped = {
        id: "pm-" + pick(msg, "id"),
        nickname: sender,
        text: pick(msg, "text") || "",
        fileUrl: pick(msg, "fileUrl"),
        fileType: pick(msg, "fileType"),
        time: pick(msg, "time"),
        timestamp: pick(msg, "timestamp")
    };
    const id = mapped.id;
    if (document.getElementById("msg-" + id)) return;

    maybeDateChip(mapped);
    const mine = sameUser(sender, window.DatChat.currentUser);
    const row = document.createElement("div");
    row.className = mine ? "message mine" : "message";
    row.id = "msg-" + id;
    row.dataset.nick = displayName(sender);
    const prev = lastMessageRow();
    if (prev && sameUser(prev.dataset.nick, sender))
        row.classList.add("grouped");

    if (!mine) {
        const av = document.createElement("div");
        av.className = "user-avatar " + avatarClass(sender);
        av.textContent = displayName(sender).charAt(0).toUpperCase();
        row.appendChild(av);
    }

    row.appendChild(buildBubble(mapped, sender, mapped.text, mine));
    const stick = isNearBottom() || mine;
    messagesBox.appendChild(row);
    if (stick) scrollBottom();
}

function removeMessage(id) {
    const realId = pick(id, "id") ?? id;
    const element = document.getElementById("msg-" + realId);
    if (!element) return;
    const body = element.querySelector(".msg-text");
    if (body) {
        body.textContent = "Сообщение удалено";
        body.classList.add("deleted-message");
    }
    setTimeout(() => element.remove(), 2500);
}

function showEmptyChat(text) {
    if (!messagesBox) return;
    const empty = document.createElement("div");
    empty.className = "empty-chat";
    empty.id = "emptyChat";
    empty.textContent = text;
    messagesBox.appendChild(empty);
}

function hideEmptyChat() {
    document.getElementById("emptyChat")?.remove();
}

async function sendMessage() {
    if (!messageInput) return;
    const text = messageInput.value.trim();
    if (!text) return;
    if (!window.DatChat.activeChat) {
        showToast?.("Сначала выберите чат");
        return;
    }
    if (!isHubConnected()) {
        showToast?.("Соединение с сервером не установлено");
        return;
    }

    try {
        const chatId = activeChatId();
        if (!chatId) return;
        let payload = text;
        if (window.DatChat.quote) {
            payload = "«" + window.DatChat.quote.nick + "»: " + window.DatChat.quote.text + "\n\n" + text;
            clearQuote();
        }
        await window.connection.invoke("SendChatMessage", chatId, payload);
        messageInput.value = "";
        autosizeComposer?.();
        focusComposer?.();
    } catch (e) {
        console.error(e);
        showToast?.(e.message || "Не удалось отправить сообщение");
    }
}

async function uploadAndSend(file) {
    if (!file) return;
    if (!window.DatChat.activeChat) {
        showToast?.("Сначала выберите чат");
        return;
    }
    const form = new FormData();
    form.append("file", file);
    try {
        const res = await fetch("/upload", { method: "POST", body: form });
        if (!res.ok) {
            showToast?.(await res.text() || "Ошибка загрузки");
            return;
        }
        const data = await res.json();
        const caption = (window.messageInput?.value || "").trim() || data.fileName || file.name;
        if (!isHubConnected()) {
            showToast?.("Нет соединения с сервером");
            return;
        }
        const chatId = activeChatId();
        if (!chatId) return;
        await window.connection.invoke("SendChatFile", chatId, caption, data.fileUrl, data.fileType);
        if (window.messageInput) window.messageInput.value = "";
        showToast?.("Файл отправлен");
    } catch (e) {
        console.error(e);
        showToast?.(e.message || "Не удалось загрузить файл");
    }
}

document.addEventListener("click", e => {
    if (!e.target.closest(".message-menu")) closeMessageMenus();
});

messagesBox?.addEventListener("scroll", updateScrollJump, { passive: true });

window.clearQuote = clearQuote;
window.setQuote = setQuote;
window.updateScrollJump = updateScrollJump;
