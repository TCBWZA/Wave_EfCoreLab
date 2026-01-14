# EfCoreLab.Tests

Comprehensive unit and integration test suite for the EfCoreLab project.

## Quick Start

```powershell
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~CustomerRepositoryTests"
```

## Test Statistics

- **Total Tests**: 88
- **Test Coverage**: Repositories, Models, Integration
- **Execution Time**: ~5 seconds
- **Success Rate**: 100%

## Project Structure

```
EfCoreLab.Tests/
|-- Models/                          # Entity model tests
|   |-- CustomerTests.cs            (5 tests)
|   |-- InvoiceTests.cs             (5 tests)
|   |-- TelephoneNumberTests.cs     (6 tests)
|-- Repositories/                    # Repository tests
|   |-- CustomerRepositoryTests.cs  (40 tests)
|   |-- InvoiceRepositoryTests.cs   (9 tests)
|-- Integration/                     # Integration tests
|   |-- AppDbContextTests.cs        (24 tests)
|-- TestHelpers/                     # Test utilities
|   |-- TestDbContextFactory.cs     (Database factory)
|-- UnitTest1.cs                     # Test suite info (3 tests)
|-- TEST_SUITE_SUMMARY.md           # Detailed documentation
|-- README.md                        # This file
```

## Test Categories

### Repository Tests (49 tests)
Tests for all CRUD operations, pagination, filtering, and EF Core features:
- CustomerRepositoryTests: 40 comprehensive tests
- InvoiceRepositoryTests: 9 tests

### Model Tests (12 tests)
Tests for entity behavior and calculated properties:
- CustomerTests: Balance calculations, initialization
- InvoiceTests: Property validation
- TelephoneNumberTests: Type validation

### Integration Tests (24 tests)
Tests for database operations and EF Core features:
- CRUD operations
- Relationships (one-to-many, foreign keys)
- Cascade deletes
- Query operations (filtering, sorting, counting)
- Change tracking (AsNoTracking)

## Key Features Tested

- **CRUD Operations** - Create, Read, Update, Delete  
- **Pagination** - Skip/Take with page metadata  
- **Filtering** - Dynamic WHERE clauses  
- **Searching** - Multiple parameter combinations  
- **Relationships** - One-to-many, navigation properties  
- **Cascade Deletes** - Related entity deletion  
- **AsNoTracking** - Read-only performance optimization  
- **Split Queries** - Avoiding cartesian explosion  
- **Entity Validation** - Calculated properties

## Technologies Used

- **Test Framework**: NUnit 3.14.0
- **Database**: EF Core In-Memory Provider 8.0.0
- **Target Framework**: .NET 8
- **Test Runner**: NUnit3TestAdapter 4.5.0

## Test Patterns

### AAA Pattern
All tests follow the Arrange-Act-Assert pattern:

```csharp
[Test]
public async Task GetByIdAsync_WithValidId_ReturnsCustomer()
{
    // Arrange - Set up test data
    long customerId = 1;

    // Act - Execute the method being tested
    var result = await _repository.GetByIdAsync(customerId);

    // Assert - Verify the results
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Id, Is.EqualTo(customerId));
}
```

### Test Isolation
Each test gets a fresh in-memory database:

```csharp
[SetUp]
public void Setup()
{
    _context = TestDbContextFactory.CreateSeededContext();
    _repository = new CustomerRepository(_context);
}

[TearDown]
public void TearDown()
{
    _context.Dispose();
}
```

## Test Data

### Seeded Test Database
The `CreateSeededContext()` method provides:

**3 Customers:**
- Acme Corporation (contact@acme.com)
- Tech Solutions Ltd (info@techsolutions.com)
- Global Industries (hello@globalindustries.com)

**3 Invoices:**
- INV-001: $1000.00 (Acme Corporation)
- INV-002: $2500.50 (Acme Corporation)
- INV-003: $750.00 (Tech Solutions Ltd)

**3 Phone Numbers:**
- Mobile: +44 7700 900123 (Acme Corporation)
- Work: +44 20 7946 0958 (Acme Corporation)
- Mobile: +44 7700 900456 (Tech Solutions Ltd)

## Running Tests in Visual Studio

1. Open **Test Explorer** (Ctrl+E, T)
2. Click **Run All** or select specific tests
3. View test results and output

## Example Test Output

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0
           Passed:    88
           Skipped:    0
           Total:     88
           Duration: 5.0s
```

## Code Coverage

To generate code coverage:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## Best Practices Demonstrated

1. **Clear Test Names**: `MethodName_Scenario_ExpectedBehavior`
2. **Test Isolation**: Each test independent from others
3. **In-Memory Database**: Fast, no external dependencies
4. **Comprehensive Coverage**: Happy path, edge cases, errors
5. **Readable Assertions**: NUnit constraint model
6. **Setup/Teardown**: Proper resource management

## EF Core Patterns Tested

### Eager Loading
```csharp
var customer = await _repository.GetByIdAsync(1, includeRelated: true);
// Loads customer with invoices and phone numbers
```

### AsNoTracking (Read-Only)
```csharp
var customers = await _repository.GetAllNoTrackingAsync();
// Better performance for read-only scenarios
```

### Pagination
```csharp
var (items, totalCount) = await _repository.GetPagedAsync(page: 1, pageSize: 10);
// Returns current page + total count
```

### Dynamic Filtering
```csharp
var results = await _repository.SearchAsync(
    name: "Acme",
    email: "acme.com",
    minBalance: 1000m
);
```

## Troubleshooting

### Tests Fail to Run
- Ensure .NET 8 SDK is installed
- Restore NuGet packages: `dotnet restore`
- Clean solution: `dotnet clean`

### In-Memory Database Issues
- Each test gets a unique database (isolated)
- Database is disposed after each test
- No persistence between tests

### Nullable Warnings
- Some warnings exist in test code
- They don't affect test execution
- Can be suppressed with null-forgiving operator `!`

## Contributing

When adding new tests:

1. Follow AAA pattern
2. Use descriptive test names
3. Test both happy path and edge cases
4. Add tests to appropriate category
5. Ensure tests are isolated
6. Update documentation

## Documentation

See `TEST_SUITE_SUMMARY.md` for:
- Detailed test breakdown
- EF Core features tested
- Test statistics
- Future enhancements

## License

Same as main EfCoreLab project.

## Support

For questions or issues, please refer to the main EfCoreLab documentation.
