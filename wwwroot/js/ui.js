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
    updateMeChip?.();
    setNavConn?.("ok");
    setConversationOpen?.(false);
}

function showToast(text) {
    const toast = document.getElementById("toast");
    if (!toast) return;
    toast.textContent = text;
    toast.classList.remove("hidden");
    clearTimeout(window.toastTimer);
    window.toastTimer = setTimeout(() => toast.classList.add("hidden"), 2500);
}

const EMOJI_CATS = [
    { id: "faces", label: "😀", title: "Смайлы", chars: "😀😁😂🤣😊😇🙂🙃😉😍😘😗😙😚😋😛😜🤪😝🤑🤗🤭🤫🤔🤐🤨😐😑😶😏😒🙄😬🤥😌😔😪🤤😴😷🤒🤕🤢🤮🤧🥵🥶🥴😵🤯🤠🥳😎🤓🧐😕😟🙁😮😯😲😳🥺😦😧😨😰😥😢😭😱😖😣😞😓😩😫🥱😤😡😠🤬😈👿💀💩🤡👻👽🤖😺😸😹😻😼😽🙀😿😾".split("") },
    { id: "hands", label: "👍", title: "Жесты", chars: "👋🤚🖐✋🖖👌🤌🤏✌🤞🤟🤘🤙👈👉👆🖕👇☝👍👎✊👊🤛🤜👏🙌👐🤲🤝🙏💪🦾✍️💅🤳".split("") },
    { id: "people", label: "👨", title: "Люди", chars: "👶👧🧒👦👩🧑👨👵🧓👴👲👳‍♀️👳‍♂️🧕👮👷💂🕵👩‍⚕️👨‍⚕️👩‍🌾👨‍🌾👩‍🍳👨‍🍳👩‍🎓👨‍🎓👩‍🎤👨‍🎤👩‍🏫👨‍🏫👩‍💻👨‍💻👩‍🚀👨‍🚀👩‍🚒👨‍🚒👰🤵🤰🤱👪👩‍👩‍👧👨‍👨‍👦".split(/(?:)/u).filter(ch => ch && ch !== "\u200d") },
    { id: "nature", label: "🌿", title: "Природа", chars: "🐶🐱🐭🐹🐰🦊🐻🐼🐨🐯🦁🐮🐷🐸🐵🙈🙉🙊🐔🐧🐦🐤🦆🦅🦉🦇🐺🐗🐴🦄🐝🐛🦋🐌🐞🐜🦟🦗🐢🐍🦎🦖🦕🐙🦑🦐🦞🦀🐡🐠🐟🐬🐳🐋🦈🐊🐅🐆🦓🦍🦧🐘🦛🦏🐪🐫🦒🦘🦥🦦🦨🦡🐾🌵🎄🌲🌳🌴🌱🌿🍀🎍🎋🍃🍂🍁🍄🐚💐🌸💮🌹🥀🌺🌻🌼🌷🌍🌎🌏⭐🌟✨⚡🔥🌈☀️⛅☁️❄️☃️⛄💧🌊".split("") },
    { id: "food", label: "🍕", title: "Еда", chars: "🍏🍎🍐🍊🍋🍌🍉🍇🍓🫐🍈🍒🍑🥭🍍🥥🥝🍅🍆🥑🥦🥬🥒🌶🫑🌽🥕🫒🧄🧅🥔🍠🥐🥯🍞🥖🥨🧀🥚🍳🥞🧇🥓🥩🍗🍖🌭🍔🍟🍕🫓🥪🥙🧆🌮🌯🫔🥗🥘🫕🥫🍝🍜🍲🍛🍣🍱🥟🦪🍤🍙🍚🍘🍥🥠🥮🍢🍡🍧🍨🍦🥧🧁🍰🎂🍮🍭🍬🍫🍿🍩🍪🌰🥜🍯🥛🍼☕🍵🧃🥤🧋🍶🍺🍻🥂🍷🥃🍸🍹🍾🥄🍴🍽🥣🥡".split("") },
    { id: "travel", label: "✈️", title: "Поездки", chars: "🚗🚕🚙🚌🚎🏎🚓🚑🚒🚐🛻🚚🚛🚜🛵🏍️🚲🛴🚏🚨🚥🚦⛽🚧🏠🏡🏢🏣🏥🏦🏨🏩🏪🏫🏬🏭🏯🏰💒🗼🗽⛪🕌🕍⛩🕋⛲⛺🌁🌃🏙🌄🌅🌆🌇🌉🎠🎡🎢🚂🚃🚄🚅🚆🚇🚈🚉🚊🚝🚞🚋🚌🚍🚎🚐🚑🚒🚓🚔🚕🚖🚗🚘🚙🚚🚛🚜🚝🚲🚏⛽🚧⚓⛵🚤🛥🛳⛴🚢✈️🛫🛬🪂🚁🚟🚠🚡🚀🛸".split("") },
    { id: "activity", label: "⚽", title: "Игры", chars: "⚽🏀🏈⚾🥎🎾🏐🏉🥏🎱🪀🏓🏸🥅🏒🏑🥍🏏🪃🥅⛳🪁🏹🎣🤿🥊🥋🎽🛹🛼🛷⛸🥌🎿⛷🏂🪂🎪🎭🩰🎨🎬🎤🎧🎼🎹🥁🎷🎺🪗🎸🪕🎻🎲♟🎯🎳🎮🕹🎰🧩".split("") },
    { id: "objects", label: "💡", title: "Вещи", chars: "⌚📱📲💻⌨️🖥🖨🖱🖲🕹🗜💽💾💿📀📼📷📸📹🎥📞☎️📟📠📺📻🎙⏰⏱⏲🕰⌛⏳📡🔋🔌💡🔦🕯📔📕📗📘📙📚📓📒📃📜📄📰📑🔖💰💴💵💶💷💳💎⚖️🧰🪛🔧🔨⚒🛠⛏🪚🔩⚙️🧱⛓🧲🔫💣🧨🪓🔪🗡⚔️🛡🚬⚰️⚱️🏺🔮📿💈⚗️🔭🔬🕳💊💉🩸🩹🩺🚪🛏🛋🚽🚿🛁🔑🗝👜💼👓🕶🧵🪡🧶".split("") },
    { id: "symbols", label: "❤️", title: "Знаки", chars: "❤️🧡💛💚💙💜🖤🤍🤎💔❣️💕💞💓💗💖💘💝💟☮️✝️☪️🕉☸️✡️🔯🕎☯️☦️🛐⛎♈♉♊♋♌♍♎♏♐♑♒♓🆔⚛️🉑☢️☣️📴📳🈶🈚🈸🈺🈷️✴️🆚💮🉐㊙️㊗️🈴🈵🈹🈲🅰️🅱️🆎🆑🅾️🆘❌⭕🛑⛔📛🚫💯💢♨️🚷🚯🚳🚱🔞📵🚭❗❓❕❔‼️⁉️🔅🔆〽️⚠️🚸🔱⚜️🔰♻️✅🈯💹❇️✳️❎🌐💠Ⓜ️🌀💤🏧🚾♿🚹🚺🚼🚻🚮🎦📶🈁🔣ℹ️🔤🔡🔠🔢#️⃣*️⃣⏏️▶️⏸️⏹️⏺️⏭️⏮️⏩⏪⏫⏬◀️🔼🔽➡️⬅️⬆️⬇️↗️↘️↙️↖️↕️↔️↪️↩️⤴️⤵️🔀🔁🔂🔄🔃🎵🎶➕➖➗✖️♾️💲💱™️©️®️〰️➰➿✔️☑️🔘🔴🟠🟡🟢🔵🟣⚫⚪🟤🔺🔻🔸🔹🔶🔷🔳🔲▪️▫️◾◽◼️◻️🟥🟧🟨🟩🟦🟪⬛⬜🟫🔈🔇🔉🔊🔔🔕📣📢💬💭🗯♠️♣️♥️♦️🃏🎴🀄🕐🕑🕒🕓🕔🕕🕖🕗🕘🕙🕚🕛".split("") }
];

