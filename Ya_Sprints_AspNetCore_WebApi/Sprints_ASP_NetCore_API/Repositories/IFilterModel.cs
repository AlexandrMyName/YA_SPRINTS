using Sprint1_Project_ASP_NetCore_API.Data.Entities;


namespace SprintASP_NetCore_API.Repositories
{

    public interface IFilterModel<T> where T : class, IEntity
    {
         
        public void Search(Func<T, object> filter)
        {


        }
    }
}
