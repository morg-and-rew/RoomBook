// Клиент RoomBook API: хранит JWT и приводит ошибки сервера к одному виду.

const SESSION_KEY = 'roombook.session';

export class ApiError extends Error {
  constructor(status, message) {
    super(message);
    this.status = status;
  }
}

let session = readStoredSession();

function readStoredSession() {
  try {
    return JSON.parse(localStorage.getItem(SESSION_KEY));
  } catch {
    return null;
  }
}

/** Текущая сессия { token, expiresAt, user } или null, если вход не выполнен или токен истёк. */
export function getSession() {
  if (session && new Date(session.expiresAt) <= new Date()) {
    storeSession(null);
    // Событие — после текущей отрисовки, чтобы не перерисовывать страницу изнутри самой себя.
    queueMicrotask(() => window.dispatchEvent(new CustomEvent('session-change', { detail: { reason: 'expired' } })));
  }
  return session;
}

/** Сохраняет сессию и сообщает приложению, что пользователь сменился. */
export function setSession(value, reason) {
  storeSession(value);
  window.dispatchEvent(new CustomEvent('session-change', { detail: { reason } }));
}

function storeSession(value) {
  session = value;
  try {
    if (value) localStorage.setItem(SESSION_KEY, JSON.stringify(value));
    else localStorage.removeItem(SESSION_KEY);
  } catch {
    // localStorage недоступен (приватный режим и т.п.) — сессия живёт до перезагрузки страницы.
  }
}

async function request(method, path, { body, query } = {}) {
  const url = new URL(path, window.location.origin);
  for (const [key, value] of Object.entries(query ?? {})) {
    for (const item of [value].flat()) {
      if (item !== undefined && item !== null && item !== '') url.searchParams.append(key, item);
    }
  }

  const headers = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const current = getSession();
  if (current) headers.Authorization = `Bearer ${current.token}`;

  let response;
  try {
    response = await fetch(url, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, 'Сервер недоступен. Проверьте, что API запущен.');
  }

  const text = await response.text();
  let data = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = null;
    }
  }

  if (response.ok) return data;

  if (response.status === 401 && current) {
    setSession(null, 'expired');
  }
  throw new ApiError(response.status, errorMessage(response.status, data));
}

function errorMessage(status, data) {
  // Ошибки валидации ASP.NET: { title, errors: { Поле: ["сообщение"] } }
  if (data?.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors).flat().filter(Boolean);
    if (messages.length) return messages.join(' ');
  }
  // Ошибки приложения из ExceptionHandlingMiddleware: { code, message }
  if (data?.message) return data.message;
  if (status === 401) return 'Нужно войти в систему.';
  if (status === 403) return 'Недостаточно прав для этого действия.';
  if (status === 404) return 'Не найдено.';
  return data?.title ?? `Ошибка сервера (${status}).`;
}

/** Скачивает файл с авторизацией (обычная ссылка не передала бы JWT). */
async function download(path, query, fallbackName) {
  const url = new URL(path, window.location.origin);
  for (const [key, value] of Object.entries(query)) url.searchParams.append(key, value);
  const current = getSession();
  let response;
  try {
    response = await fetch(url, { headers: current ? { Authorization: `Bearer ${current.token}` } : {} });
  } catch {
    throw new ApiError(0, 'Сервер недоступен. Проверьте, что API запущен.');
  }
  if (!response.ok) throw new ApiError(response.status, errorMessage(response.status, null));

  const disposition = response.headers.get('Content-Disposition') ?? '';
  const name = /filename\*=UTF-8''([^;]+)/.exec(disposition)?.[1] ?? /filename="?([^";]+)"?/.exec(disposition)?.[1];
  const link = document.createElement('a');
  link.href = URL.createObjectURL(await response.blob());
  link.download = name ? decodeURIComponent(name) : fallbackName;
  link.click();
  setTimeout(() => URL.revokeObjectURL(link.href), 1000);
}

export const api = {
  login: (email, password) =>
    request('POST', '/api/auth/login', { body: { email, password } }),
  register: (fullName, email, password) =>
    request('POST', '/api/auth/register', { body: { fullName, email, password } }),
  updateProfile: (fullName) => request('PUT', '/api/users/me', { body: { fullName } }),

  equipment: () => request('GET', '/api/equipment'),
  rooms: (query) => request('GET', '/api/rooms', { query }),
  createRoom: (room) => request('POST', '/api/rooms', { body: room }),
  updateRoom: (id, room) => request('PUT', `/api/rooms/${id}`, { body: room }),
  deactivateRoom: (id) => request('DELETE', `/api/rooms/${id}`),

  createBooking: (booking) => request('POST', '/api/bookings', { body: booking }),
  myBookings: () => request('GET', '/api/bookings/my'),
  allBookings: () => request('GET', '/api/bookings'),
  cancelBooking: (id) => request('DELETE', `/api/bookings/${id}`),
  confirmBooking: (id) => request('PUT', `/api/bookings/${id}/confirm`),
  rejectBooking: (id, reason) =>
    request('PUT', `/api/bookings/${id}/reject`, { body: { reason } }),

  notifications: () => request('GET', '/api/notifications/my'),
  markAllNotificationsRead: () => request('PUT', '/api/notifications/read-all'),

  utilization: (from, to) =>
    request('GET', '/api/reports/utilization', { query: { from, to } }),
  downloadUtilizationCsv: (from, to) =>
    download('/api/reports/utilization/csv', { from, to }, 'utilization.csv'),
};
