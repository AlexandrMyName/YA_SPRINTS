# 🚀 YA Sprints – Event Management API

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Swagger](https://img.shields.io/badge/Swagger-85EA2D?logo=swagger&logoColor=black)](https://swagger.io/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---
## 📖 Оглавление

<br>

 **Часть 1 — Монолит (Sprint 8):**
- [О проекте](#-о-проекте)
- [Технологии](#-технологии)
- [Собственные библиотеки](#-собственные-библиотеки)
- [Быстрый старт](#-быстрый-старт)
  - [Клонирование и сборка](#-клонирование-и-сборка)
  - [Генерация RSA-ключей](#-генерация-rsa-ключей)
  - [Запуск базы данных через Docker](#-запуск-базы-данных-через-docker)
  - [Применение миграций](#-применение-миграций)
  - [Запуск приложения](#-запуск-приложения)
- [Аутентификация и авторизация](#-аутентификация-и-авторизация)
  - [Схемы JWT (RS256 и HS256)](#-схемы-jwt-rs256-и-hs256)
  - [Политики авторизации](#-политики-авторизации)
  - [Источники токена](#-источники-токена)
  - [Access и Refresh токены](#-access-и-refresh-токены)
- [Работа с пользователями](#-работа-с-пользователями)
  - [Регистрация](#-регистрация)
  - [Первый администратор](#-первый-администратор)
  - [Повышение и понижение роли](#-повышение-и-понижение-роли)
- [Миграции EF Core](#-миграции-ef-core)
- [Настройка подключения к БД](#-настройка-подключения-к-бд)
- [Документация API](#-документация-api)
- [Валидация](#-валидация)
- [Примитивы синхронизации и защита от овербукинга](#-примитивы-синхронизации-и-защита-от-овербукинга)
- [Тестирование](#-тестирование)
- [Архитектура](#-архитектура)
- [Оптимизация производительности](#-оптимизация-производительности)
- [Вклад в проект](#-вклад-в-проект)
- [Лицензия](#-лицензия)


**Часть 2 — Микросервисы (Sprint 9):**
- [Микросервисная архитектура](#-микросервисная-архитектура)
- [Состав системы](#-состав-системы)
- [Sprints.Contracts — общий контракт](#-sprintscontracts--общий-контракт)
- [Kafka: топики и обработчики](#-kafka-топики-и-обработчики)
- [Поток события BookingConfirmed (saga)](#-поток-события-bookingconfirmed-saga)
- [Outbox pattern](#-outbox-pattern)
- [Inbox pattern](#-inbox-pattern)
- [JWT между сервисами](#-jwt-между-сервисами)
- [Запуск микросервисов](#-запуск-микросервисов)
- [Проверка сценария](#-проверка-сценария)


<br>

---

# Часть 1 — Монолит (Sprint 8)

## 📌 О проекте

<br>

**Event Management API** – RESTful-сервис для управления событиями и бронированием мест.
Проект выполнен в рамках учебного спринта и демонстрирует:

- Использование **Entity Framework Core** с **PostgreSQL**
- Управление схемой базы данных через **миграции**
- **JWT-аутентификацию** с двумя схемами (RS256 и HS256)
- **Refresh-токены** с ротацией
- **Ролевую авторизацию** (`User` / `Admin`) через политики
- Оптимистическую блокировку через **xmin**
- Интеграционные тесты с **Testcontainers**
- Фоновую обработку броней через **BackgroundService**
- Валидацию, глобальную обработку ошибок и **Swagger**-документацию
- Разделение на слои по принципам **Clean Architecture**
<br>

---




## 🛠 Технологии

| Компонент | Технология |
|-----------|------------|
| **Язык** | C# 12, .NET 9 |
| **Фреймворк** | ASP.NET Core Web API |
| **ORM** | Entity Framework Core 9 |
| **База данных** | PostgreSQL 15 (через Npgsql) |
| **Миграции** | EF Core Migrations |
| **Аутентификация** | JWT (RS256 + HS256), Refresh-токены |
| **Тестирование** | xUnit, Testcontainers.PostgreSql |
| **Документация** | Swagger / Swashbuckle |
| **Маппинг** | AutoMapper |
| **Контейнеризация** | Docker / Docker Compose |
| **Логирование** | ILogger (встроенный) |
| **Валидация** | DataAnnotations + кастомный ActionFilter |
| **Синхронизация** | SemaphoreSlim (асинхронные семафоры) |


---

 

## 📚 Собственные библиотеки

<br>
- **`queryBuilder_Lib`** – динамическое построение запросов через Expression Trees (фильтрация, сортировка, группировка, проекция)
- **`reflectionPropertyAccessor_Lib`** – оптимизация работы с рефлексией (кеширование методов получения и установки свойств)
- **`dataBase_AutoMigration_Lib`** – автоматическая миграция (добавление колонок в существующие таблицы)
<br>

---




## 🚀 Быстрый старт

### Клонирование и сборка

```bash
git clone https://github.com/AlexandrMyName/YA_SPRINTS.git
cd YA_SPRINTS/Ya_Sprints_AspNetCore_WebApi
dotnet restore
dotnet build
dotnet run --project Sprints_ASP_NetCore_API
```

После запуска API будет доступно по адресу:
```
https://localhost:5001
```

Swagger UI:
```
https://localhost:5001/swagger
```

---

### 🔑 Генерация RSA-ключей

Перед первым запуском нужно сгенерировать пару RSA-ключей для RS256-схемы аутентификации.
```bash
mkdir -p keys
openssl genrsa -out keys/private.pem 2048
openssl rsa -in keys/private.pem -pubout -out keys/public.pem
```

Что делают эти команды:
 	 
mkdir -p keys:
- Создаёт папку keys/. Флаг -p — не ругаться, если папка уже есть
openssl genrsa -out keys/private.pem 2048:
-	Генерирует приватный RSA-ключ длиной 2048 бит и сохраняет в keys/private.pem. genrsa — генератор RSA-ключей
openssl rsa -in keys/private.pem -pubout -out keys/public.pem:
   Извлекает публичный ключ из приватного. -pubout — «выдать публичную часть», writing RSA key в консоли — подтверждение

Зачем это нужно:

- private.pem — используется для подписи JWT-токенов (RS256). Держится в секрете. Кто владеет приватным ключом — тот может выпускать валидные токены.
- public.pem — используется для проверки подписи. Можно распространять. Даже если утечёт — подделать токен нельзя.

⚠️ Важно:
- private.pem — никогда не коммитить. Он уже добавлен в .gitignore.
- Если ключ утёк — считайте, что все токены скомпрометированы. Генерируйте заново и инвалидируйте старые.
- Минимальный размер ключа для RS256 — 2048 бит. Меньше — Microsoft.IdentityModel.Tokens откажется работать.

Если OpenSSL нет (Windows):
```powershell
# Через winget
winget install ShiningLight.OpenSSL

# Или через Git Bash (уже включён в Git for Windows)
# Откройте Git Bash и выполните те же команды
```

Файлы должны попасть в output при сборке. В Sprints_ASP_NetCore_API.csproj:

```xml
<ItemGroup>
  <None Update="keys\private.pem" CopyToOutputDirectory="PreserveNewest" />
  <None Update="keys\public.pem"  CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```
Без этого dotnet ef migrations add и запуск приложения упадут с Could not find a part of the path 'keys/public.pem'.


---

### 🐳 Запуск базы данных через Docker

В корне проекта есть папка Deployment/ с готовым docker-compose.yml.

```bash
cd Deployment
docker-compose up -d
```
 Содержимое Deployment/docker-compose.yml:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15
    container_name: ya_sprints_postgres
    environment:
      POSTGRES_HOST_AUTH_METHOD: trust
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: events_db
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

volumes:
  postgres_data:
```
⚠️ Важно: POSTGRES_HOST_AUTH_METHOD: trust упрощает аутентификацию для разработки. 
В продакшене используйте парольную аутентификацию.


Проверьте, что контейнер запущен:

```bash
docker ps
```
 

### Применение миграций

```bash
cd ..  # вернуться в корень решения
dotnet ef database update \
  --project SprintsASP_NetCore_API.Infrastructure \
  --startup-project Sprints_ASP_NetCore_API \
  --context AppDbContext
```

Или автоматически при запуске приложения (см. следующий раздел).

---


### ▶️ Запуск приложения

```bash
dotnet run --project SprintASP_NetCore_API
```

 Приложение будет доступно по адресу:
 ``` 
https://localhost:5001
Swagger: https://localhost:5001/swagger)
```

 ! При первом запуске миграции применятся автоматически благодаря вызову db.Database.Migrate() в Program.cs.

---

## 🔐 Аутентификация и авторизация

🔀 Схемы JWT (RS256 и HS256)

Проект поддерживает две схемы аутентификации одновременно. Это сделано для удобства разработки:

- Схема: Bearer	 
- Алгоритм: RS256 (асимметричный)	
- Ключ подписи: private.pem
- Ключ валидации: public.pem
- Когда использовать: Продакшн 

- Схема: BearerDev	 
- Алгоритм: HS256 (симметричный)
- Ключ подписи: 	Jwt:HsKey
- Ключ валидации: тот же Jwt:HsKey
- Когда использовать: Только локальная отладка
 

Почему RS256 — основная:

- Приватный ключ есть только у issuer'а (сервиса, выдающего токены).
- Даже если сервис-потребитель скомпрометирован, подделать токен нельзя — у него только публичный ключ.
- Публичный ключ можно свободно распространять между микросервисами.
- Соответствует стандарту OpenID Connect.

Почему HS256 — для отладки:

- Один секрет в appsettings.Development.json — не нужно возиться с PEM-файлами при каждом запуске.
- Удобно, когда хочется быстро проверить логику без настройки RSA.

Как переключать:
В appsettings.json (продакшн):

 ```json
{
  "Jwt": {
    "Mode": "RS256",
    "PrivateKeyPath": "keys/private.pem",
    "PublicKeyPath": "keys/public.pem"
  }
}
```

В appsettings.Development.json:

 ```json
{
  "Jwt": {
    "Mode": "HS256",
    "HsKey": "DEV_ONLY_SECRET_KEY_32_CHARS_MIN_LONG_1234567890"
  }
}
```

Jwt:Mode определяет, каким ключом подписываются новые токены. 
- Валидация при этом идёт по обеим схемам — то есть старые токены продолжают работать после переключения.

## 🛡 Политики авторизации

Все проверки доступа идут через политики (не через роли напрямую):

- Политика:      DefaultPolicy
Что проверяет: Аутентификация любой из схем (RS256 или HS256)
Где применена: [Authorize] без параметров

- Политика:      AnyAuthenticated
- Что проверяет: То же, но явная политика
- Где применена: EventsController, BookingsController (класс)

- Политика:      Admin
- Что проверяет: Роль Admin, любая схема
- Где применена: Опционально, для универсальных мест

- Политика:      AdminRs256Only
- Что проверяет: Роль Admin, только RS256
- Где применена: Все админские действия: создание/обновление/удаление событий, управление пользователями

Почему AdminRs256Only для админских действий:
- в dev-окружении HS256 использует общий секрет из конфига — если он утечёт, злоумышленник может подписать токен с ролью Admin и получить доступ ко всему.
- Поэтому админские действия жёстко ограничены RS256-схемой.

Как использовать в контроллере:

 ```csharp
[Authorize(Policy = AuthPolicies.AdminRs256Only)]
public async Task<IActionResult> Create(...) { ... }

[Authorize(Policy = AuthPolicies.AnyAuthenticated)]
public async Task<IActionResult> GetBooking(...) { ... }
```

## 🔍 Источники токена

Middleware проверяет токен в трёх местах в порядке приоритета:

1. Authorization: Bearer <token> — стандартный HTTP-заголовок
2. X-Access-Token: <token> — кастомный заголовок (для клиентов, которые не могут использовать Authorization)
3. Cookie jwt — HttpOnly-кука, ставится при логине

Если клиент прислал и заголовок, и куку — побеждает заголовок. 
- ! Это защищает от CSRF-атак через куки: явный заголовок имеет приоритет.
--- 

## 🔄 Access и Refresh токены

ACCESS:
- Формат: JWT (подписанный)
- Время жизни:	15 минут (Jwt:AccessTokenMinutes)
- Где хранится на сервере:	Не хранится (stateless)
- Где у клиента: Память JS / cookie jwt
- Что даёт: Доступ к API
- Ротация: -

  REFRESH:
- Формат: Случайная строка (base64 от 64 байт)
- Время жизни:	7 дней (Jwt:RefreshTokenDays)
- Где хранится на сервере:	В БД (refresh_tokens), в виде SHA-256-хеша
- Где у клиента: HttpOnly cookie refreshToken / secure storage
- Что даёт: Получить новый access
- Ротация: При использовании выдаётся новый, старый отзывается
  
Как это работает:
1. POST /api/v1/auth/login → сервер выдаёт access + refresh.
2. Клиент шлёт access в каждом запросе.
3. Когда access протух (получил 401) → POST /api/v1/auth/refresh с refresh.
4. Сервер проверяет refresh в БД, отзывает старый, выдаёт новый access + новый refresh.
5. POST /api/v1/auth/logout → refresh отзывается, куки очищаются.

 Зачем ротация: 
 - если refresh-токен утечёт, злоумышленник сможет использовать его только до момента следующего refresh. После — старый токен невалиден.  

---

## 👥 Работа с пользователями
Регистрация:

 ```bash
curl -X POST https://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "login": "admin",
    "password": "adm123"
  }'
```
Что происходит:
1. Создаётся пользователь с ролью User (не Admin).
2. Пароль хешируется через SHA-256 и сохраняется в users.PasswordHash.
3. Сервер возвращает access + refresh и ставит обе куки.
4. Поле Role в запросе отсутствует — намеренно. Это защита от privilege escalation.

Ответ (200 OK): 

 ```json
{
  "id": "1b503a21-2f4f-4b11-bf00-02e29e8da137",
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dIRUDq3y1096lzuThH9CKgSGCQX81yY+...",
  "tokenType": "Bearer",
  "expiresIn": 899,
  "expiresAtUtc": "2026-10-04T18:54:21.5742401Z"
}
```


Проверка payload access-токена (на https://jwt.io): 

 ```json
{
  "sub": "1b503a21-...",
  "userId": "1b503a21-...",
  "unique_name": "admin",
  "role": "User",           
  "jti": "14a79226-...",
  "exp": 1791140061,
  "iss": "SprintASP_NetCore_API",
  "aud": "SprintASP_NetCore_API_Clients"
}
```

--- 

## 🥇 Первый администратор

Роль Admin нельзя получить через /register. 
- Первый админ создаётся вручную через SQL — это классический bootstrap-паттерн (чтобы создать админа, нужен админ — замкнутый круг).

После регистрации пользователя admin:

 ```bash
psql -h localhost -U postgres -d events_db \
  -c "UPDATE users SET \"Role\" = 'Admin' WHERE \"Login\" = 'admin';"
```

Или из Docker:

 ```bash
docker exec -it ya_sprints_postgres \
  psql -U postgres -d events_db \
  -c "UPDATE users SET \"Role\" = 'Admin' WHERE \"Login\" = 'admin';"
```

Проверка:

 ```bash
psql -h localhost -U postgres -d events_db \
  -c "SELECT \"Login\", \"Role\" FROM users;"
```

Должно быть:

 ```text
 Login | Role
-------+-------
 admin | Admin
```
- ⚠️ Важно: старый access-токен (полученный при регистрации) всё ещё содержит "role": "User".
- Нужно залогиниться заново — JWT это снимок claims на момент выдачи.

 ```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"login": "admin", "password": "adm123"}'
```
В новом токене будет "role": "Admin".
--- 

## ⬆️ Повышение и понижение роли
После bootstrap'а первого админа остальных можно повышать через API:

Повышение до Admin (требует RS256 + роль Admin):

 ```bash
curl -X POST https://localhost:5001/api/v1/users/ivan/promote \
  -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
```


Понижение до User:

 ```bash
curl -X POST https://localhost:5001/api/v1/users/ivan/demote \
  -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
```

Оба эндпоинта защищены политикой AdminRs256Only.

- ⚠️ После смены роли пользователь должен залогиниться заново.
- или дождаться окончания access-токена и вызвать /auth/refresh, чтобы получить токен с актуальной ролью.


---



## 📐 Миграции EF Core


### Создание миграции

```bash
dotnet ef migrations add <MigrationName> \
  --project SprintsASP_NetCore_API.Infrastructure \
  --startup-project Sprints_ASP_NetCore_API \
  --context AppDbContext \
  --output-dir DataAccess/Migrations
```

Пример: 
```bash
dotnet ef migrations add AddUsersAndRefreshTokens \
  --project SprintsASP_NetCore_API.Infrastructure \
  --startup-project Sprints_ASP_NetCore_API \
  --context AppDbContext \
  --output-dir DataAccess/Migrations
```
Почему нужен --startup-project: 
- EF-инструменты запускают Program.cs startup-проекта, чтобы построить DI-контейнер и получить DbContextOptions (включая строку подключения).
- Без этого EF не сможет создать AppDbContext.
 




### Применение миграции

```bash
dotnet ef database update \
  --project SprintsASP_NetCore_API.Infrastructure \
  --startup-project Sprints_ASP_NetCore_API \
  --context AppDbContext
```

 
### Откат к предыдущей миграции

```bash
dotnet ef database update <PreviousMigrationName> \
  --project SprintsASP_NetCore_API.Infrastructure \
  --startup-project Sprints_ASP_NetCore_API \
  --context AppDbContext
```


### Удаление последней миграции (если не применена)

```bash
dotnet ef migrations remove \
  --project SprintsASP_NetCore_API.Infrastructure \
  --startup-project Sprints_ASP_NetCore_API \
  --context AppDbContext
```




## 🗄️ Настройка подключения к БД


### Строка подключения
Строка подключения задаётся в appsettings.json (или appsettings.Development.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=events_db;Username=postgres;Password=postgres"
  }
}
```
Если PostgreSQL установлен с другими параметрами – измените строку.



 
## 📖 Документация API

 
### 🔐 Аутентификация
Базовый префикс: /api/v1/auth
POST	/register	Регистрация (всегда роль User)	Анонимно
POST	/login	Вход, возвращает access + refresh	Анонимно
POST	/refresh	Обновление access по refresh (ротация)	Анонимно
POST	/logout	Отзыв refresh, очистка кук	Анонимно

### 👤 Пользователи
- Базовый префикс: /api/v1/users

- GET	/{id}	Получить пользователя по Id	Admin (RS256)
- GET	/by-login/{login}	Получить пользователя по логину	Admin (RS256)
- GET	/	Список пользователей с фильтром	Admin (RS256)
- PUT	/{id}	Обновить логин	Admin (RS256)
- DELETE	/{id}	Удалить пользователя	Admin (RS256)
- POST	/{login}/promote	Повысить до Admin	Admin (RS256)
- POST	/{login}/demote	Понизить до User	Admin (RS256)

### 📅 События
- Базовый префикс: /api/v1/events

- GET	/{id}	Получить событие	Публично
- GET	/	Список событий (фильтр, сортировка, пагинация)	Публично
- POST	/{id}/book	Создать бронирование	Аутентификация
- POST	/	Создать событие	Admin (RS256)
- POST	/range	Создать коллекцию событий	Admin (RS256)
- PUT	/{id}	Обновить событие	Admin (RS256)
- PUT	/	Обновить коллекцию	Admin (RS256)
- DELETE	/{id}	Удалить событие	Admin (RS256)

### 📋 Бронирования
- Базовый префикс: /api/v1/bookings
 
- GET	/{id}	Получить бронь (свою или любую — если Admin)	Аутентификация
- DELETE	/{id}	Отменить бронь (свою или любую — если Admin)	Аутентификация
- Модель события (Event)
- Поле	Тип	Описание
- id	Guid	Уникальный идентификатор
- title	string	Название
- description	string	Описание
- startAt	datetime	Начало
- endAt	datetime	Окончание
- totalSeats	int	Общее количество мест
- availableSeats	int	Свободных мест (вычисляется)

 

### Модель события (Event)
 
- id	(Guid)	Уникальный идентификатор
- title	(string)	Название
- description	(string)	Описание
- startAt	(datetime)	Начало
- endAt	(datetime)	Окончание
- totalSeats	(int)	Общее количество мест
- availableSeats	(int)	Свободных мест (вычисляется)

### Фильтрация событий (GET /events)
 
- Title	(string)	Фильтр по названию (contains)
- From	(datetime)	Начало >=
- To	(datetime)	Окончание <=
- SortBy	(string)	Поле сортировки
- SortDesc	(bool)	По убыванию
- Page	(int)	Номер страницы (default: 1)
- PageSize	(int)	Размер страницы (default: 10, max: 100)

### Пример запроса с фильтрацией

```http
GET /api/v1/events?Title=встреча&From=2026-03-01&To=2026-03-31&SortBy=StartAt&SortDesc=false&Page=2&PageSize=5
```


### Пример ответа (PaginatedResult)

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Встреча команды",
      "description": "Обсуждение спринта",
      "startAt": "2026-03-20T10:00:00",
      "endAt": "2026-03-20T11:30:00"
    }
  ],
  "totalCount": 25,
  "page": 2,
  "pageSize": 5,
  "totalPages": 5,
  "hasPreviousPage": true,
  "hasNextPage": true
}
```


### Пример тела запроса (CreateEventDto)

```json
{
  "title": "Встреча команды",
  "description": "Обсуждение спринта",
  "startAt": "2026-03-20T10:00:00",
  "endAt": "2026-03-20T11:30:00",
  "totalSeats": 10
}
```


### Создание бронирования
```
POST /api/v1/events/{id}/book
Authorization: Bearer <access_token>
```


### Успешный ответ

```json
{
  "id": "3fa85f64-...",
  "eventId": "3fa85f64-...",
  "userId": "1b503a21-...",
  "status": 0,
  "createdAt": "2026-03-20T10:00:00",
  "processedAt": null
}
```


### Ошибка при отсутствии мест (409 Conflict)

``` 
json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Ошибка обработки запроса",
  "status": 409,
  "detail": "No available seats for this event",
  "instance": "/api/v1/events/..."
}
```

### Ошибки:
- 400	- Событие в прошлом, невалидные данные
- 401	- Нет токена / невалидный токен / неверный пароль
- 403	- Нет прав (чужая бронь, не Admin)
- 404	- Событие/бронь/пользователь не найдены
- 409	- Нет мест, лимит броней, дубликат логина, повторная отмена

 

## 🛡️ Валидация


### Входная валидация

- **Стандартная** – автоматическая проверка через DataAnnotations (`[Required]`, `[StringLength]`, и т.д.)
- **Бизнес-правила** – проверка в кастомном фильтре `ValidateInputModelAttribute`:
  - `StartAt` не может быть позже или равен `EndAt`
  - Уникальность названий при массовом добавлении/обновлении
  - Валидация `EventFilterDto` (диапазон дат, параметры пагинации)


### Выходная валидация

- Рекурсивная проверка DTO перед отправкой ответа (также через `ValidateInputModelAttribute` после выполнения действия)


### Глобальная обработка ошибок

- **Middleware** `GlobalExceptionMiddleware` – перехватывает все необработанные исключения
- Возвращает стандартизированные ответы в формате [Problem Details](https://tools.ietf.org/html/rfc7807)
- Логирует ошибки через `ILogger`

 


## 🔒 Примитивы синхронизации и защита от овербукинга


### Зачем нужна синхронизация?
- При одновременных запросах на бронирование одного события может возникнуть ситуация гонки (race condition).
- Например, если два пользователя одновременно пытаются занять последнее свободное место, оба могут прочитать значение AvailableSeats = 1, уменьшить его до 0 и записать обратно.
- В результате будет создано 2 брони, хотя место было только одно.

  
### Используемый примитив: SemaphoreSlim
- В сервисе BookingService используется SemaphoreSlim с ёмкостью 1:
```csharp
{
   private readonly SemaphoreSlim _bookingLock = new(1, 1);
}
``` 
- Это позволяет сериализовать операции создания бронирования – только один поток может выполнять критическую секцию одновременно.
- Другие потоки ожидают освобождения семафора.

 
### Почему SemaphoreSlim, а не lock?
- SemaphoreSlim поддерживает асинхронное ожидание (await WaitAsync()), что критично для async/await операций с БД или репозиторием.
- lock не работает с await и может привести к взаимоблокировкам.

 
### Критическая секция включает:
- 1. Проверку AvailableSeats > 0
- 2. Уменьшение AvailableSeats на 1
- 3. Сохранение обновлённого события в репозитории
- 4. Создание бронирования

 
### Освобождение семафора гарантируется блоком finally, даже если произошло исключение:
```csharp
{
try
{
    await _bookingLock.WaitAsync();
    // критическая секция
}
finally
{
    _bookingLock.Release();
}
}
```


### Откат изменений при ошибке
 
- Если после успешного резервирования места происходит ошибка (например, не удалось создать бронь), сущность события откатывается – место возвращается обратно:
 
```csharp
{
catch
{
    if (seatsReserved && eventEntity != null)
        eventEntity.ReleaseSeats(1);
    throw;
}
}
- Это гарантирует, что данные останутся консистентными даже при сбоях.
```




## 📉 Пример сценария с овербукингом


### Сценарий: 5 запросов на 3 места

### Исходные данные:
- Событие с totalSeats = 3, availableSeats = 3
- 5 параллельных запросов на бронирование
 
### Ожидаемый результат:
- 3 успешных бронирования (202 Accepted)
- 2 ошибки 409 Conflict (NoAvailableSeatsException)
- availableSeats становится равным 0
 
### Как это достигается:
- 1. SemaphoreSlim пропускает только один поток в критическую секцию.
- 2. Первый поток проверяет availableSeats = 3, уменьшает до 2, создаёт бронь.
- 3. Второй поток проверяет availableSeats = 2, уменьшает до 1, создаёт бронь.
- 4. Третий поток проверяет availableSeats = 1, уменьшает до 0, создаёт бронь.
- 5. Четвёртый поток проверяет availableSeats = 0 – выбрасывает NoAvailableSeatsException.
- 6. Пятый поток – аналогично, исключение.
 

благодаря синхронизации, даже если все 5 запросов придут одновременно, овербукинг не произойдёт.


## Юнит-тест для сценария
``` 
[Fact]
public async Task ConcurrentBookings_20Requests_5Seats_Exactly5Success_15Exceptions()
{
    // Arrange
    var eventId = await CreateTestEvent(5);
    var tasks = new List<Task>();
    var success = 0;
    var exceptions = 0;

    // Act
    for (int i = 0; i < 20; i++)
    {
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await _bookingService.CreateBookingAsync(eventId);
                Interlocked.Increment(ref success);
            }
            catch (NoAvailableSeatsException)
            {
                Interlocked.Increment(ref exceptions);
            }
        }));
    }
    await Task.WhenAll(tasks);

    // Assert
    Assert.Equal(5, success);
    Assert.Equal(15, exceptions);
    var finalSeats = (await _eventRepository.GetByIdAsync(eventId)).Data.AvailableSeats;
    Assert.Equal(0, finalSeats);
}
```
---




## 🧪 Тестирование


### Запуск тестов

```bash
dotnet test
```


### Интеграционные тесты с Testcontainers

Тесты используют Testcontainers.PostgreSql, который автоматически поднимает изолированный контейнер PostgreSQL для всех тестов в коллекции (один контейнер на весь запуск).

**Требования:**
- Docker должен быть запущен.
- Установлен пакет Testcontainers.PostgreSql (уже добавлен в проект интеграционных тестов).

**Особенности:**
- Контейнер создаётся один раз для всех тестов (через `ICollectionFixture<DatabaseFixture>`).
- Схема базы данных создаётся через реальные миграции (`MigrateAsync()`).
- После каждого теста база очищается через `TRUNCATE`, чтобы тесты были изолированы друг от друга.


### InMemory-провайдер EF Core
В юнит тестах используется Microsoft.EntityFrameworkCore.InMemory – каждый тестовый класс получает уникальную базу данных, что гарантирует изоляцию тестов.

Пример настройки DI для тестов:

```csharp
var dbName = Guid.NewGuid().ToString();
var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase(dbName));
services.AddScoped<IRepository<Event>, EfCoreRepository<Event>>();
services.AddScoped<IEventService, EventsService>();
// ...
var provider = services.BuildServiceProvider();
```
Для конкурентных тестов каждый параллельный запрос использует отдельный IServiceScope.


### Покрытие тестами

- **EventService** – основные CRUD-операции, фильтрация, сортировка, пагинация
  **BookingService** – основные CRUD-операции
- **ValidateInputModelAttribute** – валидация входных/выходных данных
- **reflectionPropertyAccessor_Lib** – тесты производительности (сравнение с кешированием и без)

---




## 🧩 Архитектура (слои)
```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                       │
│  Sprints_ASP_NetCore_API                                    │
│  • Controllers/       — REST API endpoints                  │
│  • Middlewares/       — GlobalExceptionMiddleware           │
│  • Filters/           — ActionFilters (валидация, логи)     │
│  • Extensions/        — DI, CORS, Swagger, Versioning,      │
│                          JWT, Cookies, Claims               │
│  • Program.cs         — Composition Root                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                        │
│  SprintsASP_NetCore_API.Application                         │
│  • UseCases/DataServices/  — BookingService, EventsService, │
│                              UserService, AuthService        │
│  • Dtos/                   — DTO + Filters                  │
│  • Mapping/                — AutoMapper-профили             │
│  • Abstractions/           — порты (IRepository,            │
│                              ITransaction, IEntityFilter,   │
│                              IInterceptLockings, ...)        │
│  • Security/               — IPasswordHasher,               │
│                              IJwtTokenGenerator             │
│  • Internal/               — ResultDto, PaginatedResult     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       DOMAIN LAYER                          │
│  SprintsASP_NetCore_API.Domain                              │
│  • Entities/       — Event, Booking, BookingStatus,         │
│                       User, UserRole, RefreshToken          │
│  • Abstractions/   — IEntity                                │
│  • Exceptions/     — * (доменные исключения)                │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │
┌─────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                     │
│  SprintsASP_NetCore_API.Infrastructure                      │
│  • DataAccess/DbContexts/       — AppDbContext              │
│  • DataAccess/Configurations/   — IEntityTypeConfiguration  │
│  • DataAccess/Interceptors/     — SaveChangesInterceptor    │
│  • DataAccess/Migrations/       — EF Core migrations        │
│  • Repositories/                — EfCoreRepository<T>       │
│  • Security/                    — PasswordHasher,           │
│                                    JwtTokenGenerator,       │
│                                    RsaKeyLoader             │
│  • Concurrency/                 — InterceptLockings         │
│  • System/                      — RefDataService            │
│  • BackgroundServices/          — BookingBackgroundService  │
└─────────────────────────────────────────────────────────────┘
```
## Направление зависимостей:
```
 Presentation → Application → Domain
              ↑
       Infrastructure
```
- Domain — не зависит ни от чего.
- Application — зависит только от Domain.
- Infrastructure — зависит от Application и Domain.
- Presentation — зависит от Application и Infrastructure (для Composition Root).


## 📁 Структура проекта (Clean Architecture)

```
Ya_Sprints_AspNetCore_WebApi/
├── SprintsASP_NetCore_API.Domain/               # Доменный слой
│   ├── Entities/
│   │   ├── Event.cs
│   │   ├── Booking.cs
│   │   └── BookingStatus.cs
│   └── Abstractions/
│       └── IEntity.cs
│   └── Exceptions/
│       └── DuplicateEventException.cs
│       └── NoAvailableSeatsException.cs
│
├── SprintsASP_NetCore_API.Application/          # Прикладной слой
│   ├── UseCases/DataServices/
│   │   ├── BaseDataService.cs
│   │   ├── BookingService.cs
│   │   ├── EventsService.cs
│   │   └── Contracts/
│   │       ├── IBookingService.cs
│   │       └── IEventService.cs
│   ├── Dtos/
│   │   ├── EntitiesDtos/
│   │   ├── Filters/
│   │   └── Internal/
│   ├── Mapping/
│   │   ├── MappingDtoProfile.cs
│   │   └── MappingEntityProfile.cs
│   ├── Abstractions/
│   │   ├── IRepository.cs
│   │   ├── ITransaction.cs
│   │   ├── IEntityFilter.cs
│   │   ├── IInterceptLockings.cs
│   │   ├── IDataStorageService.cs
│   │   └── IReferenciesData.cs
│   └── DependencyInjection.cs
│
├── SprintsASP_NetCore_API.Infrastructure/       # Инфраструктурный слой
│   ├── DataAccess/
│   │   ├── DbContexts/
│   │   ├── Configurations/
│   │   ├── Interceptors/
│   │   └── Migrations/
│   ├── Repositories/
│   │   ├── EfCoreRepository.cs
│   │   └── BaseInMemoryRepository.cs
│   ├── Concurrency/
│   │   └── InterceptLockings.cs
│   ├── System/
│   │   └── RefDataService.cs
│   ├── BackgroundServices/
│   │   └── BookingBackgroundService.cs
│   └── DependencyInjection.cs
│
├── Sprints_ASP_NetCore_API/                     # Presentation (Web API)
│   ├── Controllers/
│   │   ├── EventsController.cs
│   │   └── BookingsController.cs
│   ├── Actions/
│   │   ├── ActionFilters/
│   │   └── Helpers/
│   ├── Middlewares/
│   │   ├── GlobalExceptionMiddleware.cs
│   │   └── Extentions/
│   ├── Extentions/
│   │   └── DatabaseInitExtensions.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── SprintASP_NetCore_API.IntegrationTests/     # Интеграционные тесты
│   ├── Fixture/
│   │   └── DatabaseFixture.cs
│   ├── BookingFilterTests.cs
│   ├── BookingRepositoryTests.cs
│   ├── EventFilterTests.cs
│   ├── EventRepositoryTests.cs
│   └── TestBase.cs
│
├── Tests/                                       # Юнит-тесты
│   ├── Tests_EventsService.cs
│   ├── Tests_BookingService.cs
│   └── ...
│
├── queryBuilder_Lib/                            # Собственные библиотеки
├── reflectionPropertyAccessor_Lib/
├── dataBase_autoMigration_Lib/
├── Concurrency_Lib/
├── RefactoringNetCompile_Lib/
├── Deployment/
│   └── docker-compose.yml
└── README.md
```
---


### 🔄 Поток данных (создание бронирования)
```
1. HTTP POST /api/v1/events/{id}/book
   │
   ▼
2. GlobalExceptionMiddleware (catch errors)
   │
   ▼
3. ValidateInputModelAttribute (OnActionExecuting)
   │   - ModelState validation
   │   - Business rules
   │
   ▼
4. EventsController.BookEvent(id)
   │
   ▼
5. BookingService.CreateBookingAsync(id)
   │   - await _bookingLock.WaitAsync()  ← 🔒 синхронизация
   │   - Проверка существования события
   │   - TryReserveSeats() → уменьшает AvailableSeats
   │   - Обновление события в репозитории
   │   - Создание брони (Pending)
   │   - Сохранение брони в репозитории
   │   - Освобождение семафора (finally)
   │
   ▼
6. BaseInMemoryRepository<T>
   │   - ConcurrentDictionary хранение
   │
   ▼
7. BookingInfoDto (возвращается клиенту)
   │   - Id, EventId, Status, CreatedAt
   │
   ▼
8. 202 Accepted + Location header
```
  

---


## ⚡ Оптимизация производительности

- **ActionFilter** – кеширование Getter и Setter выражений через `reflectionPropertyAccessor_Lib`
- **DynamicQueryBuilder** – кеширование PropertyInfo и лямбда-выражений для фильтрации/сортировки
- **PaginatedResult** – эффективный подсчет общего количества без загрузки всех данных
- **Асинхронные операции** – все обращения к БД асинхронны.
- **Оптимистическая блокировка** – через xmin для предотвращения конкурентных изменений.
- **Батчинг** – EF Core группирует несколько SaveChanges в один раунд-трип.
- **Кеширование** –  отсутствует (для простоты), но может быть добавлено при необходимости.
---


# Часть 2 — Микросервисы  (Sprint 9)

🏗 Микросервисная архитектура
В девятом спринте монолит декомпозирован на 3 независимых сервиса, связанных через Apache Kafka. 
- Каждый сервис — самостоятельное приложение со своим жизненным циклом, своей БД, своим docker-compose-сервисом.

Монолит остаётся в репозитории как историческая версия (Sprint 8). Микросервисная версия — новая архитектура для Sprint 9.

Ключевые принципы
- Нет HTTP-вызовов между сервисами. Всё общение — через Kafka
- Своя БД у каждого. Сервисы не делят схемы
- Eventual consistency. Данные сходятся к согласованному состоянию асинхронно
- Outbox + Inbox + Saga. Гарантии доставки и идемпотентности
- JWT выдаёт Users, проверяют все

📋 Состав системы

- Users	5001	   sprints_users	   5432	Регистрация, логин, выдача JWT, refresh-токены
- Events	5002  	sprints_events	  5433	CRUD событий, AvailableSeats
- Bookings	5003	sprints_bookings	5434	Создание/отмена броней
  
Инфраструктура:

- zookeeper — 2181
- kafka — 9092 (внешний), 29092 (внутренний для Docker)
- postgres-users — 5432
- postgres-events — 5433
- postgres-bookings — 5434

```Схема
text
┌──────────┐  HTTP  ┌──────────┐
│  Client  │◀──────▶│  Users   │◀── own DB (sprints_users)
└────┬─────┘        └──────────┘
     │
     │  HTTP  (JWT в заголовке)
     ├────────────────▶┌──────────┐◀── own DB (sprints_events)
     │                 │  Events  │
     │                 └────┬─────┘
     │                      ▲
     │                      │ booking-confirmed
     │                      │ booking-rejected
     │                 ┌────┴─────┐
     │                 │  Kafka   │
     │                 └────┬─────┘
     │                      │
     │  HTTP                │
     └────────────────▶┌────▼─────┐◀── own DB (sprints_bookings)
                       │ Bookings │
                       └──────────┘
```

- Events и Bookings никогда не общаются друг с другом напрямую. Только через Kafka.

📦 Sprints.Contracts — общий контракт
Отдельный проект-библиотека Sprints.Contracts, который подключают все три сервиса. Содержит только DTO и константы — ноль зависимостей (ни EF, ни MassTransit, ни Refit).

**KafkaTopics.cs**
```csharp
namespace Sprints.Contracts;

public static class KafkaTopics
{
    public const string BookingConfirmed = "booking-confirmed";
    public const string BookingRejected  = "booking-rejected";
}

```
Издатель и подписчик ссылаются на одну и ту же строку — опечатка исключена.

**BookingConfirmed.cs**
```csharp
public sealed record BookingConfirmed
{
    public Guid MessageId { get; init; }    // ← для Inbox-дедупликации
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public int SeatsCount { get; init; }
    public DateTime ConfirmedAt { get; init; }
}
```

**BookingRejected.cs**
```csharp
public sealed record BookingRejected
{
    public Guid MessageId { get; init; }
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public string Reason { get; init; } = "";
    public DateTime RejectedAt { get; init; }
}
```
Правило контракта: только то, что реально нужно подписчику. Никаких внутренних сущностей, никаких EF-типов.

📨 Kafka: топики и обработчики
 
- booking-confirmed	Bookings	Events	«Бронь подтверждена, зарезервируй место»
- booking-rejected	Events	Bookings	«Мест нет / событие не найдено, отклони бронь»
- Партиций: 3 на топик (создаётся при старте через KafkaTopicInitializer в Events).

Consumer groups

- Events	sprints-events	booking-confirmed
- Bookings	sprints-bookings	booking-rejected
- Producer группы не указывает — группа это понятие только для консьюмеров.

Как работает группа
```text
Topic booking-confirmed, partitions = 3


Group sprints-events:
  Instance A1 → P0
  Instance A2 → P1
  Instance A3 → P2
  (каждое сообщение = один instance этой группы)

Group sprints-audit:
  Instance B1 → P0, P1, P2
  (эта группа видит ВСЕ сообщения параллельно с sprints-events)
Внутри одной группы сообщения распределяются между инстансами

Разные группы видят все сообщения независимо

Порядок и ключ сообщения
Все сообщения отправляются с key = EventId.ToString():
```

```csharp
await producer.ProduceAsync("booking-confirmed", new Message<string, string>
{
    Key = message.EventId.ToString(),   // ← один партишн на EventId
    Value = json
});
```

- Гарантия: все сообщения по одному событию идут в один партишн и обрабатываются по порядку. Это важно для корректного AvailableSeats при параллельных бронях.

Ручной commit offset
``` csharp
var config = new ConsumerConfig
{
    // ...
    EnableAutoCommit = false    // ← ОБЯЗАТЕЛЬНО
};

// ...

handler.HandleAsync(message, ct).GetAwaiter().GetResult();
consumer.Commit(result);        // ← только после успеха
Если HandleAsync упал — offset не коммитится → Kafka перечитает сообщение. Inbox (ниже) защитит от повторного применения.
```


🔄 Поток события BookingConfirmed (saga)
Успешное бронирование
``` text
1. POST /api/v1/bookings/events/{eventId}
        ↓
2. Bookings создаёт Booking (Pending) в своей БД
        ↓
3. BackgroundService: Pending → Confirmed
   ├─ Transaction:
   │   • UPDATE bookings.status = Confirmed
   │   • INSERT outbox_messages (BookingConfirmed)
   │  (ОБЕ записи в одной транзакции)
        ↓
4. OutboxProcessor: читает outbox_messages
   ├─ Produce BookingConfirmed → Kafka "booking-confirmed"
   └─ UPDATE outbox_messages.processed_at
        ↓
5. Events: BookingConfirmedSubscriber (group = sprints-events)
   ├─ Inbox-check (MessageId) → если дубль, skip
   ├─ Transaction:
   │   • If AvailableSeats >= SeatsCount:
   │       UPDATE events.available_seats -= SeatsCount
   │       INSERT inbox_messages
   │   • Else:
   │       INSERT outbox_messages (BookingRejected)
   │       INSERT inbox_messages
   └─ Commit Kafka offset
        ↓
6. (только при отказе) OutboxProcessor в Events
   └─ Produce BookingRejected → Kafka
        ↓
7. (только при отказе) Bookings: BookingRejectedSubscriber
   ├─ Inbox-check
   ├─ Transaction:
   │   • UPDATE bookings.status = Rejected
   │   • INSERT inbox_messages
   └─ Commit offset
```

Сценарий с отказом
- totalSeats = 1, три параллельных брони
- Bookings создаёт 3 × Pending
- BackgroundService подтверждает все 3 → 3 × BookingConfirmed в Kafka
- Events обрабатывает:
- 1-я: AvailableSeats (1) >= 1 → уменьшает до 0, пишет Inbox
- 2-я: AvailableSeats (0) < 1 → Outbox(BookingRejected), Inbox
- 3-я: AvailableSeats (0) < 1 → Outbox(BookingRejected), Inbox
- Bookings получает 2 × BookingRejected:
- Оба переводят соответствующие брони в Rejected
- Итог: 1 бронь Confirmed, 2 брони Rejected, AvailableSeats = 0

Ничего не теряется, нет овербукинга, нет отрицательных мест.

📤 Outbox pattern
Проблема
Классическая ошибка:

``` csharp
await db.SaveChangesAsync();    // ✅ статус подтверждён
await kafka.PublishAsync(...);  // ❌ упало → Events не узнает
```
Результат: Bookings утверждает Confirmed, но Events не уменьшил места. Данные разошлись.

Или наоборот:

``` csharp
await kafka.PublishAsync(...);  // ✅ отправлено
await db.SaveChangesAsync();    // ❌ упало → Kafka-сообщение есть, но брони Confirmed нет
```
Events уменьшит место для «фантомной» брони.

Решение
Outbox — таблица в БД, куда сообщение пишется в той же транзакции, что и бизнес-изменения:

``` csharp
await using var tx = await db.Database.BeginTransactionAsync();

booking.Confirm();                                      // бизнес
await outbox.EnqueueAsync(topic, key, payload);        // Outbox (INSERT)

await db.SaveChangesAsync();                            // атомарно
await tx.CommitAsync();                                 // либо оба, либо ни одного
```
Отдельный воркер (OutboxProcessor) читает outbox_messages, публикует в Kafka, ставит processed_at. При ошибке — retry (до MaxAttempts = 10).

Схема таблицы
```sql
CREATE TABLE outbox_messages (
    "Id"           uuid PRIMARY KEY,           -- MessageId
    "Type"         varchar(100) NOT NULL,       -- "BookingConfirmed"
    "Topic"        varchar(100) NOT NULL,       -- "booking-confirmed"
    "Key"          varchar(200) NOT NULL,       -- EventId
    "Payload"      jsonb NOT NULL,              -- JSON
    "CreatedAt"    timestamptz NOT NULL,
    "ProcessedAt"  timestamptz,
    "Attempts"     int NOT NULL,
    "LastError"    varchar(2000)
);

CREATE INDEX ix_outbox_unprocessed
    ON outbox_messages ("ProcessedAt", "CreatedAt");
```
    
**Что это даёт**
- Атомарность: сообщение и бизнес-изменение либо оба сохранены, либо оба откатились
- Гарантия доставки: если Kafka недоступен — сообщение дождётся в БД
- Retry: временный сбой Kafka → повторная попытка через 2 сек
- Аудит: все сообщения в БД, можно посмотреть историю

**OutboxProcessor** — фоновый воркер
```csharp
protected override async Task ExecuteAsync(CancellationToken ct)
{
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = _options.BootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true
    };
    using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

    while (!ct.IsCancellationRequested)
    {
        var batch = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < 10)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var msg in batch)
        {
            try
            {
                await producer.ProduceAsync(msg.Topic,
                    new Message<string, string> { Key = msg.Key, Value = msg.Payload }, ct);
                msg.MarkProcessed();
            }
            catch (Exception ex) { msg.MarkFailed(ex.Message); }
        }

        await db.SaveChangesAsync(ct);
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
    }
}
```

📥 Inbox pattern
Проблема
- Kafka гарантирует at-least-once доставку. При сбое может доставить сообщение повторно. Если просто обработать дважды:

``` text
BookingConfirmed(MessageId = A)
  Первая доставка:  AvailableSeats 5 → 4 ✅
  Повторная доставка: AvailableSeats 4 → 3 ❌ (некорректно уменьшилось)
```
**Решение**
- Inbox — таблица с MessageId, которая проверяется в той же транзакции, что и бизнес-изменение:

``` csharp
await using var tx = await db.Database.BeginTransactionAsync();
 
// 1. Проверка
var already = await db.InboxMessages.FindAsync(message.MessageId);
if (already != null) { await tx.CommitAsync(); return; }    // дубль → skip

// 2. Бизнес
ev.AvailableSeats -= message.SeatsCount;

// 3. Inbox
db.InboxMessages.Add(InboxMessage.Create(message.MessageId, "BookingConfirmed"));

// 4. Атомарно
await db.SaveChangesAsync();
await tx.CommitAsync();
```

Схема
```sql
CREATE TABLE inbox_messages (
    "Id"          uuid PRIMARY KEY,           -- MessageId из Kafka
    "Type"        varchar(100) NOT NULL,
    "ReceivedAt"  timestamptz NOT NULL,
    "ProcessedAt" timestamptz NOT NULL
);
```

**Что это даёт**
- Идемпотентность: повторная доставка не изменит данные
- At-least-once эффективно превращается в exactly-once на уровне бизнес-эффекта

**Прозрачность:** видно, какие сообщения обработаны

🔐 JWT между сервисами
- JWT выдаёт только Users. Events и Bookings проверяют его локально.

Общие параметры
- Все три сервиса должны иметь одинаковые:

```yaml
Jwt__HsKey: "SHARED_SECRET_KEY_AT_LEAST_32_CHARS_LONG_123"
Jwt__Issuer: "Sprints.Users"
Jwt__Audience: "Sprints.Clients"
В appsettings.json каждого сервиса:
```

```json
"Jwt": {
  "Mode": "HS256",
  "HsKey": "SHARED_SECRET_KEY_AT_LEAST_32_CHARS_LONG_123",
  "Issuer": "Sprints.Users",
  "Audience": "Sprints.Clients"
}
```

Проверка в Events и Bookings
```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["HsKey"]!)),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Pipeline:
app.UseAuthentication();
app.UseAuthorization();
```

userId из claim
- Никаких походов в Users для проверки существования пользователя:

```csharp
var userId = User.GetUserId();   // из claim "userId" / "sub" / ClaimTypes.NameIdentifier
```
JWT подписан Users → значит, userId достоверен. Проверка существования в БД — синхронный HTTP, запрещён заданием.

⚠️ Смена роли
Если пользователь стал Admin — нужно залогиниться заново (или дождаться /auth/refresh), потому что JWT — снимок claims на момент выдачи.

🚀 Запуск микросервисов
1. **Ключи RSA** (если используете **RS256**)
```bash
mkdir -p keys
openssl genrsa -out keys/private.pem 2048
openssl rsa -in keys/private.pem -pubout -out keys/public.pem
```

Для **HS256** (dev-режим) — ключи не нужны, только Jwt:HsKey в конфиге.

2. Сборка и запуск
Из корня решения:

```bash
docker compose up --build
```
Что поднимается:
- sprints_zookeeper (2181)
- sprints_kafka (9092)
- sprints_users_db (5432 → sprints_users)
- sprints_events_db (5433 → sprints_events)
- sprints_bookings_db (5434 → sprints_bookings)
- sprints_users_service (5001)
- sprints_events_service (5002)
- sprints_bookings_service (5003)

3. Swagger UI 
- Users	http://localhost:5001/swagger
- Events	http://localhost:5002/swagger
- Bookings	http://localhost:5003/swagger
- http://, не https:// — внутри контейнера HTTPS не настроен.

4. Подключение к БД (pgAdmin / DBeaver)
Три отдельных соединения: 
- Users	localhost	5432	sprints_users	postgres / postgres
- Events	localhost	5433	sprints_events	postgres / postgres
- Bookings	localhost	5434	sprints_bookings	postgres / postgres
! Три независимых PostgreSQL — не одна БД с тремя схемами.

✅ Проверка сценария
1. Регистрация и логин (Users)
```bash
# Регистрация
curl -X POST http://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"login":"admin","password":"adm123"}'
 
# Сделать Admin (SQL)

docker exec -it sprints_users_db \
  psql -U postgres -d sprints_users \
  -c "UPDATE users SET \"Role\" = 'Admin' WHERE \"Login\" = 'admin';"

# Логин заново (новый токен с ролью Admin)
curl -X POST http://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"login":"admin","password":"adm123"}'
# → скопируйте accessToken
2. Создать событие (Events, Admin)
bash
curl -X POST http://localhost:5002/api/v1/events \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Event",
    "description": "",
    "startAt": "2027-01-01T10:00:00",
    "endAt": "2027-01-01T12:00:00",
    "totalSeats": 10
  }'
# → запомните eventId
3. Проверить AvailableSeats = 10
bash
curl http://localhost:5002/api/v1/events/<eventId>
# availableSeats = 10
4. Создать бронь (Bookings)
bash
curl -X POST http://localhost:5003/api/v1/bookings/events/<eventId> \
  -H "Authorization: Bearer <TOKEN>"
# → 202 Accepted, status = Pending
5. Подождать 2–3 секунды (BackgroundService + Kafka)
6. Проверить результат
Bookings:

bash
curl http://localhost:5003/api/v1/bookings/<bookingId> \
  -H "Authorization: Bearer <TOKEN>"
# status = Confirmed
Events:

bash
curl http://localhost:5002/api/v1/events/<eventId>
# availableSeats = 9   ← уменьшилось через Kafka!
7. Проверить Kafka
bash
# Топики созданы?
docker exec -it sprints_kafka \
  kafka-topics --bootstrap-server localhost:29092 --list
# booking-confirmed
# booking-rejected

# Outbox / Inbox
docker exec -it sprints_bookings_db psql -U postgres -d sprints_bookings \
  -c "SELECT \"Type\", \"ProcessedAt\" FROM outbox_messages ORDER BY \"CreatedAt\" DESC LIMIT 5;"

docker exec -it sprints_events_db psql -U postgres -d sprints_events \
  -c "SELECT \"Type\", \"ProcessedAt\" FROM inbox_messages ORDER BY \"ProcessedAt\" DESC LIMIT 5;"
8. Проверить Saga (нет мест)
bash
# Создать событие с totalSeats = 1
# Сделать 3 брони
# Через 3 сек:
#   Events: availableSeats = 0
#   Bookings: 1 Confirmed, 2 Rejected
# В логах:
docker compose logs events-service | grep "BookingRejected"
docker compose logs bookings-service | grep "Rejected"
```
  
 
⚡ Что добавлено в Sprint 9 (итог)
 
- Разделение на 3 сервиса	Sprints.Users.*, Sprints.Events.*, Sprints.Bookings.*
- Своя БД у каждого	3 × postgres:15 в compose
- Sprints.Contracts	Общий проект с KafkaTopics, BookingConfirmed, BookingRejected
- Kafka producer (Bookings)	KafkaPublisher (Singleton) + OutboxProcessor
- Kafka consumer (Events)	BookingConfirmedSubscriber (group = sprints-events)
- Kafka producer (Events)	KafkaPublisher + OutboxProcessor
- Kafka consumer (Bookings)	BookingRejectedSubscriber (group = sprints-bookings)
- Outbox pattern	Таблица outbox_messages в обеих БД
- Inbox pattern	Таблица inbox_messages в обеих БД
- Saga	BookingConfirmed → BookingRejected при нехватке мест
- JWT shared	HsKey / Issuer / Audience — одинаковые в трёх сервисах
- Topic creation	KafkaTopicInitializer в Events
- Manual commit	EnableAutoCommit = false + Commit после обработки
- Message key	EventId.ToString() → порядок в партишне



## 🤝 Вклад в проект

Проект является учебным, но если вы нашли ошибку – создайте Issue или Pull Request.

---

## 📄 Лицензия

MIT
