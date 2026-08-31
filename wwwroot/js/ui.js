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
    ["😀","😁","😂","🤣","😊","😍","😘","😎","🤔","😴","😭","😡","👍","👎","❤️","🔥","🎉","✨","🙏","👏","💪","✅","📌","📎","🍕","🥧","☕","🏠","💚","💙"].forEach(ch => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.textContent = ch;
        btn.addEventListener("click", e => {
            e.stopPropagation();
            const input = field();
            if (!input) return;
            input.value += ch;
            input.focus();
        });
        picker.appendChild(btn);
    });
}

function toggleEmojiPicker(e) {
    e?.preventDefault();
    e?.stopPropagation();
    const picker = document.getElementById("emojiPicker");
    if (!picker) return;
    picker.classList.toggle("hidden");
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
    document.getElementById("loginForm")?.addEventListener("submit", login);
    document.getElementById("settingsBtn")?.addEventListener("click", logout);
    document.getElementById("sendBtn")?.addEventListener("click", sendMessage);
    document.getElementById("emojiBtn")?.addEventListener("click", toggleEmojiPicker);
    document.getElementById("fileInput")?.addEventListener("change", e => {
        const file = e.target.files?.[0];
        if (file) uploadAndSend(file);
        e.target.value = "";
    });
    document.getElementById("menuBtn")?.addEventListener("click", () => {
        document.getElementById("sidebar")?.classList.toggle("open");
    });
    document.getElementById("searchBtn")?.addEventListener("click", () => {
        document.getElementById("searchModal")?.classList.remove("hidden");
        document.getElementById("searchInput")?.focus();
    });
    document.getElementById("closeSearch")?.addEventListener("click", () => {
        document.getElementById("searchModal")?.classList.add("hidden");
    });
    document.getElementById("forgotBtn")?.addEventListener("click", () => {
        showToast("Сброс пароля пока не нужен: войдите своим ником");
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

    document.addEventListener("click", e => {
        if (e.target.closest("#emojiPicker") || e.target.closest("#emojiBtn")) return;
        document.getElementById("emojiPicker")?.classList.add("hidden");
    });
}

window.showToast = showToast;
window.showLogin = showLogin;
window.showChat = showChat;
window.renderSearchResults = renderSearchResults;
