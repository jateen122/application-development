
const BASE_URL = 'http://127.0.0.1:5033/api';

const API = {
  async get(path) {
    let r;
    try { r = await fetch(`${BASE_URL}${path}`); }
    catch(e) { throw new Error('Cannot connect to server. Make sure the backend is running: cd backend-api && dotnet run'); }
    if (!r.ok) {
      const data = await r.json().catch(() => null);
      throw new Error(data?.message || `Error ${r.status}`);
    }
    return r.json();
  },
  async post(path, body) {
    let r;
    try { r = await fetch(`${BASE_URL}${path}`, { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body) }); }
    catch(e) { throw new Error('Cannot connect to server. Make sure the backend is running.'); }
    const data = await r.json().catch(() => null);
    if (!r.ok) throw Object.assign(new Error(data?.message || `Error ${r.status}`), { status:r.status, data });
    return data;
  },
  async put(path, body) {
    let r;
    try { r = await fetch(`${BASE_URL}${path}`, { method:'PUT', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body) }); }
    catch(e) { throw new Error('Cannot connect to server. Make sure the backend is running.'); }
    if (r.status === 204) return null;
    const data = await r.json().catch(() => null);
    if (!r.ok) throw Object.assign(new Error(data?.message || `Error ${r.status}`), { status:r.status, data });
    return data;
  },
  async patch(path, body) {
    let r;
    try { r = await fetch(`${BASE_URL}${path}`, { method:'PATCH', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body) }); }
    catch(e) { throw new Error('Cannot connect to server. Make sure the backend is running.'); }
    if (r.status === 204) return null;
    const data = await r.json().catch(() => null);
    if (!r.ok) throw Object.assign(new Error(data?.message || `Error ${r.status}`), { status:r.status, data });
    return data;
  },
  async del(path) {
    let r;
    try { r = await fetch(`${BASE_URL}${path}`, { method:'DELETE' }); }
    catch(e) { throw new Error('Cannot connect to server. Make sure the backend is running.'); }
    if (r.status === 204) return null;
    const data = await r.json().catch(() => null);
    if (!r.ok) throw Object.assign(new Error(data?.message || `Error ${r.status}`), { status:r.status, data });
    return data;
  }
};

//  Auth helpers 
function getUser() {
  try { return JSON.parse(sessionStorage.getItem('user') || 'null'); }
  catch { return null; }
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

// Format helpers
function fmt(n) {
  return Number(n).toLocaleString('en-US', { minimumFractionDigits:2, maximumFractionDigits:2 });
}
function fmtDate(d) {
  return new Date(d).toLocaleDateString('en-US', { month:'short', day:'numeric', year:'numeric' });
}
function fmtDateTime(d) {
  return new Date(d).toLocaleString('en-US', { month:'short', day:'numeric', year:'numeric', hour:'2-digit', minute:'2-digit' });
}

//  Toast notification 
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
  el._t = setTimeout(() => el.classList.remove('toast--show'), 4000);
}

//  Modal helpers 
function openModal(id)  { const el = document.getElementById(id); if(el) el.classList.add('open'); }
function closeModal(id) { const el = document.getElementById(id); if(el) el.classList.remove('open'); }

// Page access control 
const PAGE_ACCESS = {
  'index.html':            ['Admin', 'Staff'],
  'parts.html':            ['Admin'],
  'purchases.html':        ['Admin'],
  'vendors.html':          ['Admin'],
  'staff.html':            ['Admin'],
  'reports.html':          ['Admin', 'Staff'],
  'customers.html':        ['Admin', 'Staff'],
  'customer-detail.html':  ['Admin', 'Staff'],
  'vehicles.html':         ['Admin', 'Staff'],
  'sales.html':            ['Admin', 'Staff'],
  'service-requests.html': ['Admin', 'Staff'],
  'portal-profile.html':   ['Customer'],
  'portal-vehicles.html':  ['Customer'],
  'portal-history.html':   ['Customer'],
  'portal-services.html':  ['Customer'],
};

const NAV_ADMIN = [
  { href:'index.html',            label:'Dashboard' },
  { href:'parts.html',            label:'Parts' },
  //{ href:'customers.html',        label:'Customers' },
  //{ href:'vehicles.html',         label:'Vehicles' },
  { href:'sales.html',            label:'Sales' },
  { href:'purchases.html',        label:'Purchases' },
  { href:'vendors.html',          label:'Vendors' },
  { href:'staff.html',            label:'Staff' },
  { href:'reports.html',          label:'Reports' },
  { href:'service-requests.html', label:'Services' },
];

const NAV_STAFF = [
  { href:'index.html',            label:'Dashboard' },
  { href:'customers.html',        label:'Customers' },
  { href:'vehicles.html',         label:'Vehicles' },
  { href:'sales.html',            label:'Sales' },
  { href:'reports.html',          label:'Reports' },
  { href:'service-requests.html', label:'Services' },
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

  const links = user.role === 'Admin' ? NAV_ADMIN : NAV_STAFF;
  const nav = document.getElementById('nav-links');
  if (nav) {
    nav.innerHTML = links.map(l =>
      `<a href="${l.href}" class="nav-link${l.href===page?' nav-link--active':''}">${l.label}</a>`
    ).join('');
  }
  const userEl = document.getElementById('nav-user');
  if (userEl) {
    userEl.innerHTML = `
      <span class="nav-username">${user.firstName} ${user.lastName}</span>
      <span class="nav-role">${user.role}</span>
      <button class="nav-logout" onclick="logout()">Logout</button>`;
  }
}

//  Portal nav helper (for customer pages) 
function buildPortalNav() {
  const user = getUser();
  if (!user) return;
  const userEl = document.getElementById('nav-user');
  if (userEl) {
    userEl.innerHTML = `
      <span class="nav-username">${user.firstName} ${user.lastName}</span>
      <span class="nav-role">Customer</span>
      <button class="nav-logout" onclick="logout()">Logout</button>`;
  }
}


// Shows a friendly banner if backend is unreachable
async function checkBackendConnection() {
  try {
    await fetch('http://127.0.0.1:5033/api/reports/summary', { signal: AbortSignal.timeout(3000) });
  } catch(e) {
    const banner = document.createElement('div');
    banner.style.cssText = `
      position:fixed;bottom:0;left:0;right:0;background:#e03c3c;color:#fff;
      padding:12px 20px;text-align:center;font-size:13px;font-family:'DM Sans',sans-serif;
      z-index:9999;display:flex;align-items:center;justify-content:center;gap:12px`;
    banner.innerHTML = `
      <span>⚠ Backend not running. Open a terminal and run: <strong>cd backend-api &amp;&amp; dotnet run</strong></span>
      <button onclick="location.reload()" style="background:#fff;color:#e03c3c;border:none;border-radius:4px;padding:4px 12px;cursor:pointer;font-weight:600">Retry</button>`;
    document.body.appendChild(banner);
  }
}
