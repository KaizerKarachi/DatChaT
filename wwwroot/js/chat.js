// ===============================
// DatChaT — chat.js
// ===============================

const messagesBox = document.getElementById("messages");
const messageInput = document.getElementById("messageInput");

function clearMessages() {
    if (messagesBox) {
        messagesBox.innerHTML = "";
    }
}

function scrollBottom() {
    if (!messagesBox) return;

    messagesBox.scrollTop = messagesBox.scrollHeight;
}

function getMessageId(msg) {
    return msg?.id ?? msg?.Id;
}

function getNickname(msg) {
    return msg?.nickname ?? msg?.Nickname ?? "";
}

function getText(msg) {
    return msg?.text ?? msg?.Text ?? "";
}

function getTime(msg) {
    return msg?.timestamp ??
           msg?.Timestamp ??
           msg?.time ??
           msg?.Time ??
           "";
}

function formatMessageTime(value) {

    if (!value) return "";

    // Сервер уже прислал HH:mm
    if (
        typeof value === "string" &&
        /^\d{2}:\d{2}$/.test(value)
    ) {
        return value;
    }

    const date = new Date(value);

    if (isNaN(date.getTime())) {
        return String(value);
    }

    return date.toLocaleTimeString([], {
        hour: "2-digit",
        minute: "2-digit"
    });
}


// ===============================
// Меню сообщения
// ===============================

function closeMessageMenus() {

    document
        .querySelectorAll(".message-menu")
        .forEach(menu => menu.remove());

}


function createMessageMenu(div, id) {

    const menu = document.createElement("div");

    menu.className = "message-menu";

    const pinBtn = document.createElement("button");

    pinBtn.type = "button";
    pinBtn.textContent = "📌 Закрепить";

    pinBtn.addEventListener("click", async e => {

        e.stopPropagation();

        try {

            if (
                !window.connection ||
                window.connection.state !== "Connected"
            ) {
                showToast?.("Нет соединения с сервером");
                return;
            }

            await window.connection.invoke(
                "PinMessage",
                Number(id)
            );

            closeMessageMenus();

        } catch (err) {

            console.error("Ошибка закрепления:", err);
            showToast?.("Не удалось закрепить сообщение");

        }

    });


    const deleteBtn = document.createElement("button");

    deleteBtn.type = "button";
    deleteBtn.textContent = "🗑 Удалить";

    deleteBtn.addEventListener("click", async e => {

        e.stopPropagation();

        try {

            if (
                !window.connection ||
                window.connection.state !== "Connected"
            ) {
                showToast?.("Нет соединения с сервером");
                return;
            }

            await window.connection.invoke(
                "DeleteMessage",
                Number(id)
            );

            closeMessageMenus();

        } catch (err) {

            console.error("Ошибка удаления:", err);
            showToast?.("Не удалось удалить сообщение");

        }

    });


    menu.appendChild(pinBtn);
    menu.appendChild(deleteBtn);

    div.appendChild(menu);

    return menu;
}


// ===============================
// Отрисовать сообщение
// ===============================

function renderMessage(msg) {

    if (!messagesBox) return;

    const id = getMessageId(msg);

    if (id === undefined || id === null) {
        console.warn("Сообщение без ID:", msg);
        return;
    }

    // Защита от повторной отрисовки
    const old = document.getElementById("msg-" + id);

    if (old) {
        old.remove();
    }

    const nickname = getNickname(msg);
    const text = getText(msg);
    const time = getTime(msg);

    const mine =
        nickname === window.currentUser;

    const div = document.createElement("div");

    div.className =
        mine
            ? "message mine"
            : "message";

    div.id = "msg-" + id;


    // ---------------------------
    // Имя
    // ---------------------------

    const header =
        document.createElement("div");

    header.className =
        "message-header";

    header.textContent =
        nickname;


    // ---------------------------
    // Тело
    // ---------------------------

    const body =
        document.createElement("div");

    body.className =
        "message-body";


    const deleted =
        msg.IsDeleted ||
        msg.isDeleted;


    if (deleted) {

        body.textContent =
            "Сообщение удалено";

        body.classList.add(
            "deleted-message"
        );

    } else {

        const fileUrl =
            msg.FileUrl ||
            msg.fileUrl;

        const fileType =
            msg.FileType ||
            msg.fileType;


        if (fileUrl) {

            const link =
                document.createElement("a");

            link.href = fileUrl;
            link.target = "_blank";
            link.rel = "noopener";

            link.textContent =
                "📎 " + text;

            body.appendChild(link);

        } else {

            body.textContent =
                text;

        }

    }


    // ---------------------------
    // Время
    // ---------------------------

    const footer =
        document.createElement("div");

    footer.className =
        "message-footer";

    footer.textContent =
        formatMessageTime(time);


    // ---------------------------
    // Собираем
    // ---------------------------

    div.appendChild(header);
    div.appendChild(body);
    div.appendChild(footer);

    // ---------------------------
    // Меню
    // ПКМ
    // ---------------------------

    div.addEventListener(
        "contextmenu",
        e => {

            e.preventDefault();

            closeMessageMenus();

            createMessageMenu(
                div,
                id
            );

        }
    );


    // ---------------------------
    // Мобильный:
    // долгое нажатие
    // ---------------------------

    let pressTimer = null;

    div.addEventListener(
        "touchstart",
        () => {

            pressTimer =
                setTimeout(() => {

                    closeMessageMenus();

                    createMessageMenu(
                        div,
                        id
                    );

                }, 600);

        },
        { passive: true }
    );


    div.addEventListener(
        "touchend",
        () => {

            clearTimeout(
                pressTimer
            );

        }
    );


    div.addEventListener(
        "touchmove",
        () => {

            clearTimeout(
                pressTimer
            );

        }
    );


    messagesBox.appendChild(div);

    scrollBottom();
}


// ===============================
// Удаление сообщения из DOM
// ===============================

function removeMessage(id) {

    const realId =
        id?.Id ??
        id?.id ??
        id;

    if (
        realId === undefined ||
        realId === null
    ) {
        return;
    }

    const element =
        document.getElementById(
            "msg-" + realId
        );

    if (!element) {
        return;
    }


    // Сначала показываем
    // "Сообщение удалено"

    const body =
        element.querySelector(
            ".message-body"
        );

    if (body) {

        body.textContent =
            "Сообщение удалено";

        body.classList.add(
            "deleted-message"
        );

    }


    // Убираем меню

    element
        .querySelectorAll(
            ".message-menu"
        )
        .forEach(
            menu => menu.remove()
        );


    // Через 3 секунды удаляем
    // сообщение полностью

    setTimeout(() => {

        element.remove();

    }, 3000);

}


// ===============================
// Отправить сообщение
// ===============================

async function sendMessage() {

    if (!messageInput) return;

    const text =
        messageInput.value.trim();

    if (!text) return;

    if (
        !window.connection ||
        window.connection.state !== "Connected"
    ) {

        showToast?.(
            "Соединение с сервером не установлено"
        );

        return;

    }


    try {

        await window.connection.invoke(
            "SendMessage",
            text
        );

        messageInput.value = "";

        messageInput.focus();

    }
    catch (e) {

        console.error(
            "Ошибка отправки:",
            e
        );

        showToast?.(
            "Не удалось отправить сообщение"
        );

    }

}

document.addEventListener(
    "click",
    e => {

        if (
            !e.target.closest(
                ".message-menu"
            )
        ) {

            closeMessageMenus();

        }

    }
);
