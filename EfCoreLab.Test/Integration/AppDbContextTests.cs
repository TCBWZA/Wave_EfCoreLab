using EfCoreLab.Data;
using EfCoreLab.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace EfCoreLab.Tests.Integration
{
    [TestFixture]
    public class AppDbContextTests
    {
        private AppDbContext _context;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region Database Operations Tests

        [Test]
        public async Task Database_CanSaveAndRetrieveCustomer()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Test Company",
                Email = "test@example.com"
            };

            // Act
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var retrieved = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == "test@example.com");

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.Name, Is.EqualTo("Test Company"));
        }

        [Test]
        public async Task Database_CanSaveAndRetrieveInvoice()
        {
            // Arrange
            var customer = new Customer { Name = "Test", Email = "test@example.com" };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "INV-TEST",
                CustomerId = customer.Id,
                InvoiceDate = DateTime.UtcNow,
                Amount = 100.00m
            };

            // Act
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            var retrieved = await _context.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceNumber == "INV-TEST");

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.Amount, Is.EqualTo(100.00m));
        }

        [Test]
        public async Task Database_CanUpdateEntity()
        {
            // Arrange
            var customer = new Customer { Name = "Original Name", Email = "test@example.com" };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Act
            customer.Name = "Updated Name";
            await _context.SaveChangesAsync();

            var retrieved = await _context.Customers.FindAsync(customer.Id);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.Name, Is.EqualTo("Updated Name"));
        }

        [Test]
        public async Task Database_CanDeleteEntity()
        {
            // Arrange
            var customer = new Customer { Name = "To Delete", Email = "delete@example.com" };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            var customerId = customer.Id;

            // Act
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            var retrieved = await _context.Customers.FindAsync(customerId);

            // Assert
            Assert.That(retrieved, Is.Null);
        }

        #endregion

        #region Relationship Tests

        [Test]
        public async Task Relationships_CustomerHasInvoices()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Test Company",
                Email = "test@example.com",
                Invoices = new List<Invoice>
                {
                    new Invoice
                    {
                        InvoiceNumber = "INV-001",
                        InvoiceDate = DateTime.UtcNow,
                        Amount = 100.00m
                    },
                    new Invoice
                    {
                        InvoiceNumber = "INV-002",
                        InvoiceDate = DateTime.UtcNow,
                        Amount = 200.00m
                    }
                }
            };

            // Act
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var retrieved = await _context.Customers
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.Id == customer.Id);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.Invoices, Is.Not.Null);
            Assert.That(retrieved.Invoices.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Relationships_CustomerHasPhoneNumbers()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Test Company",
                Email = "test@example.com",
                PhoneNumbers = new List<TelephoneNumber>
                {
                    new TelephoneNumber
                    {
                        Type = "Mobile",
                        Number = "+44 7700 900123"
                    },
                    new TelephoneNumber
                    {
                        Type = "Work",
                        Number = "+44 20 7946 0958"
                    }
                }
            };

            // Act
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var retrieved = await _context.Customers
                .Include(c => c.PhoneNumbers)
                .FirstOrDefaultAsync(c => c.Id == customer.Id);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.PhoneNumbers, Is.Not.Null);
            Assert.That(retrieved.PhoneNumbers.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Relationships_InvoiceHasCustomerId()
        {
            // Arrange
            var customer = new Customer { Name = "Test", Email = "test@example.com" };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = customer.Id,
                InvoiceDate = DateTime.UtcNow,
                Amount = 100.00m
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Act
            var retrieved = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoice.Id);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.CustomerId, Is.EqualTo(customer.Id));
        }

        #endregion

        #region Cascade Delete Tests

        [Test]
        public async Task CascadeDelete_DeletingCustomer_DeletesInvoices()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Test",
                Email = "test@example.com",
                Invoices = new List<Invoice>
                {
                    new Invoice { InvoiceNumber = "INV-001", InvoiceDate = DateTime.UtcNow, Amount = 100 }
                }
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            var invoiceId = customer.Invoices[0].Id;

            // Act
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            var retrievedInvoice = await _context.Invoices.FindAsync(invoiceId);

            // Assert
            Assert.That(retrievedInvoice, Is.Null);
        }

        [Test]
        public async Task CascadeDelete_DeletingCustomer_DeletesPhoneNumbers()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Test",
                Email = "test@example.com",
                PhoneNumbers = new List<TelephoneNumber>
                {
                    new TelephoneNumber { Type = "Mobile", Number = "+44 7700 900123" }
                }
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            var phoneId = customer.PhoneNumbers[0].Id;

            // Act
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            var retrievedPhone = await _context.TelephoneNumbers.FindAsync(phoneId);

            // Assert
            Assert.That(retrievedPhone, Is.Null);
        }

        #endregion

        #region Query Tests

        [Test]
        public async Task Query_CanFilterCustomersByName()
        {
            // Arrange
            _context.Customers.AddRange(
                new Customer { Name = "Acme Corp", Email = "acme@example.com" },
                new Customer { Name = "Tech Solutions", Email = "tech@example.com" }
            );
            await _context.SaveChangesAsync();

            // Act
            var results = await _context.Customers
                .Where(c => c.Name.Contains("Acme"))
                .ToListAsync();

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Name, Is.EqualTo("Acme Corp"));
        }

        [Test]
        public async Task Query_CanOrderCustomersByName()
        {
            // Arrange
            _context.Customers.AddRange(
                new Customer { Name = "Zebra Corp", Email = "zebra@example.com" },
                new Customer { Name = "Alpha Corp", Email = "alpha@example.com" },
                new Customer { Name = "Beta Corp", Email = "beta@example.com" }
            );
            await _context.SaveChangesAsync();

            // Act
            var results = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Assert
            Assert.That(results[0].Name, Is.EqualTo("Alpha Corp"));
            Assert.That(results[1].Name, Is.EqualTo("Beta Corp"));
            Assert.That(results[2].Name, Is.EqualTo("Zebra Corp"));
        }

        [Test]
        public async Task Query_CanCountEntities()
        {
            // Arrange
            _context.Customers.AddRange(
                new Customer { Name = "Company 1", Email = "c1@example.com" },
                new Customer { Name = "Company 2", Email = "c2@example.com" },
                new Customer { Name = "Company 3", Email = "c3@example.com" }
            );
            await _context.SaveChangesAsync();

            // Act
            var count = await _context.Customers.CountAsync();

            // Assert
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public async Task Query_CanCheckExistence()
        {
            // Arrange
            _context.Customers.Add(new Customer { Name = "Test", Email = "test@example.com" });
            await _context.SaveChangesAsync();

            // Act
            var exists = await _context.Customers.AnyAsync(c => c.Email == "test@example.com");
            var notExists = await _context.Customers.AnyAsync(c => c.Email == "nonexistent@example.com");

            // Assert
            Assert.That(exists, Is.True);
            Assert.That(notExists, Is.False);
        }

        #endregion

        #region AsNoTracking Tests

        [Test]
        public async Task AsNoTracking_EntitiesAreNotTracked()
        {
            // Arrange
            _context.Customers.Add(new Customer { Name = "Test", Email = "test@example.com" });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear(); // Clear tracked entities from SaveChangesAsync

            // Act
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstAsync();

            var trackedEntities = _context.ChangeTracker.Entries().Count();

            // Assert
            Assert.That(trackedEntities, Is.EqualTo(0));
        }

        [Test]
        public async Task WithoutAsNoTracking_EntitiesAreTracked()
        {
            // Arrange
            _context.Customers.Add(new Customer { Name = "Test", Email = "test@example.com" });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var customer = await _context.Customers.FirstAsync();
            var trackedEntities = _context.ChangeTracker.Entries().Count();

            // Assert
            Assert.That(trackedEntities, Is.GreaterThan(0));
        }

        #endregion
    }
}
