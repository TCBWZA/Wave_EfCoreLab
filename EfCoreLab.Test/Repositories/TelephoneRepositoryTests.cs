using EfCoreLab.Data;
using EfCoreLab.Repositories;
using EfCoreLab.Tests.TestHelpers;

namespace EfCoreLab.Tests.Repositories
{
    [TestFixture]
    public class TelephoneRepositoryTests
    {
        private AppDbContext _context;
        private TelephoneNumberRepository _repository;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.CreateSeededContext();
            _repository = new TelephoneNumberRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region GetByIdAsync Tests

        [Test]
        public async Task GetByIdAsync_WithValidId_ReturnsTelephoneNumber()
        {
            // Arrange
            long PhoneNumberId = 1;

            // Act
            var result = await _repository.GetByIdAsync(PhoneNumberId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(PhoneNumberId));
            Assert.That(result.Number, Is.Not.Null);
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

        #region GetAllAsync Tests

        [Test]
        public async Task GetAllAsync_ReturnsAllTelephoneNumbers()
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
        public async Task GetByCustomerIdAsync_WithValidCustomerId_ReturnsCustomerTelephoneNumbers()
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
        public async Task GetByCustomerIdAsync_WithNoTelephoneNumbers_ReturnsEmptyList()
        {
            // Arrange
            long customerIdWithNoTelephoneNumbers = 3;

            // Act
            var result = await _repository.GetByCustomerIdAsync(customerIdWithNoTelephoneNumbers);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region CreateAsync Tests

        [Test]
        public async Task CreateAsync_WithValidTelephoneNumber_ReturnsCreatedTelephoneNumber()
        {
            // Arrange
            var phoneNumber = new TelephoneNumber
            {
                Number = "555 1234",
                CustomerId = 2,
                Type = "Mobile"

            };

            // Act
            var result = await _repository.CreateAsync(phoneNumber);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.Number, Is.EqualTo("555 1234"));
            Assert.That(result.CustomerId, Is.EqualTo(2));
        }

        [Test]
        public async Task CreateAsync_SavesTelephoneNumberToDatabase()
        {
            // Arrange
            var newPhoneNumber = new TelephoneNumber
            {
                Number = "555 1234",
                CustomerId = 1,
                Type = "Mobile"
            };

            // Act
            var created = await _repository.CreateAsync(newPhoneNumber);
            var retrieved = await _repository.GetByIdAsync(created.Id);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.Number, Is.EqualTo("555 1234"));
            Assert.That(retrieved.Type, Is.EqualTo("Mobile"));
        }

        #endregion

        #region UpdateAsync Tests

        [Test]
        public async Task UpdateAsync_WithValidTelephoneNumber_UpdatesTelephoneNumber()
        {
            // Arrange
            var telephone = await _repository.GetByIdAsync(1);
            telephone.Type = "Mobile";
            telephone.Number = "9999999";

            // Act
            var result = await _repository.UpdateAsync(telephone);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Type, Is.EqualTo("Mobile"));
            Assert.That(result.Number, Is.EqualTo("9999999"));
        }

        [Test]
        public async Task UpdateAsync_PersistsChangesToDatabase()
        {
            // Arrange
            var telePhone = await _repository.GetByIdAsync(1);
            telePhone.Number = "8888888888";

            // Act
            await _repository.UpdateAsync(telePhone);
            var retrieved = await _repository.GetByIdAsync(1);

            // Assert
            Assert.That(retrieved.Number, Is.EqualTo("8888888888"));
        }

        #endregion

        #region DeleteAsync Tests

        [Test]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            long telePhoneId = 3;

            // Act
            var result = await _repository.DeleteAsync(telePhoneId);

            // Assert
            Assert.That(result, Is.True);
        //}

        //[Test]
        //public async Task DeleteAsync_RemovesTelephoneNumberFromDatabase()
        //{
        //    // Arrange
        //    long telePhoneId = 3;

        //    // Act
        //    await _repository.DeleteAsync(telePhoneId);
            var retrieved = await _repository.GetByIdAsync(telePhoneId);

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

        #region TelephoneNumberNumberExistsAsync Tests

        //[Test]
        //public async Task TelephoneNumberNumberExistsAsync_WithExistingNumber_ReturnsTrue()
        //{
        //    // Arrange
        //    string existingNumber = "INV-001";

        //    // Act
        //    var result = await _repository.TelephoneNumberNumberExistsAsync(existingNumber);

        //    // Assert
        //    Assert.That(result, Is.True);
        //}

        //[Test]
        //public async Task TelephoneNumberNumberExistsAsync_WithNonExistingNumber_ReturnsFalse()
        //{
        //    // Arrange
        //    string nonExistingNumber = "INV-999";

        //    // Act
        //    var result = await _repository.TelephoneNumberNumberExistsAsync(nonExistingNumber);

        //    // Assert
        //    Assert.That(result, Is.False);
        //}

        //[Test]
        //public async Task TelephoneNumberNumberExistsAsync_WithExcludedTelephoneNumberId_ReturnsFalse()
        //{
        //    // Arrange
        //    string TelephoneNumberNumber = "INV-001";
        //    long excludeId = 1;

        //    // Act
        //    var result = await _repository.TelephoneNumberNumberExistsAsync(TelephoneNumberNumber, excludeId);

        //    // Assert
        //    Assert.That(result, Is.False);
        //}

        //[Test]
        //public async Task TelephoneNumberNumberExistsAsync_WithDifferentTelephoneNumberId_ReturnsTrue()
        //{
        //    // Arrange
        //    string TelephoneNumberNumber = "INV-001";
        //    long excludeId = 2;

        //    // Act
        //    var result = await _repository.TelephoneNumberNumberExistsAsync(TelephoneNumberNumber, excludeId);

        //    // Assert
        //    Assert.That(result, Is.True);
        //}

        #endregion
    }
}
