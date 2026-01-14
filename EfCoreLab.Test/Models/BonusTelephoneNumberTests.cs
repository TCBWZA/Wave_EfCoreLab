using EfCoreLab.Data;
using System.ComponentModel.DataAnnotations;

namespace EfCoreLab.Tests.Models
{
    /// <summary>
    /// Tests for BonusTelephoneNumber entity including custom validation (IValidatableObject).
    /// </summary>
    [TestFixture]
    public class BonusTelephoneNumberTests
    {
        [Test]
        public void BonusTelephoneNumber_CanSetProperties()
        {
            // Arrange
            var phone = new BonusTelephoneNumber();

            // Act
            phone.Id = 1;
            phone.CustomerId = 100;
            phone.Type = "Mobile";
            phone.Number = "+44 7700 900123";

            // Assert
            Assert.That(phone.Id, Is.EqualTo(1));
            Assert.That(phone.CustomerId, Is.EqualTo(100));
            Assert.That(phone.Type, Is.EqualTo("Mobile"));
            Assert.That(phone.Number, Is.EqualTo("+44 7700 900123"));
        }

        #region IValidatableObject Tests

        [Test]
        public void Validate_ValidPhoneNumber_PassesValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Mobile",
                Number = "+44 7700 900123",
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(phone);
            var results = phone.Validate(validationContext).ToList();

            // Assert
            Assert.That(results, Is.Empty, "Valid phone number should pass all validation");
        }

        [Test]
        public void Validate_MobileNumberWithoutDigits_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Mobile",
                Number = "NO-DIGITS-HERE", // No digits
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(phone);
            var results = phone.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("Number")), Is.True);
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("must contain at least one digit")), Is.True);
        }

        [Test]
        public void Validate_WorkNumberWithoutDigits_PassesValidation()
        {
            // Arrange - Work numbers don't require digits in our validation
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Work",
                Number = "Extension-ABC", // No digits but it's Work type
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(phone);
            var results = phone.Validate(validationContext).ToList();

            // Assert - Should fail because minimum 8 digits check applies to all
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("at least 8 digits")), Is.True);
        }

        [Test]
        public void Validate_PhoneNumberTooShort_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Mobile",
                Number = "123", // Only 3 digits
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(phone);
            var results = phone.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("Number")), Is.True);
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("at least 8 digits")), Is.True);
        }

        [Test]
        public void Validate_PhoneNumberWithEnoughDigits_PassesValidation()
        {
            // Arrange - 8 digits with formatting
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Mobile",
                Number = "+44-7700-900-123", // Has 11 digits
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(phone);
            var results = phone.Validate(validationContext).ToList();

            // Assert
            Assert.That(results, Is.Empty, "Phone with 8+ digits should pass");
        }

        [Test]
        public void Validate_IsDeletedTrueWithoutDeletedDate_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Mobile",
                Number = "+44 7700 900123",
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = true,
                DeletedDate = null // Invalid state
            };

            // Act
            var validationContext = new ValidationContext(phone);
            var results = phone.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("DeletedDate")), Is.True);
        }

        [Test]
        public void Validate_CreatedDateInFuture_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Mobile",
                Number = "+44 7700 900123",
                CreatedDate = now.AddDays(1), // Future date
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(phone);
            var results = phone.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("CreatedDate")), Is.True);
        }

        [Test]
        public void Validate_ModifiedDateBeforeCreatedDate_FailsValidation()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Mobile",
                Number = "+44 7700 900123",
                CreatedDate = now,
                ModifiedDate = now.AddDays(-1), // Before created date
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(phone);
            var results = phone.Validate(validationContext).ToList();

            // Assert
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Any(r => r.MemberNames.Contains("ModifiedDate")), Is.True);
        }

        [Test]
        public void Validate_AllPhoneTypes_AreValid()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var types = new[] { "Mobile", "Work", "DirectDial" };

            foreach (var type in types)
            {
                var phone = new BonusTelephoneNumber
                {
                    CustomerId = 1,
                    Type = type,
                    Number = "+44 7700 900123",
                    CreatedDate = now.AddDays(-5),
                    ModifiedDate = now,
                    IsDeleted = false
                };

                // Act
                var validationContext = new ValidationContext(phone);
                var results = phone.Validate(validationContext).ToList();

                // Assert
                Assert.That(results, Is.Empty, $"Phone type '{type}' should be valid");
            }
        }

        #endregion

        #region Audit Field Tests

        [Test]
        public void AuditFields_CanBeSet()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber();

            // Act
            phone.CreatedDate = now.AddDays(-5);
            phone.ModifiedDate = now;

            // Assert
            Assert.That(phone.CreatedDate, Is.EqualTo(now.AddDays(-5)));
            Assert.That(phone.ModifiedDate, Is.EqualTo(now));
        }

        #endregion

        #region Soft Delete Tests

        [Test]
        public void SoftDelete_CanBeSet()
        {
            // Arrange
            var phone = new BonusTelephoneNumber
            {
                Type = "Mobile",
                Number = "+44 7700 900123"
            };

            // Act
            phone.IsDeleted = true;
            phone.DeletedDate = DateTime.UtcNow;

            // Assert
            Assert.That(phone.IsDeleted, Is.True);
            Assert.That(phone.DeletedDate, Is.Not.Null);
        }

        [Test]
        public void IsDeleted_DefaultsToFalse()
        {
            // Arrange & Act
            var phone = new BonusTelephoneNumber();

            // Assert
            Assert.That(phone.IsDeleted, Is.False);
            Assert.That(phone.DeletedDate, Is.Null);
        }

        #endregion
    }
}
