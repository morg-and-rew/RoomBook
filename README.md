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

### 1. Создать базу данных
В уже установленном PostgreSQL создай пустую базу:
```sql
CREATE DATABASE roombook;
```
Если логин/пароль/хост отличаются от `postgres/postgres@localhost:5432` — поправь
строку подключения в `src/RoomBook.Api/appsettings.json` (`ConnectionStrings:Default`).

### 2. Восстановить зависимости
```bash
cd src/RoomBook.Api
dotnet restore
```

### 3. Установить EF Core CLI (если ещё не стоит)
```bash
dotnet tool install --global dotnet-ef
```

### 4. Создать и применить миграцию
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
Это создаст таблицы `users`, `rooms`, `bookings`, `notifications` в базе `roombook`.

### 5. Запустить сервис
```bash
dotnet run
```
Swagger UI будет доступен по адресу, который выведет консоль (обычно
`https://localhost:7xxx/swagger` или `http://localhost:5xxx/swagger`).

## Как выдать себе права администратора

Все новые пользователи регистрируются с ролью `User`. Чтобы протестировать
эндпоинты администратора (`/api/rooms` POST/PUT/DELETE, `/api/bookings/{id}/approve`,
`/api/reports/occupancy`), зарегистрируйся через `/api/auth/register`, а затем
вручную повысь роль в базе:
```sql
UPDATE users SET role = 1 WHERE email = 'your@email.com';
```
(`role = 1` соответствует `UserRole.Admin`). После этого получи новый токен через
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
