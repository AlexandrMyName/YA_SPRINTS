# 🚀 YA Sprints – Event Management API

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Swagger](https://img.shields.io/badge/Swagger-85EA2D?logo=swagger&logoColor=black)](https://swagger.io/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## 📖 Оглавление

- [О проекте](#-о-проекте)
- [Технологии](#-технологии)
- [Собственные библиотеки](#-собственные-библиотеки)
- [Быстрый старт](#-быстрый-старт)
  - [Клонирование и сборка](#-клонирование-и-сборка)
  - [Запуск базы данных через Docker](#-запуск-базы-данных-через-docker)
  - [Применение миграций](#-применение-миграций)
  - [Запуск приложения](#-запуск-приложения)
- [Миграции EF Core](#-миграции-ef-core)
  - [Создание миграции](#создание-миграции)
  - [Применение миграции](#применение-миграции)
  - [Откат миграции](#откат-миграции)
  - [Удаление последней миграции](#удаление-последней-миграции-если-не-применена)
- [Настройка подключения к БД](#настройка-подключения-к-бд)
- [Документация API](#-api-документация)
- [Валидация](#-валидация)
- [Примитивы синхронизации и защита от овербукинга](#-примитивы-синхронизации-и-защита-от-овербукинга)
- [Пример сценария с овербукингом](#-пример-сценария-с-овербукингом)
- [Тестирование](#-тестирование)
- [Архитектура](#-архитектура)
- [Оптимизация производительности](#-оптимизация-производительности)
- [Вклад в проект](#-вклад-в-проект)
- [Лицензия](#-лицензия)

---




## 📌 О проекте

**Event Management API** – RESTful-сервис для управления событиями и бронированием мест.  
Проект выполнен в рамках учебного спринта и демонстрирует:

- Использование **Entity Framework Core** с **PostgreSQL**
- Управление схемой базы данных через **миграции**
- Оптимистическую блокировку через **xmin**
- Интеграционные тесты с **Testcontainers**
- Фоновую обработку броней через **BackgroundService**
- Валидацию, глобальную обработку ошибок и **Swagger**-документацию

---




## 🛠 Технологии

| Компонент | Технология |
|-----------|------------|
| **Язык** | C# 12, .NET 9 |
| **Фреймворк** | ASP.NET Core Web API |
| **ORM** | Entity Framework Core 9 |
| **База данных** | PostgreSQL 15 (через Npgsql) |
| **Миграции** | EF Core Migrations |
| **Тестирование** | xUnit, Testcontainers.PostgreSql |
| **Документация** | Swagger / Swashbuckle |
| **Маппинг** | AutoMapper |
| **Контейнеризация** | Docker / Docker Compose |
| **Логирование** | ILogger (встроенный) |
| **Валидация** | DataAnnotations + кастомный ActionFilter |
| **Синхронизация** | SemaphoreSlim (асинхронные семафоры) |

---

## 📚 Собственные библиотеки

- **`queryBuilder_Lib`** – динамическое построение запросов через Expression Trees (фильтрация, сортировка, группировка, проекция)
- **`reflectionPropertyAccessor_Lib`** – оптимизация работы с рефлексией (кеширование методов получения и установки свойств)
- **`dataBase_AutoMigration_Lib`** – автоматическая миграция (добавление колонок в существующие таблицы)

---




## 🚀 Быстрый старт

### Клонирование и сборка

```bash
git clone https://github.com/AlexandrMyName/YA_SPRINTS.git
cd YA_SPRINTS/Ya_Sprints_AspNetCore_WebApi
dotnet restore
dotnet build
dotnet run
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

После запуска базы данных примените миграции:

```bash
cd ..  # вернуться в корень решения
dotnet ef database update --context AppDbContext
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




## 📐 Миграции EF Core


### Создание миграции


```bash
dotnet ef migrations add <MigrationName> --context AppDbContext
```

 Пример:
 
```bash
dotnet ef migrations add InitialCreate --context AppDbContext
```


### Применение миграции

```bash
dotnet ef database update --context AppDbContext
```

 
### Откат к предыдущей миграции

```bash
dotnet ef database update <PreviousMigrationName> --context AppDbContext
```


### Удаление последней миграции (если не применена)

```bash
dotnet ef migrations remove --context AppDbContext
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


### Базовый префикс

```
/api/v1/events
``` 


### Модель события (Event)

| Поле | Тип | Описание |
|------|-----|----------|
| `id` | `Guid` | Уникальный идентификатор события |
| `title` | `string` | Название события |
| `description` | `string` | Описание события |
| `startAt` | `datetime` | Дата и время начала события |
| `endAt` | `datetime` | Дата и время окончания события |
| `totalSeats` | `int` | **Общее количество мест** на событии (указывается при создании) |
| `availableSeats` | `int` | **Количество свободных мест** на текущий момент (вычисляется автоматически) |
 
- Поле availableSeats не передаётся при создании – оно автоматически устанавливается равным totalSeats.
- При каждом успешном бронировании availableSeats уменьшается на 1.


### Таблица методов Events

| Метод | Эндпоинт | Описание |
|-------|----------|----------|
| GET | `/{id}` | Получить событие по GUID |
| GET | `/` | Получить список событий с фильтрацией, сортировкой и пагинацией |
| POST | `/` | Создать одно событие |
| POST | `/range` | Создать несколько событий (массив) |
| PUT | `/{id}` | Обновить существующее событие |
| PUT | `/{id}/book` | Создать бронирование для события |
| PUT | `/` | Обновить несколько событий (массив) |
| DELETE | `/{id}` | Удалить событие |


### Фильтрация, сортировка и пагинация (GET /)

| Параметр | Тип | Описание |
|----------|-----|----------|
| `Title` | string | Фильтр по названию (contains) |
| `From` | datetime | Фильтр по дате начала (>=) |
| `To` | datetime | Фильтр по дате окончания (<=) |
| `SortBy` | string | Поле для сортировки (Title, StartAt, EndAt, Priority) |
| `SortDesc` | bool | Сортировка по убыванию (true) или возрастанию (false) |
| `Page` | int | Номер страницы (по умолчанию: 1) |
| `PageSize` | int | Размер страницы (по умолчанию: 10, максимум: 100) |


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
```


### Успешный ответ

```json
json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventId": "3fa85f64-5717-4562-a3fc-2c963f66afbc",
  "status": 0
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


### Базовый префикс

```
/api/v1/bookings
```


### Таблица методов Bookings

| Метод | Эндпоинт | Описание |
|-------|----------|----------|
| GET | `/{id}` | Получить информацию о бронировании по её идентификатору. | 


### Пример тела ответа (BookingInfoDto)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventId": "3fa85f64-5717-4562-a3fc-2c963f66afbc",
  "status": 0,
  "createdAt": "2026-03-20T10:00:00",
  "processedAt": null
}
```

---




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

Тесты используют Testcontainers.PostgreSql, который автоматически поднимает изолированный контейнер PostgreSQL для каждого тестового запуска.

Требования:
- Docker должен быть запущен.
- Установлен пакет Testcontainers.PostgreSql (уже добавлен в проект интеграционных тестов).

Особенности:
- Каждый тестовый класс создаёт свой контейнер.
- Схема создаётся через EnsureCreatedAsync() (не через миграции) для скорости.
- После каждого теста база очищается через TRUNCATE


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
│                      Presentation Layer                     │
│  Controllers, ActionFilters, Middleware                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       Business Layer                        │
│  Services (Events, Bookings, Background)                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Data Access Layer                      │
│  Repositories, DbContext, Configurations, Entities          │
└─────────────────────────────────────────────────────────────┘
```
- Presentation – обрабатывает HTTP, валидирует входные/выходные данные.
- Business – содержит бизнес-логику (создание брони, проверка мест, синхронизация).
- Data – взаимодействие с БД через EF Core, конфигурация моделей.




## 📁 Структура проекта

```
Ya_Sprints_AspNetCore_WebApi/
├── SprintASP_NetCore_API/                 # Основной проект
│   ├── Actions/
│   │   └── ActionFilters/
│   │       └── ValidateInputModelAttribute.cs
│   ├── Controllers/
│   │   ├── EventsController.cs
│   │   └── BookingsController.cs
│   ├── Data/
│   │   ├── DataAccess/
│   │   │   ├── Configurations/            # Fluent API конфигурации
│   │   │   │   ├── EventConfiguration.cs
│   │   │   │   └── BookingConfiguration.cs
│   │   │   └── DbContexts/
│   │   │       ├── AppDbContext.cs
│   │   │       └── BaseDbContext.cs
│   │   ├── Dtos/
│   │   │   ├── EntitiesDtos/
│   │   │   ├── Filters/
│   │   │   └── Internal/
│   │   └── Entities/
│   │       ├── Booking.cs
│   │       ├── Event.cs
│   │       └── ...
│   ├── Middlewares/
│   │   └── GlobalExceptionMiddleware.cs
│   ├── Migrations/                        # (будущие миграции)
│   ├── ProfilesAndConfigs/
│   │   └── MappingProfile.cs
│   ├── Repositories/
│   │   ├── EfCoreRepository.cs
│   │   └── IRepository.cs
│   ├── Services/
│   │   ├── Background/
│   │   │   └── BookingBackgroundService.cs
│   │   ├── DataServices/
│   │   │   ├── EventsService.cs
│   │   │   └── BookingService.cs
│   │   └── Intercepts/
│   │       └── InterceptLockings.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── ...
├── UnitTests/                                 # Тестовый проект
│   ├── Tests_EventsService_Integration.cs
│   ├── Tests_BookingService.cs
│   └── ...
├── SprintASP_NetCore_API.IntegrationTests/                                 # Тестовый проект
│   ├── BookingFilterTests.cs
│   ├── BookingRepositoryTests.cs
│   ├── EventFilterTests.cs
│   ├── EventRepositoryTests.cs
│   ├── TestBase.cs
│   └── ...

└── README.md
```
---

```
┌─────────────────────────────────────────────────────────────┐
│                     PRESENTATION LAYER                       │
│  ┌─────────────────────────────────────────────────────────┐│
│  │  Controllers/                                          ││
│  │  ├── EventsController.cs                               ││
│  │  └── BookingsController.cs                             ││
│  │  - REST API endpoints                                   ││
│  │  - Обработка HTTP запросов                             ││
│  └─────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────┐│
│  │  Actions/ActionFilters/                                 ││
│  │  - ValidateInputModelAttribute (валидация)             ││
│  │  - LogFilterAttribute (логирование)                    ││
│  └─────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────┐│
│  │  Middlewares/                                           ││
│  │  - GlobalExceptionMiddleware (глобальная обработка)    ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      BUSINESS LAYER                         │
│  ┌─────────────────────────────────────────────────────────┐│
│  │  Services/                                              ││
│  │  ├── EventsService.cs                                   ││
│  │  ├── BookingService.cs                                 ││
│  │  └── IDataStorageService<T> (интерфейс)               ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     DATA LAYER                              │
│  ┌─────────────────────────────────────────────────────────┐│
│  │  Repositories/                                          ││
│  │  - BaseInMemoryRepository<T> (in-memory хранилище)     ││
│  │  - IRepository<T> (интерфейс)                          ││
│  └─────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────┐│
│  │  Data/Entities/                                         ││
│  │  - Event.cs, IEvent.cs, Booking.cs, IBooking.cs        ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       DTO LAYER                             │
│  ┌─────────────────────────────────────────────────────────┐│
│  │  Data/Dtos/                                             ││
│  │  - EntitiesDtos/ (EventInfoDto, CreateEventDto,        ││
│  │                    BookingInfoDto, IEntityDto)          ││
│  │  - Filters/ (EventFilterDto, IFilter<T>)               ││
│  │  - Internal/ (ApiBaseResult, PaginatedResult)          ││
│  └─────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────┐│
│  │  ProfilesAndConfigs/                                    ││
│  │  - MappingDtoProfile.cs (AutoMapper)                   ││
│  │  - MappingEntityProfile.cs (AutoMapper)                ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
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




## 🤝 Вклад в проект

Проект является учебным, но если вы нашли ошибку – создайте Issue или Pull Request.

---

## 📄 Лицензия

MIT
