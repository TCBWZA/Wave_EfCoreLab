using EfCoreLab.Data;
using System.ComponentModel.DataAnnotations;

namespace EfCoreLab.Tests.Models
{
    /// <summary>
    /// Tests for BonusInvoice entity including custom validation (IValidatableObject).
    /// </summary>
    [TestFixture]
    public class BonusInvoiceTests
    {
        [Test]
        public void BonusInvoice_CanSetProperties()
        {
            // Arrange
            var invoice = new BonusInvoice();
            var now = DateTime.UtcNow;

            // Act
            invoice.Id = 1;
            invoice.InvoiceNumber = "INV-12345";
            invoice.CustomerId = 100;
            invoice.InvoiceDate = now;
            invoice.Amount = 500.50m;

            // Assert
            Assert.That(invoice.Id, Is.EqualTo(1));
            Assert.That(invoice.InvoiceNumber, Is.EqualTo("INV-12345"));
            Assert.That(invoice.CustomerId, Is.EqualTo(100));
            Assert.That(invoice.InvoiceDate, Is.EqualTo(now));
            Assert.That(invoice.Amount, Is.EqualTo(500.50m));
        }

        #region IValidatableObject Tests

        [Test]
        public void Validate_ValidInvoice_PassesValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var invoice = new BonusInvoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = 1,
                InvoiceDate = now.AddDays(-5),
                Amount = 100.00m,
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(invoice);
            var results = invoice.Validate(validationContext).ToList();

            // Assert
            Assert.That(results, Is.Empty, "Valid invoice should pass all validation");
        }

        [Test]
        public void Validate_InvoiceDateInFuture_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var invoice = new BonusInvoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = 1,
                InvoiceDate = now.AddDays(5), // Future date
                Amount = 100.00m,
                CreatedDate = now,
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(invoice);
            var results = invoice.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("InvoiceDate")), Is.True);
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("cannot be in the future")), Is.True);
        }

        [Test]
        public void Validate_InvoiceDateTooOld_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var invoice = new BonusInvoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = 1,
                InvoiceDate = now.AddYears(-11), // More than 10 years old
                Amount = 100.00m,
                CreatedDate = now.AddYears(-11),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(invoice);
            var results = invoice.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("InvoiceDate")), Is.True);
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("more than 10 years")), Is.True);
        }

        [Test]
        public void Validate_NegativeAmount_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var invoice = new BonusInvoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = 1,
                InvoiceDate = now.AddDays(-5),
                Amount = -100.00m, // Negative amount
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(invoice);
            var results = invoice.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("Amount")), Is.True);
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("must be greater than zero")), Is.True);
        }

        [Test]
        public void Validate_ZeroAmount_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var invoice = new BonusInvoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = 1,
                InvoiceDate = now.AddDays(-5),
                Amount = 0m, // Zero amount
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(invoice);
            var results = invoice.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("Amount")), Is.True);
        }

        [Test]
        public void Validate_IsDeletedTrueWithoutDeletedDate_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var invoice = new BonusInvoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = 1,
                InvoiceDate = now.AddDays(-5),
                Amount = 100.00m,
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = true,
                DeletedDate = null // Invalid state
            };

            // Act
            var validationContext = new ValidationContext(invoice);
            var results = invoice.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("DeletedDate")), Is.True);
        }

        [Test]
        public void Validate_CreatedDateInFuture_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var invoice = new BonusInvoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = 1,
                InvoiceDate = now.AddDays(-5),
                Amount = 100.00m,
                CreatedDate = now.AddDays(1), // Future date
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(invoice);
            var results = invoice.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("CreatedDate")), Is.True);
        }

        [Test]
        public void Validate_ModifiedDateBeforeCreatedDate_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var invoice = new BonusInvoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = 1,
                InvoiceDate = now.AddDays(-5),
                Amount = 100.00m,
                CreatedDate = now,
                ModifiedDate = now.AddDays(-1), // Before created date
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(invoice);
            var results = invoice.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("ModifiedDate")), Is.True);
        }

        #endregion

        #region Audit Field Tests

        [Test]
        public void AuditFields_CanBeSet()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var invoice = new BonusInvoice();

            // Act
            invoice.CreatedDate = now.AddDays(-5);
            invoice.ModifiedDate = now;

            // Assert
            Assert.That(invoice.CreatedDate, Is.EqualTo(now.AddDays(-5)));
            Assert.That(invoice.ModifiedDate, Is.EqualTo(now));
        }

        #endregion

        #region Soft Delete Tests

        [Test]
        public void SoftDelete_CanBeSet()
        {
            // Arrange
            var invoice = new BonusInvoice
            {
                InvoiceNumber = "INV-001",
                Amount = 100m
            };

            // Act
            invoice.IsDeleted = true;
            invoice.DeletedDate = DateTime.UtcNow;

            // Assert
            Assert.That(invoice.IsDeleted, Is.True);
            Assert.That(invoice.DeletedDate, Is.Not.Null);
        }

        [Test]
        public void IsDeleted_DefaultsToFalse()
        {
            // Arrange & Act
            var invoice = new BonusInvoice();

            // Assert
            Assert.That(invoice.IsDeleted, Is.False);
            Assert.That(invoice.DeletedDate, Is.Null);
        }

        #endregion
    }
}
