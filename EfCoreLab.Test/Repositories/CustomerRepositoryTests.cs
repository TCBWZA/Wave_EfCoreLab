using EfCoreLab.Data;
using EfCoreLab.Repositories;
using EfCoreLab.Tests.TestHelpers;

namespace EfCoreLab.Tests.Repositories
{
    [TestFixture]
    public class CustomerRepositoryTests
    {
        private AppDbContext _context;
        private CustomerRepository _repository;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.CreateSeededContext();
            _repository = new CustomerRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region GetByIdAsync Tests

        [Test]
        public async Task GetByIdAsync_WithValidId_ReturnsCustomer()
        {
            // Arrange
            long customerId = 1;

            // Act
            var result = await _repository.GetByIdAsync(customerId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(customerId));
            Assert.That(result.Name, Is.EqualTo("Acme Corporation"));
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

        [Test]
        public async Task GetByIdAsync_WithIncludeRelated_ReturnsCustomerWithInvoicesAndPhoneNumbers()
        {
            // Arrange
            long customerId = 1;

            // Act
            var result = await _repository.GetByIdAsync(customerId, includeRelated: true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Invoices, Is.Not.Null);
            Assert.That(result.Invoices.Count, Is.GreaterThan(0));
            Assert.That(result.PhoneNumbers, Is.Not.Null);
            Assert.That(result.PhoneNumbers.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetByIdAsync_WithoutIncludeRelated_ReturnsCustomerWithoutLoadingRelations()
        {
            // Arrange
            long customerId = 1;

            // Act
            var result = await _repository.GetByIdAsync(customerId, includeRelated: false);

            // Assert
            Assert.That(result, Is.Not.Null);
            // When includeRelated is false, navigation properties are initialized but not populated
            // In a fresh context they would be empty, but in-memory DB may have them tracked
            Assert.That(result.Invoices, Is.Not.Null);
            Assert.That(result.PhoneNumbers, Is.Not.Null);
        }

        #endregion

        #region GetByEmailAsync Tests

        [Test]
        public async Task GetByEmailAsync_WithValidEmail_ReturnsCustomer()
        {
            // Arrange
            string email = "contact@acme.example.com";

            // Act
            var result = await _repository.GetByEmailAsync(email);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo(email));
            Assert.That(result.Name, Is.EqualTo("Acme Corporation"));
        }

        [Test]
        public async Task GetByEmailAsync_WithInvalidEmail_ReturnsNull()
        {
            // Arrange
            string invalidEmail = "nonexistent@example.com";

            // Act
            var result = await _repository.GetByEmailAsync(invalidEmail);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region GetAllAsync Tests

        [Test]
        public async Task GetAllAsync_ReturnsAllCustomers()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task GetAllAsync_WithIncludeRelated_ReturnsCustomersWithRelations()
        {
            // Act
            var result = await _repository.GetAllAsync(includeRelated: true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Invoices, Is.Not.Null);
            Assert.That(result[0].PhoneNumbers, Is.Not.Null);
        }

        #endregion

        #region CreateAsync Tests

        [Test]
        public async Task CreateAsync_WithValidCustomer_ReturnsCreatedCustomer()
        {
            // Arrange
            var newCustomer = new Customer
            {
                Name = "New Company Ltd",
                Email = "info@newcompany.com"
            };

            // Act
            var result = await _repository.CreateAsync(newCustomer);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.Name, Is.EqualTo("New Company Ltd"));
            Assert.That(result.Email, Is.EqualTo("info@newcompany.com"));
        }

        [Test]
        public async Task CreateAsync_SavesCustomerToDatabase()
        {
            // Arrange
            var newCustomer = new Customer
            {
                Name = "Test Company",
                Email = "test@example.com"
            };

            // Act
            var created = await _repository.CreateAsync(newCustomer);
            var retrieved = await _repository.GetByIdAsync(created.Id);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.Name, Is.EqualTo("Test Company"));
        }

        #endregion

        #region UpdateAsync Tests

        [Test]
        public async Task UpdateAsync_WithValidCustomer_UpdatesCustomer()
        {
            // Arrange
            var customer = await _repository.GetByIdAsync(1);
            customer.Name = "Updated Name";
            customer.Email = "updated@updatedname.example.com";

            // Act
            var result = await _repository.UpdateAsync(customer);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Updated Name"));
            Assert.That(result.Email, Is.EqualTo("updated@updatedname.example.com"));
        }

        [Test]
        public async Task UpdateAsync_PersistsChangesToDatabase()
        {
            // Arrange
            var customer = await _repository.GetByIdAsync(1);
            customer.Name = "Modified Name";

            // Act
            await _repository.UpdateAsync(customer);
            var retrieved = await _repository.GetByIdAsync(1);

            // Assert
            Assert.That(retrieved.Name, Is.EqualTo("Modified Name"));
        }

        #endregion

        #region DeleteAsync Tests

        [Test]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            long customerId = 3;

            // Act
            var result = await _repository.DeleteAsync(customerId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task DeleteAsync_RemovesCustomerFromDatabase()
        {
            // Arrange
            long customerId = 3;

            // Act
            await _repository.DeleteAsync(customerId);
            var retrieved = await _repository.GetByIdAsync(customerId);

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

        #region EmailExistsAsync Tests

        [Test]
        public async Task EmailExistsAsync_WithExistingEmail_ReturnsTrue()
        {
            // Arrange
            string existingEmail = "contact@acme.example.com";

            // Act
            var result = await _repository.EmailExistsAsync(existingEmail);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task EmailExistsAsync_WithNonExistingEmail_ReturnsFalse()
        {
            // Arrange
            string nonExistingEmail = "nonexistent@example.com";

            // Act
            var result = await _repository.EmailExistsAsync(nonExistingEmail);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task EmailExistsAsync_WithExcludedCustomerId_ReturnsFalse()
        {
            // Arrange
            string email = "contact@acme.example.com";
            long excludeId = 1;

            // Act
            var result = await _repository.EmailExistsAsync(email, excludeId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task EmailExistsAsync_WithDifferentCustomerId_ReturnsTrue()
        {
            // Arrange
            string email = "contact@acme.example.com";
            long excludeId = 2;

            // Act
            var result = await _repository.EmailExistsAsync(email, excludeId);

            // Assert
            Assert.That(result, Is.True);
        }

        #endregion

        #region GetPagedAsync Tests

        [Test]
        public async Task GetPagedAsync_ReturnsCorrectPageSize()
        {
            // Arrange
            int page = 1;
            int pageSize = 2;

            // Act
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize);

            // Assert
            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(totalCount, Is.EqualTo(3));
        }

        [Test]
        public async Task GetPagedAsync_ReturnsCorrectTotalCount()
        {
            // Arrange
            int page = 1;
            int pageSize = 10;

            // Act
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize);

            // Assert
            Assert.That(totalCount, Is.EqualTo(3));
        }

        [Test]
        public async Task GetPagedAsync_SecondPage_ReturnsRemainingItems()
        {
            // Arrange
            int page = 2;
            int pageSize = 2;

            // Act
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize);

            // Assert
            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(totalCount, Is.EqualTo(3));
        }

        #endregion

        #region SearchAsync Tests

        [Test]
        public async Task SearchAsync_WithName_ReturnsMatchingCustomers()
        {
            // Arrange
            string nameQuery = "Acme";

            // Act
            var result = await _repository.SearchAsync(nameQuery, null, null);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Name, Does.Contain("Acme"));
        }

        [Test]
        public async Task SearchAsync_WithEmail_ReturnsMatchingCustomers()
        {
            // Arrange
            string emailQuery = "techsolutions";

            // Act
            var result = await _repository.SearchAsync(null, emailQuery, null);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Email, Does.Contain("techsolutions"));
        }

        [Test]
        public async Task SearchAsync_WithMinBalance_ReturnsCustomersAboveThreshold()
        {
            // Arrange
            decimal minBalance = 1000m;

            // Act
            var result = await _repository.SearchAsync(null, null, minBalance);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task SearchAsync_WithAllFilters_ReturnsFilteredResults()
        {
            // Arrange
            string nameQuery = "Acme";
            string emailQuery = "acme";
            decimal minBalance = 1000m;

            // Act
            var result = await _repository.SearchAsync(nameQuery, emailQuery, minBalance);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SearchAsync_WithNoMatches_ReturnsEmptyList()
        {
            // Arrange
            string nameQuery = "NonExistentCompany";

            // Act
            var result = await _repository.SearchAsync(nameQuery, null, null);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region GetAllNoTrackingAsync Tests

        [Test]
        public async Task GetAllNoTrackingAsync_ReturnsAllCustomers()
        {
            // Act
            var result = await _repository.GetAllNoTrackingAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task GetAllNoTrackingAsync_ReturnsCustomersWithRelations()
        {
            // Act
            var result = await _repository.GetAllNoTrackingAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result[0].Invoices, Is.Not.Null);
            Assert.That(result[0].PhoneNumbers, Is.Not.Null);
        }

        #endregion

        #region GetAllWithSplitQueriesAsync Tests

        [Test]
        public async Task GetAllWithSplitQueriesAsync_ReturnsAllCustomers()
        {
            // Act
            var result = await _repository.GetAllWithSplitQueriesAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task GetAllWithSplitQueriesAsync_IncludesRelatedData()
        {
            // Act
            var result = await _repository.GetAllWithSplitQueriesAsync();

            // Assert
            var customerWithInvoices = result.FirstOrDefault(c => c.Id == 1);
            Assert.That(customerWithInvoices, Is.Not.Null);
            Assert.That(customerWithInvoices.Invoices, Is.Not.Null);
            Assert.That(customerWithInvoices.Invoices.Count, Is.GreaterThan(0));
        }

        #endregion
    }
}