let activeEmojiCat = "faces";

function fillEmojiPicker() {
    const cats = document.getElementById("emojiCats");
    const grid = document.getElementById("emojiGrid");
    if (!cats || !grid) return;
    cats.replaceChildren();
    EMOJI_CATS.forEach(cat => {
        const tab = document.createElement("button");
        tab.type = "button";
        tab.className = "emoji-cat" + (cat.id === activeEmojiCat ? " active" : "");
        tab.dataset.cat = cat.id;
        tab.title = cat.title;
        tab.setAttribute("aria-label", cat.title);
        tab.textContent = cat.label;
        tab.addEventListener("click", e => {
            e.stopPropagation();
            showEmojiCat(cat.id);
        });
        cats.appendChild(tab);
    });
    showEmojiCat(activeEmojiCat);
}

function showEmojiCat(id) {
    const cat = EMOJI_CATS.find(c => c.id === id) || EMOJI_CATS[0];
    activeEmojiCat = cat.id;
    document.querySelectorAll(".emoji-cat").forEach(tab => {
        tab.classList.toggle("active", tab.dataset.cat === cat.id);
    });
    const grid = document.getElementById("emojiGrid");
    if (!grid) return;
    grid.replaceChildren();
    cat.chars.forEach(ch => {
        if (!ch || ch === "\u200d" || ch === "\ufe0f") return;
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "emoji-cell";
        btn.textContent = ch;
        grid.appendChild(btn);
    });
    grid.scrollTop = 0;
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
    const width = 320;
    const height = Math.min(340, window.innerHeight - 24);
    picker.style.left = Math.max(12, Math.min(rect.right - width, window.innerWidth - width - 12)) + "px";
    picker.style.top = Math.max(12, rect.top - height - 8) + "px";
}

