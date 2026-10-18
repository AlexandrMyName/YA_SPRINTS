using Sprints.Users.Domain.Entities; 


namespace Sprints.Users.Application.Abstractions;


public interface IFilterModel<T> where T : class, IEntity
{

    public void Search(Func<T, object> filter)
    {


    }
}
