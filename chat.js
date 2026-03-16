const sessionId = "session_" + Date.now() + "_" + Math.random().toString(36).substr(2, 9);

async function sendMessage() {
    sendBtn.disabled = true;

    const input = document.getElementById("messageInput");
    const hero = document.getElementById("hero-section");
    const chatBox = document.getElementById("chatBox");
    const message = input.value;

    if (message.trim() === "") return;

    if (!hero.classList.contains('hidden-smooth')) {
        hero.classList.add('hidden-smooth');
        chatBox.classList.remove('hidden');

        setTimeout(() => {
            chatBox.classList.add("show");
            document.querySelector(".ChatWrapper").classList.add("transition");
        }, 50);
    }

    // User MSG
    chatBox.innerHTML += `
        <div class="message-row user-row">
            <div class="bubble">${message}</div>
            <div class="icon">
                <img src='assets/ic-profile-pic.svg' alt='User Icon' class='user-icon'>
            </div>
        </div>`;

    input.value = "";
    chatBox.scrollTop = chatBox.scrollHeight;

    // Typing indicator
    const typingId = "typing_" + Date.now();

    chatBox.innerHTML += `
<div id="${typingId}" class="message-row bot-row">
    <div class="icon">
        <img src='assets/ic-ai-bot.svg' class='bot-icon'>
    </div>
    <div class="bubble typing">
        <span></span>
        <span></span>
        <span></span>
    </div>
</div>`;

    try {
        const response = await fetch("api/chat", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                message: message,
                sessionId: sessionId
            })
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Server Error: ${response.status} - ${errorText}`);
        }

        const data = await response.json();

        // Bot MSG
        const typingBubble = document.getElementById(typingId);
        typingBubble.remove();

        chatBox.innerHTML += `
        <div class="message-row bot-row">
            <div class="icon">
                <img src='assets/ic-ai-bot.svg' class='bot-icon'>
            </div>
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


const inputField = document.getElementById("messageInput");
const sendBtn = document.getElementById("sendBtn");

inputField.addEventListener("input", () => {
    if (inputField.value.trim() === "") {
        sendBtn.disabled = true;
    } else {
        sendBtn.disabled = false;
    }
});