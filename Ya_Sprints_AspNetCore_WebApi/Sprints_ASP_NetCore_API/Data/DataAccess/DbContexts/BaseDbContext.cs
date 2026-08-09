using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sprints_Project_ASP_NetCore_API.Data.Entities;


namespace SprintASP_NetCore_API.Data.DataAccess.DbContexts
{

    public class BaseDbContext : DbContext
    {

        private static readonly object _lock = new();
        private static bool _databaseInitialized;
         


        #region Примеры компиляции

        //// 1. Определяем запрос один раз в статическом поле для переиспользования
        //private static readonly Func<BaseDbContext, int, Task<object?>> GetUserByIdCompiled =
        //    EF.CompileAsyncQuery((BaseDbContext db, int id) =>
        //       null; // db.Users.FirstOrDefault(u => u.Id == id));/

        //// 2. Использование в логике приложения
        //public async Task<object?> GetUserAsync(int id)
        //{
        //    return await GetUserByIdCompiled(this, id);
        //}

        #endregion


        #region Пример JSONB (Высокоэфективный тип для работы с JSON) 

        // 1. Поиск администраторов (чтение из JSONB)
        //var admins = await db.Users
        //    .Where(u => u.Metadata["Role"] == "Admin")
        //    .ToListAsync();

        //// 2. Обновление метаданных конкретного пользователя
        //var firstAdmin = admins.FirstOrDefault();
        //if (firstAdmin != null)
        //{
        //    firstAdmin.Metadata["LastLogin"] = DateTime.UtcNow.ToString();
        //    await db.SaveChangesAsync();
        //} 

        #endregion


        #region Пример Concurrency 

        // В PostgreSQL у каждой строки есть скрытая системная колонка xmin,
        // которая автоматически меняется при любом обновлении записи.
        // EF Core может использовать её как универсальный индикатор версии.

        // Конфигурация во Fluent API для PostgreSQL
        //modelBuilder.Entity<Product>()
        //.Property<uint>("Version")
        //.IsRowVersion();

        //// При сохранении EF Core проверит, совпадает ли Version в памяти с тем, что в БД
        //try 
        //{
        //    await db.SaveChangesAsync();
        //    }
        //catch (DbUpdateConcurrencyException ex)
        //{
        //    // Логика разрешения конфликта: уведомить пользователя, что данные уже изменились
        //}

        #endregion
         
        public BaseDbContext(DbContextOptions options ) : base(options)
        { 

            //if (!_databaseInitialized)
            //{
            //    lock (_lock)
            //    {
            //        if (!_databaseInitialized)
            //        {
            //            Database.EnsureCreated();
                        
            //            _databaseInitialized = true; 
            //        }
            //    }
            //}
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
           
            return await base.SaveChangesAsync(cancellationToken);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Применяем все конфигурации из сборки
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            // Конвертер для DateTime
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var dateTimeNullableConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue && v.Value.Kind != DateTimeKind.Utc ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            // Применяем ко всем свойствам типа DateTime и DateTime?
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {

                if (typeof(IEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<uint>("Version")
                        .IsRowVersion();
                } // указываем на объект синхронизации

                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(dateTimeNullableConverter);
                    }
                }
            }

            // общий префикс для таблиц
            // modelBuilder.HasDefaultSchema("yaPracticum_");
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                if (Database.IsNpgsql())
                {
                    // Подтянуть настройки из IOptions 
                    // optionsBuilder.UseNpgsql("Host=localhost;Database=mydb");
                }
                else if (Database.IsRelational())
                {
                    throw new NotSupportedException("Используемая база данных не поддерживается текущей версией приложения");
                }

            }
        }

    }
}
