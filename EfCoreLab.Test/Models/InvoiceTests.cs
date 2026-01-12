using EfCoreLab.Data;

namespace EfCoreLab.Tests.Models
{
    [TestFixture]
    public class InvoiceTests
    {
        [Test]
        public void Invoice_CanSetAllProperties()
        {
            // Arrange
            var invoice = new Invoice();
            var testDate = DateTime.UtcNow;

            // Act
            invoice.Id = 1;
            invoice.InvoiceNumber = "INV-001";
            invoice.CustomerId = 42;
            invoice.InvoiceDate = testDate;
            invoice.Amount = 1234.56m;

            // Assert
            Assert.That(invoice.Id, Is.EqualTo(1));
            Assert.That(invoice.InvoiceNumber, Is.EqualTo("INV-001"));
            Assert.That(invoice.CustomerId, Is.EqualTo(42));
            Assert.That(invoice.InvoiceDate, Is.EqualTo(testDate));
            Assert.That(invoice.Amount, Is.EqualTo(1234.56m));
        }

        [Test]
        public void Invoice_Amount_CanBeZero()
        {
            // Arrange
            var invoice = new Invoice
            {
                Amount = 0m
            };

            // Assert
            Assert.That(invoice.Amount, Is.EqualTo(0m));
        }

        [Test]
        public void Invoice_Amount_CanBeDecimal()
        {
            // Arrange
            var invoice = new Invoice
            {
                Amount = 123.45m
            };

            // Assert
            Assert.That(invoice.Amount, Is.EqualTo(123.45m));
        }

        [Test]
        public void Invoice_CustomerId_CanBeSet()
        {
            // Arrange
            var invoice = new Invoice
            {
                CustomerId = 42
            };

            // Assert
            Assert.That(invoice.CustomerId, Is.EqualTo(42));
        }

        [Test]
        public void Invoice_InvoiceNumber_CanBeSet()
        {
            // Arrange
            var invoice = new Invoice
            {
                InvoiceNumber = "INV-TEST-123"
            };

            // Assert
            Assert.That(invoice.InvoiceNumber, Is.EqualTo("INV-TEST-123"));
        }
    }
}
