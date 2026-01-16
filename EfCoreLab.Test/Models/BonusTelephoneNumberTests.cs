using EfCoreLab.Data;
using System.ComponentModel.DataAnnotations;

namespace EfCoreLab.Tests.Models
{
    /// <summary>
    /// Tests for BonusTelephoneNumber entity including custom validation (IValidatableObject).
    /// 
    /// KNOWN ISSUES IN THESE TESTS:
    /// 1. Validate_WorkNumberWithoutDigits_PassesValidation: Test name says "Passes" but expects
    ///    validation to FAIL (Is.GreaterThan(0)). Name is misleading.
    /// 2. Validate_CreatedDateInFuture_FailsValidation: Creates cascade validation error
    /// 3. Phone number validation has TWO separate checks:
    ///    a) Mobile numbers must contain at least one digit (Type-specific)
    ///    b) ALL numbers must have at least 8 digits after removing spaces/dashes/plus signs
    ///    This means Mobile numbers can trigger BOTH validation errors
    /// 4. The minimum digit count validation strips spaces, dashes, and plus signs, so
    ///    "+1-234-567-8901" has 11 digits and passes, but "123" with 3 digits fails
    /// 5. Type validation via RegularExpression only allows: Mobile, Work, or DirectDial
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
        //public void Validate_WorkNumberWithoutDigits_PassesValidation()
        // ISSUE: Test name says "PassesValidation" but expects FAILURE (Is.GreaterThan(0))
        // ISSUE: Test comment incorrectly states "Work numbers don't require digits"
        // ACTUAL BEHAVIOR: ALL phone numbers (Mobile, Work, DirectDial) must have at least 8 digits
        // after stripping spaces, dashes, and plus signs
        // SUGGESTION: Rename to "Validate_WorkNumberWithoutDigits_FailsValidation"
         public void Validate_WorkNumberWithoutDigits_FailsValidation()
        {
            // Arrange - Work number with insufficient digits (only 3 letters, 0 digits)
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Work",
                Number = "ABC", // No digits - violates minimum 8 digit requirement
                CreatedDate = now.AddDays(-5),
                ModifiedDate = now,
                IsDeleted = false
            };

            // Act
            var validationContext = new ValidationContext(phone);
            var results = phone.Validate(validationContext).ToList();

            // Assert - Should fail because minimum 8 digits check applies to ALL phone types
            Assert.That(results.Count, Is.GreaterThan(0)); // Is.Not.Empty
            Assert.That(results.Any(r => r.ErrorMessage!.Contains("at least 8 digits")), Is.True);
        }

        [Test]
        public void Validate_PhoneNumberTooShort_FailsValidation()
        {
            // ISSUE: Mobile type numbers have TWO separate validation checks that can both fail:
            // 1. Mobile-specific: Must contain at least one digit
            // 2. Universal: ALL phone types must have at least 8 digits after stripping spaces/dashes/plus signs
            // The test number "123" has only 3 digits, so it will fail the universal 8-digit rule
            // It technically passes the Mobile-specific "at least one digit" check
            // RESULT: Test may get multiple validation errors for this number
            
            // Arrange
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Mobile",
                Number = "123", // Only 3 digits - fails universal 8-digit minimum
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
            // ISSUE: This test creates a CASCADE validation error
            // CreatedDate is set to future (now + 1 day), but ModifiedDate is set to 'now'
            // This violates TWO rules:
            // 1. CreatedDate cannot be in the future (the intended test)
            // 2. ModifiedDate cannot be before CreatedDate (unintended side effect)
            // RESULT: Test gets TWO validation errors instead of just the one being tested
            // SUGGESTION: Set ModifiedDate = now.AddDays(2) to ensure ModifiedDate > CreatedDate
            // This isolates the test to only validate the "CreatedDate in future" rule
            
            // Arrange
            var now = DateTime.UtcNow;
            var phone = new BonusTelephoneNumber
            {
                CustomerId = 1,
                Type = "Mobile",
                Number = "+44 7700 900123",
                CreatedDate = now.AddDays(1), // Future date - violates rule 1
                ModifiedDate = now, // now < CreatedDate - also violates rule 2 (.AddDays(2) would fix)
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
