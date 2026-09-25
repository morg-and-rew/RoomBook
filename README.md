# RoomBook API

Серверная часть (backend) сервиса бронирования аудиторий и переговорных комнат.
ASP.NET Core 8 Web API + PostgreSQL + Entity Framework Core.

## Стек
- .NET 8 / C#
- PostgreSQL (через Npgsql.EntityFrameworkCore.PostgreSQL)
- JWT-аутентификация
- Swagger (OpenAPI) — доступен в Development-режиме

## Структура проекта
```
src/RoomBook.Api/
  Controllers/     — HTTP-эндпоинты (Presentation layer)
  Services/        — бизнес-логика (Application layer)
  Data/            — AppDbContext (Data Access layer)
  Entities/        — модель данных (User, Room, Booking, Notification)
  Dtos/            — объекты передачи данных между клиентом и сервером
  Common/          — сквозные механизмы: обработка ошибок, JWT-хелперы
```

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
Swagger UI: http://localhost:5080/swagger

В Visual Studio / Rider достаточно нажать F5 — браузер со Swagger откроется сам.
Профиль `https` дополнительно слушает https://localhost:7080 (для него нужен
доверенный dev-сертификат: `dotnet dev-certs https --trust`).

При старте в режиме Development сервис сам создаёт базу `roombook` и применяет
миграции из `src/RoomBook.Api/Migrations` — появятся таблицы `Users`, `Rooms`,
`Bookings`, `Notifications`.

### Новые миграции
После изменения сущностей:
```bash
dotnet tool install --global dotnet-ef --version 8.0.8   # один раз
cd src/RoomBook.Api
dotnet ef migrations add <Название>
```
Миграция применится автоматически при следующем запуске.

## Как выдать себе права администратора

Все новые пользователи регистрируются с ролью `User`. Чтобы протестировать
эндпоинты администратора (`/api/rooms` POST/PUT/DELETE, `/api/bookings/{id}/approve`,
`/api/reports/occupancy`), зарегистрируйся через `/api/auth/register`, а затем
вручную повысь роль в базе (psql, pgAdmin, DBeaver и т.п.):
```sql
UPDATE "Users" SET "Role" = 1 WHERE "Email" = 'your@email.com';
```
(`"Role" = 1` соответствует `UserRole.Admin`; имена таблиц и колонок EF создаёт
в PascalCase, поэтому в PostgreSQL их нужно брать в кавычки). После этого получи новый токен через
`/api/auth/login` — в нём уже будет claim `role = Admin`.

## Быстрая проверка сценария (happy path)

1. `POST /api/auth/register` — создать пользователя.
2. `POST /api/auth/login` — получить JWT, положить в заголовок `Authorization: Bearer <token>`.
3. Повысить себя до Admin (см. выше) и получить новый токен.
4. `POST /api/rooms` — создать помещение (`name`, `capacity`, `equipment: ["Проектор"]`).
5. Залогиниться обычным пользователем, `GET /api/rooms` — увидеть помещение.
6. `POST /api/bookings` — создать заявку на бронирование.
7. Под Admin-токеном: `PUT /api/bookings/{id}/approve` — подтвердить.
8. `GET /api/bookings/my` — увидеть заявку в статусе `Approved`.
9. `GET /api/notifications/my` — увидеть уведомление о подтверждении.
10. `GET /api/reports/occupancy?dateFrom=...&dateTo=...` (Admin) — увидеть отчёт.

Даты хранятся в UTC. Время без часового пояса (`2026-10-01T09:00:00`, `2026-10-01`)
считается заданным в UTC, время со смещением (`2026-10-01T12:00:00+03:00`)
переводится в UTC.

## Соответствие функциональным требованиям

| Требование | Где реализовано |
|---|---|
| ФТ1 — список помещений и занятость | `GET /api/rooms` |
| ФТ2 — регистрация/вход | `AuthController`, `AuthService` |
| ФТ3 — фильтр по вместимости/оборудованию | `RoomService.GetAllAsync` |
| ФТ4 — создание брони | `BookingService.CreateAsync` |
| ФТ5/ФТ11 (US11) — проверка конфликтов | `BookingService.CreateAsync` (транзакция Serializable) |
| ФТ6 — список своих броней | `GET /api/bookings/my` |
| ФТ7 — отмена брони | `BookingService.CancelAsync` |
| ФТ8 — уведомления | `NotificationService`, `GET /api/notifications/my` |
| ФТ9 — управление помещениями | `RoomsController` (Admin) |
| ФТ10 — подтверждение/отклонение заявки | `BookingService.ApproveAsync/RejectAsync` |
| ФТ11 (US10) — отчёт по загруженности | `ReportsController` |
| НФТ2 — безопасность (JWT) | `Program.cs`, `[Authorize]` |
| НФТ3 — согласованность БД | `IsolationLevel.Serializable` в `BookingService.CreateAsync` |
