function field() {
    return window.messageInput || document.getElementById("messageInput");
}

function showLogin() {
    document.getElementById("loginScreen")?.classList.remove("hidden");
    document.getElementById("app")?.classList.add("hidden");
}

function showChat() {
    document.getElementById("loginScreen")?.classList.add("hidden");
    document.getElementById("app")?.classList.remove("hidden");
}

function showToast(text) {
    const toast = document.getElementById("toast");
    if (!toast) return;
    toast.textContent = text;
    toast.classList.remove("hidden");
    clearTimeout(window.toastTimer);
    window.toastTimer = setTimeout(() => toast.classList.add("hidden"), 2500);
}

function fillEmojiPicker() {
    const picker = document.getElementById("emojiPicker");
    if (!picker) return;
    picker.replaceChildren();
    const chars = ["😀","😁","😂","🤣","😊","😍","😘","😎","🤔","😴","😭","😡","👍","👎","❤","🔥","🎉","✨","🙏","👏","💪","✅","📌","📎","🍕","🥧","☕","🏠","💚","💙"];
    chars.forEach(ch => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.textContent = ch;
        picker.appendChild(btn);
    });
}

function placeEmojiPicker() {
    const picker = document.getElementById("emojiPicker");
    const btn = document.getElementById("emojiBtn");
    if (!picker || !btn) return;
    if (window.matchMedia("(max-width: 820px)").matches) {
        picker.style.left = "8px";
        picker.style.right = "8px";
        picker.style.width = "auto";
        picker.style.top = "auto";
        picker.style.bottom = Math.max(12, window.innerHeight - btn.getBoundingClientRect().top + 8) + "px";
        return;
    }
    picker.style.right = "";
    picker.style.bottom = "";
    picker.style.width = "";
    const rect = btn.getBoundingClientRect();
    const width = 280;
    picker.style.left = Math.max(12, Math.min(rect.right - width, window.innerWidth - width - 12)) + "px";
    picker.style.top = Math.max(12, rect.top - 220) + "px";
}

function setSidebarOpen(open) {
    document.getElementById("sidebar")?.classList.toggle("open", open);
    const scrim = document.getElementById("sidebarScrim");
    if (scrim) scrim.hidden = !open;
}

function syncViewportHeight() {
    const h = window.visualViewport?.height || window.innerHeight;
    document.documentElement.style.setProperty("--vvh", h + "px");
}

function openEmojiPicker() {
    const picker = document.getElementById("emojiPicker");
    if (!picker) return;
    picker.classList.add("open");
    picker.setAttribute("aria-hidden", "false");
    placeEmojiPicker();
}

function closeEmojiPicker() {
    const picker = document.getElementById("emojiPicker");
    if (picker) {
        picker.classList.remove("open");
        picker.setAttribute("aria-hidden", "true");
    }
}

function insertEmoji(ch) {
    const input = field();
    if (!input) return;
    input.value += ch;
    input.focus();
}

function renderSearchResults(results) {
    const box = document.getElementById("searchResults");
    if (!box) return;
    box.replaceChildren();
    (results || []).forEach(msg => {
        const item = document.createElement("div");
        item.className = "search-hit";
        item.textContent = displayName(pick(msg, "nickname")) + ": " + (pick(msg, "text") || "");
        item.addEventListener("click", () => {
            document.getElementById("searchModal")?.classList.add("hidden");
            document.getElementById("msg-" + pick(msg, "id"))?.scrollIntoView({ behavior: "smooth", block: "center" });
        });
        box.appendChild(item);
    });
    if (!results?.length) box.textContent = "Ничего не найдено";
}

function bindUi() {
    fillEmojiPicker();
    const picker = document.getElementById("emojiPicker");
    if (picker) picker.setAttribute("aria-hidden", "true");
    syncViewportHeight();
    window.visualViewport?.addEventListener("resize", syncViewportHeight);
    window.addEventListener("resize", () => {
        syncViewportHeight();
        if (document.getElementById("emojiPicker")?.classList.contains("open"))
            placeEmojiPicker();
    });

    document.getElementById("emojiBtn")?.addEventListener("click", e => {
        e.preventDefault();
        e.stopPropagation();
        const p = document.getElementById("emojiPicker");
        if (p?.classList.contains("open")) closeEmojiPicker();
        else openEmojiPicker();
    });
    document.getElementById("sendBtn")?.addEventListener("click", e => {
        e.preventDefault();
        sendMessage();
    });
    document.getElementById("sidebarScrim")?.addEventListener("click", () => setSidebarOpen(false));

    document.addEventListener("click", e => {
        const t = e.target instanceof Element ? e.target : e.target.parentElement;
        if (!t) return;

        if (t.closest("#emojiPicker button")) {
            e.preventDefault();
            insertEmoji(t.closest("button").textContent);
            return;
        }

        if (t.closest("#menuBtn")) {
            const sidebar = document.getElementById("sidebar");
            setSidebarOpen(!sidebar?.classList.contains("open"));
            return;
        }

        if (t.closest("#searchBtn")) {
            document.getElementById("searchModal")?.classList.remove("hidden");
            document.getElementById("searchInput")?.focus();
            return;
        }

        if (t.closest("#closeSearch")) {
            document.getElementById("searchModal")?.classList.add("hidden");
            return;
        }

        if (!t.closest("#emojiPicker") && !t.closest("#emojiBtn"))
            closeEmojiPicker();
    });

    document.getElementById("loginForm")?.addEventListener("submit", login);
    document.getElementById("fileInput")?.addEventListener("change", e => {
        const file = e.target.files?.[0];
        if (file) {
            showToast("Загрузка файла...");
            uploadAndSend(file);
        }
        e.target.value = "";
    });

    let searchTimer;
    document.getElementById("searchInput")?.addEventListener("input", e => {
        clearTimeout(searchTimer);
        const q = e.target.value.trim();
        if (q.length < 2) return;
        searchTimer = setTimeout(() => window.connection?.invoke("SearchMessages", q), 250);
    });

    field()?.addEventListener("keydown", e => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });
}

window.showToast = showToast;
window.showLogin = showLogin;
window.showChat = showChat;
window.renderSearchResults = renderSearchResults;
window.closeEmojiPicker = closeEmojiPicker;
window.setSidebarOpen = setSidebarOpen;
