# RoomBook

Сервис бронирования аудиторий и переговорных комнат:
ASP.NET Core 8 Web API + PostgreSQL + Entity Framework Core и веб-интерфейс,
который раздаёт тот же сервис.

## Стек
- .NET 8 / C#
- PostgreSQL (через Npgsql.EntityFrameworkCore.PostgreSQL)
- JWT-аутентификация
- Swagger (OpenAPI) — доступен в Development-режиме
- Веб-интерфейс — HTML/CSS/JavaScript без фреймворков и сборки (`wwwroot/`)

## Архитектура

Код следует UML-диаграмме классов и ER-диаграмме БД из лабораторной работы №3.

```
src/RoomBook.Api/
  Controllers/        — HTTP-эндпоинты (Presentation layer)
  Dtos/               — объекты передачи данных и их маппинг из сущностей
  Services/           — сервисы (Application layer): User, Room, Booking, Notification, Report
  Repositories/       — интерфейсы репозиториев (пакет Repositories на диаграмме)
  Data/               — AppDbContext и реализации репозиториев на EF Core (Data Access layer)
  Entities/           — сущности с поведением и перечисления (пакеты Entities и Enums)
  Migrations/         — схема БД
  Common/             — обработка ошибок, JWT-хелперы, работа с датами
  wwwroot/            — веб-интерфейс (index.html, css/, js/)
```

Запрос проходит слои строго сверху вниз: контроллер → сервис → репозиторий → БД.
Бизнес-правила живут в сущностях, сервисы их вызывают.

| Диаграмма классов | Код |
|---|---|
| `UserRole`, `BookingStatus`, `NotificationType` | `Entities/Enums.cs` |
| `User`: `isAdmin()`, `verifyPassword()` | `Entities/User.cs` |
| `Room`: `addEquipment()`, `removeEquipment()`, `deactivate()`, `matchesFilter()` | `Entities/Room.cs` |
| `Equipment`: `getDisplayName()` | `Entities/Equipment.cs` |
| `Booking`: `confirm()`, `reject()`, `cancel()`, `isActive()`, `overlapsWith()` | `Entities/Booking.cs` |
| `Notification`: `markAsRead()` | `Entities/Notification.cs` |
| `UserService`: `register`, `authenticate`, `updateProfile` | `Services/UserService.cs` |
| `RoomService`: `listRooms`, `getRoom`, `createRoom`, `updateRoom`, `deactivateRoom` | `Services/RoomService.cs` |
| `BookingService`: `createBooking`, `confirmBooking`, `rejectBooking`, `cancelBooking`, `findConflicts`, `listUserBookings` | `Services/BookingService.cs` |
| `NotificationService`: `notifyStatusChange`, `notifyUser` | `Services/NotificationService.cs` |
| `ReportService`: `utilizationReport`, `exportCsv` | `Services/ReportService.cs` |
| `UserRepository`, `RoomRepository`, `BookingRepository`, `NotificationRepository` | `Repositories/I*Repository.cs` + `Data/Repositories/*Repository.cs` |

По соглашениям C# интерфейсы названы с префиксом `I`, а асинхронные методы — с
суффиксом `Async` (`findConflicts` → `FindConflictsAsync`).

Что добавлено сверх диаграммы, потому что без этого не работают сайт и API:
- `IEquipmentRepository` — справочник оборудования для форм и фильтров;
- `UserService.GetByIdAsync` — пользователь из JWT текущего запроса;
- `BookingService.ListAllBookingsAsync` и `IBookingRepository.FindAllAsync` — список заявок для администратора;
- `IBookingRepository.FindConfirmedStartingInAsync` — выборка для отчёта по всем помещениям;
- `NotificationService.ListUserNotificationsAsync`, `MarkAsReadAsync`, `MarkAllAsReadAsync` — чтение уведомлений.

### База данных (ER-диаграмма)

