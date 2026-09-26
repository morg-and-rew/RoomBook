import { api } from '../api.js';
import {
  h, icon, toast, confirmDialog, pageHeader, tabs, emptyState, errorState, skeleton,
  statusBadge, dateTile, formatTime, formatRange, timeAgo,
} from '../ui.js';

let activeTab = 'upcoming';

const isUpcoming = (booking) =>
  (booking.status === 'Pending' || booking.status === 'Confirmed') && new Date(booking.endAt) > new Date();

export function myBookingsPage(view) {
  const tabsHost = h('div');
  const list = h('div', { class: 'list' });
  let bookings = [];

  view.append(pageHeader('Мои брони', 'Ваши заявки на бронирование и их статус.'), tabsHost, list);
  load();

  async function load() {
    list.replaceChildren(...skeleton(2, 'row'));
    try {
      bookings = await api.myBookings();
      if (view.isConnected) render();
    } catch (err) {
      if (view.isConnected) list.replaceChildren(errorState(err, load));
    }
  }

  function render() {
    const upcoming = bookings.filter(isUpcoming).sort((a, b) => new Date(a.startAt) - new Date(b.startAt));
    const all = [...bookings].sort((a, b) => new Date(b.startAt) - new Date(a.startAt));
    tabsHost.replaceChildren(tabs([
      { id: 'upcoming', label: 'Предстоящие', count: upcoming.length },
      { id: 'all', label: 'Все', count: all.length },
    ], activeTab, (id) => { activeTab = id; render(); }));

    const items = activeTab === 'upcoming' ? upcoming : all;
    if (!items.length) {
      list.replaceChildren(emptyState('calendar',
        activeTab === 'upcoming' ? 'Предстоящих броней нет' : 'Броней пока нет',
        'Выберите помещение и свободное время — заявка появится здесь.',
        h('a', { class: 'btn btn--primary', href: '#/rooms' }, 'Выбрать помещение')));
      return;
    }
    list.replaceChildren(...items.map(bookingRow));
  }

  function bookingRow(booking) {
    const canCancel = isUpcoming(booking);
    const isPast = new Date(booking.endAt) < new Date();
    return h('article', { class: `card booking${isPast ? ' is-past' : ''}` },
      dateTile(booking.startAt),
      h('div', { class: 'booking__body' },
        h('div', { class: 'booking__title' }, h('h3', {}, booking.roomName), statusBadge(booking.status)),
        h('p', { class: 'booking__meta' }, icon('clock'), `${formatTime(booking.startAt)}–${formatTime(booking.endAt)}`),
        booking.purpose && h('p', { class: 'booking__purpose' }, booking.purpose),
        booking.rejectReason && h('p', { class: 'booking__reason' }, `Причина отказа: ${booking.rejectReason}`),
        booking.decidedAt && h('p', { class: 'booking__created' },
          `Решение: ${booking.decidedByName ?? 'администратор'}, ${timeAgo(booking.decidedAt)}`)),
      canCancel && h('div', { class: 'booking__actions' },
        h('button', { class: 'btn btn--ghost btn--danger-text', type: 'button', onclick: () => cancel(booking) }, 'Отменить')));
  }

  async function cancel(booking) {
    const confirmed = await confirmDialog('Отменить бронь?',
      `${booking.roomName}, ${formatRange(booking.startAt, booking.endAt)}. Отменённую заявку нельзя восстановить.`,
      { confirmText: 'Отменить бронь', danger: true });
    if (!confirmed) return;
    try {
      await api.cancelBooking(booking.id);
      toast('Бронь отменена');
      load();
    } catch (err) {
      toast(err.message, 'error');
    }
  }
}
