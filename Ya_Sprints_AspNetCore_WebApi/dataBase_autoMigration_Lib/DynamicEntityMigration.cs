using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore; 
using System.Diagnostics;
using System.Reflection;
 

namespace dataBase_autoMigration_Lib
{
    /// <summary>
    /// Класс расширения для DbContext для автоматического обновления таблиц БД 
    /// </summary>
    public static class DynamicEntityMigration
    {

        /// <summary>
        /// Метод автоматически добавляет не достающие колонки в таблицы БД 
        /// Вызывать следует при первом запуске приложения и если есть флаг изменения данных (в рантайме)
        /// </summary>
        /// <param name="dbContext"></param>
        public static void CreateOrUpdateMigration(this DbContext dbContext)  => EnsureColumnsForAllEntities(dbContext);
    

        private static void EnsureColumnsForAllEntities(DbContext context)
        {
            try
            {
                var dbSetProperties = context.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

                foreach (var prop in dbSetProperties)
                {
                    var entityType = prop.PropertyType.GetGenericArguments()[0];
                    var tableName = prop.Name;  //  GetTableName(entityType);

                    var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanWrite && p.GetMethod != null)
                        .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                        .Where(p => IsSupportedType(p.PropertyType)); // фильтруем только поддерживаемые типы

                    foreach (var property in properties)
                    {
                        string columnName = property.Name;
                        bool isNullable = Nullable.GetUnderlyingType(property.PropertyType) != null || !property.PropertyType.IsValueType;

                        string defaultValue = GetDefaultValue(property.PropertyType);
                        string nullNotNull = isNullable ? "NULL" : $"NOT NULL DEFAULT {defaultValue}";

                        string pgType = MapToPostgreSqlType(property);

                        string sql = $@"
                            DO $$ 
                            BEGIN
                                IF EXISTS (
                                    SELECT 1 FROM information_schema.tables WHERE table_name = '{tableName}'
                                ) THEN
                                    IF NOT EXISTS (
                                        SELECT 1 FROM information_schema.columns 
                                        WHERE table_name = '{tableName}' AND column_name = '{columnName}'
                                    ) THEN
                                        ALTER TABLE ""{tableName}"" ADD COLUMN ""{columnName}"" {pgType} {nullNotNull};
                                    END IF;
                                END IF;
                            END $$;";
                        Debug.WriteLine($"Executing SQL: {sql}"); // или Console.WriteLine
                        context.Database.ExecuteSqlRaw(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Исключение выполнения миграции", ex.Message + " " + ex.InnerException?.Message);
            }
        }
         
        private static string GetDefaultValue(Type type)
        {
            // Для nullable типов берём underlying
            Type t = Nullable.GetUnderlyingType(type) ?? type;

            if (t == typeof(string)) return "''";
            if (t == typeof(int)) return "0";
            if (t == typeof(long)) return "0";
            if (t == typeof(short)) return "0";
            if (t == typeof(byte)) return "0";
            if (t == typeof(decimal)) return "0.0";
            if (t == typeof(float)) return "0.0";
            if (t == typeof(double)) return "0.0";
            if (t == typeof(bool)) return "false";
            if (t == typeof(DateTime)) return "'1970-01-01 00:00:00'";
            if (t == typeof(DateTimeOffset)) return "'1970-01-01 00:00:00+00'";
            if (t == typeof(TimeSpan)) return "'00:00:00'";
            if (t == typeof(Guid)) return "'00000000-0000-0000-0000-000000000000'";
            if (t.IsEnum) return "0"; // первое значение enum

            throw new NotSupportedException($"Неизвестный тип для значения по умолчанию: {t.Name}");
        }
        private static string GetTableName(Type entityType)
        {
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            if (tableAttr != null && !string.IsNullOrEmpty(tableAttr.Name))
                return tableAttr.Name;

            return entityType.Name.ToLowerInvariant();
        }

        private static bool IsSupportedType(Type type)
        {
            // Проверяем, является ли тип коллекцией (кроме строки)
            if (type != typeof(string) && type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                return false;

            // Проверяем, является ли тип сложным классом (не примитив, не enum, не известный тип)
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType.IsPrimitive) return true;
            if (underlyingType.IsEnum) return true;
            if (underlyingType == typeof(string)) return true;
            if (underlyingType == typeof(decimal)) return true;
            if (underlyingType == typeof(DateTime)) return true;
            if (underlyingType == typeof(DateTimeOffset)) return true;
            if (underlyingType == typeof(TimeSpan)) return true;
            if (underlyingType == typeof(Guid)) return true;
            if (underlyingType == typeof(byte[])) return true;
            if (underlyingType == typeof(byte)) return true;

            // Если это значимый тип (struct) и не попал в список – пропускаем
            if (underlyingType.IsValueType) return false;

            // Это класс – пропускаем
            return false;
        }

        private static string MapToPostgreSqlType(PropertyInfo prop)
        {
            Type type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (type == typeof(int)) return "integer";
            if (type == typeof(long)) return "bigint";
            if (type == typeof(short)) return "smallint";
            if (type == typeof(byte)) return "smallint";
            if (type == typeof(decimal)) return "numeric(18,2)";
            if (type == typeof(float)) return "real";
            if (type == typeof(double)) return "double precision";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(DateTime)) return "timestamp with time zone";
            if (type == typeof(DateTimeOffset)) return "timestamp with time zone";
            if (type == typeof(TimeSpan)) return "interval";
            if (type == typeof(string))
            {
                var maxLen = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length
                             ?? prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;
                return maxLen.HasValue ? $"character varying({maxLen.Value})" : "text";
            }

            if (type == typeof(Guid)) return "uuid";
            if (type == typeof(byte[])) return "bytea";
            if (type.IsEnum) return "integer";

            throw new NotSupportedException($"Тип {type.Name} не поддерживается для автоматической миграции");
        }
    }
}