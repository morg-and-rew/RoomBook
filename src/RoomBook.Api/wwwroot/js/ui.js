// Помощники интерфейса: построение DOM, диалоги, уведомления, форматирование дат.

/**
 * Создаёт DOM-элемент. Дочерние строки вставляются как текст, а не HTML,
 * поэтому данные пользователей (названия, ФИО, цели брони) безопасно выводить как есть.
 */
export function h(tag, props, ...children) {
  const el = document.createElement(tag);
  let value;
  for (const [key, val] of Object.entries(props ?? {})) {
    if (val === undefined || val === null || val === false) continue;
    if (key === 'class') el.className = val;
    else if (key === 'value') value = val;
    else if (key.startsWith('on') && typeof val === 'function') el.addEventListener(key.slice(2), val);
    else el.setAttribute(key, val === true ? '' : val);
  }
  for (const child of children.flat(Infinity)) {
    if (child === undefined || child === null || child === false) continue;
    el.append(child instanceof Node ? child : String(child));
  }
  // value — после детей, чтобы у <select> уже были варианты.
  if (value !== undefined) el.value = value;
  return el;
}

const ICONS = {
  building: ['M6 22V4a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v18Z', 'M6 12H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h2', 'M18 9h2a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2h-2', 'M10 6h4', 'M10 10h4', 'M10 14h4', 'M10 18h4'],
  list: ['M8 6h13', 'M8 12h13', 'M8 18h13', 'M3 6h.01', 'M3 12h.01', 'M3 18h.01'],
  bell: ['M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9', 'M10.3 21a1.94 1.94 0 0 0 3.4 0'],
  inbox: ['M22 12h-6l-2 3h-4l-2-3H2', 'M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z'],
  chart: ['M3 3v18h18', 'M7 16v-4', 'M12 16V8', 'M17 16v-7'],
  calendar: ['M8 2v4', 'M16 2v4', 'M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z', 'M3 10h18'],
  calendarCheck: ['M8 2v4', 'M16 2v4', 'M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z', 'M3 10h18', 'm9 16 2 2 4-4'],
  clock: ['M21 12a9 9 0 1 1-18 0a9 9 0 1 1 18 0', 'M12 7v5l3 2'],
  users: ['M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2', 'M13 7a4 4 0 1 1-8 0a4 4 0 1 1 8 0', 'M22 21v-2a4 4 0 0 0-3-3.87', 'M16 3.13a4 4 0 0 1 0 7.75'],
  user: ['M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2', 'M16 7a4 4 0 1 1-8 0a4 4 0 1 1 8 0'],
  plus: ['M12 5v14', 'M5 12h14'],
  edit: ['M12 20h9', 'M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z'],
  trash: ['M3 6h18', 'M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2', 'M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6'],
  check: ['M20 6 9 17l-5-5'],
  x: ['M18 6 6 18', 'M6 6l12 12'],
  login: ['M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4', 'M10 17l5-5-5-5', 'M15 12H3'],
  logout: ['M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4', 'M16 17l5-5-5-5', 'M21 12H9'],
  lock: ['M7 11V7a5 5 0 0 1 10 0v4', 'M5 11h14a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2z'],
  alert: ['M21 12a9 9 0 1 1-18 0a9 9 0 1 1 18 0', 'M12 8v4', 'M12 16h.01'],
  search: ['M19 11a8 8 0 1 1-16 0a8 8 0 1 1 16 0', 'm21 21-4.3-4.3'],
};

export function icon(name) {
  const ns = 'http://www.w3.org/2000/svg';
  const svg = document.createElementNS(ns, 'svg');
  svg.setAttribute('viewBox', '0 0 24 24');
  svg.setAttribute('aria-hidden', 'true');
  svg.setAttribute('class', 'icon');
  for (const d of ICONS[name] ?? []) {
    const path = document.createElementNS(ns, 'path');
    path.setAttribute('d', d);
    svg.append(path);
  }
  return svg;
}

// ---------- Уведомления и диалоги ----------

const MAX_TOASTS = 3;

export function toast(message, type = 'success') {
  const host = document.getElementById('toasts');
  const el = h('div', { class: `toast toast--${type}`, role: type === 'error' ? 'alert' : 'status' },
    icon(type === 'error' ? 'alert' : 'check'), h('span', {}, message));
  host.append(el);
  while (host.children.length > MAX_TOASTS) host.firstElementChild.remove();
  setTimeout(() => {
    el.classList.add('is-hiding');
    setTimeout(() => el.remove(), 300);
  }, 4000);
}

