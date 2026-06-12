# 🚀 YA Sprint One – Event Management API

RESTful API для управления событиями. Проект выполнен в рамках учебного спринта.  
Реализованы базовые CRUD-операции, валидация входных/выходных данных, версионирование, Swagger-документация, in‑memory репозиторий.

## 📦 Технологии
- **.NET 9**
- **ASP.NET Core Web API**
- **Swagger / Swashbuckle** – автоматическая документация
- **AutoMapper** – маппинг сущностей и DTO
- **DataAnnotations** – валидация моделей + кастомный `ActionFilter` для бизнес-правил
- **In‑memory хранение** (`ConcurrentDictionary`)
- **API Versioning** (v1.0)
- **CORS** – настроены политики доступа

## 🛠 Установка и запуск

### Требования
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Любая IDE (Visual Studio, Rider, VS Code)

## 🛠 Клонирование, сборка и запуск
- git clone https://github.com/AlexandrMyName/YA_SPRINTS.git
- cd YA_SPRINTS/sprint-1/Sprint1_Project_ASP_NetCore_API
- dotnet restore
- dotnet build
- dotnet run

- После запуска API будет доступно по адресу:
- https://localhost:5001 (или другой порт, указанный в launchSettings.json)
- Swagger UI: https://localhost:5001/swagger


## 🛠 Валидация
Входная – проверяется автоматически через [Required], [StringLength] и т.д. (DataAnnotations).
Бизнес-правила (например, StartAt не может быть позже EndAt) – проверяются в кастомном фильтре ValidateInputModelAttribute.
Выходная валидация – рекурсивно проверяет DTO перед отправкой ответа (также через ValidateInputModelAttribute после выполнения действия).

## 📖 Краткая документация API
Базовый префикс: /api/v1/events

Метод	Эндпоинт	Описание
GET	 /{id}	Получить событие по GUID
GET	 /	Получить список всех событий
POST /{id}	/	Создать одно событие
POST /range	Создать несколько событий (массив)
PUT	 /{id}	Обновить существующее событие
PUT	 /	Обновить несколько событий (массив)
DELETE/{id}	Удалить событие

Пример тела запроса (EventDto)
json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Встреча команды",
  "description": "Обсуждение спринта",
  "startAt": "2026-03-20T10:00:00",
  "endAt": "2026-03-20T11:30:00"
} 
 
## 🧪 Тестирование API 
Рекомендуется использовать Swagger UI (доступен после запуска) или Postman / Insomnia.
 
### 📁 Структура проекта (основные папки)
text
""Sprint1_Project_ASP_NetCore_API/
├── Controllers/
├── Data/
│   ├── Dtos/
│   ├── Entities/
│   └── Repositories/
├── Filters/
├── Services/
├── Profiles/ (AutoMapper)
└── Program.cs""


### 🤝 Вклад в проект
Проект является учебным, но если вы нашли ошибку – создайте Issue или Pull Request.


### 📄 Лицензия
MIT




 
