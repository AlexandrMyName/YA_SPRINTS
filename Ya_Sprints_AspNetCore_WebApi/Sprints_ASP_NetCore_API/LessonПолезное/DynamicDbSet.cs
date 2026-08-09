using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;


namespace SprintASP_NetCore_API.LessonПолезное
{

    //public class DynamicDbContext : DbContext
    //{
    //    private readonly List<Type> _dynamicEntityTypes;

    //    public DynamicDbContext(DbContextOptions options, List<Type> dynamicTypes)
    //        : base(options)
    //    {
    //        _dynamicEntityTypes = dynamicTypes;
    //    }

    //    protected override void OnModelCreating(ModelBuilder modelBuilder)
    //    {
    //        foreach (var type in _dynamicEntityTypes)
    //        {
    //            // Регистрируем сущность динамически!
    //            var entityBuilder = modelBuilder.Entity(type);

    //            // Указываем имя таблицы в БД (например, берём из атрибута или имени класса)
    //            entityBuilder.ToTable(type.Name);

    //            // Динамически прописываем Primary Key (допустим, свойство "Id")
    //            var idProp = type.GetProperty("Id");
    //            if (idProp != null)
    //                entityBuilder.HasKey(idProp.Name);

    //            // Остальные свойства настроятся по умолчанию (маппинг по имени колонок)
    //        }
    //    }
    //}


    //// Допустим, пользователь создал тип "Product" и мы получили его Type
    //Type userType = GetUserCreatedType("Product");

    //// Получаем IQueryable для этого типа
    //IQueryable dynamicQueryable = dbContext.Set(userType);

    //// Применяем динамический фильтр (например, "Price > 100")
    //ParameterExpression param = Expression.Parameter(userType, "x");
    //PropertyInfo priceProp = userType.GetProperty("Price");
    //MemberExpression priceAccess = Expression.Property(param, priceProp);
    //ConstantExpression threshold = Expression.Constant(100m);
    //BinaryExpression body = Expression.GreaterThan(priceAccess, threshold);
    //LambdaExpression lambda = Expression.Lambda(body, param);

    //// ПРИМЕНЯЕМ WHERE (вызываем статический метод Queryable.Where)
    //IQueryable filteredQuery = Queryable.Where(dynamicQueryable, (dynamic)lambda);

    //// Выполняем в БД
    //List<object> results = await filteredQuery.ToListAsync(); // Все объекты лежат как object


    // Допустим, есть OrdersType и UsersType
    // IQueryable usersQuery = dbContext.Set(usersType);
    // IQueryable ordersQuery = dbContext.Set(ordersType);

    // Вызываем GroupJoin через рефлексию или динамический вызов
    // Проще всего использовать Queryable.GroupJoin с передачей LambdaExpression



    // Решение: Генерация миграций на лету
    //Вот пошаговая стратегия, которая решит вашу проблему раз и навсегда.

    //Шаг 1. Синхронизация модели
    //Перед тем как что-то менять в БД, вы должны обновить свою динамическую модель(класс, который вы создали через TypeBuilder), добавив в него новое свойство.Это обязательное условие.

    //Шаг 2. Создание "пустой" миграции
    //Вместо того чтобы пытаться применить миграции через dbContext.Database.Migrate() (что требует наличия всей истории), вы будете использовать EF Core как генератор SQL-скриптов.

    //Добавьте в свой проект новый класс миграции, но не через dotnet ef migrations add, а программно, используя DesignTimeServices(это сложно).

    //Более простой путь: добавьте пустую миграцию вручную.

    //csharp
    //// Создайте класс миграции, например, Migrations/AddNewColumn.cs
    //public partial class AddNewColumn : Migration
    //    {
    //        protected override void Up(MigrationBuilder migrationBuilder)
    //        {
    //            // Здесь будет ваш код, который мы сгенерируем на следующем шаге
    //        }