/** Модальный диалог на <dialog>: Esc, фокус и затемнение фона браузер делает сам. */
export function openDialog(title, body, { wide = false } = {}) {
  const dialog = h('dialog', { class: `dialog${wide ? ' dialog--wide' : ''}`, 'aria-label': title },
    h('div', { class: 'dialog__inner' },
      h('header', { class: 'dialog__head' },
        h('h2', {}, title),
        h('button', { class: 'icon-btn', type: 'button', 'aria-label': 'Закрыть', onclick: () => dialog.close() }, icon('x'))),
      body));

  // Закрываем по клику на фон, но не когда выделение текста в поле закончилось за его пределами.
  let pressedOnBackdrop = false;
  dialog.addEventListener('mousedown', (event) => { pressedOnBackdrop = event.target === dialog; });
  dialog.addEventListener('click', (event) => {
    if (pressedOnBackdrop && event.target === dialog) dialog.close();
  });
  dialog.addEventListener('close', () => dialog.remove());

  document.body.append(dialog);
  dialog.showModal();
  return dialog;
}

export function confirmDialog(title, message, { confirmText = 'Подтвердить', danger = false } = {}) {
  return new Promise((resolve) => {
    let confirmed = false;
    const dialog = openDialog(title, h('div', {},
      h('p', { class: 'dialog__text' }, message),
      h('div', { class: 'dialog__actions' },
        h('button', { class: 'btn btn--ghost', type: 'button', onclick: () => dialog.close() }, 'Отмена'),
        h('button', {
          class: `btn ${danger ? 'btn--danger' : 'btn--primary'}`,
          type: 'button',
          onclick: () => { confirmed = true; dialog.close(); },
        }, confirmText))));
    dialog.addEventListener('close', () => resolve(confirmed));
  });
}

// ---------- Формы и состояния страниц ----------

export function field(label, control, hint) {
  return h('label', { class: 'field' },
    h('span', { class: 'field__label' }, label),
    control,
    hint && h('span', { class: 'field__hint' }, hint));
}

export function errorBox() {
  return h('div', { class: 'form-error', role: 'alert', hidden: true });
}

export function showError(box, error) {
  box.textContent = typeof error === 'string' ? error : error.message;
  box.hidden = false;
}

/** Блокирует кнопку и показывает индикатор, пока выполняется действие. */
export async function withBusy(button, action) {
  button.disabled = true;
  button.classList.add('is-busy');
  try {
    return await action();
  } finally {
    button.disabled = false;
    button.classList.remove('is-busy');
  }
}

export function pageHeader(title, subtitle, ...actions) {
  return h('div', { class: 'page-head' },
    h('div', {}, h('h1', {}, title), subtitle && h('p', { class: 'page-head__sub' }, subtitle)),
    h('div', { class: 'page-head__actions' }, actions));
}

export function tabs(items, active, onSelect) {
  return h('div', { class: 'tabs', role: 'tablist' }, items.map((item) =>
    h('button', {
      class: `tabs__item${item.id === active ? ' is-active' : ''}`,
      type: 'button',
      role: 'tab',
      'aria-selected': String(item.id === active),
      onclick: () => onSelect(item.id),
    }, item.label, item.count !== undefined && h('span', { class: 'tabs__count' }, item.count))));
}

export function emptyState(iconName, title, text, action) {
  return h('div', { class: 'empty' },
    h('span', { class: 'empty__icon' }, icon(iconName)),
    h('h3', {}, title),
    text && h('p', {}, text),
    action);
}

export function errorState(error, retry) {
  return emptyState('alert', 'Не удалось загрузить данные', error.message,
    h('button', { class: 'btn btn--ghost', type: 'button', onclick: retry }, 'Повторить'));
}

export function skeleton(count, kind = 'card') {
  return Array.from({ length: count }, () => h('div', { class: `skeleton skeleton--${kind}`, 'aria-hidden': 'true' }));
}

// ---------- Статусы броней ----------

const STATUSES = {
  Pending: { label: 'Ожидает', tone: 'warn' },
  Approved: { label: 'Подтверждена', tone: 'ok' },
  Rejected: { label: 'Отклонена', tone: 'danger' },
  Cancelled: { label: 'Отменена', tone: 'muted' },
};

export const statusLabel = (status) => STATUSES[status]?.label ?? status;

export function statusBadge(status) {
  return h('span', { class: `status status--${STATUSES[status]?.tone ?? 'muted'}` }, statusLabel(status));
}

// ---------- Даты ----------
// Сервер хранит время в UTC; на странице всё показывается и вводится в часовом поясе браузера.

const pad = (n) => String(n).padStart(2, '0');

/** Date → "2026-10-01" (локальная дата, для <input type="date">). */
export const toDateInput = (date) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;

/** "2026-10-01" → локальная полночь этого дня. */
export const startOfDay = (dateStr) => new Date(`${dateStr}T00:00`);

/** "2026-10-01" → последняя миллисекунда этого дня (локально). */
export function endOfDay(dateStr) {
  const next = startOfDay(dateStr);
  next.setDate(next.getDate() + 1);
  return new Date(next.getTime() - 1);
}

/** "2026-10-01" + "09:30" → локальные дата и время. */
export const combine = (dateStr, timeStr) => new Date(`${dateStr}T${timeStr}`);

export const toTimeInput = (hours) => `${pad(hours)}:00`;

