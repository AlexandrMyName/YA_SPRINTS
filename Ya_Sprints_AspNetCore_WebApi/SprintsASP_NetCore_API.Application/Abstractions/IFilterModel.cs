using Sprints_Project_ASP_NetCore_API.Data.Entities;


namespace SprintsASP_NetCore_API.Application.Abstractions
{

    public interface IFilterModel<T> where T : class, IEntity
    {

        public void Search(Func<T, object> filter)
        {


        }
    }
}
