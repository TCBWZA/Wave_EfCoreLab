using EfCoreLab.Data;

namespace EfCoreLab.Tests.Models
{
    [TestFixture]
    public class CustomerTests
    {
        [Test]
        public void Customer_InitializesWithEmptyCollections()
        {
            // Act
            var customer = new Customer();

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
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com"
            };

            // Act
            var balance = customer.Balance;

            // Assert
            Assert.That(balance, Is.EqualTo(0));
        }

        [Test]
        public void Balance_WithSingleInvoice_ReturnsInvoiceAmount()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Invoices = new List<Invoice>
                {
                    new Invoice { Id = 1, Amount = 100.50m, CustomerId = 1 }
                }
            };

            // Act
            var balance = customer.Balance;

            // Assert
            Assert.That(balance, Is.EqualTo(100.50m));
        }

        [Test]
        public void Balance_WithMultipleInvoices_ReturnsSumOfAmounts()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Invoices = new List<Invoice>
                {
                    new Invoice { Id = 1, Amount = 100.00m, CustomerId = 1 },
                    new Invoice { Id = 2, Amount = 250.50m, CustomerId = 1 },
                    new Invoice { Id = 3, Amount = 49.99m, CustomerId = 1 }
                }
            };

            // Act
            var balance = customer.Balance;

            // Assert
            Assert.That(balance, Is.EqualTo(400.49m));
        }

        [Test]
        public void Balance_WithNullInvoices_ReturnsZero()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Invoices = null
            };

            // Act
            var balance = customer.Balance;

            // Assert
            Assert.That(balance, Is.EqualTo(0));
        }

        [Test]
        public void Customer_CanSetProperties()
        {
            // Arrange
            var customer = new Customer();

            // Act
            customer.Id = 42;
            customer.Name = "Acme Corp";
            customer.Email = "contact@acme.com";

            // Assert
            Assert.That(customer.Id, Is.EqualTo(42));
            Assert.That(customer.Name, Is.EqualTo("Acme Corp"));
            Assert.That(customer.Email, Is.EqualTo("contact@acme.com"));
        }
    }
}
