const pinnedBar = document.getElementById("pinnedBar");
const pinnedText = document.getElementById("pinnedText");
const unpinBtn = document.getElementById("unpinBtn");

function renderPinned(msg) {
    if (!pinnedBar || !pinnedText) return;
    if (!msg) {
        clearPinned(true);
        return;
    }

    window.DatChat.lastPinned = msg;
    const nickname = displayName(pick(msg, "nickname"));
    const text = pick(msg, "text") || "";
    pinnedText.textContent = (nickname ? nickname + ": " : "") + text;
    pinnedBar.classList.remove("hidden");
    if (unpinBtn)
        unpinBtn.classList.toggle("hidden", !window.DatChat.isAdmin);
}

function hidePinned() {
    pinnedBar?.classList.add("hidden");
}

function clearPinned(forget) {
    if (pinnedText) pinnedText.textContent = "";
    pinnedBar?.classList.add("hidden");
    if (forget) window.DatChat.lastPinned = null;
}

unpinBtn?.addEventListener("click", async () => {
    try {
        await window.connection.invoke("UnpinMessage");
    } catch {
        showToast?.("Не удалось открепить");
    }
});
