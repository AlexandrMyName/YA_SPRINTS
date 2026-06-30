using Sprint1_Project_ASP_NetCore_API.Data.Entities;


namespace Sprint1_Project_ASP_NetCore_API.Data.Dtos.Internal
{

#pragma warning disable

    public interface IResultEntity<T>
    { 

        string? Reason { get; }  
        bool IsSuccesfuly { get; }
        string? Message { get; } 
        T? Data { get; }
    }

    public class ResultEntity<T> : IResultEntity<T> where T : class, IEntity
    {

        public static implicit operator bool(ResultEntity<T> resultDto) =>  resultDto.IsSuccesfuly;
         
        public static ResultEntity<T> Ok(T data, string msg) => new() { Data = data, IsSuccesfuly  = true, Message = msg , Reason = ""};
        public static ResultEntity<T> Ok( string msg) => new() { Data = null, IsSuccesfuly = true, Message = msg, Reason = "" };

        public static ResultEntity<T> Fail(string reason) => new() { Data = null, IsSuccesfuly = false, Message = "", Reason = reason};

         
        public string? Reason { get; private set; }

        public bool IsSuccesfuly { get; private set; }

        public string? Message { get; private set; }

        public T? Data { get; private set; }
    }
}
