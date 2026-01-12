using EfCoreLab.Data;
using EfCoreLab.Repositories;
using EfCoreLab.Tests.TestHelpers;

namespace EfCoreLab.Tests.Repositories
{
    [TestFixture]
    public class InvoiceRepositoryTests
    {
        private AppDbContext _context;
        private InvoiceRepository _repository;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.CreateSeededContext();
            _repository = new InvoiceRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region GetByIdAsync Tests

        [Test]
        public async Task GetByIdAsync_WithValidId_ReturnsInvoice()
        {
            // Arrange
            long invoiceId = 1;

            // Act
            var result = await _repository.GetByIdAsync(invoiceId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(invoiceId));
            Assert.That(result.InvoiceNumber, Is.EqualTo("INV-001"));
        }

        [Test]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            long invalidId = 999;

            // Act
            var result = await _repository.GetByIdAsync(invalidId);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region GetByInvoiceNumberAsync Tests

        [Test]
        public async Task GetByInvoiceNumberAsync_WithValidNumber_ReturnsInvoice()
        {
            // Arrange
            string invoiceNumber = "INV-001";

            // Act
            var result = await _repository.GetByInvoiceNumberAsync(invoiceNumber);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.InvoiceNumber, Is.EqualTo(invoiceNumber));
            Assert.That(result.Id, Is.EqualTo(1));
        }

        [Test]
        public async Task GetByInvoiceNumberAsync_WithInvalidNumber_ReturnsNull()
        {
            // Arrange
            string invalidNumber = "INV-999";

            // Act
            var result = await _repository.GetByInvoiceNumberAsync(invalidNumber);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region GetAllAsync Tests

        [Test]
        public async Task GetAllAsync_ReturnsAllInvoices()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
        }

        #endregion

        #region GetByCustomerIdAsync Tests

        [Test]
        public async Task GetByCustomerIdAsync_WithValidCustomerId_ReturnsCustomerInvoices()
        {
            // Arrange
            long customerId = 1;

            // Act
            var result = await _repository.GetByCustomerIdAsync(customerId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(i => i.CustomerId == customerId), Is.True);
        }

        [Test]
        public async Task GetByCustomerIdAsync_WithNoInvoices_ReturnsEmptyList()
        {
            // Arrange
            long customerIdWithNoInvoices = 3;

            // Act
            var result = await _repository.GetByCustomerIdAsync(customerIdWithNoInvoices);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region CreateAsync Tests

        [Test]
        public async Task CreateAsync_WithValidInvoice_ReturnsCreatedInvoice()
        {
            // Arrange
            var newInvoice = new Invoice
            {
                InvoiceNumber = "INV-NEW-001",
                CustomerId = 1,
                InvoiceDate = DateTime.UtcNow,
                Amount = 500.00m
            };

            // Act
            var result = await _repository.CreateAsync(newInvoice);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.InvoiceNumber, Is.EqualTo("INV-NEW-001"));
            Assert.That(result.Amount, Is.EqualTo(500.00m));
        }

        [Test]
        public async Task CreateAsync_SavesInvoiceToDatabase()
        {
            // Arrange
            var newInvoice = new Invoice
            {
                InvoiceNumber = "INV-TEST-001",
                CustomerId = 2,
                InvoiceDate = DateTime.UtcNow,
                Amount = 1500.00m
            };

            // Act
            var created = await _repository.CreateAsync(newInvoice);
            var retrieved = await _repository.GetByIdAsync(created.Id);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.InvoiceNumber, Is.EqualTo("INV-TEST-001"));
            Assert.That(retrieved.Amount, Is.EqualTo(1500.00m));
        }

        #endregion

        #region UpdateAsync Tests

        [Test]
        public async Task UpdateAsync_WithValidInvoice_UpdatesInvoice()
        {
            // Arrange
            var invoice = await _repository.GetByIdAsync(1);
            invoice.Amount = 999.99m;
            invoice.InvoiceNumber = "INV-UPDATED";

            // Act
            var result = await _repository.UpdateAsync(invoice);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Amount, Is.EqualTo(999.99m));
            Assert.That(result.InvoiceNumber, Is.EqualTo("INV-UPDATED"));
        }

        [Test]
        public async Task UpdateAsync_PersistsChangesToDatabase()
        {
            // Arrange
            var invoice = await _repository.GetByIdAsync(1);
            invoice.Amount = 2000.00m;

            // Act
            await _repository.UpdateAsync(invoice);
            var retrieved = await _repository.GetByIdAsync(1);

            // Assert
            Assert.That(retrieved.Amount, Is.EqualTo(2000.00m));
        }

        #endregion

        #region DeleteAsync Tests

        [Test]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            long invoiceId = 3;

            // Act
            var result = await _repository.DeleteAsync(invoiceId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task DeleteAsync_RemovesInvoiceFromDatabase()
        {
            // Arrange
            long invoiceId = 3;

            // Act
            await _repository.DeleteAsync(invoiceId);
            var retrieved = await _repository.GetByIdAsync(invoiceId);

            // Assert
            Assert.That(retrieved, Is.Null);
        }

        [Test]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            long invalidId = 999;

            // Act
            var result = await _repository.DeleteAsync(invalidId);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region ExistsAsync Tests

        [Test]
        public async Task ExistsAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange
            long existingId = 1;

            // Act
            var result = await _repository.ExistsAsync(existingId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ExistsAsync_WithNonExistingId_ReturnsFalse()
        {
            // Arrange
            long nonExistingId = 999;

            // Act
            var result = await _repository.ExistsAsync(nonExistingId);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region InvoiceNumberExistsAsync Tests

        [Test]
        public async Task InvoiceNumberExistsAsync_WithExistingNumber_ReturnsTrue()
        {
            // Arrange
            string existingNumber = "INV-001";

            // Act
            var result = await _repository.InvoiceNumberExistsAsync(existingNumber);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task InvoiceNumberExistsAsync_WithNonExistingNumber_ReturnsFalse()
        {
            // Arrange
            string nonExistingNumber = "INV-999";

            // Act
            var result = await _repository.InvoiceNumberExistsAsync(nonExistingNumber);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task InvoiceNumberExistsAsync_WithExcludedInvoiceId_ReturnsFalse()
        {
            // Arrange
            string invoiceNumber = "INV-001";
            long excludeId = 1;

            // Act
            var result = await _repository.InvoiceNumberExistsAsync(invoiceNumber, excludeId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task InvoiceNumberExistsAsync_WithDifferentInvoiceId_ReturnsTrue()
        {
            // Arrange
            string invoiceNumber = "INV-001";
            long excludeId = 2;

            // Act
            var result = await _repository.InvoiceNumberExistsAsync(invoiceNumber, excludeId);

            // Assert
            Assert.That(result, Is.True);
        }

        #endregion
    }
}
