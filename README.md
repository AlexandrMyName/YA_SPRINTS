# 🚀 YA Sprints – Event Management API

RESTful API для управления событиями. Проект выполнен в рамках учебного спринта.  
Реализованы базовые CRUD-операции, валидация входных/выходных данных, версионирование, Swagger-документация, in‑memory репозиторий, пагинация, фильтрация, сортировка.

---

## 📦 Технологии

- **.NET 9**
- **ASP.NET Core Web API**
- **Swagger / Swashbuckle** – автоматическая документация
- **AutoMapper** – маппинг сущностей и DTO
- **DataAnnotations** – валидация моделей + кастомный `ActionFilter` для бизнес-правил
- **In‑memory хранение** (`ConcurrentDictionary`)
- **API Versioning** (v1.0)
- **CORS** – настроены политики доступа
- **xUnit + Moq** – юнит-тестирование

### 📚 Собственные библиотеки

- **`queryBuilder_Lib`** – динамическое построение запросов через Expression Trees (фильтрация, сортировка, группировка, проекция)
- **`reflectionPropertyAccessor_Lib`** – оптимизация работы с рефлексией (кеширование методов получения и установки свойств)
- **`dataBase_AutoMigration_Lib`** – автоматическая миграция (добавление колонок в существующие таблицы)

---

## 🚀 Быстрый старт

### Требования

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Любая IDE (Visual Studio, Rider, VS Code)

### 🛠 Клонирование, сборка и запуск

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




#### Пример запроса с фильтрацией

```http
GET /api/v1/events?Title=встреча&From=2026-03-01&To=2026-03-31&SortBy=StartAt&SortDesc=false&Page=2&PageSize=5
```

#### Пример ответа (PaginatedResult)

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
``` 
- При одновременных запросах на бронирование одного события может возникнуть ситуация гонки (race condition).
- Например, если два пользователя одновременно пытаются занять последнее свободное место, оба могут прочитать значение AvailableSeats = 1, уменьшить его до 0 и записать обратно.
- В результате будет создано 2 брони, хотя место было только одно.
 

### Используемый примитив: SemaphoreSlim
- В сервисе BookingService используется SemaphoreSlim с ёмкостью 1:
``` 
private readonly SemaphoreSlim _bookingLock = new(1, 1);
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
``` 
try
{
    await _bookingLock.WaitAsync();
    // критическая секция
}
finally
{
    _bookingLock.Release();
}
```

### Откат изменений при ошибке
 
- Если после успешного резервирования места происходит ошибка (например, не удалось создать бронь), сущность события откатывается – место возвращается обратно:
 
``` 
catch
{
    if (seatsReserved && eventEntity != null)
        eventEntity.ReleaseSeats(1);
    throw;
}
Это гарантирует, что данные останутся консистентными даже при сбоях.
```

## 📉 Пример сценария с овербукингом

### Сценарий: 5 запросов на 3 места
``` 
### Исходные данные:
``` 
- Событие с totalSeats = 3, availableSeats = 3
- 5 параллельных запросов на бронирование
 
``` 
### Ожидаемый результат:
``` 
- 3 успешных бронирования (202 Accepted)
- 2 ошибки 409 Conflict (NoAvailableSeatsException)
- availableSeats становится равным 0
 
``` 
### Как это достигается:
```
- 1. SemaphoreSlim пропускает только один поток в критическую секцию.
- 2. Первый поток проверяет availableSeats = 3, уменьшает до 2, создаёт бронь.
- 3. Второй поток проверяет availableSeats = 2, уменьшает до 1, создаёт бронь.
- 4. Третий поток проверяет availableSeats = 1, уменьшает до 0, создаёт бронь.
- 5. Четвёртый поток проверяет availableSeats = 0 – выбрасывает NoAvailableSeatsException.
- 6. Пятый поток – аналогично, исключение.
 
### Как это достигается:
- благодаря синхронизации, даже если все 5 запросов придут одновременно, овербукинг не произойдёт.

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

### Покрытие тестами

- **EventService** – основные CRUD-операции, фильтрация, сортировка, пагинация
  **BookingService** – основные CRUD-операции
- **ValidateInputModelAttribute** – валидация входных/выходных данных
- **reflectionPropertyAccessor_Lib** – тесты производительности (сравнение с кешированием и без)

---

## 📁 Структура проекта

