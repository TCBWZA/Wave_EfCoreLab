using EfCoreLab.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCoreLab.Data
{
    /// <summary>
    /// Bonus Challenge Database Context with advanced features:
    /// - Global query filters for soft delete
    /// - Audit interceptor for automatic timestamps
    /// - SQL query logging
    /// - Separate tables from main AppDbContext
    /// </summary>
    public class BonusDbContext : DbContext
    {
        private readonly ILogger<BonusDbContext>? _logger;

        public BonusDbContext(DbContextOptions<BonusDbContext> options, ILogger<BonusDbContext>? logger = null)
            : base(options)
        {
            _logger = logger;
        }

        public DbSet<BonusCustomer> BonusCustomers { get; set; } = null!;
        public DbSet<BonusInvoice> BonusInvoices { get; set; } = null!;
        public DbSet<BonusTelephoneNumber> BonusTelephoneNumbers { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Add audit interceptor
            optionsBuilder.AddInterceptors(new AuditInterceptor());

            // Enable sensitive data logging in development
            if (_logger != null)
            {
                optionsBuilder
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors()
                    .LogTo(
                        message => _logger.LogInformation(message),
                        new[] { DbLoggerCategory.Database.Command.Name },
                        LogLevel.Information);
            }

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure BonusCustomer
            modelBuilder.Entity<BonusCustomer>(b =>
            {
                b.ToTable("BonusCustomers");
                b.HasKey(c => c.Id);
                b.Property(c => c.Id).ValueGeneratedOnAdd();
                b.Property(c => c.Name).HasMaxLength(200).IsRequired();
                b.Property(c => c.Email).HasMaxLength(200).IsRequired();
                b.Property(c => c.CreatedDate).IsRequired();
                b.Property(c => c.ModifiedDate).IsRequired();
                b.Property(c => c.IsDeleted).IsRequired().HasDefaultValue(false);

                // Relationships
                b.HasMany(c => c.Invoices)
                    .WithOne()
                    .HasForeignKey(i => i.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

                b.HasMany(c => c.PhoneNumbers)
                    .WithOne()
                    .HasForeignKey(t => t.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Ignore calculated property
                b.Ignore(c => c.Balance);

                // Indexes
                b.HasIndex(c => c.Email).IsUnique();
                b.HasIndex(c => c.IsDeleted);
                b.HasIndex(c => c.CreatedDate);

                // Global query filter for soft delete
                b.HasQueryFilter(c => !c.IsDeleted);
            });

            // Configure BonusInvoice
            modelBuilder.Entity<BonusInvoice>(b =>
            {
                b.ToTable("BonusInvoices");
                b.HasKey(i => i.Id);
                b.Property(i => i.Id).ValueGeneratedOnAdd();
                b.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
                b.Property(i => i.Amount).HasColumnType("decimal(18,2)").IsRequired();
                b.Property(i => i.InvoiceDate).IsRequired();
                b.Property(i => i.CustomerId).IsRequired();
                b.Property(i => i.CreatedDate).IsRequired();
                b.Property(i => i.ModifiedDate).IsRequired();
                b.Property(i => i.IsDeleted).IsRequired().HasDefaultValue(false);

                // Indexes
                b.HasIndex(i => i.InvoiceNumber).IsUnique();
                b.HasIndex(i => i.CustomerId);
                b.HasIndex(i => i.IsDeleted);
                b.HasIndex(i => i.InvoiceDate);

                // Constraints
                b.ToTable(t => t.HasCheckConstraint("CK_BonusInvoice_Amount", "Amount >= 0"));

                // Global query filter for soft delete
                b.HasQueryFilter(i => !i.IsDeleted);
            });

            // Configure BonusTelephoneNumber
            modelBuilder.Entity<BonusTelephoneNumber>(b =>
            {
                b.ToTable("BonusTelephoneNumbers");
                b.HasKey(t => t.Id);
                b.Property(t => t.Id).ValueGeneratedOnAdd();
                b.Property(t => t.Number).HasMaxLength(50).IsRequired();
                b.Property(t => t.Type).HasMaxLength(20).IsRequired();
                b.Property(t => t.CustomerId).IsRequired();
                b.Property(t => t.CreatedDate).IsRequired();
                b.Property(t => t.ModifiedDate).IsRequired();
                b.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);

                // Indexes
                b.HasIndex(t => t.CustomerId);
                b.HasIndex(t => t.IsDeleted);
                b.HasIndex(t => t.Type);

                // Constraints
                b.ToTable(t => t.HasCheckConstraint("CK_BonusTelephoneNumber_Type",
                    "Type IN ('Mobile', 'Work', 'DirectDial')"));

                // Global query filter for soft delete
                b.HasQueryFilter(t => !t.IsDeleted);
            });

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Override SaveChanges to include soft delete entities in queries when needed.
        /// This allows explicit operations on soft-deleted entities.
        /// </summary>
        public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
