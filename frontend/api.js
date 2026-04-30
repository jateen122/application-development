/* ═══════════════════════════════════════════════════════
   API Helper — connects to http://localhost:5033
   ═══════════════════════════════════════════════════════ */

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

// ── Notification helper ───────────────────────────────────────
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

// ── Format helpers ────────────────────────────────────────────
function fmt(n) {
  return Number(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function fmtDate(d) {
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}
function fmtDateTime(d) {
  return new Date(d).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

// ── Modal helpers ─────────────────────────────────────────────
function openModal(id) {
  document.getElementById(id).classList.add('open');
}
function closeModal(id) {
  document.getElementById(id).classList.remove('open');
}

// ── Auth helpers ──────────────────────────────────────────────
function getUser() {
  const raw = sessionStorage.getItem('user');
  return raw ? JSON.parse(raw) : null;
}

function logout() {
  sessionStorage.removeItem('user');
  window.location.href = 'login.html';
}

// ── Role-based access guard ───────────────────────────────────
// Call requireRole('Admin') or requireRole('Admin','Staff') at top of each page.
function requireRole(...allowedRoles) {
  const user = getUser();
  if (!user) {
    window.location.href = 'login.html';
    return null;
  }
  if (!allowedRoles.includes(user.role)) {
    // Wrong role — send to their correct home
    if (user.role === 'Customer') {
      window.location.href = 'portal-profile.html';
    } else {
      window.location.href = 'index.html';
    }
    return null;
  }
  return user;
}

// ── Navbar: inject user info + logout + hide restricted links ─
// Runs automatically on DOMContentLoaded for every admin/staff page.
(function initNavbar() {
  document.addEventListener('DOMContentLoaded', () => {
    const navbar = document.querySelector('.navbar');
    if (!navbar) return;                          // no navbar on this page (e.g. login)

    const user = getUser();

    // Not logged in → redirect
    if (!user) {
      window.location.href = 'login.html';
      return;
    }

    // Customer landed on an admin/staff page → send to portal
    if (user.role === 'Customer') {
      window.location.href = 'portal-profile.html';
      return;
    }

    // ── Inject user chip + logout into .nav-right ─────────────
    const navRight = navbar.querySelector('.nav-right');
    if (navRight) {
      // Build role badge colour
      const roleColor = user.role === 'Admin' ? 'var(--accent)' : 'var(--accent-2)';

      navRight.innerHTML = `
        <span class="status-dot"></span>
        <span class="status-text">Live</span>
        <span style="
          margin-left: 12px;
          padding: 4px 10px;
          background: ${user.role === 'Admin' ? 'var(--accent-dim)' : 'var(--accent-2-dim)'};
          border: 1px solid ${roleColor};
          border-radius: 20px;
          font-family: 'JetBrains Mono', monospace;
          font-size: 11px;
          color: ${roleColor};
          letter-spacing: 1px;
        ">${user.role.toUpperCase()}</span>
        <span style="
          margin-left: 8px;
          font-size: 13px;
          color: var(--text-2);
          font-weight: 500;
          max-width: 140px;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        ">${user.firstName} ${user.lastName}</span>
        <button onclick="logout()" style="
          margin-left: 10px;
          padding: 6px 14px;
          background: var(--danger-dim);
          border: 1px solid var(--danger);
          border-radius: var(--radius, 6px);
          color: var(--danger);
          font-family: 'Barlow Condensed', sans-serif;
          font-weight: 700;
          font-size: 12px;
          letter-spacing: 1px;
          text-transform: uppercase;
          cursor: pointer;
          transition: background 0.2s;
        "
        onmouseover="this.style.background='var(--danger)';this.style.color='#0a0b0d'"
        onmouseout="this.style.background='var(--danger-dim)';this.style.color='var(--danger)'"
        >Logout</button>
      `;
    }

    // ── Hide nav links Staff should not see ───────────────────
    if (user.role === 'Staff') {
      const staffRestricted = ['parts.html', 'purchases.html', 'vendors.html', 'staff.html'];
      staffRestricted.forEach(page => {
        document.querySelectorAll(`.nav-link[href="${page}"]`).forEach(el => {
          el.closest('li').style.display = 'none';
        });
      });
    }

    // ── Set active nav link ───────────────────────────────────
    const page = location.pathname.split('/').pop() || 'index.html';
    document.querySelectorAll('.nav-link').forEach(a => {
      if (a.getAttribute('href') === page) a.classList.add('active');
      else a.classList.remove('active');
    });
  });
})();