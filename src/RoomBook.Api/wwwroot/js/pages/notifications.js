import { api, getSession } from '../api.js';
import { h, icon, pageHeader, emptyState, errorState, skeleton, timeAgo } from '../ui.js';

// API не хранит отметку «прочитано», поэтому время последнего просмотренного
// уведомления запоминается в браузере — по нему считается бейдж в меню.
const seenKey = (userId) => `roombook.notifications-seen.${userId}`;

function readSeen(userId) {
  try {
    return localStorage.getItem(seenKey(userId));
  } catch {
    return null;
  }
}

function writeSeen(userId, value) {
  try {
    localStorage.setItem(seenKey(userId), value);
  } catch {
    // Без localStorage бейдж просто не запомнит просмотр.
  }
}

const isNew = (item, seen) => !seen || new Date(item.createdAt) > new Date(seen);

function setBadge(count) {
  const badge = document.getElementById('notif-badge');
  if (!badge) return;
  badge.textContent = count > 9 ? '9+' : String(count);
  badge.hidden = count === 0;
}

export async function refreshNotificationBadge() {
  const user = getSession()?.user;
  if (!user) return;
  try {
    const items = await api.notifications();
    setBadge(items.filter((item) => isNew(item, readSeen(user.id))).length);
  } catch {
    // Бейдж — необязательная подсказка, ошибку не показываем.
  }
}

export function notificationsPage(view, { user }) {
  const list = h('div', { class: 'card notif-list' });
  view.append(pageHeader('Уведомления', 'События по вашим заявкам на бронирование.'), list);
  load();

  async function load() {
    list.replaceChildren(...skeleton(3, 'line'));
    try {
      const items = await api.notifications();
      if (!view.isConnected) return;
      const seen = readSeen(user.id);
      // Сервер отдаёт новые уведомления первыми.
      if (items.length) writeSeen(user.id, items[0].createdAt);
      setBadge(0);
      list.replaceChildren(...(items.length
        ? items.map((item) => notification(item, isNew(item, seen)))
        : [emptyState('bell', 'Уведомлений нет', 'Здесь появятся сообщения о создании, подтверждении и отклонении ваших заявок.')]));
    } catch (err) {
      if (view.isConnected) list.replaceChildren(errorState(err, load));
    }
  }
}

function notification(item, fresh) {
  // «ожидает подтверждения» — не то же, что «подтверждена», поэтому ищем слова целиком.
  const kind = /отклонена/i.test(item.message) ? 'danger' : /подтверждена/i.test(item.message) ? 'ok' : 'info';
  const iconName = { ok: 'check', danger: 'x', info: 'clock' }[kind];
  return h('div', { class: `notif${fresh ? ' is-new' : ''}` },
    h('span', { class: `notif__icon notif__icon--${kind}` }, icon(iconName)),
    h('div', { class: 'notif__body' },
      h('p', {}, item.message),
      h('time', { datetime: item.createdAt, title: new Date(item.createdAt).toLocaleString('ru-RU') }, timeAgo(item.createdAt))),
    fresh && h('span', { class: 'notif__dot', title: 'Новое' }));
}
