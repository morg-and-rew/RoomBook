import { api, getSession } from '../api.js';
import { openAuthDialog } from '../auth.js';
import {
  h, icon, toast, openDialog, confirmDialog, field, errorBox, showError, withBusy, pageHeader,
  emptyState, errorState, skeleton, timeline, timelineLegend, toDateInput, toTimeInput, startOfDay,
  combine, formatTime, formatDay, plural, DAY_START, DAY_END,
} from '../ui.js';
import { refreshNotificationBadge } from './notifications.js';

// Фильтры сохраняются при переходах между разделами.
const filters = { date: toDateInput(new Date()), capacity: '', equipment: new Set() };
let knownEquipment = [];

export function roomsPage(view, { user }) {
  const isAdmin = user?.role === 'Admin';
  let loadId = 0;
  let capacityTimer;

  const grid = h('div', { class: 'room-grid' });
  const summary = h('p', { class: 'results-note' });
  const chips = h('div', { class: 'chips' });

  const dateInput = h('input', {
    type: 'date',
    required: true,
    value: filters.date,
    onchange: () => {
      filters.date = dateInput.value || toDateInput(new Date());
      dateInput.value = filters.date;
      load();
    },
  });
  const capacityInput = h('input', {
    type: 'number',
    min: 1,
    max: 1000,
    inputmode: 'numeric',
    placeholder: 'Любая',
    value: filters.capacity,
    oninput: () => {
      clearTimeout(capacityTimer);
      capacityTimer = setTimeout(() => {
        filters.capacity = capacityInput.value;
        load();
      }, 300);
    },
  });

  // После входа страница перерисовывается заново, поэтому старый экземпляр
  // просит приложение обновить текущий раздел целиком.
  const reload = () => (view.isConnected ? load() : window.dispatchEvent(new Event('app-refresh')));

  view.append(
    pageHeader('Помещения', 'Выберите дату, чтобы увидеть занятость, и забронируйте свободное время.',
      isAdmin && h('button', { class: 'btn btn--primary', type: 'button', onclick: () => openRoomDialog(null, reload) },
        icon('plus'), 'Добавить помещение')),
    h('section', { class: 'card filters', 'aria-label': 'Фильтры' },
      field('Дата', dateInput),
      field('Вместимость, от', capacityInput),
      h('div', { class: 'field filters__equipment' }, h('span', { class: 'field__label' }, 'Оборудование'), chips)),
    summary,
    grid);

  renderChips();
  load();

  async function load() {
    const id = ++loadId;
    grid.replaceChildren(...skeleton(3));
    try {
      const rooms = await api.rooms({
        date: startOfDay(filters.date).toISOString(),
        capacity: filters.capacity || undefined,
        equipment: [...filters.equipment],
      });
      if (id !== loadId || !view.isConnected) return;

      if (!filters.capacity && filters.equipment.size === 0) {
        knownEquipment = [...new Set(rooms.flatMap((room) => room.equipment))].sort((a, b) => a.localeCompare(b, 'ru'));
      }
      renderChips();
      summary.textContent = rooms.length
        ? `${rooms.length} ${plural(rooms.length, 'помещение', 'помещения', 'помещений')} · ${formatDay(startOfDay(filters.date))}`
        : '';
      grid.replaceChildren(...(rooms.length ? rooms.map(roomCard) : [emptyRooms()]));
    } catch (err) {
      if (id === loadId && view.isConnected) grid.replaceChildren(errorState(err, load));
    }
  }

  function renderChips() {
    const names = [...new Set([...knownEquipment, ...filters.equipment])];
    chips.replaceChildren(...(names.length
      ? names.map((name) => {
        const active = filters.equipment.has(name);
        return h('button', {
          class: `chip${active ? ' is-active' : ''}`,
          type: 'button',
          'aria-pressed': String(active),
          onclick: () => {
            if (active) filters.equipment.delete(name);
            else filters.equipment.add(name);
            renderChips();
            load();
          },
        }, name);
      })
      : [h('span', { class: 'muted small' }, 'Появится, когда у помещений будет оборудование')]));
  }

  function emptyRooms() {
    if (filters.capacity || filters.equipment.size) {
      return emptyState('search', 'Ничего не найдено', 'Под выбранные фильтры не подходит ни одно помещение.',
        h('button', {
          class: 'btn btn--ghost',
          type: 'button',
          onclick: () => {
            filters.capacity = '';
            filters.equipment.clear();
            capacityInput.value = '';
            renderChips();
            load();
          },
        }, 'Сбросить фильтры'));
    }
    return isAdmin
      ? emptyState('building', 'Помещений пока нет', 'Добавьте первое помещение — оно сразу станет доступно для бронирования.',
        h('button', { class: 'btn btn--primary', type: 'button', onclick: () => openRoomDialog(null, reload) }, icon('plus'), 'Добавить помещение'))
      : emptyState('building', 'Помещений пока нет', 'Администратор ещё не добавил помещения.');
  }

  function roomCard(room) {
    const slots = room.busySlots ?? [];
    const isPastDay = filters.date < toDateInput(new Date());
    return h('article', { class: 'card room' },
      h('header', { class: 'room__head' },
        h('h3', { class: 'room__name' }, room.name),
        h('span', { class: 'room__capacity', title: 'Вместимость' },
          icon('users'), `${room.capacity} ${plural(room.capacity, 'место', 'места', 'мест')}`)),
      h('div', { class: 'tags' }, room.equipment.length
        ? room.equipment.map((item) => h('span', { class: 'tag' }, item))
        : h('span', { class: 'muted small' }, 'Без оборудования')),
      timeline(filters.date, slots),
      h('p', { class: `room__status${slots.length ? '' : ' is-free'}` }, slots.length
        ? `Занято: ${slots.map((slot) => `${formatTime(slot.startTime)}–${formatTime(slot.endTime)}`).join(', ')}`
        : 'Свободно весь день'),
      h('footer', { class: 'room__actions' },
        h('button', {
          class: 'btn btn--primary',
          type: 'button',
          disabled: isPastDay,
          title: isPastDay ? 'Этот день уже прошёл' : null,
          onclick: () => startBooking(room),
        }, 'Забронировать'),
        isAdmin && h('button', {
          class: 'icon-btn', type: 'button', title: 'Изменить', 'aria-label': `Изменить «${room.name}»`,
          onclick: () => openRoomDialog(room, reload),
        }, icon('edit')),
        isAdmin && h('button', {
          class: 'icon-btn icon-btn--danger', type: 'button', title: 'Скрыть', 'aria-label': `Скрыть «${room.name}»`,
          onclick: () => hideRoom(room),
        }, icon('trash'))));
  }

  async function startBooking(room) {
    if (!getSession() && !(await openAuthDialog('login'))) return;
    openBookingDialog(room, filters.date, reload);
  }

  async function hideRoom(room) {
    const confirmed = await confirmDialog('Скрыть помещение?',
      `«${room.name}» пропадёт из списка и станет недоступно для бронирования. История броней сохранится.`,
      { confirmText: 'Скрыть', danger: true });
    if (!confirmed) return;
    try {
      await api.deleteRoom(room.id);
      toast(`Помещение «${room.name}» скрыто`);
      reload();
    } catch (err) {
      toast(err.message, 'error');
    }
  }
}