```
Ya_Sprints_AspNetCore_WebApi/
├── .vs/                                    # Visual Studio файлы
│
├── Sprints_ASP_NetCore_API/                # 📦 Основной проект
│   ├── Actions/
│   │   └── ActionFilters/
│   │       ├── LogFilterAttribute.cs
│   │       └── ValidateInputModelAttribute.cs
│   │
│   ├── Controllers/
│   │   ├── EventsController.cs
│   │   └── BookingsController.cs
│   │
│   ├── Data/
│   │   ├── Dtos/
│   │   │   ├── EntitiesDtos/
│   │   │   │   ├── BookingInfoDto.cs
│   │   │   │   ├── CreateEventDto.cs
│   │   │   │   ├── EventInfoDto.cs
│   │   │   │   └── IEntityDto.cs
│   │   │   ├── Filters/
│   │   │   │   ├── BookingFilterDto.cs
│   │   │   │   ├── EventFilterDto.cs
│   │   │   │   ├── IEntityFilter.cs
│   │   │   │   └── IFilter.cs
│   │   │   └── Internal/
│   │   │       ├── ApiBaseResult.cs
│   │   │       └── PaginatedResult.cs
│   │   ├── Entities/
│   │   │   ├── Booking.cs
│   │   │   ├── Event.cs
│   │   │   ├── IBooking.cs
│   │   │   ├── IEvent.cs
│   │   │   └── IEntity.cs
│   │   └── LessonПолезное/
│   │
│   ├── Helpers/
│   │   └── ValidatorHelper.cs
│   │
│   ├── Middlewares/
│   │   ├── Extentions/
│   │   │   ├── Configurations/
│   │   │   │   ├── ConfigureApiVersioned_Ext.cs
│   │   │   │   ├── ConfigureControllersWithCacheProfiles_Ext.cs
│   │   │   │   ├── ConfigureCors_Ext.cs
│   │   │   │   └── SwaggerGen_Ext.cs
│   │   │   └── Endpoints/
│   │   │       └── ProductsEndpoints.cs
│   │   └── GlobalExceptionMiddleware.cs
│   │
│   ├── ProfilesAndConfigs/
│   │   ├── MappingDtoProfile.cs
│   │   └── MappingEntityProfile.cs
│   │
│   ├── Properties/
│   │   └── launchSettings.json
│   │
│   ├── Repositories/
│   │   ├── Extenions/
│   │   │   └── AddRepositoryExtention.cs
│   │   ├── BaseInMemoryRepository.cs
│   │   ├── IFilterModel.cs
│   │   └── IRepository.cs
│   │
│   ├── Services/
│   │   ├── Background/
│   │   |   └── BookingBackgroundService.cs
│   │   ├── DataServices/
│   │   |   ├── BaseDataService.cs
│   │   |   ├── BookingService.cs
│   │   │   └── EventsService.cs
│   │   └── Extentions/
│   │       ├── AddServicesExtention.cs
│   │   ├── IBookingService.cs
│   │   ├── IDataStorageService.cs
│   │   └── IEventService.cs
│   ├── Program.cs
│   ├── SprintASP_NetCore_API.csproj
│   ├── SprintASP_NetCore_API.csproj.user
│   ├── Sprints_ASP_NetCore_API.http
│   ├── appsettings.Development.json
│   ├── appsettings.Production.json
│   ├── appsettings.json
│   └── Записки.txt
│
├── Tests/                                  # 🧪 Тестовый проект
│   ├── ActionFilterHelpers/
│   │   └── FilterTestHelper.cs
│   ├── Factories/
│   │   └── TestDataFactory.cs
│   ├── Tests_BookingService.cs
│   ├── Tests_EventsService.cs
│   ├── Tests_Reflection.cs
│   ├── Tests_ValidateInputModelAttribute.cs
│   └── Tests.csproj
│
├── dataBase_autoMigration_Lib/             # 📚 Библиотека миграции
│   ├── DynamicEntityMigration.cs
│   └── dataBase_autoMigration_Lib.csproj
│
├── queryBuilder_Lib/                       # 📚 Библиотека построения запросов
│   ├── DemoRunner.cs
│   ├── DynamicQueryBuilder.cs
│   └── queryBuilder_Lib.csproj
│
├── reflectionPropertyAccessor_Lib/         # 📚 Библиотека рефлексии
│   ├── PropertyAccessor.cs
│   └── reflectionPropertyAccessor_Lib.csproj
│
├── Sprints_ASP_NetCore_API.sln             # Решение
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
- **In‑memory репозиторий** – потокобезопасный `ConcurrentDictionary` для конкурентного доступа
- **PaginatedResult** – эффективный подсчет общего количества без загрузки всех данных

---

## 🤝 Вклад в проект

Проект является учебным, но если вы нашли ошибку – создайте Issue или Pull Request.

---

## 📄 Лицензия

MIT