const timeFormat = new Intl.DateTimeFormat('ru-RU', { hour: '2-digit', minute: '2-digit' });
const dayFormat = new Intl.DateTimeFormat('ru-RU', { weekday: 'long', day: 'numeric', month: 'long' });
const shortDateTimeFormat = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
const monthFormat = new Intl.DateTimeFormat('ru-RU', { month: 'short' });
const weekdayFormat = new Intl.DateTimeFormat('ru-RU', { weekday: 'short' });
const numberFormat = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 1 });

export const formatTime = (value) => timeFormat.format(new Date(value));
export const formatDay = (value) => dayFormat.format(new Date(value));
export const formatNumber = (value) => numberFormat.format(value);

/** "1 октября, 09:00–11:00" или "1 окт., 22:00 – 2 окт., 02:00" для брони через полночь. */
export function formatRange(start, end) {
  const s = new Date(start);
  const e = new Date(end);
  if (toDateInput(s) === toDateInput(e)) {
    return `${formatDay(s)}, ${formatTime(s)}–${formatTime(e)}`;
  }
  return `${shortDateTimeFormat.format(s)} – ${shortDateTimeFormat.format(e)}`;
}

export function timeAgo(value) {
  const seconds = (Date.now() - new Date(value).getTime()) / 1000;
  const rtf = new Intl.RelativeTimeFormat('ru', { numeric: 'auto' });
  if (seconds < 60) return 'только что';
  if (seconds < 3600) return rtf.format(-Math.round(seconds / 60), 'minute');
  if (seconds < 86400) return rtf.format(-Math.round(seconds / 3600), 'hour');
  if (seconds < 7 * 86400) return rtf.format(-Math.round(seconds / 86400), 'day');
  return shortDateTimeFormat.format(new Date(value));
}

/** Русское склонение: plural(5, 'место', 'места', 'мест') → 'мест'. */
export function plural(n, one, few, many) {
  const mod10 = n % 10;
  const mod100 = n % 100;
  if (mod10 === 1 && mod100 !== 11) return one;
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return few;
  return many;
}

export function dateTile(value) {
  const date = new Date(value);
  return h('div', { class: 'date-tile', 'aria-hidden': 'true' },
    h('span', { class: 'date-tile__day' }, date.getDate()),
    h('span', { class: 'date-tile__month' }, monthFormat.format(date).replace('.', '')),
    h('span', { class: 'date-tile__weekday' }, weekdayFormat.format(date)));
}

// ---------- Шкала занятости помещения на день ----------

export const DAY_START = 8;
export const DAY_END = 22;

/**
 * Полоса с 8:00 до 22:00: занятые интервалы (подтверждённые и ожидающие),
 * прошедшее время, отметка «сейчас» и, в форме брони, выбранный интервал.
 */
export function timeline(dateStr, slots, { selection = null, conflict = false } = {}) {
  const dayStart = startOfDay(dateStr).getTime();
  const span = DAY_END - DAY_START;
  const position = (value) => {
    const hours = (new Date(value).getTime() - dayStart) / 3_600_000;
    return Math.min(Math.max((hours - DAY_START) / span, 0), 1) * 100;
  };
  const block = (start, end, className, title) => {
    const left = position(start);
    const width = position(end) - left;
    return width > 0 ? h('div', { class: className, style: `left:${left}%;width:${width}%`, title }) : null;
  };

  const now = new Date();
  const isToday = toDateInput(now) === dateStr;
  const track = h('div', { class: 'timeline__track' },
    isToday && block(dayStart, now, 'timeline__past', 'Прошедшее время'),
    slots.map((slot) => block(slot.startTime, slot.endTime,
      `timeline__slot timeline__slot--${slot.status.toLowerCase()}`,
      `${formatTime(slot.startTime)}–${formatTime(slot.endTime)} · ${statusLabel(slot.status)}`)),
    selection && block(selection.start, selection.end,
      `timeline__selection${conflict ? ' is-conflict' : ''}`, 'Выбранное время'));

  if (isToday) {
    const nowPosition = position(now);
    if (nowPosition > 0 && nowPosition < 100) {
      track.append(h('div', { class: 'timeline__now', style: `left:${nowPosition}%`, title: `Сейчас ${formatTime(now)}` }));
    }
  }

  const ticks = [];
  for (let hour = DAY_START; hour <= DAY_END; hour += 2) {
    ticks.push(h('span', { style: `left:${((hour - DAY_START) / span) * 100}%` }, `${hour}:00`));
  }

  return h('div', { class: 'timeline', role: 'img', 'aria-label': `Занятость с ${DAY_START}:00 до ${DAY_END}:00` },
    track, h('div', { class: 'timeline__ticks', 'aria-hidden': 'true' }, ticks));
}

export function timelineLegend() {
  const item = (kind, label) => h('span', { class: 'legend__item' }, h('span', { class: `legend__swatch legend__swatch--${kind}` }), label);
  return h('div', { class: 'legend' },
    item('approved', 'Подтверждено'),
    item('pending', 'Ожидает подтверждения'),
    item('selection', 'Ваш выбор'));
}
