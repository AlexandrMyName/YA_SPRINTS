using SprintsASP_NetCore_API.Application.Abstractions;
using SprintASP_NetCore_API.Domain.Entities;
using System.Linq.Expressions;


namespace SprintsASP_NetCore_API.Application.Dtos.Filters;


public class UserFilterDto : IEntityFilter<User>
{
    public string? Login { get; set; }
    public UserRole? Role { get; set; }
    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 20;

    public Expression<Func<User, bool>> ToPredicate()
        => u => (string.IsNullOrEmpty(Login) || u.Login.Contains(Login))
             && (!Role.HasValue || u.Role == Role.Value);
}