    //        protected override void Down(MigrationBuilder migrationBuilder)
    //        {
    //        }
    //    }
    //    Шаг 3. Генерация SQL через MigrationBuilder(Самый важный шаг)
    //Вместо того чтобы писать сырой SQL, вы используете API MigrationBuilder.Он сам сгенерирует правильный SQL для PostgreSQL с учетом всех нюансов.

    //csharp
    //// В методе Up вашей миграции
    //protected override void Up(MigrationBuilder migrationBuilder)
    //    {
    //        // 1. Получаем ваш динамический тип
    //        Type dynamicEntityType = GetUserCreatedType("YourTableName");

    //        // 2. Создаем объект, описывающий новую колонку
    //        var column = new AddColumnOperation
    //        {
    //            Name = "NewColumnName",
    //            Table = "YourTableName", // Имя таблицы в БД
    //            Schema = "public",
    //            ClrType = typeof(string), // Тип свойства
    //            MaxLength = 100,
    //            IsNullable = true, // или false, если есть DefaultValue
    //                               // DefaultValue = "some value" // если нужно
    //        };

    //        // 3. Генерируем SQL команду
    //        migrationBuilder.AddColumn<string>(
    //            name: "NewColumnName",
    //            table: "YourTableName",
    //            type: "character varying(100)", // Npgsql сам смаппит тип
    //            nullable: true);
    //    }
    //    Шаг 4. Получение готового SQL-скрипта(Вместо Migrate())
    //Теперь самый главный фокус.Вместо того чтобы применять миграцию к БД, вы просите EF Core сгенерировать для неё SQL-скрипт.Это и есть ваш "патч" для клиентов.

    //csharp
    //using Microsoft.EntityFrameworkCore.Design;
    //using Microsoft.EntityFrameworkCore.Migrations.Design;

    //// ... в вашем коде

    //// 1. Создаете ваш DbContext (с обновленной динамической моделью)
    //using (var context = new YourDynamicDbContext())
    //{
    //    // 2. Получаете сервис для генерации скриптов
    //    var migrator = context.GetService<IMigrator>();

    //    // 3. Генерируете SQL-скрипт для конкретной миграции
    //    // Параметры: fromMigration (null - с начала), toMigration (имя вашей миграции)
    //    string sqlScript = migrator.GenerateScript(null, "AddNewColumn");

    //    // 4. Сохраняете скрипт в файл или выполняете через NpgsqlCommand
    //    File.WriteAllText("UpdateScript.sql", sqlScript);

    //    // 5. (Опционально) Выполняете скрипт для текущего клиента
    //    // context.Database.ExecuteSqlRaw(sqlScript);
    //}
    //Как это решает вашу проблему
    //Единый источник правды: Вы работаете с C# и MigrationBuilder, а не с сырыми строками SQL.

    //Корректный SQL: Npgsql(провайдер для PostgreSQL) знает, как правильно экранировать имена, преобразовывать типы (например, DateTime в timestamp with time zone) и добавлять индексы.

    //Патчи для клиентов: Вы генерируете SQL - скрипт один раз у себя в офисе и отправляете его клиентам. Они выполняют его через любой инструмент (pgAdmin, psql) — это безопасно и предсказуемо.

    //История миграций: Вы можете даже вести историю миграций в коде, но для клиентов применять только свежесгенерированные скрипты.

    //Альтернативный, но рискованный путь
    //Можно использовать dbContext.Database.ExecuteSqlRaw() прямо в коде приложения, но я настоятельно не рекомендую это для продакшена. Это приводит к "дрейфу схемы" (schema drift), когда код и структура БД у разных клиентов начинают различаться, и вы теряете контроль над происходящим.
    // Этот подход превратит адский ручной труд в контролируемый и автоматизированный процесс.Удачи



    // Авто добавление 

    //    protected override void Up(MigrationBuilder migrationBuilder)
    //    {
    //        // 1. Получаем список динамических типов, которые были зарегистрированы
    //        //    (вы храните их где-то, например, в статическом списке)
    //        var entityTypes = DynamicEntityRegistry.GetAllTypes();

