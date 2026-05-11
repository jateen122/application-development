const BASE_URL = 'http://127.0.0.1:5033/api';

const API = {
  async get(path) {
    try {
      const r = await fetch(`${BASE_URL}${path}`);
      if (!r.ok) {
        const data = await r.json().catch(() => null);
        throw Object.assign(
          new Error(data?.message || `Request failed: ${r.status} ${r.statusText}`),
          { status: r.status, data }
        );
      }
      return r.json();
    } catch (err) {
      if (err.name === 'TypeError') {
        throw new Error('Cannot connect to server. Make sure the backend is running on port 5033.');
      }
      throw err;
    }
  },
  async post(path, body) {
    try {
      const r = await fetch(`${BASE_URL}${path}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
      const data = await r.json().catch(() => null);
      if (!r.ok) throw Object.assign(new Error(data?.message || `POST ${path} → ${r.status}`), { status: r.status, data });
      return data;
    } catch (err) {
      if (err.name === 'TypeError') throw new Error('Cannot connect to server.');
      throw err;
    }
  },
  async put(path, body) {
    try {
      const r = await fetch(`${BASE_URL}${path}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
      if (r.status === 204) return null;
      const data = await r.json().catch(() => null);
      if (!r.ok) throw Object.assign(new Error(data?.message || `PUT ${path} → ${r.status}`), { status: r.status, data });
      return data;
    } catch (err) {
      if (err.name === 'TypeError') throw new Error('Cannot connect to server.');
      throw err;
    }
  },
  async patch(path, body) {
    try {
      const r = await fetch(`${BASE_URL}${path}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
      if (r.status === 204) return null;
      const data = await r.json().catch(() => null);
      if (!r.ok) throw Object.assign(new Error(data?.message || `PATCH ${path} → ${r.status}`), { status: r.status, data });
      return data;
    } catch (err) {
      if (err.name === 'TypeError') throw new Error('Cannot connect to server.');
      throw err;
    }
  },
  async del(path) {
    try {
      const r = await fetch(`${BASE_URL}${path}`, { method: 'DELETE' });
      if (r.status === 204) return null;
      const data = await r.json().catch(() => null);
      if (!r.ok) throw Object.assign(new Error(data?.message || `DELETE ${path} → ${r.status}`), { status: r.status, data });
      return data;
    } catch (err) {
      if (err.name === 'TypeError') throw new Error('Cannot connect to server.');
      throw err;
    }
  }
};

function getUser() {
  const raw = sessionStorage.getItem('user');
  return raw ? JSON.parse(raw) : null;
}

function logout() {
  sessionStorage.removeItem('user');
  window.location.href = 'login.html';
}

function requireRole(...roles) {
  const user = getUser();
  if (!user) { window.location.href = 'login.html'; return null; }
  if (!roles.includes(user.role)) {
    if (user.role === 'Customer') window.location.href = 'portal-profile.html';
    else window.location.href = 'index.html';
    return null;
  }
  return user;
}

function fmt(n) {
  return Number(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function fmtDate(d) {
  if (!d) return '—';
  const dt = new Date(d);
  if (isNaN(dt.getTime()) || dt.getFullYear() < 2000) return '—';
  return dt.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function fmtDateTime(d) {
  if (!d) return '—';
  const dt = new Date(d);
  if (isNaN(dt.getTime()) || dt.getFullYear() < 2000) return '—';
  return dt.toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function notify(msg, type = 'info') {
  let el = document.getElementById('__notify');
  if (!el) {
    el = document.createElement('div');
    el.id = '__notify';
    el.className = 'toast';
    document.body.appendChild(el);
  }
  el.className = `toast toast--${type}`;
  el.textContent = msg;
  el.classList.add('toast--show');
  clearTimeout(el._t);
  el._t = setTimeout(() => el.classList.remove('toast--show'), 4500);
}

function openModal(id)  { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }

// Access map: page -> allowed roles
const PAGE_ACCESS = {
  'index.html':            ['Admin', 'Staff'],
  'parts.html':            ['Admin'],
  'purchases.html':        ['Admin'],
  'vendors.html':          ['Admin'],
  'staff.html':            ['Admin'],
  'reports.html':          ['Admin', 'Staff'],
  'customers.html':        ['Admin', 'Staff'],
  'vehicles.html':         ['Admin', 'Staff'],
  'sales.html':            ['Admin', 'Staff'],
  'portal-profile.html':   ['Customer'],
  'portal-vehicles.html':  ['Customer'],
  'portal-history.html':   ['Customer'],
};

// Nav links per role
const NAV_ADMIN = [
  { href: 'index.html',     label: 'Dashboard' },
  { href: 'parts.html',     label: 'Parts' },
  { href: 'customers.html', label: 'Customers' },
  { href: 'vehicles.html',  label: 'Vehicles' },
  { href: 'sales.html',     label: 'Sales' },
  { href: 'purchases.html', label: 'Purchases' },
  { href: 'vendors.html',   label: 'Vendors' },
  { href: 'staff.html',     label: 'Staff' },
  { href: 'reports.html',   label: 'Reports' },
];

const NAV_STAFF = [
  { href: 'index.html',     label: 'Dashboard' },
  { href: 'customers.html', label: 'Customers' },
  { href: 'vehicles.html',  label: 'Vehicles' },
  { href: 'sales.html',     label: 'Sales' },
  { href: 'reports.html',   label: 'Reports' },
];

const NAV_CUSTOMER = [
  { href: 'portal-profile.html',  label: 'My Profile' },
  { href: 'portal-vehicles.html', label: 'My Vehicles' },
  { href: 'portal-history.html',  label: 'History' },
];

function buildNav() {
  const user = getUser();
  const page = location.pathname.split('/').pop() || 'index.html';

  if (!user) return;

  const accessList = PAGE_ACCESS[page];
  if (accessList && !accessList.includes(user.role)) {
    if (user.role === 'Customer') window.location.href = 'portal-profile.html';
    else window.location.href = 'index.html';
    return;
  }

  let links;
  if (user.role === 'Admin') links = NAV_ADMIN;
  else if (user.role === 'Staff') links = NAV_STAFF;
  else links = NAV_CUSTOMER;

  const nav = document.getElementById('nav-links');
  if (!nav) return;

  nav.innerHTML = links.map(l => `
    <a href="${l.href}" class="nav-link${l.href === page ? ' nav-link--active' : ''}">${l.label}</a>
  `).join('');

  const userEl = document.getElementById('nav-user');
  if (userEl) {
    userEl.innerHTML = `
      <span class="nav-username">${user.firstName} ${user.lastName}</span>
      <span class="nav-role">${user.role}</span>
      <button class="nav-logout" onclick="logout()">Logout</button>
    `;
  }
}