| Таблица | Колонки |
|---|---|
| `user` | `id`, `email`, `password_hash`, `full_name`, `role`, `created_at` |
| `room` | `id`, `name`, `building`, `floor`, `capacity`, `description`, `is_active`, `created_at` |
| `equipment` | `id`, `code`, `name` — справочник, заполняется миграцией |
| `room_equipment` | `room_id`, `equipment_id` — оснащение помещений (многие-ко-многим) |
| `booking` | `id`, `room_id`, `user_id`, `purpose`, `start_at`, `end_at`, `status`, `reject_reason`, `decided_at`, `decided_by`, `created_at` |
| `notification` | `id`, `user_id`, `booking_id`, `type`, `message`, `sent_at`, `read_at` |

Роль, статус и тип уведомления хранятся строками (`'Admin'`, `'Confirmed'` …).
Расхождения диаграмм между собой решены так: `building` — строка (как на
диаграмме классов), у уведомления — `read_at` (`readAt` на диаграмме классов) вместо
`delivered_at`, `password_hash` есть в таблице `user` (он нужен для `verifyPassword`).

Пересечение броней запрещает сама PostgreSQL: на таблице `booking` есть
ограничение-исключение `ex_booking_room_period` (расширение `btree_gist`). Две активные
заявки (`Pending`/`Confirmed`) на одно помещение не могут пересекаться по времени,
даже если приходят одновременно. Вторая получает ответ 409.

## Запуск локально

Нужны [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) и PostgreSQL.

### 1. Поднять PostgreSQL
Любой из вариантов:
- **Уже установленный PostgreSQL** — сервис ожидает `postgres/postgres@localhost:5432`.
  Если логин/пароль/хост отличаются — поправь строку подключения в
  `src/RoomBook.Api/appsettings.json` (`ConnectionStrings:Default`).
- **Docker** — из корня репозитория:
  ```bash
  docker compose up -d
  ```

Создавать базу `roombook` вручную не нужно.

### 2. Запустить сервис
```bash
cd src/RoomBook.Api
dotnet run
```
- Сайт: http://localhost:5080
- Swagger UI: http://localhost:5080/swagger

В Visual Studio / Rider достаточно нажать F5 — браузер с сайтом откроется сам.
Профиль `https` дополнительно слушает https://localhost:7080 (для него нужен
доверенный dev-сертификат: `dotnet dev-certs https --trust`).

При старте в режиме Development сервис сам создаёт базу `roombook` и применяет
миграции из `src/RoomBook.Api/Migrations`: появятся таблицы `user`, `room`,
`equipment`, `room_equipment`, `booking`, `notification`.

> **Если база `roombook` создана старой версией проекта** (таблицы `Users`, `Rooms`…),
> удалите её один раз: `DROP DATABASE roombook;` (в psql или pgAdmin). При следующем
> `dotnet run` она создастся заново по новой схеме.

### Новые миграции
После изменения сущностей:
```bash
dotnet tool install --global dotnet-ef --version 8.0.8   # один раз
cd src/RoomBook.Api
dotnet ef migrations add <Название>
```
Миграция применится автоматически при следующем запуске.

## Веб-интерфейс

Открывается на http://localhost:5080 после `dotnet run`, отдельно ничего ставить не нужно.

- **Помещения** — карточки с вместимостью, оборудованием и шкалой занятости на
  выбранный день (подтверждённые и ожидающие брони, прошедшее время, «сейчас»);
  корпус, этаж и описание; фильтры по дате, вместимости, корпусу и оборудованию.
  Гостю доступно без входа.
- **Бронирование** — дата и время, на шкале видно выбранный интервал и
  пересечения с чужими бронями ещё до отправки заявки.
- **Мои брони** — предстоящие и все заявки со статусами, отмена.
- **Уведомления** — события по заявкам; счётчик новых в меню.
- **Заявки** (администратор) — подтверждение, отклонение с причиной, отмена подтверждённой брони.
- **Отчёт** (администратор) — загруженность помещений за период: показатели,
  диаграмма часов по помещениям и выгрузка в CSV для Excel.
- **Профиль** — щелчок по своему имени в шапке позволяет изменить имя.
- Администратор добавляет, редактирует и скрывает помещения прямо на странице
  «Помещения». Есть тёмная тема и мобильная вёрстка.

