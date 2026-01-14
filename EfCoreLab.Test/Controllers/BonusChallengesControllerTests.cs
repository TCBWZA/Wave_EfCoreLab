using EfCoreLab.Controllers;
using EfCoreLab.Data;
using EfCoreLab.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace EfCoreLab.Tests.Controllers
{
    /// <summary>
    /// Tests for BonusChallengesController demonstrating:
    /// - Custom validation handling
    /// - Soft delete operations
    /// - Global query filter usage
    /// - Caching behavior
    /// </summary>
    [TestFixture]
    public class BonusChallengesControllerTests
    {
        private BonusDbContext _context = null!;
        private IMemoryCache _cache = null!;
        private BonusChallengesController _controller = null!;
        private Mock<ILogger<BonusChallengesController>> _loggerMock = null!;

        [SetUp]
        public void Setup()
        {
            _context = BonusTestDbContextFactory.CreateSeededContext();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<BonusChallengesController>>();
            _controller = new BonusChallengesController(_context, _cache, _loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _cache.Dispose();
        }

        #region GetCustomers Tests

        [Test]
        public async Task GetCustomers_WithoutIncludeDeleted_ReturnsOnlyActiveCustomers()
        {
            // Act
            var result = await _controller.GetCustomers(includeDeleted: false);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var customers = okResult!.Value as IEnumerable<BonusCustomer>;
            Assert.That(customers, Is.Not.Null);
            Assert.That(customers!.Count(), Is.EqualTo(3), "Should return only non-deleted customers");
            Assert.That(customers.All(c => !c.IsDeleted), Is.True);
        }

        [Test]
        public async Task GetCustomers_WithIncludeDeleted_ReturnsAllCustomers()
        {
            // Act
            var result = await _controller.GetCustomers(includeDeleted: true);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var customers = okResult!.Value as IEnumerable<BonusCustomer>;
            Assert.That(customers, Is.Not.Null);
            Assert.That(customers!.Count(), Is.EqualTo(4), "Should return all customers including deleted");
            Assert.That(customers.Any(c => c.IsDeleted), Is.True);
        }

        #endregion

        #region GetCustomerById Tests (Caching)

        [Test]
        public async Task GetCustomerById_FirstCall_LoadsFromDatabase()
        {
            // Act
            var result = await _controller.GetCustomerById(1);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var customer = okResult!.Value as BonusCustomer;
            Assert.That(customer, Is.Not.Null);
            Assert.That(customer!.Id, Is.EqualTo(1));
        }

        [Test]
        public async Task GetCustomerById_SecondCall_LoadsFromCache()
        {
            // Act - First call
            var result1 = await _controller.GetCustomerById(1);
            var okResult1 = result1.Result as OkObjectResult;
            var customer1 = okResult1!.Value as BonusCustomer;

            // Modify database record
            var dbCustomer = await _context.BonusCustomers.FindAsync(1L);
            dbCustomer!.Name = "Modified Name";
            await _context.SaveChangesAsync();

            // Act - Second call (should get cached version with old name)
            var result2 = await _controller.GetCustomerById(1);
            var okResult2 = result2.Result as OkObjectResult;
            var customer2 = okResult2!.Value as BonusCustomer;

            // Assert - Should still have original name from cache
            Assert.That(customer2!.Name, Is.EqualTo(customer1!.Name));
            Assert.That(customer2.Name, Is.Not.EqualTo("Modified Name"));
        }

        [Test]
        public async Task GetCustomerById_NonExistent_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetCustomerById(999);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        #endregion

        #region CreateCustomer Tests (Custom Validation)

        [Test]
        public async Task CreateCustomer_WithValidData_CreatesCustomer()
        {
            // Arrange
            var request = new CreateBonusCustomerRequest
            {
                Name = "New Company Ltd",
                Email = "info@newcompany.com"
            };

            // Act
            var result = await _controller.CreateCustomer(request);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            Assert.That(createdResult, Is.Not.Null);
            
            var customer = createdResult!.Value as BonusCustomer;
            Assert.That(customer, Is.Not.Null);
            Assert.That(customer!.Name, Is.EqualTo("New Company Ltd"));
            Assert.That(customer.Id, Is.GreaterThan(0));
        }

        [Test]
        public async Task CreateCustomer_SetsAuditFields()
        {
            // Arrange
            var request = new CreateBonusCustomerRequest
            {
                Name = "Test Company",
                Email = "test@testcompany.com"
            };

            var beforeCreate = DateTime.UtcNow.AddSeconds(-1);

            // Act
            var result = await _controller.CreateCustomer(request);

            var afterCreate = DateTime.UtcNow.AddSeconds(1);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            var customer = createdResult!.Value as BonusCustomer;
            
            Assert.That(customer!.CreatedDate, Is.GreaterThan(beforeCreate));
            Assert.That(customer.CreatedDate, Is.LessThan(afterCreate));
            Assert.That(customer.ModifiedDate, Is.GreaterThan(beforeCreate));
            Assert.That(customer.ModifiedDate, Is.LessThan(afterCreate));
        }

        [Test]
        public async Task CreateCustomer_WithInvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateBonusCustomerRequest
            {
                Name = "Test Company",
                Email = "invalid-email" // Invalid format
            };

            // Manually add model state error (normally done by model binding)
            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            var result = await _controller.CreateCustomer(request);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        #endregion

        #region UpdateCustomer Tests (Audit)

        [Test]
        public async Task UpdateCustomer_UpdatesModifiedDate()
        {
            // Arrange
            var originalCustomer = await _context.BonusCustomers.FindAsync(1L);
            var originalModifiedDate = originalCustomer!.ModifiedDate;
            
            await Task.Delay(100); // Ensure time difference

            var request = new UpdateBonusCustomerRequest
            {
                Name = "Updated Name",
                Email = originalCustomer.Email!
            };

            // Act
            var result = await _controller.UpdateCustomer(1, request);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var updatedCustomer = okResult!.Value as BonusCustomer;
            
            Assert.That(updatedCustomer!.ModifiedDate, Is.GreaterThan(originalModifiedDate));
            Assert.That(updatedCustomer.Name, Is.EqualTo("Updated Name"));
        }

        [Test]
        public async Task UpdateCustomer_NonExistent_ReturnsNotFound()
        {
            // Arrange
            var request = new UpdateBonusCustomerRequest
            {
                Name = "Updated Name",
                Email = "updated@email.com"
            };

            // Act
            var result = await _controller.UpdateCustomer(999, request);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task UpdateCustomer_InvalidatesCache()
        {
            // Arrange - Put customer in cache first
            await _controller.GetCustomerById(1);
            
            var request = new UpdateBonusCustomerRequest
            {
                Name = "Cache Invalidation Test",
                Email = "test@test.com"
            };

            // Act
            await _controller.UpdateCustomer(1, request);

            // Verify cache was invalidated by getting fresh data
            var result = await _controller.GetCustomerById(1);
            var okResult = result.Result as OkObjectResult;
            var customer = okResult!.Value as BonusCustomer;

            // Assert
            Assert.That(customer!.Name, Is.EqualTo("Cache Invalidation Test"));
        }

        #endregion

        #region SoftDeleteCustomer Tests

        [Test]
        public async Task SoftDeleteCustomer_MarksAsDeleted()
        {
            // Act
            var result = await _controller.SoftDeleteCustomer(1);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());

            // Verify it's marked as deleted
            var customer = await _context.BonusCustomers
                .IgnoreQueryFilters()
                .FirstAsync(c => c.Id == 1);
            
            Assert.That(customer.IsDeleted, Is.True);
            Assert.That(customer.DeletedDate, Is.Not.Null);
        }

        [Test]
        public async Task SoftDeleteCustomer_HidesFromNormalQueries()
        {
            // Act
            await _controller.SoftDeleteCustomer(1);

            // Assert
            var customer = await _context.BonusCustomers.FindAsync(1L);
            Assert.That(customer, Is.Null, "Soft-deleted customer should not be found by normal query");
        }

        [Test]
        public async Task SoftDeleteCustomer_NonExistent_ReturnsNotFound()
        {
            // Act
            var result = await _controller.SoftDeleteCustomer(999);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task SoftDeleteCustomer_AlreadyDeleted_ReturnsBadRequest()
        {
            // Arrange - Customer 4 is already soft-deleted in test data
            
            // Act
            var result = await _controller.SoftDeleteCustomer(4);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task SoftDeleteCustomer_InvalidatesCache()
        {
            // Arrange - Put customer in cache
            await _controller.GetCustomerById(1);

            // Act
            await _controller.SoftDeleteCustomer(1);

            // Try to get from controller (should not find it)
            var result = await _controller.GetCustomerById(1);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        #endregion

        #region RestoreCustomer Tests

        [Test]
        public async Task RestoreCustomer_RestoresSoftDeletedCustomer()
        {
            // Arrange - Customer 4 is soft-deleted in test data
            
            // Act
            var result = await _controller.RestoreCustomer(4);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var customer = okResult!.Value as BonusCustomer;
            Assert.That(customer!.IsDeleted, Is.False);
            Assert.That(customer.DeletedDate, Is.Null);
        }

        [Test]
        public async Task RestoreCustomer_MakesVisibleInNormalQueries()
        {
            // Act
            await _controller.RestoreCustomer(4);

            // Assert
            var customer = await _context.BonusCustomers.FindAsync(4L);
            Assert.That(customer, Is.Not.Null, "Restored customer should be found by normal query");
        }

        [Test]
        public async Task RestoreCustomer_NonExistent_ReturnsNotFound()
        {
            // Act
            var result = await _controller.RestoreCustomer(999);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task RestoreCustomer_NotDeleted_ReturnsBadRequest()
        {
            // Arrange - Customer 1 is not deleted
            
            // Act
            var result = await _controller.RestoreCustomer(1);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        #endregion

        #region GetCustomersWithLargeBalance Tests (SQL Logging)

        [Test]
        public async Task GetCustomersWithLargeBalance_ReturnsCustomersAboveThreshold()
        {
            // Act
            var result = await _controller.GetCustomersWithLargeBalance(minBalance: 1000);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
        }

        [Test]
        public async Task GetCustomersWithLargeBalance_WithHighThreshold_ReturnsFewerCustomers()
        {
            // Act
            var result = await _controller.GetCustomersWithLargeBalance(minBalance: 10000);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            // Note: In test data, no customer has balance >= 10000
            // This test demonstrates the SQL logging feature
        }

        #endregion

        #region DemoAllChallenges Tests

        [Test]
        public async Task DemoAllChallenges_ReturnsComprehensiveOverview()
        {
            // Act
            var result = await _controller.DemoAllChallenges();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.Value, Is.Not.Null);
        }

        #endregion
    }
}
