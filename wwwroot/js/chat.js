const messagesBox = document.getElementById("messages");
const messageInput = document.getElementById("messageInput");
window.messageInput = messageInput;
let lastDateKey = "";

const AVATAR_COLORS = ["av-green", "av-purple", "av-orange", "av-blue", "av-pink", "av-teal"];

function avatarClass(name) {
    const str = displayName(name || "");
    let hash = 0;
    for (let i = 0; i < str.length; i++) hash = str.charCodeAt(i) + ((hash << 5) - hash);
    return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length];
}

function clearMessages() {
    if (messagesBox) messagesBox.innerHTML = "";
    lastDateKey = "";
}

function scrollBottom() {
    if (!messagesBox) return;
    messagesBox.scrollTop = messagesBox.scrollHeight;
}

function formatMessageTime(value) {
    if (!value) return "";
    if (typeof value === "string" && /^\d{2}:\d{2}$/.test(value)) return value;
    const date = new Date(value);
    if (isNaN(date.getTime())) return String(value);
    return date.toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit" });
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

function closeMessageMenus() {
    document.querySelectorAll(".message-menu").forEach(menu => menu.remove());
}

function createMessageMenu(div, id) {
    const menu = document.createElement("div");
    menu.className = "message-menu";

    if (window.DatChat.activeChat === "family" && window.DatChat.isAdmin) {
        const pinBtn = document.createElement("button");
        pinBtn.type = "button";
        pinBtn.textContent = "📌 Закрепить";
        pinBtn.addEventListener("click", async e => {
            e.stopPropagation();
            try {
                await window.connection.invoke("PinMessage", Number(id));
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
                await window.connection.invoke("DeleteMessage", Number(id));
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
        }, 600);
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

    if (pick(msg, "isDeleted")) {
        body.textContent = "Сообщение удалено";
        body.classList.add("deleted-message");
    } else {
        appendFile(body, msg, text);
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

    const nickname = pick(msg, "nickname", "user") || "";
    const text = pick(msg, "text") || "";
    const mine = sameUser(nickname, window.DatChat.currentUser);

    const row = document.createElement("div");
    row.className = mine ? "message mine" : "message";
    row.id = "msg-" + id;

    if (!mine) {
        const av = document.createElement("div");
        av.className = "user-avatar " + avatarClass(nickname);
        av.textContent = displayName(nickname).charAt(0).toUpperCase();
        row.appendChild(av);
    }

    row.appendChild(buildBubble(msg, nickname, text, mine));
    attachMenu(row, id);
    messagesBox.appendChild(row);
    scrollBottom();
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

    if (!mine) {
        const av = document.createElement("div");
        av.className = "user-avatar " + avatarClass(sender);
        av.textContent = displayName(sender).charAt(0).toUpperCase();
        row.appendChild(av);
    }

    row.appendChild(buildBubble(mapped, sender, mapped.text, mine));
    messagesBox.appendChild(row);
    scrollBottom();
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

async function sendMessage() {
    if (!messageInput) return;
    const text = messageInput.value.trim();
    if (!text) return;
    if (!window.connection || window.connection.state !== "Connected") {
        showToast?.("Соединение с сервером не установлено");
        return;
    }

    try {
        if (window.DatChat.activeChat === "family")
            await window.connection.invoke("SendMessage", text, null, null);
        else
            await window.connection.invoke("SendPrivateMessage", window.DatChat.activeChat, text, null, null);
        messageInput.value = "";
        messageInput.focus();
    } catch (e) {
        console.error(e);
        showToast?.("Не удалось отправить сообщение");
    }
}

async function uploadAndSend(file) {
    if (!file) return;
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
        if (!window.connection || window.connection.state !== "Connected") {
            showToast?.("Нет соединения с сервером");
            return;
        }
        if (window.DatChat.activeChat === "family")
            await window.connection.invoke("SendMessage", caption, data.fileUrl, data.fileType);
        else
            await window.connection.invoke("SendPrivateMessage", window.DatChat.activeChat, caption, data.fileUrl, data.fileType);
        if (window.messageInput) window.messageInput.value = "";
        showToast?.("Файл отправлен");
    } catch (e) {
        console.error(e);
        showToast?.("Не удалось загрузить файл");
    }
}

document.addEventListener("click", e => {
    if (!e.target.closest(".message-menu")) closeMessageMenus();
});
