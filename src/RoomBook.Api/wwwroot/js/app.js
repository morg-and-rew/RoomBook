import { getSession, setSession } from './api.js';
import { openAuthDialog } from './auth.js';
import { api } from './api.js';
import { h, icon, toast, emptyState, openDialog, field, errorBox, showError, withBusy } from './ui.js';
import { roomsPage } from './pages/rooms.js';
import { myBookingsPage } from './pages/bookings.js';
import { notificationsPage, refreshNotificationBadge } from './pages/notifications.js';
import { requestsPage } from './pages/requests.js';
import { reportPage } from './pages/report.js';

// Разделы сайта: auth — нужен вход, admin — нужна роль Admin.
const ROUTES = [
  { path: 'rooms', title: 'Помещения', icon: 'building', page: roomsPage },
  { path: 'my', title: 'Мои брони', icon: 'list', page: myBookingsPage, auth: true },
  { path: 'notifications', title: 'Уведомления', icon: 'bell', page: notificationsPage, auth: true, badge: true },
  { path: 'requests', title: 'Заявки', icon: 'inbox', page: requestsPage, admin: true },
  { path: 'report', title: 'Отчёт', icon: 'chart', page: reportPage, admin: true },
];

const topbar = document.getElementById('topbar');
const main = document.getElementById('app');

function currentRoute() {
  const path = window.location.hash.replace(/^#\/?/, '');
  return ROUTES.find((route) => route.path === path) ?? ROUTES[0];
}

function canOpen(route, user) {
  if (route.admin) return user?.role === 'Admin';
  if (route.auth) return Boolean(user);
  return true;
}

function render() {
  const user = getSession()?.user ?? null;
  const route = currentRoute();
  document.title = `${route.title} · RoomBook`;
  renderTopbar(user, route);

  const view = h('div', { class: 'view' });
  main.replaceChildren(view);
  if (canOpen(route, user)) route.page(view, { user });
  else view.append(accessGate(route, user));

  refreshNotificationBadge();
}

function accessGate(route, user) {
  if (!user) {
    return emptyState('lock', 'Нужно войти', `Раздел «${route.title}» доступен после входа в систему.`,
      h('button', { class: 'btn btn--primary', type: 'button', onclick: () => openAuthDialog('login') }, icon('login'), 'Войти'));
  }
  return emptyState('lock', 'Только для администраторов',
    'Роль администратора выдаётся в базе данных (см. README). Если её уже выдали — выйдите и войдите снова.');
}

function renderTopbar(user, active) {
  const links = ROUTES.filter((route) => canOpen(route, user)).map((route) =>
    h('a', {
      class: `nav__link${route === active ? ' is-active' : ''}`,
      href: `#/${route.path}`,
      'aria-current': route === active ? 'page' : null,
    },
      icon(route.icon),
      h('span', {}, route.title),
      route.badge && h('span', { class: 'nav__badge', id: 'notif-badge', hidden: true })));

  const account = user
    ? h('div', { class: 'account' },
      h('button', { class: 'account__profile', type: 'button', title: 'Профиль', onclick: () => openProfileDialog(user) },
        h('span', { class: 'avatar', 'aria-hidden': 'true' }, initials(user.fullName)),
        h('span', { class: 'account__text' },
          h('span', { class: 'account__name' }, user.fullName),
          h('span', { class: 'account__role' }, user.role === 'Admin' ? 'Администратор' : 'Пользователь'))),
      h('button', { class: 'icon-btn', type: 'button', title: 'Выйти', 'aria-label': 'Выйти', onclick: logout }, icon('logout')))
    : h('div', { class: 'account' },
      h('button', { class: 'btn btn--ghost account__register', type: 'button', onclick: () => openAuthDialog('register') }, 'Регистрация'),
      h('button', { class: 'btn btn--primary', type: 'button', onclick: () => openAuthDialog('login') }, icon('login'), 'Войти'));

  topbar.replaceChildren(h('div', { class: 'topbar__inner container' },
    h('a', { class: 'brand', href: '#/rooms' }, h('span', { class: 'brand__mark' }, icon('calendarCheck')), 'RoomBook'),
    h('nav', { class: 'nav', 'aria-label': 'Разделы' }, links),
    account));
}

/** Профиль: изменение имени (UserService.updateProfile). Сервер выдаёт новый токен с новым именем. */
function openProfileDialog(user) {
  const fullName = h('input', { required: true, maxlength: 200, autocomplete: 'name', value: user.fullName });
  const error = errorBox();
  const submit = h('button', { class: 'btn btn--primary', type: 'submit' }, 'Сохранить');
  const form = h('form', {
    class: 'form',
    onsubmit: async (event) => {
      event.preventDefault();
      error.hidden = true;
      await withBusy(submit, async () => {
        try {
          const result = await api.updateProfile(fullName.value.trim());
          dialog.close();
          setSession(result);
          toast('Профиль обновлён');
        } catch (err) {
          showError(error, err);
        }
      });
    },
  },
    h('p', { class: 'dialog__text' }, `${user.email} · ${user.role === 'Admin' ? 'администратор' : 'пользователь'}`),
    error,
    field('Имя и фамилия', fullName),
    h('div', { class: 'dialog__actions' },
      h('button', { class: 'btn btn--ghost', type: 'button', onclick: () => dialog.close() }, 'Отмена'),
      submit));
  const dialog = openDialog('Профиль', form);
  fullName.focus();
}

function initials(fullName) {
  return fullName.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0].toUpperCase()).join('');
}

function logout() {
  window.location.hash = '#/rooms';
  setSession(null);
  toast('Вы вышли из аккаунта');
}

window.addEventListener('hashchange', render);
window.addEventListener('app-refresh', render);
window.addEventListener('session-change', (event) => {
  if (event.detail?.reason === 'expired') toast('Сессия истекла — войдите снова.', 'error');
  render();
});

render();