function openBookingDialog(room, dateStr, onDone) {
  const now = new Date();
  const today = toDateInput(now);
  const date = dateStr < today ? today : dateStr;
  const startHour = date === today ? Math.min(Math.max(now.getHours() + 1, DAY_START), DAY_END - 1) : 9;

  const dateInput = h('input', { type: 'date', required: true, min: today, value: date });
  const startInput = h('input', { type: 'time', required: true, step: 900, value: toTimeInput(startHour) });
  const endInput = h('input', { type: 'time', required: true, step: 900, value: toTimeInput(startHour + 1) });
  const purposeInput = h('textarea', { rows: 3, maxlength: 500, placeholder: 'Например: защита курсового проекта' });
  const preview = h('div', { class: 'booking-preview' });
  const error = errorBox();
  const submit = h('button', { class: 'btn btn--primary', type: 'submit' }, 'Отправить заявку');

  let slots = date === dateStr ? room.busySlots ?? [] : [];
  let slotsDate = date === dateStr ? date : null;

  const selection = () => {
    if (!dateInput.value || !startInput.value || !endInput.value) return null;
    const start = combine(dateInput.value, startInput.value);
    const end = combine(dateInput.value, endInput.value);
    return end > start ? { start, end } : null;
  };

  function renderPreview() {
    const day = dateInput.value || date;
    const daySlots = slotsDate === day ? slots : [];
    const range = selection();
    const conflict = Boolean(range) && daySlots.some((slot) =>
      new Date(slot.startTime) < range.end && new Date(slot.endTime) > range.start);
    // replaceChildren превратил бы false в текст «false», поэтому пустые элементы отбрасываем.
    preview.replaceChildren(...[
      timeline(day, daySlots, { selection: range, conflict }),
      timelineLegend(),
      conflict && h('p', { class: 'notice notice--danger' }, icon('alert'), 'Выбранное время пересекается с другой бронью.'),
      !range && h('p', { class: 'notice notice--warn' }, icon('alert'), 'Время окончания должно быть позже начала.'),
    ].filter(Boolean));
  }

  async function loadSlots() {
    const day = dateInput.value;
    if (!day || day === slotsDate) {
      renderPreview();
      return;
    }
    renderPreview();
    try {
      const rooms = await api.rooms({ date: startOfDay(day).toISOString() });
      if (dateInput.value !== day) return; // дату успели поменять ещё раз
      slots = rooms.find((r) => r.id === room.id)?.busySlots ?? [];
    } catch {
      slots = [];
    }
    slotsDate = day;
    renderPreview();
  }

  dateInput.addEventListener('change', loadSlots);
  startInput.addEventListener('input', renderPreview);
  endInput.addEventListener('input', renderPreview);

  const form = h('form', {
    class: 'form',
    onsubmit: async (event) => {
      event.preventDefault();
      error.hidden = true;
      const range = selection();
      if (!range) {
        showError(error, 'Время окончания должно быть позже времени начала.');
        return;
      }
      if (range.start < new Date()) {
        showError(error, 'Нельзя забронировать время, которое уже прошло.');
        return;
      }
      await withBusy(submit, async () => {
        try {
          await api.createBooking({
            roomId: room.id,
            startTime: range.start.toISOString(),
            endTime: range.end.toISOString(),
            purpose: purposeInput.value.trim() || null,
          });
          dialog.close();
          toast('Заявка отправлена и ждёт подтверждения администратора.');
          refreshNotificationBadge();
          onDone();
        } catch (err) {
          showError(error, err);
        }
      });
    },
  },
    h('div', { class: 'booking-summary' },
      h('span', { class: 'room__capacity' }, icon('users'), `${room.capacity} ${plural(room.capacity, 'место', 'места', 'мест')}`),
      room.equipment.map((item) => h('span', { class: 'tag' }, item))),
    error,
    h('div', { class: 'form__row' },
      field('Дата', dateInput),
      field('Начало', startInput),
      field('Окончание', endInput)),
    preview,
    field('Цель', purposeInput),
    h('div', { class: 'dialog__actions' },
      h('button', { class: 'btn btn--ghost', type: 'button', onclick: () => dialog.close() }, 'Отмена'),
      submit));

  const dialog = openDialog(`Бронирование: ${room.name}`, form, { wide: true });
  loadSlots();
}

