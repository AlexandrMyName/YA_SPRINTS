using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace dataBase_autoMigration_Lib
{
    /// <summary>
    /// Класс расширения для DbContext для автоматического добавления недостающих колонок в таблицы БД.
    /// Внимание: этот метод не изменяет существующие колонки, только добавляет новые.
    /// Рекомендуется использовать только для прототипов, для продакшена используйте миграции EF Core.
    /// </summary>
    public static class DynamicEntityMigration
    {
        /// <summary>
        /// Добавляет недостающие колонки для всех сущностей в DbContext.
        /// </summary>
        public static void CreateOrUpdateMigration(this DbContext dbContext)
            => EnsureColumnsForAllEntities(dbContext);

        private static void EnsureColumnsForAllEntities(DbContext context)
        {
            try
            {
                // Получаем все DbSet свойства
                var dbSetProperties = context.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType.IsGenericType &&
                                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

                foreach (var prop in dbSetProperties)
                {
                    var entityType = prop.PropertyType.GetGenericArguments()[0];
                    var tableName = GetTableName(entityType);
                    var schema = GetSchema(entityType) ?? "public";

                    // Свойства, не помеченные [NotMapped] и поддерживаемых типов
                    var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanWrite && p.GetMethod != null)
                        .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                        .Where(p => IsSupportedType(p.PropertyType));

                    foreach (var property in properties)
                    {
                        string columnName = GetColumnName(property);
                        bool isNullable = Nullable.GetUnderlyingType(property.PropertyType) != null ||
                                          !property.PropertyType.IsValueType;
                        string defaultValue = GetDefaultValue(property.PropertyType);
                        string nullNotNull = isNullable ? "NULL" : $"NOT NULL DEFAULT {defaultValue}";
                        string pgType = MapToPostgreSqlType(property);

                        string sql = $@"
                            DO $$ 
                            BEGIN
                                IF EXISTS (
                                    SELECT 1 FROM information_schema.tables 
                                    WHERE table_schema = '{schema}' AND table_name = '{tableName}'
                                ) THEN
                                    IF NOT EXISTS (
                                        SELECT 1 FROM information_schema.columns 
                                        WHERE table_schema = '{schema}' AND table_name = '{tableName}' 
                                          AND column_name = '{columnName}'
                                    ) THEN
                                        ALTER TABLE ""{schema}"".""{tableName}"" 
                                        ADD COLUMN ""{columnName}"" {pgType} {nullNotNull};
                                    END IF;
                                END IF;
                            END $$;";

                        Debug.WriteLine($"Executing SQL: {sql}");
                        context.Database.ExecuteSqlRaw(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Исключение выполнения миграции: {ex.Message} {ex.InnerException?.Message}");
            }
        }

        private static string GetTableName(Type entityType)
        {
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            return tableAttr != null && !string.IsNullOrEmpty(tableAttr.Name)
                ? tableAttr.Name
                : entityType.Name.ToLowerInvariant();
        }

        private static string GetSchema(Type entityType)
        {
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            return tableAttr?.Schema;
        }

        private static string GetColumnName(PropertyInfo prop)
        {
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
            return columnAttr != null && !string.IsNullOrEmpty(columnAttr.Name)
                ? columnAttr.Name
                : prop.Name;
        }

        private static string GetDefaultValue(Type type)
        {
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
            if (t == typeof(DateTime)) return "'1970-01-01 00:00:00+00'"; // для timestamp with time zone
            if (t == typeof(DateTimeOffset)) return "'1970-01-01 00:00:00+00'";
            if (t == typeof(TimeSpan)) return "'00:00:00'";
            if (t == typeof(Guid)) return "'00000000-0000-0000-0000-000000000000'::uuid";
            if (t.IsEnum) return "0";

            throw new NotSupportedException($"Неизвестный тип для значения по умолчанию: {t.Name}");
        }

        private static bool IsSupportedType(Type type)
        {
            // Исключаем коллекции (кроме строки)
            if (type != typeof(string) && type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                return false;

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

            // Все остальные значимые типы и классы не поддерживаем
            return false;
        }

        private static string MapToPostgreSqlType(PropertyInfo prop)
        {
            Type type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            // Если задан явный тип через ColumnAttribute.TypeName – используем его
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr != null && !string.IsNullOrEmpty(columnAttr.TypeName))
                return columnAttr.TypeName;

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