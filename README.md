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
cd YA_SPRINTS/Sprints_Project_ASP_NetCore_API/Sprints_ASP_NetCore_API
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

### Таблица методов

| Метод | Эндпоинт | Описание |
|-------|----------|----------|
| GET | `/{id}` | Получить событие по GUID |
| GET | `/` | Получить список событий с фильтрацией, сортировкой и пагинацией |
| POST | `/` | Создать одно событие |
| POST | `/range` | Создать несколько событий (массив) |
| PUT | `/{id}` | Обновить существующее событие |
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

### Пример тела запроса (EventDto)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Встреча команды",
  "description": "Обсуждение спринта",
  "startAt": "2026-03-20T10:00:00",
  "endAt": "2026-03-20T11:30:00"
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
- В `Development` окружении возвращает детальную информацию (стек-трейс), в `Production` – общее сообщение

---

## 🧪 Тестирование

### Запуск тестов

```bash
dotnet test
```

### Покрытие тестами

- **EventService** – основные CRUD-операции, фильтрация, сортировка, пагинация
- **ValidateInputModelAttribute** – валидация входных/выходных данных
- **reflectionPropertyAccessor_Lib** – тесты производительности (сравнение с кешированием и без)

---

## 📁 Структура проекта

```
Sprints_ASP_NetCore_API/
├── Controllers/
│   └── EventsController.cs
├── Data/
│   ├── Dtos/
│   │   ├── EntitiesDtos/
│   │   │   └── EventDto.cs
│   │   ├── Filters/
│   │   │   └── EventFilterDto.cs
│   │   └── Internal/
│   │       ├── IResultDto.cs
│   │       └── ResultDto.cs
│   ├── Entities/
│   │   ├── IEvent.cs
│   │   └── Event.cs
│   └── Repositories/
│       ├── IRepository.cs
│       └── BaseInMemoryRepository.cs
├── Filters/
│   └── ActionFilters/
│       └── ValidateInputModelAttribute.cs
├── Middleware/
│   └── GlobalExceptionMiddleware.cs
├── Services/
│   └── DataServices/
│       └── EventsService.cs
├── Profiles/
│   └── MappingProfile.cs (AutoMapper)
├── Program.cs
└── appsettings.json

Tests/
├── Services/
│   └── EventsServiceTests.cs
├── Filters/
│   └── ValidateInputModelAttributeTests.cs
└── Helpers/
    └── FilterTestHelper.cs
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
