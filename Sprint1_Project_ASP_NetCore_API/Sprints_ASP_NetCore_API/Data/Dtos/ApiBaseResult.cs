using System.Diagnostics.CodeAnalysis;
using System.Net;


namespace Sprint1_Project_ASP_NetCore_API.Data.Dtos;

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

     
    public static ApiResult Ok(string msg)
    {
        return new()
        {
            Message = msg,
            StatusCode = HttpStatusCode.OK,
            Success = true,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult Fail(string reason)
    {
        return new()
        {
            Message = reason,
            StatusCode = (HttpStatusCode) 400,
            Success = true,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult Created(string msg)
    {
        return new()
        {
            Message = msg,
            StatusCode = HttpStatusCode.Created,
            Success = true,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult NotFound(string reason)
    {
        return new()
        {
            Message = reason,
            StatusCode = HttpStatusCode.NotFound,
            Success = false,
            DateTime = System.DateTime.UtcNow,
        };
    }

    public static ApiResult ServerError(string reason)
    {
        return new()
        {
            Message = reason,
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
    public T? Data { get; set; }

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
    public static ApiResult<T> Ok<T>(T data, string msg)
    {
        return new()
        {
            Data = data,
            Message = msg,
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
    public static ApiResult<T> NotFound<T>( string reason)
    {
        return new()
        {
            Data = default(T),
            Message = reason,
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
    public static ApiResult<T> ServerError<T>(string exception)
    {
        return new()
        {
            Data = default(T),
            Message = exception,
            StatusCode = HttpStatusCode.InternalServerError,
            Success = false,
            DateTime = System.DateTime.UtcNow,
        };
    }

}