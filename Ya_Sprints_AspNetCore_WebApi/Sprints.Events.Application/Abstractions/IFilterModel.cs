using Sprints.Events.Domain.Entities; 


namespace Sprints.Events.Application.Abstractions;


public interface IFilterModel<T> where T : class, IEntity
{

    public void Search(Func<T, object> filter)
    {


    }
}
