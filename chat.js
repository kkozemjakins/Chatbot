window.useChip = function (text) {
    const inputField = document.getElementById("messageInput");
    const sendBtn = document.getElementById("sendBtn");

    if (inputField && sendBtn) {
        inputField.value = text;
        inputField.style.height = "auto";
        inputField.style.height = (inputField.scrollHeight) + "px";
        sendBtn.disabled = false;
        window.sendMessage();
    }
};



const sessionId = "session_" + Date.now() + "_" + Math.random().toString(36).substr(2, 9);
const inputField = document.getElementById("messageInput");
const sendBtn = document.getElementById("sendBtn");
const hero = document.getElementById("hero-section");
const chips = document.getElementById("suggestion-chips");
const chatBox = document.getElementById("chatBox");

async function sendMessage() {
    const message = inputField.value.trim();
    if (message === "" || sendBtn.disabled && message !== "") return;


    sendBtn.disabled = true;


    if (!hero.classList.contains('hidden-smooth')) {
        hero.classList.add('hidden-smooth');
        chips.classList.add('hidden-chips');
        chatBox.classList.remove('hidden');

        setTimeout(() => {
            chatBox.classList.add("show");
            document.querySelector(".ChatWrapper").classList.add("transition");
        }, 50);
    }

    // Render User Message
    chatBox.innerHTML += `
        <div class="message-row user-row">
            <div class="bubble">${message}</div>
            <div class="icon">
                <img src='assets/ic-profile-pic.svg' alt='User Icon' class='user-icon'>
            </div>
        </div>`;

    // Clear input and reset height
    inputField.value = "";
    inputField.style.height = "auto";
    chatBox.scrollTop = chatBox.scrollHeight;

    // Show Typing Indicator
    const typingId = "typing_" + Date.now();
    chatBox.innerHTML += `
        <div id="${typingId}" class="message-row bot-row">
            <div class="icon">
                <img src='assets/ic-ai-bot.svg' class='bot-icon'>
            </div>
            <div class="bubble typing">
                <span></span><span></span><span></span>
            </div>
        </div>`;
    chatBox.scrollTop = chatBox.scrollHeight;

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
            throw new Error(`Server Error: ${response.status}`);
        }

        const data = await response.json();

        // Remove Typing Indicator
        const typingBubble = document.getElementById(typingId);
        if (typingBubble) typingBubble.remove();


        chatBox.innerHTML += `
            <div class="message-row bot-row">
                <div class="icon">
                    <img src='assets/ic-ai-bot.svg' class='bot-icon'>
                </div>
                <div class="bubble">
                    ${data.reply}
                </div>
            </div>`;

    } catch (e) {
        console.error("Connection Error:", e);
        const typingBubble = document.getElementById(typingId);
        if (typingBubble) typingBubble.remove();

        chatBox.innerHTML += `<p style='color:red; text-align:center; font-size: 12px;'>System Error: Could not connect to the server.</p>`;
    }

    chatBox.scrollTop = chatBox.scrollHeight;
}


// Handle Send Button state
inputField.addEventListener("input", () => {
    sendBtn.disabled = inputField.value.trim() === "";
});


inputField.addEventListener("keydown", (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

// Auto-expanding textarea
inputField.addEventListener("input", function () {
    this.style.height = "auto";
    this.style.height = (this.scrollHeight) + "px";
});