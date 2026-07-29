import re

print("🔧 Читаем файл wwwroot/index.html...")
with open('wwwroot/index.html', 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Обновляем панель ввода (добавляем скрепку и смайлик)
new_input_area = '''<div class="input-area">
            <button class="attach-btn" onclick="document.getElementById('fileInput').click()" title="Прикрепить файл">📎</button>
            <input type="text" class="message-input" id="messageInput" placeholder="Сообщение..." onkeypress="if(event.key==='Enter')sendMessage()" autocomplete="off">
            <button class="emoji-btn" onclick="toggleEmoji()" title="Эмодзи">😊</button>
            <button class="send-btn" onclick="sendMessage()" title="Отправить"></button>
            <input type="file" id="fileInput" style="display: none;" onchange="uploadFile(this.files[0])" accept="image/*,.pdf,.doc,.docx,.txt">
        </div>'''

pattern = re.compile(r'<div class="input-area[^"]*">.*?</div>', re.DOTALL)
if pattern.search(content):
    content = pattern.sub(new_input_area, content)
    print("✅ 1. Панель ввода (скрепка и смайлик) восстановлена!")
else:
    print("⚠️ 1. Не удалось найти блок input-area.")

# 2. Добавляем HTML панели эмодзи (если её нет)
emoji_html = '''
    <div class="emoji-picker" id="emojiPicker" style="display: none;">
        <span onclick="addEmoji('😀')">😀</span><span onclick="addEmoji('')">😃</span><span onclick="addEmoji('😄')">😄</span>
        <span onclick="addEmoji('😁')">😁</span><span onclick="addEmoji('😆')"></span><span onclick="addEmoji('😅')">😅</span>
        <span onclick="addEmoji('🤣')">🤣</span><span onclick="addEmoji('😊')">😊</span><span onclick="addEmoji('🙂')">🙂</span>
        <span onclick="addEmoji('😉')">😉</span><span onclick="addEmoji('😍')">😍</span><span onclick="addEmoji('🥰')">🥰</span>
        <span onclick="addEmoji('')">😘</span><span onclick="addEmoji('😗')">😗</span><span onclick="addEmoji('😚')">😚</span>
        <span onclick="addEmoji('😙')">😙</span><span onclick="addEmoji('😋')">😋</span><span onclick="addEmoji('😛')">😛</span>
        <span onclick="addEmoji('😜')"></span><span onclick="addEmoji('')">🤪</span><span onclick="addEmoji('😝')">😝</span>
        <span onclick="addEmoji('🤗')"></span><span onclick="addEmoji('')">🤭</span><span onclick="addEmoji('🤫')">🤫</span>
        <span onclick="addEmoji('🤔')">🤔</span><span onclick="addEmoji('🤐')">🤐</span><span onclick="addEmoji('🤨')">🤨</span>
        <span onclick="addEmoji('😐')">😐</span><span onclick="addEmoji('😶')">😶</span><span onclick="addEmoji('😏')"></span>
        <span onclick="addEmoji('😒')">😒</span><span onclick="addEmoji('🙄')">🙄</span><span onclick="addEmoji('🤥')"></span>
        <span onclick="addEmoji('😌')">😌</span><span onclick="addEmoji('😔')">😔</span><span onclick="addEmoji('😪')">😪</span>
        <span onclick="addEmoji('')">🤤</span><span onclick="addEmoji('😴')">😴</span><span onclick="addEmoji('😷')">😷</span>
        <span onclick="addEmoji('')">🤒</span><span onclick="addEmoji('🤕')">🤕</span><span onclick="addEmoji('🤢')"></span>
        <span onclick="addEmoji('🤮')">🤮</span><span onclick="addEmoji('🤧')"></span><span onclick="addEmoji('😵')">😵</span>
        <span onclick="addEmoji('🤯')">🤯</span><span onclick="addEmoji('🤠')"></span><span onclick="addEmoji('')">🥳</span>
        <span onclick="addEmoji('🤓')"></span><span onclick="addEmoji('')">🧐</span><span onclick="addEmoji('😕')">😕</span>
        <span onclick="addEmoji('😟')">😟</span><span onclick="addEmoji('')">🙁</span><span onclick="addEmoji('😯')">😯</span>
        <span onclick="addEmoji('😲')"></span><span onclick="addEmoji('')">🥺</span><span onclick="addEmoji('😦')">😦</span>
        <span onclick="addEmoji('😧')"></span><span onclick="addEmoji('😨')">😨</span><span onclick="addEmoji('😰')">😰</span>
        <span onclick="addEmoji('😥')">😥</span><span onclick="addEmoji('😢')">😢</span><span onclick="addEmoji('😭')">😭</span>
        <span onclick="addEmoji('😱')">😱</span><span onclick="addEmoji('😖')">😖</span><span onclick="addEmoji('😣')">😣</span>
        <span onclick="addEmoji('')">😞</span><span onclick="addEmoji('😓')">😓</span><span onclick="addEmoji('😩')">😩</span>
        <span onclick="addEmoji('😫')"></span><span onclick="addEmoji('')">🥱</span><span onclick="addEmoji('😤')">😤</span>
        <span onclick="addEmoji('😡')">😡</span><span onclick="addEmoji('')">😠</span><span onclick="addEmoji('😈')">😈</span>
        <span onclick="addEmoji('👿')"></span><span onclick="addEmoji('💀')">💀</span><span onclick="addEmoji('☠️')">☠️</span>
        <span onclick="addEmoji('💩')"></span><span onclick="addEmoji('')">👹</span><span onclick="addEmoji('👺')">👺</span>
        <span onclick="addEmoji('❤️')">❤️</span><span onclick="addEmoji('')">💛</span><span onclick="addEmoji('💚')">💚</span>
        <span onclick="addEmoji('💙')">💙</span><span onclick="addEmoji('💜')">💜</span><span onclick="addEmoji('🖤')">🖤</span>
        <span onclick="addEmoji('🤍')">🤍</span><span onclick="addEmoji('💔')">💔</span><span onclick="addEmoji('❣️')">❣️</span>
        <span onclick="addEmoji('💕')">💕</span><span onclick="addEmoji('')">💞</span><span onclick="addEmoji('💓')">💓</span>
        <span onclick="addEmoji('💗')"></span><span onclick="addEmoji('💖')">💖</span><span onclick="addEmoji('💘')">💘</span>
        <span onclick="addEmoji('💝')">💝</span><span onclick="addEmoji('👍')">👍</span><span onclick="addEmoji('👎')">👎</span>
        <span onclick="addEmoji('✌️')">✌️</span><span onclick="addEmoji('🤞')"></span><span onclick="addEmoji('')">🤙</span>
        <span onclick="addEmoji('👈')">👈</span><span onclick="addEmoji('👆')">👆</span><span onclick="addEmoji('👇')">👇</span>
        <span onclick="addEmoji('👏')">👏</span><span onclick="addEmoji('🙌')">🙌</span><span onclick="addEmoji('👐')">👐</span>
        <span onclick="addEmoji('🤝')">🤝</span><span onclick="addEmoji('🙏')">🙏</span><span onclick="addEmoji('✍️')">️</span>
        <span onclick="addEmoji('💪')">💪</span>
    </div>
'''
if 'id="emojiPicker"' not in content:
    content = content.replace('</body>', emoji_html + '\n</body>')
    print("✅ 2. HTML панель эмодзи добавлена!")
else:
    print("ℹ️ 2. Панель эмодзи уже есть.")

# 3. Добавляем CSS стили для эмодзи
css_styles = '''
        .emoji-picker { 
            position: absolute; bottom: 80px; right: 60px; width: 320px; height: 200px; 
            background: rgba(255, 255, 255, 0.95); backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.5); border-radius: 16px; padding: 10px; 
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1); overflow-y: auto; z-index: 1000; 
            display: none; grid-template-columns: repeat(8, 1fr); gap: 5px; 
        }
        .emoji-picker span { font-size: 24px; cursor: pointer; padding: 5px; text-align: center; border-radius: 8px; }
        .emoji-picker span:hover { background: #f0f0f0; transform: scale(1.2); }
'''
if '.emoji-picker' not in content:
    content = content.replace('</style>', css_styles + '\n    </style>')
    print("✅ 3. CSS стили для эмодзи добавлены!")
else:
    print("ℹ️ 3. CSS стили уже есть.")

# 4. Добавляем JavaScript функции
js_functions = '''
        function toggleEmoji() {
            const picker = document.getElementById('emojiPicker');
            if (picker) picker.style.display = picker.style.display === 'none' || picker.style.display === '' ? 'grid' : 'none';
        }
        function addEmoji(emoji) {
            const input = document.getElementById('messageInput');
            if (input) { input.value += emoji; input.focus(); }
            const picker = document.getElementById('emojiPicker');
            if (picker) picker.style.display = 'none';
        }
        async function uploadFile(file) {
            if (!file) return;
            if (file.size > 10 * 1024 * 1024) { alert("Файл слишком большой! Максимум 10MB"); return; }
            try {
                const formData = new FormData();
                formData.append("file", file);
                const response = await fetch("/upload", { method: "POST", body: formData });
                if (response.ok) {
                    const data = await response.json();
                    await connection.invoke("SendFile", file.name, data.fileUrl, data.fileType);
                } else { alert("Ошибка загрузки файла"); }
            } catch (err) { console.error("Ошибка загрузки:", err); alert("Не удалось загрузить файл"); }
        }
        document.addEventListener('click', function(e) {
            const picker = document.getElementById('emojiPicker');
            const btn = document.querySelector('.emoji-btn');
            if (picker && btn && !picker.contains(e.target) && !btn.contains(e.target)) {
                picker.style.display = 'none';
            }
        });
'''
if 'function toggleEmoji()' not in content:
    content = content.replace('</script>', js_functions + '\n    </script>')
    print("✅ 4. JavaScript функции добавлены!")
else:
    print("ℹ️ 4. JavaScript функции уже есть.")

# Сохраняем файл
with open('wwwroot/index.html', 'w', encoding='utf-8') as f:
    f.write(content)

print("\n🎉 ГОТОВО! Файл успешно обновлен.")
