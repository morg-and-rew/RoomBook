import { api } from '../api.js';
import {
  h, icon, toast, openDialog, field, errorBox, showError, withBusy, pageHeader, tabs,
  emptyState, errorState, skeleton, statusBadge, dateTile, formatTime, formatRange, timeAgo,
} from '../ui.js';

const FILTERS = [
  { id: 'Pending', label: 'Ожидают' },
  { id: 'Approved', label: 'Подтверждены' },
  { id: 'Rejected', label: 'Отклонены' },
  { id: 'Cancelled', label: 'Отменены' },
  { id: 'all', label: 'Все' },
];

let activeStatus = 'Pending';

export function requestsPage(view) {
  const tabsHost = h('div');
  const list = h('div', { class: 'list' });
  let bookings = [];

  view.append(pageHeader('Заявки', 'Подтверждайте или отклоняйте заявки пользователей на бронирование.'), tabsHost, list);
  load();

  async function load() {
    if (!bookings.length) list.replaceChildren(...skeleton(3, 'row'));
    try {
      bookings = await api.allBookings();
      if (view.isConnected) render();
    } catch (err) {
      if (view.isConnected) list.replaceChildren(errorState(err, load));
    }
  }

  function render() {
    tabsHost.replaceChildren(tabs(FILTERS.map((filter) => ({
      ...filter,
      count: filter.id === 'all' ? bookings.length : bookings.filter((b) => b.status === filter.id).length,
    })), activeStatus, (id) => { activeStatus = id; render(); }));

    // Ожидающие — ближайшие первыми (их нужно обработать раньше), остальные — сначала новые.
    const items = bookings
      .filter((b) => activeStatus === 'all' || b.status === activeStatus)
      .sort((a, b) => (activeStatus === 'Pending'
        ? new Date(a.startTime) - new Date(b.startTime)
        : new Date(b.startTime) - new Date(a.startTime)));

    if (!items.length) {
      list.replaceChildren(activeStatus === 'Pending'
        ? emptyState('check', 'Новых заявок нет', 'Все заявки обработаны.')
        : emptyState('inbox', 'Здесь пока пусто', 'Заявок с таким статусом нет.'));
      return;
    }
    list.replaceChildren(...items.map(requestRow));
  }

  function requestRow(booking) {
    const isPast = new Date(booking.endTime) < new Date();
    return h('article', { class: `card request${isPast ? ' is-past' : ''}` },
      dateTile(booking.startTime),
      h('div', { class: 'request__main' },
        h('div', { class: 'booking__title' },
          h('h3', {}, booking.roomName),
          statusBadge(booking.status),
          isPast && booking.status === 'Pending' && h('span', { class: 'tag tag--muted' }, 'время прошло')),
        h('p', { class: 'booking__meta' },
          icon('clock'), `${formatTime(booking.startTime)}–${formatTime(booking.endTime)}`,
          h('span', { class: 'booking__sep' }, '·'),
          icon('user'), booking.userFullName),
        booking.purpose && h('p', { class: 'booking__purpose' }, booking.purpose),
        booking.rejectionReason && h('p', { class: 'booking__reason' }, `Причина отказа: ${booking.rejectionReason}`),
        h('p', { class: 'booking__created' }, `Заявка создана ${timeAgo(booking.createdAt)}`)),
      booking.status === 'Pending' && h('div', { class: 'request__actions' },
        h('button', {
          class: 'btn btn--success',
          type: 'button',
          onclick: (event) => approve(booking, event.currentTarget),
        }, icon('check'), 'Подтвердить'),
        h('button', { class: 'btn btn--ghost btn--danger-text', type: 'button', onclick: () => reject(booking) },
          icon('x'), 'Отклонить')));
  }

  async function approve(booking, button) {
    await withBusy(button, async () => {
      try {
        await api.approveBooking(booking.id);
        toast(`Подтверждено: ${booking.roomName}, ${formatRange(booking.startTime, booking.endTime)}`);
        await load();
      } catch (err) {
        toast(err.message, 'error');
      }
    });
  }

  function reject(booking) {
    const reason = h('textarea', { rows: 3, maxlength: 500, placeholder: 'Например: в это время в аудитории ремонт' });
    const error = errorBox();
    const submit = h('button', { class: 'btn btn--danger', type: 'submit' }, 'Отклонить заявку');
    const form = h('form', {
      class: 'form',
      onsubmit: async (event) => {
        event.preventDefault();
        error.hidden = true;
        await withBusy(submit, async () => {
          try {
            await api.rejectBooking(booking.id, reason.value.trim() || null);
            dialog.close();
            toast('Заявка отклонена');
            load();
          } catch (err) {
            showError(error, err);
          }
        });
      },
    },
      h('p', { class: 'dialog__text' },
        `${booking.userFullName} · ${booking.roomName}, ${formatRange(booking.startTime, booking.endTime)}`),
      error,
      field('Причина (необязательно)', reason, 'Пользователь увидит её в уведомлении.'),
      h('div', { class: 'dialog__actions' },
        h('button', { class: 'btn btn--ghost', type: 'button', onclick: () => dialog.close() }, 'Отмена'),
        submit));

    const dialog = openDialog('Отклонить заявку', form);
    reason.focus();
  }
}
