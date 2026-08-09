using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;


namespace SprintASP_NetCore_API.Data.DataAccess.Interceptors;


public class ConnectionInterceptor : DbConnectionInterceptor
{

    // Срабатывает сразу после того, как соединение с Postgres было открыто
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        // Устанавливаем часовой пояс для текущей сессии
        command.CommandText = "SET TIME ZONE 'UTC';";
        command.ExecuteNonQuery();

        Console.WriteLine("Настройки сессии Postgres применены успешно.");
    }

    
    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        using var command = connection.CreateCommand();
        // Устанавливаем часовой пояс для текущей сессии
        command.CommandText = "SET TIME ZONE 'UTC';";
        command.ExecuteNonQuery();

        Console.WriteLine("Настройки сессии Postgres применены успешно.");

        return base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }
}
