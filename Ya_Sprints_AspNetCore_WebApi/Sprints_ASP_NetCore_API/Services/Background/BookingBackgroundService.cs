


using SprintASP_NetCore_API.Data.Dtos.Filters;

namespace SprintASP_NetCore_API.Services.Background;


public class BookingBackgroundService : BackgroundService
{

    public BookingBackgroundService(ILogger<BookingBackgroundService> logger, IBookingService bookingService)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    private ILogger<BookingBackgroundService> _logger;
    private IBookingService _bookingService;


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

                var filtredDatas = await _bookingService.GetFilteredAsync(new BookingFilterDto()
                {
                    Status = Data.Entities.BookingStatus.Pending,
                });

                if(filtredDatas.TotalCount > 0)
                { 
                    foreach(var data in filtredDatas.Items)
                    {
                        await Task.Delay(2000, stoppingToken); 
                        data.Status = Data.Entities.BookingStatus.Confirmed;
                        data.ProcessedAt = DateTime.Now;
                        await _bookingService.UpdateBookingAsync(data);
                    } 
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch(OperationCanceledException operationCanceledExcept)
            {
                _logger.LogDebug("Работа BookingService была отменена: " + operationCanceledExcept.Message);
            }
            catch(Exception ex)
            {
                
                _logger.LogError("Вызвано исключение (BookingBackground): " + ex.Message);
            } 
        }

        _logger.LogDebug("Работа BookingService завершена"); 
    }
}
