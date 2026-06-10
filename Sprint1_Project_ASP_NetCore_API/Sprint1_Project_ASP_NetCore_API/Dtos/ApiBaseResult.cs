using System.Diagnostics.CodeAnalysis;
using System.Net;


namespace Sprint1_Project_ASP_NetCore_API.Dtos;

/// <summary>
/// Базовый интерфейс для работы с ApiResult
/// </summary>
public interface IApiResult
{
    /// <summary>
    /// Получить прикреплённые данные
    /// </summary>
    /// <returns></returns>
    object GetData();
}

public abstract class ApiBaseResult : IApiResult
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

    /// <summary>
    /// Получить прикреплённые данные
    /// </summary>
    /// <returns></returns>
    public virtual object GetData() => "";
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
    [NotNull]
    public required T Data { get; set; }

    /// <summary>
    /// Получить прикреплённые данные  
    /// </summary>
    /// <returns></returns>
    public override object GetData() => Data;

    /// <summary>
    /// OK - 200
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Created - 201
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
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

    /// <summary>
    /// NotFounded - 404
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Internal Server Error - 500
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
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