function openRoomDialog(room, onDone) {
  const isEdit = Boolean(room);
  const name = h('input', { required: true, maxlength: 200, placeholder: 'Переговорная «Байкал»', value: room?.name ?? '' });
  const capacity = h('input', { type: 'number', required: true, min: 1, max: 1000, value: room?.capacity ?? 10 });
  const equipment = h('input', { placeholder: 'Проектор, Доска, Видеосвязь', value: room?.equipment.join(', ') ?? '' });
  const error = errorBox();
  const submit = h('button', { class: 'btn btn--primary', type: 'submit' }, isEdit ? 'Сохранить' : 'Добавить');

  const parseEquipment = () => [...new Set(equipment.value.split(',').map((item) => item.trim()).filter(Boolean))];
  const suggestions = knownEquipment.length > 0 && h('div', { class: 'chips chips--small' },
    knownEquipment.map((item) => h('button', {
      class: 'chip',
      type: 'button',
      onclick: () => {
        const items = parseEquipment();
        if (!items.includes(item)) equipment.value = [...items, item].join(', ');
      },
    }, `+ ${item}`)));

  const form = h('form', {
    class: 'form',
    onsubmit: async (event) => {
      event.preventDefault();
      error.hidden = true;
      const payload = { name: name.value.trim(), capacity: Number(capacity.value), equipment: parseEquipment() };
      await withBusy(submit, async () => {
        try {
          if (isEdit) await api.updateRoom(room.id, { ...payload, isActive: true });
          else await api.createRoom(payload);
          dialog.close();
          toast(isEdit ? 'Изменения сохранены' : `Помещение «${payload.name}» добавлено`);
          onDone();
        } catch (err) {
          showError(error, err);
        }
      });
    },
  },
    error,
    field('Название', name),
    field('Вместимость, человек', capacity),
    field('Оборудование', equipment, 'Через запятую'),
    suggestions,
    h('div', { class: 'dialog__actions' },
      h('button', { class: 'btn btn--ghost', type: 'button', onclick: () => dialog.close() }, 'Отмена'),
      submit));

  const dialog = openDialog(isEdit ? 'Изменить помещение' : 'Новое помещение', form);
  name.focus();
}
