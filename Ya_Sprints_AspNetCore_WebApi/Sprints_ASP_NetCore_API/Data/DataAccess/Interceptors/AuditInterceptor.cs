using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;


namespace SprintASP_NetCore_API.Data.DataAccess.Interceptors
{

    public class AuditInterceptor : SaveChangesInterceptor
    {

        public override InterceptionResult<int> SavingChanges(
          DbContextEventData eventData,
          InterceptionResult<int> result)
        {
            var entries = eventData.Context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                // Если у сущности есть нужные свойства, заполняем их
                // Например: entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }

            return base.SavingChanges(eventData, result);
        }
    }
}
