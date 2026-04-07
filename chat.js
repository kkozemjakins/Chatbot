// 1. Session Variables
let sessionId = "";
const SESSION_KEY = "psych_chat_session";

// 2. Chip Handler
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

// 3. UI Transition Logic
function transitionChatUI() {
    const hero = document.getElementById("hero-section");
    const chips = document.getElementById("suggestion-chips");
    const chatBox = document.getElementById("chatBox");

    if (!hero.classList.contains('hidden-smooth')) {
        hero.classList.add('hidden-smooth');
        chips.classList.add('hidden-chips');
        chatBox.classList.remove('hidden');

        setTimeout(() => {
            chatBox.classList.add("show");
            document.querySelector(".ChatWrapper").classList.add("transition");
        }, 50);
    }
}

// 4. Send Message Logic
window.sendMessage = async function () {
    const inputField = document.getElementById("messageInput");
    const sendBtn = document.getElementById("sendBtn");
    const chatBox = document.getElementById("chatBox");

    const message = inputField.value.trim();
    if (message === "" || (sendBtn.disabled && message !== "")) return;

    sendBtn.disabled = true;

    // Transition UI if first message
    transitionChatUI();

    // Render User Message
    chatBox.innerHTML += `
        <div class="message-row user-row">
            <div class="bubble">${message}</div>
            <div class="icon">
                <img src='assets/ic-profile-pic.svg' alt='User Icon' class='user-icon'>
            </div>
        </div>`;

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
            body: JSON.stringify({ message: message, sessionId: sessionId })
        });

        if (!response.ok) throw new Error(`Server Error: ${response.status}`);
        const data = await response.json();

        // Remove Typing Indicator
        const typingBubble = document.getElementById(typingId);
        if (typingBubble) typingBubble.remove();

        chatBox.innerHTML += `
            <div class="message-row bot-row">
                <div class="icon">
                    <img src='assets/ic-ai-bot.svg' class='bot-icon'>
                </div>
                <div class="bubble">${data.reply}</div>
            </div>`;

    } catch (e) {
        console.error("Connection Error:", e);
        const typingBubble = document.getElementById(typingId);
        if (typingBubble) typingBubble.remove();
        chatBox.innerHTML += `<p style='color:red; text-align:center; font-size: 12px;'>System Error: Could not connect to the server.</p>`;
    }

    chatBox.scrollTop = chatBox.scrollHeight;
};

// 5. Session Management Functions
window.startNewSession = function () {
    // Generate new ID
    sessionId = "session_" + Date.now() + "_" + Math.random().toString(36).substr(2, 9);
    localStorage.setItem(SESSION_KEY, sessionId);

    // Clear the chat box UI in case there's old stuff there
    const chatBox = document.getElementById("chatBox");
    if (chatBox) chatBox.innerHTML = "";

    // Hide the modal
    document.getElementById("sessionModal").classList.add("hidden");
};

window.continueSession = async function () {
    document.getElementById("sessionModal").classList.add("hidden");
    transitionChatUI(); // Hide hero and show chat area immediately

    const chatBox = document.getElementById("chatBox");
    chatBox.innerHTML = "<p style='text-align:center; color:#888;'>Loading your conversation...</p>";

    try {
        const response = await fetch(`api/chat/history/${sessionId}`);
        if (!response.ok) throw new Error("Could not fetch history");

        const history = await response.json();
        chatBox.innerHTML = ""; // Clear loading text

        if (history.length === 0) {
            chatBox.innerHTML = "<p style='text-align:center; color:#888;'>No previous messages found.</p>";
            return;
        }

        // Re-render past messages
        history.forEach(msg => {
            if (msg.Role === "user") {
                chatBox.innerHTML += `
                    <div class="message-row user-row">
                        <div class="bubble">${msg.Text}</div>
                        <div class="icon">
                            <img src='assets/ic-profile-pic.svg' alt='User Icon' class='user-icon'>
                        </div>
                    </div>`;
            } else {
                chatBox.innerHTML += `
                    <div class="message-row bot-row">
                        <div class="icon">
                            <img src='assets/ic-ai-bot.svg' class='bot-icon'>
                        </div>
                        <div class="bubble">${msg.Text}</div>
                    </div>`;
            }
        });

        chatBox.scrollTop = chatBox.scrollHeight;

    } catch (e) {
        console.error("Error loading history:", e);
        chatBox.innerHTML = "<p style='text-align:center; color:red;'>Error loading conversation history.</p>";
    }
};

// 6. Init on Page Load
document.addEventListener("DOMContentLoaded", () => {
    const inputField = document.getElementById("messageInput");
    const sendBtn = document.getElementById("sendBtn");

    // Check Local Storage for Existing Session
    const savedSession = localStorage.getItem(SESSION_KEY);

    if (savedSession) {
        sessionId = savedSession;
        // Prompt user to continue or start fresh
        document.getElementById("sessionModal").classList.remove("hidden");
    } else {
        // No previous session, generate a new one quietly
        window.startNewSession();
    }

    // Input listeners
    if (inputField) {
        inputField.addEventListener("input", () => {
            sendBtn.disabled = inputField.value.trim() === "";
            inputField.style.height = "auto";
            inputField.style.height = (inputField.scrollHeight) + "px";
        });

        inputField.addEventListener("keydown", (e) => {
            if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                window.sendMessage();
            }
        });
    }
});