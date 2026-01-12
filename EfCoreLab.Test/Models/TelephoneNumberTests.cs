using EfCoreLab.Data;

namespace EfCoreLab.Tests.Models
{
    [TestFixture]
    public class TelephoneNumberTests
    {
        [Test]
        public void TelephoneNumber_CanSetAllProperties()
        {
            // Arrange
            var phoneNumber = new TelephoneNumber();

            // Act
            phoneNumber.Id = 1;
            phoneNumber.CustomerId = 42;
            phoneNumber.Type = "Mobile";
            phoneNumber.Number = "+44 7700 900123";

            // Assert
            Assert.That(phoneNumber.Id, Is.EqualTo(1));
            Assert.That(phoneNumber.CustomerId, Is.EqualTo(42));
            Assert.That(phoneNumber.Type, Is.EqualTo("Mobile"));
            Assert.That(phoneNumber.Number, Is.EqualTo("+44 7700 900123"));
        }

        [Test]
        public void TelephoneNumber_Type_CanBeMobile()
        {
            // Arrange
            var phoneNumber = new TelephoneNumber
            {
                Type = "Mobile"
            };

            // Assert
            Assert.That(phoneNumber.Type, Is.EqualTo("Mobile"));
        }

        [Test]
        public void TelephoneNumber_Type_CanBeWork()
        {
            // Arrange
            var phoneNumber = new TelephoneNumber
            {
                Type = "Work"
            };

            // Assert
            Assert.That(phoneNumber.Type, Is.EqualTo("Work"));
        }

        [Test]
        public void TelephoneNumber_Type_CanBeDirectDial()
        {
            // Arrange
            var phoneNumber = new TelephoneNumber
            {
                Type = "DirectDial"
            };

            // Assert
            Assert.That(phoneNumber.Type, Is.EqualTo("DirectDial"));
        }

        [Test]
        public void TelephoneNumber_CustomerId_CanBeSet()
        {
            // Arrange
            var phoneNumber = new TelephoneNumber
            {
                CustomerId = 100
            };

            // Assert
            Assert.That(phoneNumber.CustomerId, Is.EqualTo(100));
        }

        [Test]
        public void TelephoneNumber_Number_CanContainSpecialCharacters()
        {
            // Arrange
            var phoneNumber = new TelephoneNumber
            {
                Number = "+44 (20) 7946-0958"
            };

            // Assert
            Assert.That(phoneNumber.Number, Is.EqualTo("+44 (20) 7946-0958"));
        }
    }
}
