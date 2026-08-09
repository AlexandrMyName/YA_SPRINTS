

namespace SprintASP_NetCore_API.Services.Referencies;

/// <summary>
/// сервис для хранения ссылок на доп. информацию
/// </summary>
public class RefDataService : IReferenciesData
{

    public RefDataService()
    {
        ProcessorCount = Environment.ProcessorCount;
    }

    /// <summary>
    /// Число ядер всех процессоров
    /// </summary>
    public int ProcessorCount { get; set; }


}


public interface IReferenciesData
{
    /// <summary>
    /// Число ядер всех процессоров
    /// </summary>
    int ProcessorCount { get; }

}