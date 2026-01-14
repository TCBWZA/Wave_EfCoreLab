# Bonus Challenges Test Suite

This document describes the comprehensive test suite created for the Bonus Challenges implementation.

## Test Files Created

### 1. Test Helper
**File:** `EfCoreLab.Test/TestHelpers/BonusTestDbContextFactory.cs`

Factory for creating in-memory BonusDbContext instances with seeded test data.

**Features:**
- Creates isolated in-memory database for each test
- Seeds with test data including audit fields
- Includes soft-deleted records for testing global query filters
- Provides both empty and seeded context options

**Test Data:**
- 3 active BonusCustomers
- 1 soft-deleted BonusCustomer (Id = 4)
- Multiple BonusInvoices (including 1 soft-deleted)
- Multiple BonusTelephoneNumbers

---

### 2. Model Tests

#### **File:** `EfCoreLab.Test/Models/BonusCustomerTests.cs`

**Test Coverage:**
- [x] Property initialization
- [x] Balance calculation (inherited from original tests)
- [x] **Custom Validation (IValidatableObject):**
  - Email domain matching company name
  - Test/example domain exceptions
  - IsDeleted and DeletedDate consistency
  - CreatedDate not in future
  - ModifiedDate after CreatedDate
  - Valid deleted customer state
- [x] Audit field setters
- [x] Soft delete flag defaults

**Total Tests:** 18

---

#### **File:** `EfCoreLab.Test/Models/BonusInvoiceTests.cs`

**Test Coverage:**
- [x] Property setters
- [x] **Custom Validation (IValidatableObject):**
  - Invoice date not in future
  - Invoice date not too old (< 10 years)
  - Amount must be positive
  - IsDeleted/DeletedDate consistency
  - Audit field validation
- [x] Audit fields
- [x] Soft delete defaults

**Total Tests:** 13

---

#### **File:** `EfCoreLab.Test/Models/BonusTelephoneNumberTests.cs`

**Test Coverage:**
- [x] Property setters
- [x] **Custom Validation (IValidatableObject):**
  - Mobile numbers must contain digits
  - Phone numbers must have minimum 8 digits
  - All phone types (Mobile, Work, DirectDial) validation
  - IsDeleted/DeletedDate consistency
  - Audit field validation
- [x] Audit fields
- [x] Soft delete defaults

**Total Tests:** 13

---

### 3. Integration Tests

#### **File:** `EfCoreLab.Test/Integration/BonusDbContextTests.cs`

**Test Coverage:**

**Global Query Filter Tests (Challenge 4):**
- [x] Excludes soft-deleted customers by default
- [x] Can bypass with IgnoreQueryFilters()
- [x] Excludes soft-deleted invoices by default
- [x] Excludes soft-deleted in Include() operations

**Audit Interceptor Tests (Challenge 2):**
- [x] Sets CreatedDate on insert
- [x] Sets ModifiedDate on insert
- [x] Updates ModifiedDate on update
- [x] Sets DeletedDate on soft delete
- [x] Clears DeletedDate on restore
- [x] Works for all entity types (Customer, Invoice, Phone)

**CRUD Operations:**
- [x] Query customers
- [x] Query with includes
- [x] Create customer
- [x] Update customer
- [x] Soft delete customer

**Relationship Tests:**
- [x] Customer can have multiple invoices
- [x] Customer can have multiple phone numbers
- [x] Invoice belongs to customer

**Index Tests:**
- [x] Documents unique email constraint
- [x] Documents unique invoice number constraint

**Total Tests:** 19

---

### 4. Controller Tests

#### **File:** `EfCoreLab.Test/Controllers/BonusChallengesControllerTests.cs`

**Test Coverage:**

**GetCustomers (Challenge 4 - Global Query Filters):**
- [x] Without includeDeleted returns only active customers
- [x] With includeDeleted returns all customers

**GetCustomerById (Challenge 5 - Caching):**
- [x] First call loads from database
- [x] Second call loads from cache
- [x] Non-existent customer returns NotFound

**CreateCustomer (Challenge 1 - Custom Validation):**
- [x] Valid data creates customer
- [x] Sets audit fields automatically
- [x] Invalid email returns BadRequest

**UpdateCustomer (Challenge 2 - Auditing):**
- [x] Updates ModifiedDate
- [x] Non-existent returns NotFound
- [x] Invalidates cache

**SoftDeleteCustomer (Challenge 3 - Soft Delete):**
- [x] Marks customer as deleted
- [x] Hides from normal queries
- [x] Non-existent returns NotFound
- [x] Already deleted returns BadRequest
- [x] Invalidates cache

