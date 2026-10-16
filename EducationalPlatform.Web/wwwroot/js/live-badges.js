(function () {
  const chatBadge  = document.querySelector('[data-chat-badge]');
  const notifBadge = document.querySelector('[data-notif-badge]');
  const bell  = document.getElementById('notifBell');
  const panel = document.getElementById('notifPanel');
  const list  = document.getElementById('notifList');

  function setBadge(el, n) {
    if (!el) return;
    el.textContent = n > 99 ? '99+' : String(n);
    el.hidden = !(n > 0);
  }

  async function loadChatCount() {
    if (!chatBadge) return;
    try {
      const r = await fetch('/Chat/UnreadCount', { credentials: 'same-origin' });
      if (r.ok) setBadge(chatBadge, (await r.json()).count);
    } catch { }
  }

  const iconFor = t =>
    t && t.endsWith('Grade') ? 'bi-award' :
    t === 'ExamAssigned' ? 'bi-file-earmark-text' :
    t === 'QuizAssigned' ? 'bi-ui-checks' :
    t === 'TrainingAssigned' ? 'bi-journal-check' :
    t === 'PostCreated' ? 'bi-megaphone' : 'bi-bell';

  function renderItems(items) {
    if (!list) return;
    list.innerHTML = '';
    if (!items.length) {
      list.innerHTML = '<div class="ad-notif-empty">No notifications yet.</div>';
      return;
    }
    items.forEach(n => {
      const a = document.createElement('a');
      a.className = 'ad-notif-item' + (n.isRead ? '' : ' unread');
      a.href = '/Notifications/Open/' + n.id;
      const i = document.createElement('i');
      i.className = 'bi ' + iconFor(n.type);
      const body = document.createElement('span');
      const t = document.createElement('strong'); t.textContent = n.title;
      const m = document.createElement('small'); m.textContent = n.message;
      body.append(t, m);
      a.append(i, body);
      list.appendChild(a);
    });
  }

  async function loadNotifications() {
    if (!notifBadge) return;
    try {
      const r = await fetch('/Notifications/Summary', { credentials: 'same-origin' });
      if (!r.ok) return;
      const d = await r.json();
      setBadge(notifBadge, d.unreadCount);
      renderItems(d.items);
    } catch { }
  }

  if (bell && panel) {
    bell.addEventListener('click', e => {
      e.stopPropagation();
      panel.hidden = !panel.hidden;
      bell.setAttribute('aria-expanded', String(!panel.hidden));
      if (!panel.hidden) loadNotifications();
    });
    document.addEventListener('click', e => {
      if (!panel.hidden && !panel.contains(e.target)) {
        panel.hidden = true;
        bell.setAttribute('aria-expanded', 'false');
      }
    });
  }

  const markAll = document.getElementById('notifMarkAll');
  if (markAll) markAll.addEventListener('click', async () => {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    await fetch('/Notifications/MarkAllRead', {
      method: 'POST',
      headers: { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token || '' },
      credentials: 'same-origin'
    });
    loadNotifications();
  });

  // SignalR
  if (window.signalR) {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl('/chatHub').withAutomaticReconnect().build();

    conn.on('UnreadCountChanged', p => setBadge(chatBadge, p.count));
    conn.on('ReceiveMessage', loadChatCount);   // new incoming message
    conn.on('ReceiveNotification', p => {
      setBadge(notifBadge, p.unreadCount);
      if (panel && !panel.hidden) loadNotifications();
    });
    conn.on('NotificationCountChanged', p => setBadge(notifBadge, p.unreadCount));
    conn.onreconnected(() => { loadChatCount(); loadNotifications(); });

    conn.start().catch(() => { });
  }

  loadChatCount();
  loadNotifications();
})();
