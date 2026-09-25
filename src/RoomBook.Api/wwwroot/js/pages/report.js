import { api } from '../api.js';
import {
  h, field, errorBox, showError, pageHeader, emptyState, errorState, skeleton,
  toDateInput, startOfDay, endOfDay, formatNumber, plural,
} from '../ui.js';

const monthRange = (offset) => {
  const now = new Date();
  return [new Date(now.getFullYear(), now.getMonth() + offset, 1), new Date(now.getFullYear(), now.getMonth() + offset + 1, 0)];
};

const PRESETS = [
  {
    label: 'Эта неделя',
    range: () => {
      const now = new Date();
      const monday = new Date(now.getFullYear(), now.getMonth(), now.getDate() - ((now.getDay() + 6) % 7));
      return [monday, new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + 6)];
    },
  },
  { label: 'Этот месяц', range: () => monthRange(0) },
  { label: 'Следующий месяц', range: () => monthRange(1) },
];

const [defaultFrom, defaultTo] = monthRange(0);
const period = { from: toDateInput(defaultFrom), to: toDateInput(defaultTo) };

export function reportPage(view) {
  const fromInput = h('input', { type: 'date', required: true, value: period.from });
  const toInput = h('input', { type: 'date', required: true, value: period.to });
  const error = errorBox();
  const result = h('div', { class: 'report' });
  let loadId = 0;

  const form = h('form', {
    class: 'card filters filters--report',
    onsubmit: (event) => {
      event.preventDefault();
      load();
    },
  },
    field('С', fromInput),
    field('По', toInput),
    h('div', { class: 'field' },
      h('span', { class: 'field__label' }, 'Быстрый выбор'),
      h('div', { class: 'chips' }, PRESETS.map((preset) => h('button', {
        class: 'chip',
        type: 'button',
        onclick: () => {
          const [from, to] = preset.range();
          fromInput.value = toDateInput(from);
          toInput.value = toDateInput(to);
          load();
        },
      }, preset.label)))),
    h('button', { class: 'btn btn--primary', type: 'submit' }, 'Показать'));

  view.append(
    pageHeader('Отчёт по загруженности', 'Подтверждённые брони по помещениям за выбранный период.'),
    form, error, result);
  load();

  async function load() {
    error.hidden = true;
    if (!fromInput.value || !toInput.value) return;
    if (fromInput.value > toInput.value) {
      showError(error, 'Дата начала периода позже даты окончания.');
      return;
    }
    period.from = fromInput.value;
    period.to = toInput.value;

    const id = ++loadId;
    result.replaceChildren(...skeleton(1, 'block'));
    try {
      const [report, rooms] = await Promise.all([
        api.occupancy(startOfDay(period.from).toISOString(), endOfDay(period.to).toISOString()),
        api.rooms(),
      ]);
      if (id !== loadId || !view.isConnected) return;
      const rows = mergeRows(report.rooms, rooms);
      const totalBookings = rows.reduce((sum, row) => sum + row.bookings, 0);
      result.replaceChildren(
        stats(rows),
        totalBookings
          ? chart(rows)
          : emptyState('chart', 'Нет подтверждённых броней',
            'За этот период ничего не подтверждено. Выберите другой период или обработайте заявки в разделе «Заявки».'));
    } catch (err) {
      if (id === loadId && view.isConnected) result.replaceChildren(errorState(err, load));
    }
  }
}

/** Отчёт API содержит только помещения с бронями — добавляем остальные активные с нулём. */
function mergeRows(reportRooms, rooms) {
  const rows = reportRooms.map((r) => ({ id: r.roomId, name: r.roomName, bookings: r.totalBookings, hours: r.totalHoursBooked }));
  for (const room of rooms) {
    if (!rows.some((row) => row.id === room.id)) rows.push({ id: room.id, name: room.name, bookings: 0, hours: 0 });
  }
  return rows.sort((a, b) => b.hours - a.hours || a.name.localeCompare(b.name, 'ru'));
}

function stats(rows) {
  const bookings = rows.reduce((sum, row) => sum + row.bookings, 0);
  const hours = rows.reduce((sum, row) => sum + row.hours, 0);
  const top = rows[0]?.hours > 0 ? rows[0] : null;
  const tile = (label, value, note) => h('div', { class: 'card stat' },
    h('div', { class: 'stat__label' }, label),
    h('div', { class: 'stat__value', title: String(value) }, value),
    note && h('div', { class: 'stat__note' }, note));
  return h('div', { class: 'stats' },
    tile('Подтверждённых броней', formatNumber(bookings)),
    tile('Забронировано часов', formatNumber(hours)),
    tile('Самое загруженное', top ? top.name : '—', top ? `${formatNumber(top.hours)} ч` : 'нет данных'));
}

function chart(rows) {
  const max = Math.max(...rows.map((row) => row.hours));
  return h('section', { class: 'card chart', 'aria-labelledby': 'chart-title' },
    h('h2', { class: 'chart__title', id: 'chart-title' }, 'Часы бронирования по помещениям'),
    h('div', { class: 'bars', role: 'table', 'aria-label': 'Часы бронирования по помещениям' },
      rows.map((row) => {
        const bookingsText = `${row.bookings} ${plural(row.bookings, 'бронь', 'брони', 'броней')}`;
        return h('div', { class: 'bar', role: 'row', title: `${row.name}: ${formatNumber(row.hours)} ч, ${bookingsText}` },
          h('span', { class: 'bar__label', role: 'rowheader' }, row.name),
          h('span', { class: 'bar__track', role: 'cell', 'aria-hidden': 'true' },
            row.hours > 0 && h('span', { class: 'bar__fill', style: `width:${(row.hours / max) * 100}%` })),
          h('span', { class: 'bar__value', role: 'cell' },
            h('strong', {}, `${formatNumber(row.hours)} ч`), h('span', { class: 'bar__count' }, bookingsText)));
      })));
}