Время на сайте вводится и показывается в часовом поясе браузера, на сервер
уходит в UTC.

## Как выдать себе права администратора

Все новые пользователи регистрируются с ролью `User`. Чтобы протестировать
эндпоинты администратора (`/api/rooms` POST/PUT/DELETE, `/api/bookings/{id}/confirm`,
`/api/reports/utilization`), зарегистрируйся через `/api/auth/register`, а затем
вручную повысь роль в базе (psql, pgAdmin, DBeaver и т.п.):
```sql
UPDATE "user" SET role = 'Admin' WHERE email = 'your@email.com';
```
(`user` — зарезервированное слово PostgreSQL, поэтому имя таблицы в кавычках;
email хранится в нижнем регистре). После этого получи новый токен через
`/api/auth/login` — в нём уже будет claim `role = Admin`.

## Быстрая проверка сценария (happy path)

1. `POST /api/auth/register` — создать пользователя.
2. `POST /api/auth/login` — получить JWT, положить в заголовок `Authorization: Bearer <token>`.
3. Повысить себя до Admin (см. выше) и получить новый токен.
4. `GET /api/equipment` — коды оборудования; `POST /api/rooms` — создать помещение
   (`name`, `building`, `floor`, `capacity`, `description`, `equipmentCodes: ["PROJECTOR"]`).
5. Залогиниться обычным пользователем, `GET /api/rooms` — увидеть помещение.
6. `POST /api/bookings` — создать заявку (`roomId`, `startAt`, `endAt`, `purpose`).
7. Под Admin-токеном: `GET /api/bookings?status=Pending` — найти заявку,
   `PUT /api/bookings/{id}/confirm` — подтвердить (или `.../reject` с причиной).
8. `GET /api/bookings/my` — увидеть заявку в статусе `Confirmed`.
9. `GET /api/notifications/my` — увидеть уведомление о подтверждении;
   `PUT /api/notifications/read-all` — отметить прочитанными.
10. `GET /api/reports/utilization?from=...&to=...` (Admin) — отчёт,
    `GET /api/reports/utilization/csv?from=...&to=...` — он же в CSV.

Даты хранятся в UTC. Время без часового пояса (`2026-10-01T09:00:00`, `2026-10-01`)
считается заданным в UTC, время со смещением (`2026-10-01T12:00:00+03:00`)
переводится в UTC.

## Соответствие функциональным требованиям

| Требование | Где реализовано |
|---|---|
| ФТ1 — список помещений и занятость | `GET /api/rooms?date=...` (`busySlots` — занятые интервалы на сутки) |
| ФТ2 — регистрация/вход | `AuthController`, `UserService.RegisterAsync/AuthenticateAsync` |
| ФТ3 — фильтр по вместимости/оборудованию | `RoomService.ListRoomsAsync`, `RoomRepository.FindAvailableAsync`, `Room.MatchesFilter` |
| ФТ4 — создание брони | `BookingService.CreateBookingAsync` |
| ФТ5/ФТ11 (US11) — проверка конфликтов | `BookingService.FindConflictsAsync`, `Booking.OverlapsWith`, ограничение `ex_booking_room_period` |
| ФТ6 — список своих броней | `BookingService.ListUserBookingsAsync`, `GET /api/bookings/my` |
| ФТ7 — отмена брони | `BookingService.CancelBookingAsync`, `Booking.Cancel` |
| ФТ8 — уведомления | `NotificationService.NotifyStatusChangeAsync`, `GET /api/notifications/my` |
| ФТ9 — управление помещениями | `RoomService`, `RoomsController` (Admin) |
| ФТ10 — подтверждение/отклонение заявки | `BookingService.ConfirmBookingAsync/RejectBookingAsync`, `Booking.Confirm/Reject` |
| ФТ11 (US10) — отчёт по загруженности | `ReportService.UtilizationReportAsync/ExportCsv`, `ReportsController` |
| НФТ2 — безопасность (JWT) | `Program.cs`, `[Authorize]` |
| НФТ3 — согласованность БД | ограничение-исключение `ex_booking_room_period` в PostgreSQL |
