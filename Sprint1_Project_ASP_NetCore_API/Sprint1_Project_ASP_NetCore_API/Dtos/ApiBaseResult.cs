using System.Net;


namespace Sprint1_Project_ASP_NetCore_API.Dtos;

public abstract class ApiBaseResult 
{   
    /// <summary>
    /// Флаг, указывающий на успешность выполненного запроса
    /// </summary>
    public required bool Success { get; set; }  
    /// <summary>
    /// Возвращаемый HTTP-код
    /// </summary>
    public required HttpStatusCode StatusCode { get; set; }  
    /// <summary>
    /// Дата и время ответа
    /// </summary>
    public DateTime DateTime { get; set; }  
    /// <summary>
    /// Кастомное сообщение с дополнительной информацией
    /// Здесь может быть информация об ошибке в случае неуспеха
    /// </summary>
    public required string Message { get; set; } 
}

 
public class ApiResult : ApiBaseResult {

    public static ApiResult Ok()
    {
        return new()
        {
            Message = "",
            StatusCode = HttpStatusCode.OK,
            Success = true,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult Created()
    {
        return new()
        {
            Message = "",
            StatusCode = HttpStatusCode.Created,
            Success = true,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult NotFound()
    {
        return new()
        {
            Message = "",
            StatusCode = HttpStatusCode.NotFound,
            Success = false,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult ServerError()
    {
        return new()
        {
            Message = "",
            StatusCode = HttpStatusCode.InternalServerError,
            Success = false,
            DateTime = System.DateTime.UtcNow,
        };
    }


}
 
public class ApiResult<T> : ApiBaseResult
{

    /// <summary>
    /// Возвращаемые данные метода
    /// </summary>
    public required T Data { get; set; }


    public static ApiResult<T> Ok<T>(T data)
    {
        return new()
        {
            Data = data,
            Message = "",
            StatusCode = HttpStatusCode.OK,
            Success = true,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult<T> Created<T>(T data)
    {
        return new()
        {
            Data = data,
            Message = "",
            StatusCode = HttpStatusCode.Created,
            Success = true,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult<T> NotFound<T>(T data)
    {
        return new()
        {
            Data = data,
            Message = "",
            StatusCode = HttpStatusCode.NotFound,
            Success = false,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult<T> ServerError<T>(T data)
    {
        return new()
        {
            Data = data,
            Message = "",
            StatusCode = HttpStatusCode.InternalServerError,
            Success = false,
            DateTime = System.DateTime.UtcNow,
        };
    }

}