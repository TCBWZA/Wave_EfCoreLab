using EfCoreLab.Data;
using System.ComponentModel.DataAnnotations;

namespace EfCoreLab.Tests.Models
{
    /// <summary>
    /// Tests for BonusCustomer entity including custom validation (IValidatableObject).
    /// 
    /// KNOWN ISSUES IN THESE TESTS:
    /// 1. Validate_CreatedDateInFuture_FailsValidation: Creates cascade validation error because
    ///    ModifiedDate ends up before CreatedDate, triggering multiple validation errors
    /// 2. Tests rely on DateTime.UtcNow which can cause timing issues in CI/CD pipelines
    /// 3. Email domain validation logic requires the email domain to contain a word from the company name
    ///    (minimum 4 characters), or be from test.com/example.com domains
    /// </summary>
    [TestFixture]
    public class BonusCustomerTests
    {
        [Test]
        public void BonusCustomer_InitializesWithEmptyCollections()
        {
            // Act
            var customer = new BonusCustomer();

            // Assert
            Assert.That(customer.Invoices, Is.Not.Null);
            Assert.That(customer.PhoneNumbers, Is.Not.Null);
            Assert.That(customer.Invoices.Count, Is.EqualTo(0));
            Assert.That(customer.PhoneNumbers.Count, Is.EqualTo(0));
        }

        [Test]
        public void Balance_WithNoInvoices_ReturnsZero()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@test.example.com"
            };

            // Act
            var balance = customer.Balance;

            // Assert
            Assert.That(balance, Is.EqualTo(0));
        }

        [Test]
        public void Balance_WithMultipleInvoices_ReturnsSumOfAmounts()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@test.example.com",
                Invoices = new List<BonusInvoice>
                {
                    new BonusInvoice { Id = 1, Amount = 100.00m, CustomerId = 1, InvoiceNumber = "INV-001", InvoiceDate = DateTime.UtcNow },
                    new BonusInvoice { Id = 2, Amount = 250.50m, CustomerId = 1, InvoiceNumber = "INV-002", InvoiceDate = DateTime.UtcNow },
                    new BonusInvoice { Id = 3, Amount = 49.99m, CustomerId = 1, InvoiceNumber = "INV-003", InvoiceDate = DateTime.UtcNow }
                }
            };

            // Act
            var balance = customer.Balance;

            // Assert
            Assert.That(balance, Is.EqualTo(400.49m));
        }

        #region IValidatableObject Tests

        [Test]
        public void Validate_WithMatchingEmailDomain_PassesValidation()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Name = "Acme Corporation",
                Email = "contact@acmecorporation.example.com",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                ModifiedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(customer);
            var results = customer.Validate(validationContext).ToList();

            // Assert
            Assert.That(results, Is.Empty, "Should pass validation when email domain matches company name");
        }

        [Test]
        public void Validate_WithNonMatchingEmailDomain_FailsValidation()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Name = "Acme Corporation",
                Email = "contact@wrongdomain.com",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                ModifiedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(customer);
            var results = customer.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0), "Should fail validation when email domain doesn't match");
            Assert.That(results.Any(r => r.MemberNames.Contains("Email")), Is.True);
        }

        [Test]
        public void Validate_WithTestEmailDomain_PassesValidation()
        {
            // Arrange - test/example domains are allowed
            var customer = new BonusCustomer
            {
                Name = "Acme Corporation",
                Email = "contact@test.com",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                ModifiedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(customer);
            var results = customer.Validate(validationContext).ToList();

            // Assert
            Assert.That(results, Is.Empty, "Should pass validation for test/example email domains");
        }

        [Test]
        public void Validate_IsDeletedTrueWithoutDeletedDate_FailsValidation()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Name = "Test Customer",
                Email = "test@testcustomer.com",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                ModifiedDate = DateTime.UtcNow,
                IsDeleted = true,
                DeletedDate = null // Invalid state
            };

            // Act
            var validationContext = new ValidationContext(customer);
            var results = customer.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("DeletedDate")), Is.True);
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("DeletedDate must be set")), Is.True);
        }

        [Test]
        public void Validate_IsDeletedFalseWithDeletedDate_FailsValidation()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Name = "Test Customer",
                Email = "test@testcustomer.com",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                ModifiedDate = DateTime.UtcNow,
                IsDeleted = false,
                DeletedDate = DateTime.UtcNow // Invalid state
            };

            // Act
            var validationContext = new ValidationContext(customer);
            var results = customer.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("DeletedDate")), Is.True);
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("should be null")), Is.True);
        }

        [Test]
        public void Validate_CreatedDateInFuture_FailsValidation()
        {
            // ISSUE: This test has a timing issue - if test runs slowly, DateTime.UtcNow values may differ
            // CreatedDate is set to UtcNow.AddDays(1) but ModifiedDate is set to UtcNow
            // This means ModifiedDate will be BEFORE CreatedDate, causing an additional validation error
            // SUGGESTION: Set ModifiedDate = CreatedDate or use AddDays(2) for ModifiedDate to avoid cascade failures
            // SUGGESTION: The test will still pass but may get 2 validation errors instead of 1
            
            // Arrange
            var customer = new BonusCustomer
            {
                Name = "Test Customer",
                Email = "test@testcustomer.com",
                CreatedDate = DateTime.UtcNow.AddDays(1), // Future date
                ModifiedDate = DateTime.UtcNow.AddDays(2),
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(customer);
            var results = customer.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("CreatedDate")), Is.True);
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("cannot be in the future")), Is.True);
        }

        [Test]
        public void Validate_ModifiedDateBeforeCreatedDate_FailsValidation()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Name = "Test Customer",
                Email = "test@testcustomer.com",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow.AddDays(-1), // Before created
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(customer);
            var results = customer.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("ModifiedDate")), Is.True);
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("cannot be before CreatedDate")), Is.True);
        }

        [Test]
        public void Validate_ValidDeletedCustomer_PassesValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var customer = new BonusCustomer
            {
                Name = "Deleted Customer",
                Email = "deleted@deletedcustomer.com",
                CreatedDate = now.AddDays(-10),
                ModifiedDate = now,
                IsDeleted = true,
                DeletedDate = now
            };

            // Act
            var validationContext = new ValidationContext(customer);
            var results = customer.Validate(validationContext).ToList();

            // Assert
            Assert.That(results, Is.Empty, "Valid deleted customer should pass all validation");
        }

        #endregion

        #region Audit Field Tests

        [Test]
        public void AuditFields_CanBeSet()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var customer = new BonusCustomer();

            // Act
            customer.CreatedDate = now.AddDays(-5);
            customer.ModifiedDate = now;

            // Assert
            Assert.That(customer.CreatedDate, Is.EqualTo(now.AddDays(-5)));
            Assert.That(customer.ModifiedDate, Is.EqualTo(now));
        }

        #endregion

        #region Soft Delete Tests

        [Test]
        public void SoftDelete_CanBeSet()
        {
            // Arrange
            var customer = new BonusCustomer
            {
                Name = "Test",
                Email = "test@test.com"
            };

            // Act
            customer.IsDeleted = true;
            customer.DeletedDate = DateTime.UtcNow;

            // Assert
            Assert.That(customer.IsDeleted, Is.True);
            Assert.That(customer.DeletedDate, Is.Not.Null);
        }

        [Test]
        public void IsDeleted_DefaultsToFalse()
        {
            // Arrange & Act
            var customer = new BonusCustomer();

            // Assert
            Assert.That(customer.IsDeleted, Is.False);
            Assert.That(customer.DeletedDate, Is.Null);
        }

        #endregion
    }
}
