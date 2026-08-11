// ===============================
// DatChaT v2
// pinned.js
// ===============================

const pinnedContainer =
    document.getElementById("pinnedText");

// ---------------------
// Показать закреп
// ---------------------

function renderPinned(msg) {

    if (!pinnedContainer)
        return;

    if (!msg) {

        clearPinned();

        return;

    }

    const nickname =
        msg.nickname ||
        msg.Nickname ||
        "";

    const text =
        msg.text ||
        msg.Text ||
        "";

    pinnedContainer.innerHTML = `

        <div class="pinned-card">

            <div class="pinned-title">

                📌 Закреплённое сообщение

            </div>

            <div class="pinned-author">

                ${nickname}

            </div>

            <div class="pinned-text">

                ${text}

            </div>

        </div>

    `;

}

// ---------------------
// Очистить закреп
// ---------------------

function clearPinned() {

    if (!pinnedContainer)
        return;

    pinnedContainer.innerHTML = `

        <div class="pinned-empty">

            📌 Нет закреплённого сообщения

        </div>

    `;

}

// При старте сразу показываем заглушку
clearPinned();
