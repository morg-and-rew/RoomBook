import { api, getSession } from '../api.js';
import { h, icon, pageHeader, emptyState, errorState, skeleton, timeAgo } from '../ui.js';

// Отметка «прочитано» хранится на сервере (Notification.readAt).
const KINDS = {
  Created: { tone: 'info', icon: 'clock' },
  Confirmed: { tone: 'ok', icon: 'check' },
  Rejected: { tone: 'danger', icon: 'x' },
  Cancelled: { tone: 'muted', icon: 'x' },
};

function setBadge(count) {
  const badge = document.getElementById('notif-badge');
  if (!badge) return;
  badge.textContent = count > 9 ? '9+' : String(count);
  badge.hidden = count === 0;
}

export async function refreshNotificationBadge() {
  if (!getSession()) return;
  try {
    const items = await api.notifications();
    setBadge(items.filter((item) => !item.isRead).length);
  } catch {
    // Бейдж — необязательная подсказка, ошибку не показываем.
  }
}

export function notificationsPage(view) {
  const list = h('div', { class: 'card notif-list' });
  view.append(pageHeader('Уведомления', 'События по вашим заявкам на бронирование.'), list);
  load();

  async function load() {
    list.replaceChildren(...skeleton(3, 'line'));
    try {
      const items = await api.notifications();
      if (!view.isConnected) return;
      list.replaceChildren(...(items.length
        ? items.map(notification)
        : [emptyState('bell', 'Уведомлений нет', 'Здесь появятся сообщения о создании, подтверждении, отклонении и отмене ваших заявок.')]));
      // Новые остаются подсвеченными на этой странице, а на сервере отмечаются прочитанными.
      if (items.some((item) => !item.isRead)) {
        await api.markAllNotificationsRead();
        setBadge(0);
      }
    } catch (err) {
      if (view.isConnected) list.replaceChildren(errorState(err, load));
    }
  }
}

function notification(item) {
  const kind = KINDS[item.type] ?? { tone: 'info', icon: 'bell' };
  return h('div', { class: `notif${item.isRead ? '' : ' is-new'}` },
    h('span', { class: `notif__icon notif__icon--${kind.tone}` }, icon(kind.icon)),
    h('div', { class: 'notif__body' },
      h('p', {}, item.message),
      h('time', { datetime: item.sentAt, title: new Date(item.sentAt).toLocaleString('ru-RU') }, timeAgo(item.sentAt))),
    !item.isRead && h('span', { class: 'notif__dot', title: 'Новое' }));
}
