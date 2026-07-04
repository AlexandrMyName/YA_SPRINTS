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
│   │   └── EventsController.cs
│   │
│   ├── Data/
│   │   ├── Dtos/
│   │   │   ├── EntitiesDtos/
│   │   │   │   ├── EventDto.cs
│   │   │   │   └── IEntityDto.cs
│   │   │   ├── Filters/
│   │   │   │   ├── EventFilterDto.cs
│   │   │   │   ├── IEntityFilter.cs
│   │   │   │   └── IFilter.cs
│   │   │   └── Internal/
│   │   │       ├── ApiBaseResult.cs
│   │   │       └── PaginatedResult.cs
│   │   ├── Entities/
│   │   │   ├── Event.cs
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
│   │   ├── DataServices/
│   │   │   └── EventsService.cs
│   │   └── Extentions/
│   │       ├── AddServicesExtention.cs
│   │       └── IDataStorageService.cs
│   │
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
│   │   └── TestDataFactory.cs/
│   ├── Tests.csproj
│   ├── Tests_EventsServicer.cs
│   ├── Tests_Reflection.cs
│   └── Tests_ValidateInputModelAttribute.cs
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
│  │  Controllers/EventsController.cs                        ││
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
│  │  - EventsService.cs (бизнес-логика)                    ││
│  │  - IDataStorageService<T> (интерфейс)                  ││
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
│  │  - Event.cs, IEvent.cs, IEntity.cs                     ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       DTO LAYER                             │
│  ┌─────────────────────────────────────────────────────────┐│
│  │  Data/Dtos/                                             ││
│  │  - EntitiesDtos/ (EventDto, IEntityDto)                ││
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

### 🔍 Ключевые зависимости и взаимодействия
```
 ```mermaid
graph TB
    subgraph Presentation["🎨 Presentation Layer"]
        A[EventsController]
        B[ValidateInputModelAttribute]
        C[GlobalExceptionMiddleware]
        D[LogFilterAttribute]
    end
    
    subgraph Business["⚙️ Business Layer"]
        E[EventsService]
        F[IDataStorageService]
        G[IMapper]
    end
    
    subgraph Data["💾 Data Layer"]
        H[IRepository]
        I[BaseInMemoryRepository]
        J[IQueryable]
    end
    
    subgraph DTO["📦 DTO Layer"]
        K[EventDto]
        L[EventFilterDto]
        M[PaginatedResult]
        N[IEntityFilter]
    end
    
    subgraph Libraries["📚 Own Libraries"]
        O[queryBuilder_Lib]
        P[reflectionPropertyAccessor_Lib]
        Q[dataBase_autoMigration_Lib]
    end
    
    subgraph Validation["✅ Validation"]
        R[ValidatorHelper]
        S[ModelState]
    end
    
    %% Связи
    A -->|HTTP Request| E
    E -->|GetQueryAsync| H
    H -->|returns| I
    I -->|returns| J
    E -->|Map| G
    G -->|maps| K
    E -->|Apply| L
    E -->|returns| M
    
    %% Фильтры и Middleware
    A -.->|Before Action| B
    A -.->|After Action| B
    A -.->|Global| C
    A -.->|Log| D
    
    %% Библиотеки
    E -->|uses| O
    B -->|uses| P
    E -->|uses| P
    O -->|builds| J
    P -->|optimizes| B
    P -->|optimizes| O
    
    %% Валидация
    B -->|uses| R
    B -->|checks| S
    L -->|validates| S
    K -->|validates| S
    
    %% DTO связи
    E -->|returns| M
    M -->|contains| K
    E -->|receives| L
    L -->|implements| N
```
### 🔄 Поток данных
```
1. HTTP Request
   │
   ▼
2. GlobalExceptionMiddleware (catch errors)
   │
   ▼
3. ValidateInputModelAttribute (OnActionExecuting)
   │   - ModelState validation
   │   - Business rules (StartAt < EndAt)
   │   - Filter validation (Page, PageSize)
   │
   ▼
4. EventsController.GetAll()
   │
   ▼
5. EventsService.GetFilteredAsync()
   │   - Получение IQueryable из репозитория
   │   - Применение фильтра через DynamicQueryBuilder
   │   - Подсчет TotalCount
   │   - Применение пагинации
   │   - Маппинг через AutoMapper
   │
   ▼
6. BaseInMemoryRepository<T>
   │   - ConcurrentDictionary хранение
   │
   ▼
7. PaginatedResult<EventDto>
   │   - Items, TotalCount, Page, PageSize
   │   - TotalPages, HasPreviousPage, HasNextPage
   │
   ▼
8. ValidateInputModelAttribute (OnActionExecuted)
   │   - Валидация выходных данных
   │
   ▼
9. HTTP Response
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
