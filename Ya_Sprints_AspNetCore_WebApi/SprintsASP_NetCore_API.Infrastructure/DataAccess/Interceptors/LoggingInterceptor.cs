using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;


namespace SprintsASP_NetCore_API.Infrastructure.DataAccess.Interceptors;

public class LoggingInterceptor : DbCommandInterceptor
{

    public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
    {
        _logger = logger;
    }

    private ILogger<LoggingInterceptor> _logger;

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        if (eventData.Duration.TotalMilliseconds > 500)
        {
            // Логика: запись в лог текста команды и времени выполнения
            _logger.LogInformation($"Медленный запрос ({eventData.Duration.TotalMilliseconds}ms): {command.CommandText}");
        }
        return base.ReaderExecuted(command, eventData, result);
    }
}