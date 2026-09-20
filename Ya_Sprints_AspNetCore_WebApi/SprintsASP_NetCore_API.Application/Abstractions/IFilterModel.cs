using SprintASP_NetCore_API.Domain.Entities; 


namespace SprintsASP_NetCore_API.Application.Abstractions;


public interface IFilterModel<T> where T : class, IEntity
{

    public void Search(Func<T, object> filter)
    {


    }
}
