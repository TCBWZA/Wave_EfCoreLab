using EfCoreLab.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCoreLab.Tests.TestHelpers
{
    /// <summary>
    /// Factory for creating in-memory BonusDbContext instances for testing.
    /// Each test gets its own isolated database instance.
    /// </summary>
    public static class BonusTestDbContextFactory
    {
        /// <summary>
        /// Creates a new BonusDbContext using an in-memory database.
        /// Each context gets a unique database name to ensure test isolation.
        /// </summary>
        /// <returns>A new BonusDbContext instance configured for testing</returns>
        public static BonusDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<BonusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new BonusDbContext(options);
        }

        /// <summary>
        /// Creates and seeds a BonusDbContext with test data.
        /// </summary>
        public static BonusDbContext CreateSeededContext()
        {
            var context = CreateInMemoryContext();
            SeedTestData(context);
            return context;
        }

        /// <summary>
        /// Seeds the database with standard test data including audit fields.
        /// </summary>
        private static void SeedTestData(BonusDbContext context)
        {
            var now = DateTime.UtcNow;

            var customers = new[]
            {
                new BonusCustomer
                {
                    Id = 1,
                    Name = "Acme Corporation",
                    Email = "contact@acmecorporation.com",
                    CreatedDate = now.AddDays(-30),
                    ModifiedDate = now.AddDays(-30),
                    IsDeleted = false
                },
                new BonusCustomer
                {
                    Id = 2,
                    Name = "Tech Solutions Ltd",
                    Email = "info@techsolutions.com",
                    CreatedDate = now.AddDays(-20),
                    ModifiedDate = now.AddDays(-10),
                    IsDeleted = false
                },
                new BonusCustomer
                {
                    Id = 3,
                    Name = "Global Industries",
                    Email = "hello@globalindustries.com",
                    CreatedDate = now.AddDays(-15),
                    ModifiedDate = now.AddDays(-5),
                    IsDeleted = false
                },
                new BonusCustomer
                {
                    Id = 4,
                    Name = "Deleted Company",
                    Email = "deleted@deletedcompany.com",
                    CreatedDate = now.AddDays(-40),
                    ModifiedDate = now.AddDays(-7),
                    IsDeleted = true,
                    DeletedDate = now.AddDays(-7)
                }
            };

            var invoices = new[]
            {
                new BonusInvoice
                {
                    Id = 1,
                    InvoiceNumber = "INV-001",
                    CustomerId = 1,
                    InvoiceDate = DateTime.UtcNow.AddDays(-30),
                    Amount = 1000.00m,
                    CreatedDate = now.AddDays(-30),
                    ModifiedDate = now.AddDays(-30),
                    IsDeleted = false
                },
                new BonusInvoice
                {
                    Id = 2,
                    InvoiceNumber = "INV-002",
                    CustomerId = 1,
                    InvoiceDate = DateTime.UtcNow.AddDays(-15),
                    Amount = 2500.50m,
                    CreatedDate = now.AddDays(-15),
                    ModifiedDate = now.AddDays(-15),
                    IsDeleted = false
                },
                new BonusInvoice
                {
                    Id = 3,
                    InvoiceNumber = "INV-003",
                    CustomerId = 2,
                    InvoiceDate = DateTime.UtcNow.AddDays(-10),
                    Amount = 750.00m,
                    CreatedDate = now.AddDays(-10),
                    ModifiedDate = now.AddDays(-10),
                    IsDeleted = false
                },
                new BonusInvoice
                {
                    Id = 4,
                    InvoiceNumber = "INV-004",
                    CustomerId = 1,
                    InvoiceDate = DateTime.UtcNow.AddDays(-5),
                    Amount = 500.00m,
                    CreatedDate = now.AddDays(-5),
                    ModifiedDate = now.AddDays(-5),
                    IsDeleted = true,
                    DeletedDate = now.AddDays(-2)
                }
            };

            var phoneNumbers = new[]
            {
                new BonusTelephoneNumber
                {
                    Id = 1,
                    CustomerId = 1,
                    Type = "Mobile",
                    Number = "+44 7700 900123",
                    CreatedDate = now.AddDays(-30),
                    ModifiedDate = now.AddDays(-30),
                    IsDeleted = false
                },
                new BonusTelephoneNumber
                {
                    Id = 2,
                    CustomerId = 1,
                    Type = "Work",
                    Number = "+44 20 7946 0958",
                    CreatedDate = now.AddDays(-30),
                    ModifiedDate = now.AddDays(-30),
                    IsDeleted = false
                },
                new BonusTelephoneNumber
                {
                    Id = 3,
                    CustomerId = 2,
                    Type = "Mobile",
                    Number = "+44 7700 900456",
                    CreatedDate = now.AddDays(-20),
                    ModifiedDate = now.AddDays(-20),
                    IsDeleted = false
                }
            };

            context.BonusCustomers.AddRange(customers);
            context.BonusInvoices.AddRange(invoices);
            context.BonusTelephoneNumbers.AddRange(phoneNumbers);
            context.SaveChanges();
        }
    }
}
