

using Microsoft.AspNetCore.Mvc;

namespace SprintASP_NetCore_API.Data.Dtos.Filters;


public interface IFilter<T>
{
      
    string? SortBy { get; set; } 
    bool SortDesc { get; set; }   
    int? Page { get; set; }   
    int? PageSize { get; set; } 

    IQueryable<T> Apply(IQueryable<T> query);
}