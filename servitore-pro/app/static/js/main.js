// ─── Alert dismiss ───────────────────────────────────────────────────────────
document.querySelectorAll('.alert-close').forEach(btn => {
    btn.addEventListener('click', () => {
        btn.closest('.alert').style.opacity = '0';
        setTimeout(() => btn.closest('.alert').remove(), 300);
    });
});

// Auto-dismiss flash messages after 5s
setTimeout(() => {
    document.querySelectorAll('.alert').forEach(el => {
        el.style.transition = 'opacity 0.4s';
        el.style.opacity = '0';
        setTimeout(() => el.remove(), 400);
    });
}, 5000);

// ─── Mobile sidebar toggle ───────────────────────────────────────────────────
const toggleBtn = document.getElementById('mobile-toggle');
const sidebar = document.getElementById('sidebar');
const overlay = document.getElementById('sidebar-overlay');

if (toggleBtn && sidebar) {
    toggleBtn.addEventListener('click', () => {
        sidebar.classList.toggle('open');
    });
    if (overlay) {
        overlay.addEventListener('click', () => sidebar.classList.remove('open'));
    }
}

// ─── Quick View auto-refresh (every 60 seconds) ──────────────────────────────
function refreshQuickView() {
    fetch('/api/quick-view')
        .then(r => r.ok ? r.json() : null)
        .then(data => {
            if (!data) return;
            Object.entries(data).forEach(([key, val]) => {
                const el = document.getElementById(`qv-${key}`);
                if (el) el.textContent = val;
            });
        })
        .catch(() => {});
}

if (document.querySelector('[id^="qv-"]')) {
    setInterval(refreshQuickView, 60000);
}

// ─── Form submit spinner ─────────────────────────────────────────────────────
document.querySelectorAll('form').forEach(form => {
    form.addEventListener('submit', function() {
        if (!form.checkValidity()) return;
        const btn = form.querySelector('[type="submit"]');
        if (btn && !btn.dataset.noSpinner) {
            btn.disabled = true;
            const orig = btn.innerHTML;
            btn.innerHTML = '<span class="spinner"></span> Saving...';
            setTimeout(() => { btn.disabled = false; btn.innerHTML = orig; }, 15000);
        }
    });
});

// ─── Confirm delete ───────────────────────────────────────────────────────────
document.querySelectorAll('[data-confirm]').forEach(el => {
    el.addEventListener('click', function(e) {
        if (!confirm(this.dataset.confirm || 'Are you sure?')) {
            e.preventDefault();
        }
    });
});

// ─── Dynamic date greeting ────────────────────────────────────────────────────
const greetEl = document.getElementById('header-greeting');
if (greetEl) {
    const h = new Date().getHours();
    const g = h < 12 ? 'Good Morning' : h < 18 ? 'Good Afternoon' : 'Good Evening';
    greetEl.textContent = g + ', ' + (greetEl.dataset.name || '');
}

// ─── Table sort ───────────────────────────────────────────────────────────────
document.querySelectorAll('th[data-sort]').forEach(th => {
    th.style.cursor = 'pointer';
    th.addEventListener('click', function() {
        const table = th.closest('table');
        const tbody = table.querySelector('tbody');
        const col = [...th.parentElement.children].indexOf(th);
        const rows = [...tbody.querySelectorAll('tr')];
        const asc = th.dataset.dir !== 'asc';
        th.dataset.dir = asc ? 'asc' : 'desc';
        rows.sort((a, b) => {
            const av = a.cells[col]?.textContent.trim() || '';
            const bv = b.cells[col]?.textContent.trim() || '';
            return asc ? av.localeCompare(bv, undefined, {numeric: true})
                       : bv.localeCompare(av, undefined, {numeric: true});
        });
        rows.forEach(r => tbody.appendChild(r));
    });
});

// ─── Challan dynamic line items ───────────────────────────────────────────────
const addLineBtn = document.getElementById('add-challan-line');
if (addLineBtn) {
    addLineBtn.addEventListener('click', function() {
        const container = document.getElementById('challan-lines');
        const template = container.querySelector('.challan-line');
        const clone = template.cloneNode(true);
        clone.querySelectorAll('input, select').forEach(el => el.value = '');
        container.appendChild(clone);
    });
    document.addEventListener('click', function(e) {
        if (e.target.closest('.remove-line')) {
            const lines = document.querySelectorAll('.challan-line');
            if (lines.length > 1) e.target.closest('.challan-line').remove();
        }
    });
}
