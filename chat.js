async function sendMessage() {

    const input = document.getElementById("messageInput");
    const message = input.value;

    if (message.trim() === "") return;

    const chatBox = document.getElementById("chatBox");

    // show user message
    chatBox.innerHTML += "<p class='user'><b>You:</b> " + message + "</p>";

    input.value = "";



    // send message to API
    const response = await fetch("/api/chat", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            message: message
        })
    });

    const text = await response.text();
    console.log(text);

    const data = JSON.parse(text);


    // show bot reply
    chatBox.innerHTML +=
        "<p class='bot'><b>Bot:</b> " + data.reply +
        "<br><i>Emotion detected: " + data.emotion + "</i></p>";


    chatBox.scrollTop = chatBox.scrollHeight;
}
