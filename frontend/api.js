const BASE_URL = 'http://localhost:5033/api';

const API = {
  async get(path) {
    const r = await fetch(`${BASE_URL}${path}`);
    if (!r.ok) throw new Error(`GET ${path} → ${r.status}`);
    return r.json();
  },
  async post(path, body) {
    const r = await fetch(`${BASE_URL}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    const data = await r.json().catch(() => null);
    if (!r.ok) throw Object.assign(new Error(data?.message || `POST ${path} → ${r.status}`), { status: r.status, data });
    return data;
  },
  async put(path, body) {
    const r = await fetch(`${BASE_URL}${path}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    if (r.status === 204) return null;
    const data = await r.json().catch(() => null);
    if (!r.ok) throw Object.assign(new Error(data?.message || `PUT ${path} → ${r.status}`), { status: r.status, data });
    return data;
  },
  async patch(path, body) {
    const r = await fetch(`${BASE_URL}${path}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    if (r.status === 204) return null;
    const data = await r.json().catch(() => null);
    if (!r.ok) throw Object.assign(new Error(data?.message || `PATCH ${path} → ${r.status}`), { status: r.status, data });
    return data;
  },
  async del(path) {
    const r = await fetch(`${BASE_URL}${path}`, { method: 'DELETE' });
    if (r.status === 204) return null;
    const data = await r.json().catch(() => null);
    if (!r.ok) throw Object.assign(new Error(data?.message || `DELETE ${path} → ${r.status}`), { status: r.status, data });
    return data;
  }
};

// Notification helper
function notify(msg, type = 'info') {
  let el = document.getElementById('__notify');
  if (!el) {
    el = document.createElement('div');
    el.id = '__notify';
    el.className = 'notification';
    document.body.appendChild(el);
  }
  el.className = `notification ${type}`;
  el.innerHTML = `<span>${type === 'success' ? '✓' : type === 'error' ? '✗' : 'ℹ'}</span> ${msg}`;
  el.classList.add('show');
  clearTimeout(el._t);
  el._t = setTimeout(() => el.classList.remove('show'), 3500);
}

// Format helpers
function fmt(n) {
  return Number(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function fmtDate(d) {
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}
function fmtDateTime(d) {
  return new Date(d).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

// Modal helpers
function openModal(id) {
  document.getElementById(id).classList.add('open');
}
function closeModal(id) {
  document.getElementById(id).classList.remove('open');
}

// Set active nav link
(function() {
  const page = location.pathname.split('/').pop() || 'index.html';
  document.querySelectorAll('.nav-link').forEach(a => {
    if (a.getAttribute('href') === page) a.classList.add('active');
    else a.classList.remove('active');
  });
})();