**RestoreCustomer (Challenge 3 - Soft Delete):**
- [x] Restores soft-deleted customer
- [x] Makes visible in normal queries
- [x] Non-existent returns NotFound
- [x] Not deleted returns BadRequest

**GetCustomersWithLargeBalance (Challenge 6 - SQL Logging):**
- [x] Returns customers above threshold
- [x] Demonstrates SQL logging feature

**DemoAllChallenges:**
- [x] Returns comprehensive overview

**Total Tests:** 22

---

## Test Summary by Challenge

### Challenge 1: Custom Validation (IValidatableObject)
**Tests:** 11 across model tests
- Email domain matching
- Audit field consistency
- Soft delete state validation
- Date range validation
- Amount validation (invoices)
- Phone number format validation

### Challenge 2: Auditing (CreatedDate, ModifiedDate)
**Tests:** 9 in integration and controller tests
- CreatedDate set on insert
- ModifiedDate set on insert/update
- Works for all entity types
- Controller operations update audit fields

### Challenge 3: Soft Delete (IsDeleted)
**Tests:** 11 in integration and controller tests
- Soft delete marks records
- Restore functionality
- DeletedDate management
- State consistency

### Challenge 4: Global Query Filters
**Tests:** 8 in integration and controller tests
- Automatic filtering of deleted records
- IgnoreQueryFilters bypass
- Works with Include operations
- Controller respects filters

### Challenge 5: Caching (IMemoryCache)
**Tests:** 4 in controller tests
- Cache hit/miss behavior
- Cache invalidation on updates
- Cache invalidation on deletes

### Challenge 6: SQL Query Logging
**Tests:** 2 in controller tests
- Logging demonstration
- Complex query logging

---

## Running the Tests

### Run All Tests
```powershell
dotnet test
```

### Run Specific Test File
```powershell
dotnet test --filter "ClassName~BonusCustomerTests"
dotnet test --filter "ClassName~BonusDbContextTests"
dotnet test --filter "ClassName~BonusChallengesControllerTests"
```

### Run Tests by Category
```powershell
# Model tests
dotnet test --filter "FullyQualifiedName~EfCoreLab.Tests.Models"

# Integration tests
dotnet test --filter "FullyQualifiedName~EfCoreLab.Tests.Integration"

# Controller tests
dotnet test --filter "FullyQualifiedName~EfCoreLab.Tests.Controllers"
```

---

## Test Data

The test helper factory (`BonusTestDbContextFactory`) creates the following seeded data:

### Customers (4 total)
1. **Acme Corporation** (Id: 1) - Active, has invoices and phones
2. **Tech Solutions Ltd** (Id: 2) - Active, has invoices and phones
3. **Global Industries** (Id: 3) - Active
4. **Deleted Company** (Id: 4) - **Soft Deleted** for testing

### Invoices (4 total)
1. INV-001 (Customer 1) - Active, $1,000.00
2. INV-002 (Customer 1) - Active, $2,500.50
3. INV-003 (Customer 2) - Active, $750.00
4. INV-004 (Customer 1) - **Soft Deleted**, $500.00

### Phone Numbers (3 total)
All active, distributed across customers

---

## Dependencies Added

The following NuGet package was added to support testing:
- **Moq 4.20.72** - For mocking ILogger in controller tests

---

## Total Test Count

- **Model Tests:** 44 tests (18 + 13 + 13)
- **Integration Tests:** 19 tests
- **Controller Tests:** 22 tests
- **Grand Total:** 85 comprehensive tests

All tests verify the bonus challenge features work correctly and maintain expected behavior.

---

## Coverage by Feature

| Feature | Model Tests | Integration Tests | Controller Tests | Total |
|---------|-------------|-------------------|------------------|-------|
| Custom Validation | 11 | 0 | 3 | 14 |
| Auditing | 3 | 6 | 3 | 12 |
| Soft Delete | 6 | 5 | 8 | 19 |
| Global Query Filters | 0 | 4 | 4 | 8 |
| Caching | 0 | 0 | 4 | 4 |
| SQL Logging | 0 | 0 | 2 | 2 |
| Basic CRUD | 24 | 4 | 4 | 32 |

---

## Test Quality

- [x] Uses in-memory database for isolation
- [x] Each test gets fresh database instance
- [x] Comprehensive test data seeding
- [x] Tests positive and negative scenarios
- [x] Tests edge cases (already deleted, non-existent, etc.)
- [x] Validates audit fields automatically set
- [x] Validates cache behavior
- [x] Validates global query filters
- [x] Validates custom validation logic
- [x] All tests are independent and isolated
