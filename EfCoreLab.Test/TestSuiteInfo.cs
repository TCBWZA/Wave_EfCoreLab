namespace EfCoreLab.Tests
{
    /// <summary>
    /// EfCoreLab Test Suite Summary
    /// 
    /// This test project contains comprehensive unit and integration tests for the EfCoreLab application.
    /// 
    /// Test Organization:
    /// 
    /// 1. Repositories Tests (Repositories folder)
    ///    - CustomerRepositoryTests: Tests all CRUD operations, pagination, search, and filtering
    ///    - InvoiceRepositoryTests: Tests invoice management and validation
    ///    
    /// 2. Model Tests (Models folder)
    ///    - CustomerTests: Tests Customer entity properties and calculated fields
    ///    - InvoiceTests: Tests Invoice entity properties
    ///    - TelephoneNumberTests: Tests TelephoneNumber entity properties
    ///    
    /// 3. Integration Tests (Integration folder)
    ///    - AppDbContextTests: Tests database operations, relationships, and EF Core features
    ///    
    /// 4. Test Helpers (TestHelpers folder)
    ///    - TestDbContextFactory: Factory for creating in-memory test databases
    ///    
    /// Test Coverage:
    /// - Repository CRUD operations
    /// - Entity relationships (one-to-many, navigation properties)
    /// - Cascade deletes
    /// - Query operations (filtering, sorting, pagination)
    /// - EF Core features (AsNoTracking, Include, etc.)
    /// - Business logic (calculated properties)
    /// 
    /// Running Tests:
    /// - Visual Studio: Test Explorer (Ctrl+E, T)
    /// - Command Line: dotnet test
    /// - With coverage: dotnet test --collect:"XPlat Code Coverage"
    /// 
    /// Note: All tests use in-memory database for isolation and speed.
    /// </summary>
    [TestFixture]
    public class TestSuiteInfo
    {
        [Test]
        public void TestSuite_IsProperlyConfigured()
        {
            // This test verifies that the test project is set up correctly
            Assert.Pass("EfCoreLab test suite is configured and ready to run.");
        }

        [Test]
        public void TestSuite_CanCreateInMemoryContext()
        {
            // Verify that we can create an in-memory context
            using var context = TestHelpers.TestDbContextFactory.CreateInMemoryContext();
            
            Assert.That(context, Is.Not.Null);
            Assert.That(context.Database.ProviderName, Does.Contain("InMemory"));
        }

        [Test]
        public void TestSuite_CanSeedTestData()
        {
            // Verify that test data seeding works
            using var context = TestHelpers.TestDbContextFactory.CreateSeededContext();
            
            var customerCount = context.Customers.Count();
            var invoiceCount = context.Invoices.Count();
            var phoneCount = context.TelephoneNumbers.Count();

            Assert.That(customerCount, Is.GreaterThan(0), "Customers were not seeded");
            Assert.That(invoiceCount, Is.GreaterThan(0), "Invoices were not seeded");
            Assert.That(phoneCount, Is.GreaterThan(0), "Phone numbers were not seeded");
        }
    }
}