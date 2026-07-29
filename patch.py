import sys

# Читаем файл
with open('wwwroot/index.html', 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Добавляем панель закреплённого сообщения ПЕРЕД лентой сообщений
old_html = '<div class="messages" id="messages"></div>'
new_html = '''<div id="pinned-bar" style="display: none; background: rgba(45, 80, 22, 0.15); padding: 10px 15px; border-bottom: 2px solid #2d5016; flex-shrink: 0;">
            <div style="font-size: 12px; color: #2d5016; margin-bottom: 4px; font-weight: bold;">
                📌 Закреплено <span id="pinned-by"></span> в <span id="pinned-at"></span>
            </div>
            <div style="font-size: 14px; color: #333;">
                <strong id="pinned-nick"></strong>: <span id="pinned-text"></span>
            </div>
        </div>
        <div class="messages" id="messages"></div>'''

if old_html in content:
    content = content.replace(old_html, new_html)
    print("✅ HTML панель добавлена")
else:
    print("⚠️ HTML шаблон не найден (возможно, уже изменён)")

# 2. Добавляем обработчики событий SignalR для закреплённых сообщений
old_js = 'connection.on("LoadHistory", (messages) => { messages.forEach(msg => addMessage(msg)); });'
new_js = '''connection.on("LoadHistory", (messages) => { messages.forEach(msg => addMessage(msg)); });
        connection.on("PinnedMessage", (msg) => {
            const bar = document.getElementById('pinned-bar');
            if (bar) {
                document.getElementById('pinned-by').textContent = msg.PinnedBy || 'Пользователь';
                document.getElementById('pinned-at').textContent = msg.PinnedAt || '';
                document.getElementById('pinned-nick').textContent = msg.Nickname || '';
                document.getElementById('pinned-text').textContent = msg.Text || '';
                bar.style.display = 'block';
            }
            addSystemMessage("📌 Закреплено: " + (msg.Text ? msg.Text.substring(0, 40) + '...' : ''));
        });
        connection.on("MessageUnpinned", () => {
            const bar = document.getElementById('pinned-bar');
            if (bar) bar.style.display = 'none';
            addSystemMessage("📌 Сообщение откреплено");
        });'''

if old_js in content:
    content = content.replace(old_js, new_js)
    print("✅ JS обработчики добавлены")
else:
    print("⚠️ JS шаблон не найден")

# Сохраняем файл
with open('wwwroot/index.html', 'w', encoding='utf-8') as f:
    f.write(content)

print("🎉 Файл wwwroot/index.html успешно обновлён!")
