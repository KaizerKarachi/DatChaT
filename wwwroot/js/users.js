// ===============================
// DatChaT v2
// users.js
// ===============================

const usersContainer = document.getElementById("usersList");
const onlineCounter = document.getElementById("userCount");

// ---------------------
// Отрисовка пользователей
// ---------------------

function renderUsers(users) {

    if (!usersContainer)
        return;

    usersContainer.innerHTML = "";

    if (!users)
        users = [];

    if (onlineCounter)
        onlineCounter.textContent = users.length;

    users.forEach(user => {

        const name =
            user.nickname ||
            user.Nickname ||
            user;

        const div =
            document.createElement("div");

        div.className = "user-item";

        div.innerHTML = `
            <div class="user-avatar">
                👤
            </div>

            <div class="user-info">
                <div class="user-name">
                    ${name}
                </div>

                <div class="user-status">
                    🟢 Онлайн
                </div>
            </div>
        `;

        usersContainer.appendChild(div);

    });

}

// ---------------------
// Очистить список
// ---------------------

function clearUsers() {

    if (usersContainer)
        usersContainer.innerHTML = "";

    if (onlineCounter)
        onlineCounter.textContent = "0";

}
