using EfCoreLab.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreLab.Interceptors
{
    /// <summary>
    /// EF Core interceptor that automatically sets audit fields (CreatedDate, ModifiedDate)
    /// and handles soft delete timestamps (DeletedDate) when saving changes.
    /// 
    /// This interceptor runs before SaveChanges and updates the appropriate audit fields
    /// based on the entity state (Added, Modified, Deleted).
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        /// <summary>
        /// Intercepts SaveChanges synchronously to add audit information.
        /// </summary>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            UpdateAuditFields(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Intercepts SaveChangesAsync to add audit information.
        /// </summary>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            UpdateAuditFields(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Updates audit fields for all tracked entities based on their state.
        /// </summary>
        private void UpdateAuditFields(DbContext? context)
        {
            if (context == null) return;

            var entries = context.ChangeTracker.Entries();
            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                // Handle BonusCustomer entities
                if (entry.Entity is BonusCustomer customer)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            customer.CreatedDate = now;
                            customer.ModifiedDate = now;
                            break;
                        case EntityState.Modified:
                            customer.ModifiedDate = now;
                            // If being soft deleted, set DeletedDate
                            if (customer.IsDeleted && !customer.DeletedDate.HasValue)
                            {
                                customer.DeletedDate = now;
                            }
                            // If being restored, clear DeletedDate
                            if (!customer.IsDeleted && customer.DeletedDate.HasValue)
                            {
                                customer.DeletedDate = null;
                            }
                            break;
                    }
                }
                // Handle BonusInvoice entities
                else if (entry.Entity is BonusInvoice invoice)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            invoice.CreatedDate = now;
                            invoice.ModifiedDate = now;
                            break;
                        case EntityState.Modified:
                            invoice.ModifiedDate = now;
                            if (invoice.IsDeleted && !invoice.DeletedDate.HasValue)
                            {
                                invoice.DeletedDate = now;
                            }
                            if (!invoice.IsDeleted && invoice.DeletedDate.HasValue)
                            {
                                invoice.DeletedDate = null;
                            }
                            break;
                    }
                }
                // Handle BonusTelephoneNumber entities
                else if (entry.Entity is BonusTelephoneNumber phone)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            phone.CreatedDate = now;
                            phone.ModifiedDate = now;
                            break;
                        case EntityState.Modified:
                            phone.ModifiedDate = now;
                            if (phone.IsDeleted && !phone.DeletedDate.HasValue)
                            {
                                phone.DeletedDate = now;
                            }
                            if (!phone.IsDeleted && phone.DeletedDate.HasValue)
                            {
                                phone.DeletedDate = null;
                            }
                            break;
                    }
                }
            }
        }
    }
}
