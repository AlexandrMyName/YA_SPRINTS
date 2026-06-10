using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprint1_Project_ASP_NetCore_API.Data.Entities;

namespace Sprint1_Project_ASP_NetCore_API.Data.Dtos.Internal
{
    public interface IResultDto<T>
    {

        string? Reason { get; }
        bool IsSuccesfuly { get; }
        string? Message { get; }
        T? Data { get; }
    }

    public class ResultDto<T> : IResultDto<T> where T : class, IEntityDto
    {

        public static implicit operator bool(ResultDto<T> resultDto) => resultDto.IsSuccesfuly;

        public static ResultDto<T> Ok(T data, string msg) => new() { Data = data, IsSuccesfuly = true, Message = msg, Reason = "" };
        public static ResultDto<T> Ok(string msg) => new() { Data = null, IsSuccesfuly = true, Message = msg, Reason = "" }; 
        public static ResultDto<T> Fail(string reason) => new() { Data = null, IsSuccesfuly = false, Message = "", Reason = reason };
         
        public string? Reason { get; private set; }

        public bool IsSuccesfuly { get; private set; }

        public string? Message { get; private set; }

        public T? Data { get; private set; }
    }
}
