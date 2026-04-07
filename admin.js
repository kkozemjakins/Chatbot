let authToken = localStorage.getItem('adminToken');

// On load, check for have a token
window.onload = () => {
    if (authToken) {
        showAdminView();
        loadPrompt();
    }
};

async function login() {
    const user = document.getElementById('username').value;
    const pass = document.getElementById('password').value;

    const res = await fetch('api/admin/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: user, password: pass })
    });

    if (res.ok) {
        const data = await res.json();
        authToken = data.token;
        localStorage.setItem('adminToken', authToken);
        document.getElementById('loginError').classList.add('hidden');
        showAdminView();
        loadPrompt();
    } else {
        document.getElementById('loginError').classList.remove('hidden');
    }
}

async function loadPrompt() {
    const res = await fetch('api/admin/prompt', {
        headers: { 'Authorization': `Bearer ${authToken}` }
    });

    if (res.ok) {
        const data = await res.json();
        document.getElementById('promptText').value = data.prompt;
    } else {
        logout(); // Token expired or server restarted
    }
}

async function savePrompt() {
    const newPrompt = document.getElementById('promptText').value;
    const btn = document.querySelector('#adminView button');
    btn.disabled = true;

    const res = await fetch('api/admin/prompt', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${authToken}`
        },
        body: JSON.stringify({ prompt: newPrompt })
    });

    if (res.ok) {
        document.getElementById('status').innerText = "Prompt saved successfully!";
        setTimeout(() => document.getElementById('status').innerText = "", 3000);
    } else {
        alert("Failed to save. Please try logging in again.");
    }

    btn.disabled = false;
}

function showAdminView() {
    document.getElementById('loginView').classList.add('hidden');
    document.getElementById('adminView').classList.remove('hidden');
}

function logout() {
    authToken = null;
    localStorage.removeItem('adminToken');
    document.getElementById('loginView').classList.remove('hidden');
    document.getElementById('adminView').classList.add('hidden');
    document.getElementById('username').value = '';
    document.getElementById('password').value = '';
}