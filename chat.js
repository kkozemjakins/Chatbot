async function sendMessage() {
    const input = document.getElementById("messageInput");
    const hero = document.getElementById("hero-section");
    const chatBox = document.getElementById("chatBox");
    const message = input.value;

    if (message.trim() === "") return;

    if (!hero.classList.contains('hidden')) {
        hero.classList.add('hidden');
        chatBox.classList.remove('hidden');
    }

    chatBox.innerHTML += `
        <div class="message-row user-row">
            <div class="bubble">${message}</div>
            <div class="icon">👤</div>
        </div>`;

    input.value = "";
    chatBox.scrollTop = chatBox.scrollHeight;

    try {
        // Change to 'api/chat' (no leading slash) or the full localhost URL
        const response = await fetch("api/chat", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ message: message })
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Server Error: ${response.status} - ${errorText}`);
        }

        const data = await response.json();

        chatBox.innerHTML += `
            <div class="message-row bot-row">
                <div class="icon">✨</div>
                <div class="bubble">
                    ${data.reply}
                    <span class="emotion-tag">Emotion detected: ${data.emotion}</span>
                </div>
            </div>`;

    } catch (e) {
        console.error("Connection Error:", e);
        chatBox.innerHTML += `<p style='color:red; text-align:center;'>System Error: Could not connect to the server.</p>`;
    }

    chatBox.scrollTop = chatBox.scrollHeight;
}