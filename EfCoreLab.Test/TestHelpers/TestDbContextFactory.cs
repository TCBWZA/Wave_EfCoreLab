using EfCoreLab.Data;
using Microsoft.EntityFrameworkCore;

namespace EfCoreLab.Tests.TestHelpers
{
    /// <summary>
    /// Factory for creating in-memory database contexts for testing.
    /// Each test gets its own isolated database instance.
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates a new AppDbContext using an in-memory database.
        /// Each context gets a unique database name to ensure test isolation.
        /// </summary>
        /// <returns>A new AppDbContext instance configured for testing</returns>
        public static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        /// <summary>
        /// Creates and seeds a database context with test data.
        /// </summary>
        public static AppDbContext CreateSeededContext()
        {
            var context = CreateInMemoryContext();
            SeedTestData(context);
            return context;
        }

        /// <summary>
        /// Seeds the database with standard test data.
        /// </summary>
        private static void SeedTestData(AppDbContext context)
        {
            var customers = new[]
            {
                new Customer
                {
                    Id = 1,
                    Name = "Acme Corporation",
                    Email = "contact@example.com"
                },
                new Customer
                {
                    Id = 2,
                    Name = "Tech Solutions Ltd",
                    Email = "info@example.com"
                },
                new Customer
                {
                    Id = 3,
                    Name = "Global Industries",
                    Email = "hello@example.com"
                }
            };

            var invoices = new[]
            {
                new Invoice
                {
                    Id = 1,
                    InvoiceNumber = "INV-001",
                    CustomerId = 1,
                    InvoiceDate = DateTime.UtcNow.AddDays(-30),
                    Amount = 1000.00m
                },
                new Invoice
                {
                    Id = 2,
                    InvoiceNumber = "INV-002",
                    CustomerId = 1,
                    InvoiceDate = DateTime.UtcNow.AddDays(-15),
                    Amount = 2500.50m
                },
                new Invoice
                {
                    Id = 3,
                    InvoiceNumber = "INV-003",
                    CustomerId = 2,
                    InvoiceDate = DateTime.UtcNow.AddDays(-10),
                    Amount = 750.00m
                }
            };

            var phoneNumbers = new[]
            {
                new TelephoneNumber
                {
                    Id = 1,
                    CustomerId = 1,
                    Type = "Mobile",
                    Number = "+44 7700 900123"
                },
                new TelephoneNumber
                {
                    Id = 2,
                    CustomerId = 1,
                    Type = "Work",
                    Number = "+44 20 7946 0958"
                },
                new TelephoneNumber
                {
                    Id = 3,
                    CustomerId = 2,
                    Type = "Mobile",
                    Number = "+44 7700 900456"
                }
            };

            context.Customers.AddRange(customers);
            context.Invoices.AddRange(invoices);
            context.TelephoneNumbers.AddRange(phoneNumbers);
            context.SaveChanges();
        }
    }
}
