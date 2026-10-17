using Sprints.Bookings.Domain.Entities; 


namespace Sprints.Bookings.Application.Abstractions;


public interface IFilterModel<T> where T : class, IEntity
{

    public void Search(Func<T, object> filter)
    {


    }
}
