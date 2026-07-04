using Sprints_Project_ASP_NetCore_API.Data.Entities;


namespace SprintASP_NetCore_API.Data.Dtos.Filters;


public interface IEntityFilter<T> : IFilter<T> where T : IEntity { }

 