function isMobileIm() {
    return window.matchMedia("(max-width: 820px)").matches;
}

function setImPane(pane) {
    const app = document.getElementById("app");
    if (!app) return;
    if (isMobileIm())
        app.classList.toggle("im-chat", pane === "chat");
    else
        app.classList.remove("im-chat");
    document.getElementById("sidebar")?.classList.remove("open");
}

function setSidebarOpen(open) {
    setImPane(open ? "dialogs" : "chat");
}

function focusComposer() {
    if (isMobileIm()) return;
    field()?.focus();
}

function autosizeComposer() {
    const el = field();
    if (!el || el.tagName !== "TEXTAREA") return;
    el.style.height = "auto";
    el.style.height = Math.min(el.scrollHeight, 120) + "px";
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
    if (!input || !ch) return;
    const start = input.selectionStart ?? input.value.length;
    const end = input.selectionEnd ?? start;
    input.value = input.value.slice(0, start) + ch + input.value.slice(end);
    const pos = start + ch.length;
    input.setSelectionRange(pos, pos);
    autosizeComposer();
    if (!isMobileIm()) input.focus();
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

function bindDragDrop() {
    const stage = document.getElementById("convWork");
    const mask = document.getElementById("dropMask");
    if (!stage) return;
    let dragCount = 0;
    const show = on => {
        mask?.classList.toggle("hidden", !on);
    };
    stage.addEventListener("dragenter", e => {
        e.preventDefault();
        dragCount++;
        show(true);
    });
    stage.addEventListener("dragover", e => e.preventDefault());
    stage.addEventListener("dragleave", e => {
        e.preventDefault();
        dragCount = Math.max(0, dragCount - 1);
        if (!dragCount) show(false);
    });
    stage.addEventListener("drop", e => {
        e.preventDefault();
        dragCount = 0;
        show(false);
        const file = e.dataTransfer?.files?.[0];
        if (file) {
            showToast("Загрузка файла...");
            uploadAndSend(file);
        }
    });
}

function bindUi() {
    fillEmojiPicker();
    const picker = document.getElementById("emojiPicker");
    if (picker) picker.setAttribute("aria-hidden", "true");
    syncViewportHeight();
    window.visualViewport?.addEventListener("resize", syncViewportHeight);
    window.addEventListener("resize", () => {
        syncViewportHeight();
        if (!isMobileIm()) setImPane("chat");
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
    document.getElementById("scrollJump")?.addEventListener("click", () => scrollBottom?.());
    document.getElementById("quoteCancel")?.addEventListener("click", () => clearQuote?.());
    document.getElementById("tabConversations")?.addEventListener("click", () => setNavTab?.("conversations"));
    document.getElementById("tabContacts")?.addEventListener("click", () => setNavTab?.("contacts"));
    document.getElementById("toggleConvSearch")?.addEventListener("click", () => {
        const wrap = document.getElementById("convSearchWrap");
        wrap?.classList.toggle("hidden");
        if (!wrap?.classList.contains("hidden"))
            document.getElementById("convFilter")?.focus();
    });
    document.getElementById("convFilter")?.addEventListener("input", () => applyConvFilter?.());
    document.getElementById("navMenuBtn")?.addEventListener("click", e => {
        e.stopPropagation();
        document.getElementById("navMenu")?.classList.toggle("hidden");
    });
    document.getElementById("logoutMenuBtn")?.addEventListener("click", () => logout());
    bindDragDrop();

    document.addEventListener("click", e => {
        const t = e.target instanceof Element ? e.target : e.target.parentElement;
        if (!t) return;

        if (!t.closest("#navMenu") && !t.closest("#navMenuBtn"))
            document.getElementById("navMenu")?.classList.add("hidden");

        if (t.closest("#emojiGrid button")) {
            e.preventDefault();
            insertEmoji(t.closest("button").textContent);
            return;
        }

        if (t.closest("#menuBtn")) {
            setConversationOpen?.(false);
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

    document.addEventListener("keydown", e => {
        if (e.key !== "Escape") return;
        if (window.DatChat.quote) {
            clearQuote?.();
            return;
        }
        if (isMobileIm() && document.getElementById("app")?.classList.contains("im-chat"))
            setConversationOpen?.(false);
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

    let typingTimer;
    field()?.addEventListener("input", () => {
        autosizeComposer();
        const chatId = activeChatId();
        if (!chatId || !isHubConnected()) return;
        window.connection.invoke("SetChatAction", chatId, "typing").catch(() => {});
        clearTimeout(typingTimer);
        typingTimer = setTimeout(() => {
            window.connection.invoke("SetChatAction", chatId, "cancel").catch(() => {});
        }, 1600);
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
window.setImPane = setImPane;
window.isMobileIm = isMobileIm;
window.focusComposer = focusComposer;
window.autosizeComposer = autosizeComposer;
