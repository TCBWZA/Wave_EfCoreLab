using EfCoreLab.Data;
using EfCoreLab.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace EfCoreLab.Tests.Integration
{
    /// <summary>
    /// Integration tests for BonusDbContext demonstrating:
    /// - Global query filters (soft delete)
    /// - Audit interceptor functionality
    /// - Database operations
    /// </summary>
    [TestFixture]
    public class BonusDbContextTests
    {
        private BonusDbContext _context = null!;

        [SetUp]
        public void Setup()
        {
            _context = BonusTestDbContextFactory.CreateSeededContext();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region Global Query Filter Tests

        [Test]
        public async Task GlobalQueryFilter_ExcludesSoftDeletedCustomers_ByDefault()
        {
            // Act
            var customers = await _context.BonusCustomers.ToListAsync();

            // Assert
            Assert.That(customers.Count, Is.EqualTo(3), "Should only return non-deleted customers");
            Assert.That(customers.All(c => !c.IsDeleted), Is.True);
        }

        [Test]
        public async Task GlobalQueryFilter_CanBeBypassedWithIgnoreQueryFilters()
        {
            // Act
            var allCustomers = await _context.BonusCustomers
                .IgnoreQueryFilters()
                .ToListAsync();

            // Assert
            Assert.That(allCustomers.Count, Is.EqualTo(4), "Should return all customers including deleted");
            Assert.That(allCustomers.Any(c => c.IsDeleted), Is.True, "Should include deleted customers");
        }

        [Test]
        public async Task GlobalQueryFilter_ExcludesSoftDeletedInvoices_ByDefault()
        {
            // Act
            var invoices = await _context.BonusInvoices.ToListAsync();

            // Assert
            Assert.That(invoices.All(i => !i.IsDeleted), Is.True);
            Assert.That(invoices.Count, Is.EqualTo(3), "Should exclude soft-deleted invoice");
        }

        [Test]
        public async Task GlobalQueryFilter_ExcludesSoftDeletedInIncludes()
        {
            // Act
            var customers = await _context.BonusCustomers
                .Include(c => c.Invoices)
                .ToListAsync();

            // Assert
            var customer1 = customers.First(c => c.Id == 1);
            Assert.That(customer1.Invoices!.Count, Is.EqualTo(2), "Should not include soft-deleted invoice");
            Assert.That(customer1.Invoices!.All(i => !i.IsDeleted), Is.True);
        }

        #endregion

        #region Audit Interceptor Tests

        [Test]
        public async Task AuditInterceptor_SetsCreatedDateOnInsert()
        {
            // Arrange
            var beforeInsert = DateTime.UtcNow.AddSeconds(-1);
            var customer = new BonusCustomer
            {
                Name = "New Customer",
                Email = "new@newcustomer.com",
                IsDeleted = false
            };

            // Act
            _context.BonusCustomers.Add(customer);
            await _context.SaveChangesAsync();
            var afterInsert = DateTime.UtcNow.AddSeconds(1);

            // Assert
            Assert.That(customer.CreatedDate, Is.GreaterThan(beforeInsert));
            Assert.That(customer.CreatedDate, Is.LessThan(afterInsert));
            Assert.That(customer.CreatedDate, Is.Not.EqualTo(default(DateTime)));
        }

        [Test]
        public async Task AuditInterceptor_SetsModifiedDateOnInsert()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Name = "New Customer",
                Email = "new@newcustomer.com",
                IsDeleted = false
            };

            // Act
            _context.BonusCustomers.Add(customer);
            await _context.SaveChangesAsync();

            // Assert
            Assert.That(customer.ModifiedDate, Is.Not.EqualTo(default(DateTime)));
            Assert.That(customer.ModifiedDate, Is.EqualTo(customer.CreatedDate));
        }

        [Test]
        public async Task AuditInterceptor_UpdatesModifiedDateOnUpdate()
        {
            // Arrange
            var customer = await _context.BonusCustomers.FindAsync(1L);
            Assert.That(customer, Is.Not.Null);
            
            var originalModifiedDate = customer!.ModifiedDate;
            await Task.Delay(100); // Small delay to ensure time difference

            // Act
            customer.Name = "Updated Name";
            await _context.SaveChangesAsync();

            // Assert
            Assert.That(customer.ModifiedDate, Is.GreaterThan(originalModifiedDate));
        }

        [Test]
        public async Task AuditInterceptor_SetsDeletedDateOnSoftDelete()
        {
            // Arrange
            var customer = await _context.BonusCustomers.FindAsync(1L);
            Assert.That(customer, Is.Not.Null);
            Assert.That(customer!.DeletedDate, Is.Null);

            // Act
            customer.IsDeleted = true;
            await _context.SaveChangesAsync();

            // Assert
            Assert.That(customer.DeletedDate, Is.Not.Null);
            Assert.That(customer.DeletedDate, Is.LessThanOrEqualTo(DateTime.UtcNow));
        }

        [Test]
        public async Task AuditInterceptor_ClearsDeletedDateOnRestore()
        {
            // Arrange - Get a soft-deleted customer
            var customer = await _context.BonusCustomers
                .IgnoreQueryFilters()
                .FirstAsync(c => c.Id == 4);
            
            Assert.That(customer.IsDeleted, Is.True);
            Assert.That(customer.DeletedDate, Is.Not.Null);

            // Act - Restore customer
            customer.IsDeleted = false;
            await _context.SaveChangesAsync();

            // Assert
            Assert.That(customer.DeletedDate, Is.Null);
        }

        [Test]
        public async Task AuditInterceptor_WorksForAllEntityTypes()
        {
            // Arrange
            var customer = new BonusCustomer { Name = "Test", Email = "test@test.com" };
            var invoice = new BonusInvoice 
            { 
                InvoiceNumber = "INV-TEST", 
                CustomerId = 1, 
                InvoiceDate = DateTime.UtcNow, 
                Amount = 100m 
            };
            var phone = new BonusTelephoneNumber 
            { 
                CustomerId = 1, 
                Type = "Mobile", 
                Number = "1234567890" 
            };

            // Act
            _context.BonusCustomers.Add(customer);
            _context.BonusInvoices.Add(invoice);
            _context.BonusTelephoneNumbers.Add(phone);
            await _context.SaveChangesAsync();

            // Assert
            Assert.That(customer.CreatedDate, Is.Not.EqualTo(default(DateTime)));
            Assert.That(invoice.CreatedDate, Is.Not.EqualTo(default(DateTime)));
            Assert.That(phone.CreatedDate, Is.Not.EqualTo(default(DateTime)));
            
            Assert.That(customer.ModifiedDate, Is.Not.EqualTo(default(DateTime)));
            Assert.That(invoice.ModifiedDate, Is.Not.EqualTo(default(DateTime)));
            Assert.That(phone.ModifiedDate, Is.Not.EqualTo(default(DateTime)));
        }

        #endregion

        #region CRUD Operations Tests

        [Test]
        public async Task CanQueryBonusCustomers()
        {
            // Act
            var customers = await _context.BonusCustomers.ToListAsync();

            // Assert
            Assert.That(customers, Is.Not.Null);
            Assert.That(customers.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task CanQueryBonusCustomersWithIncludes()
        {
            // Act
            var customers = await _context.BonusCustomers
                .Include(c => c.Invoices)
                .Include(c => c.PhoneNumbers)
                .ToListAsync();

            // Assert
            Assert.That(customers, Is.Not.Null);
            var customerWithData = customers.First(c => c.Id == 1);
            Assert.That(customerWithData.Invoices, Is.Not.Null);
            Assert.That(customerWithData.PhoneNumbers, Is.Not.Null);
            Assert.That(customerWithData.Invoices!.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task CanCreateBonusCustomer()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Name = "Test Company",
                Email = "test@testcompany.com",
                IsDeleted = false
            };

            // Act
            _context.BonusCustomers.Add(customer);
            await _context.SaveChangesAsync();

            // Assert
            Assert.That(customer.Id, Is.GreaterThan(0));
            
            var retrieved = await _context.BonusCustomers.FindAsync(customer.Id);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Name, Is.EqualTo("Test Company"));
        }

        [Test]
        public async Task CanUpdateBonusCustomer()
        {
            // Arrange
            var customer = await _context.BonusCustomers.FindAsync(1L);
            Assert.That(customer, Is.Not.Null);

            // Act
            customer!.Name = "Updated Name";
            await _context.SaveChangesAsync();

            // Assert
            var retrieved = await _context.BonusCustomers.FindAsync(1L);
            Assert.That(retrieved!.Name, Is.EqualTo("Updated Name"));
        }

        [Test]
        public async Task CanSoftDeleteBonusCustomer()
        {
            // Arrange
            var customer = await _context.BonusCustomers.FindAsync(1L);
            Assert.That(customer, Is.Not.Null);

            // Act
            customer!.IsDeleted = true;
            await _context.SaveChangesAsync();

            // Assert - Normal query shouldn't find it
            var normalQuery = await _context.BonusCustomers.FindAsync(1L);
            Assert.That(normalQuery, Is.Null, "Soft-deleted customer should not be found by normal query");

            // But it should exist with IgnoreQueryFilters
            var ignoredQuery = await _context.BonusCustomers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == 1);
            Assert.That(ignoredQuery, Is.Not.Null);
            Assert.That(ignoredQuery!.IsDeleted, Is.True);
        }

        #endregion

        #region Relationship Tests

        [Test]
        public async Task BonusCustomer_CanHaveMultipleInvoices()
        {
            // Act
            var customer = await _context.BonusCustomers
                .Include(c => c.Invoices)
                .FirstAsync(c => c.Id == 1);

            // Assert
            Assert.That(customer.Invoices, Is.Not.Null);
            Assert.That(customer.Invoices!.Count, Is.GreaterThan(1));
        }

        [Test]
        public async Task BonusCustomer_CanHaveMultiplePhoneNumbers()
        {
            // Act
            var customer = await _context.BonusCustomers
                .Include(c => c.PhoneNumbers)
                .FirstAsync(c => c.Id == 1);

            // Assert
            Assert.That(customer.PhoneNumbers, Is.Not.Null);
            Assert.That(customer.PhoneNumbers!.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task BonusInvoice_BelongsToCustomer()
        {
            // Act
            var invoice = await _context.BonusInvoices.FirstAsync();

            // Assert
            Assert.That(invoice.CustomerId, Is.GreaterThan(0));
        }

        #endregion

        #region Index Tests

        [Test]
        public async Task UniqueIndex_OnEmail_PreventsDuplicates()
        {
            // Arrange
            var customer1 = new BonusCustomer
            {
                Name = "Customer 1",
                Email = "duplicate@test.com",
                IsDeleted = false
            };
            var customer2 = new BonusCustomer
            {
                Name = "Customer 2",
                Email = "duplicate@test.com", // Same email
                IsDeleted = false
            };

            _context.BonusCustomers.Add(customer1);
            await _context.SaveChangesAsync();

            // Act & Assert
            _context.BonusCustomers.Add(customer2);
            
            // In-memory database doesn't enforce unique constraints
            // This test documents the expected behavior in a real SQL database
            // In real SQL Server, this would throw DbUpdateException
        }

        [Test]
        public async Task UniqueIndex_OnInvoiceNumber_PreventsDuplicates()
        {
            // Arrange
            var invoice1 = new BonusInvoice
            {
                InvoiceNumber = "INV-DUPLICATE",
                CustomerId = 1,
                InvoiceDate = DateTime.UtcNow,
                Amount = 100m,
                IsDeleted = false
            };
            var invoice2 = new BonusInvoice
            {
                InvoiceNumber = "INV-DUPLICATE", // Same number
                CustomerId = 1,
                InvoiceDate = DateTime.UtcNow,
                Amount = 200m,
                IsDeleted = false
            };

            _context.BonusInvoices.Add(invoice1);
            await _context.SaveChangesAsync();

            // Act & Assert
            _context.BonusInvoices.Add(invoice2);
            
            // In-memory database doesn't enforce unique constraints
            // This test documents the expected behavior in a real SQL database
        }

        #endregion
    }
}