    //        foreach (Type entityType in entityTypes)
    //        {
    //            string tableName = entityType.Name; // или возьмите из атрибута Table
    //            EnsureColumnsExist(migrationBuilder, entityType, tableName);
    //        }
    //    }

    //    private void EnsureColumnsExist(MigrationBuilder migrationBuilder, Type entityType, string tableName)
    //    {
    //        // 2. Получаем все публичные свойства (кроме игнорируемых)
    //        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //                                   .Where(p => p.CanWrite && p.GetMethod != null);

    //        // 3. Для каждого свойства генерируем ALTER TABLE ADD COLUMN,
    //        //    но только если колонка ещё не существует (проверка через INFORMATION_SCHEMA)
    //        foreach (var prop in properties)
    //        {
    //            // Пропускаем свойства, помеченные [NotMapped] или [Ignore] (если используете)
    //            if (prop.GetCustomAttribute<NotMappedAttribute>() != null)
    //                continue;

    //            // Определяем имя колонки (можно взять из [Column] атрибута)
    //            string columnName = prop.Name;
    //            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
    //            if (columnAttr != null && !string.IsNullOrEmpty(columnAttr.Name))
    //                columnName = columnAttr.Name;

    //            // Определяем, является ли тип nullable
    //            bool isNullable = Nullable.GetUnderlyingType(prop.PropertyType) != null
    //                              || !prop.PropertyType.IsValueType;

    //            // Определяем тип PostgreSQL
    //            string pgType = MapToPostgreSqlType(prop);

    //            // Формируем SQL с проверкой существования колонки (безопасно для повторных запусков)
    //            string sql = $@"
    //DO $$ 
    //BEGIN
    //    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
    //                   WHERE table_name = '{tableName}' AND column_name = '{columnName}') 
    //    THEN
    //        ALTER TABLE ""{tableName}"" ADD COLUMN ""{columnName}"" {pgType} {(isNullable ? "NULL" : "NOT NULL")};
    //    END IF;
    //END $$;";

    //            // Выполняем SQL
    //            migrationBuilder.Sql(sql);
    //        }
    //    }

    //    private string MapToPostgreSqlType(PropertyInfo prop)
    //    {
    //        Type type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

    //        // Базовая маппинг (можно расширить под ваши нужды)
    //        if (type == typeof(int)) return "integer";
    //        if (type == typeof(long)) return "bigint";
    //        if (type == typeof(short)) return "smallint";
    //        if (type == typeof(decimal)) return "numeric(18,2)"; // можно вынести в атрибут
    //        if (type == typeof(float)) return "real";
    //        if (type == typeof(double)) return "double precision";
    //        if (type == typeof(bool)) return "boolean";
    //        if (type == typeof(DateTime)) return "timestamp with time zone";
    //        if (type == typeof(DateTimeOffset)) return "timestamp with time zone";
    //        if (type == typeof(TimeSpan)) return "interval";
    //        if (type == typeof(string))
    //        {
    //            // Читаем атрибут MaxLength
    //            var maxLen = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length
    //                         ?? prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;
    //            return maxLen.HasValue ? $"character varying({maxLen.Value})" : "text";
    //        }
    //        if (type == typeof(Guid)) return "uuid";
    //        if (type == typeof(byte[])) return "bytea";
    //        // JSON, enum, и т.д. – добавляйте по необходимости

    //        throw new NotSupportedException($"Тип {type.Name} не поддерживается для автоматической миграции");
    //    }


    // В методе инициализации (например, после OnConfiguring)
    //using (var context = new YourDbContext())
    //{
    //    foreach (var type in DynamicEntityRegistry.GetAllTypes())
    //    {
    //        string sql = GenerateAddColumnSql(type);
    //        context.Database.ExecuteSqlRaw(sql);
    //    }
    //}
}
