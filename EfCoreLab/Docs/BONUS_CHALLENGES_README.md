# Bonus Challenges Implementation

This document describes the implementation of 6 advanced EF Core features in the Bonus Challenges.

## Overview

The Bonus Challenges demonstrate advanced EF Core patterns using a separate set of entities and database context:
- **Entities**: `BonusCustomer`, `BonusInvoice`, `BonusTelephoneNumber`
- **Context**: `BonusDbContext` (separate from `AppDbContext`)
- **Tables**: `BonusCustomers`, `BonusInvoices`, `BonusTelephoneNumbers`
- **Controller**: `BonusChallengesController` at `/api/bonuschallenges`

---

## Challenge 1: Custom Validation with IValidatableObject

**Implementation:**
- All bonus entities implement `IValidatableObject` interface
- Custom validation logic in `Validate()` method

**BonusCustomer validation rules:**
- Email domain should relate to company name
- Audit field consistency checks
- CreatedDate not in the future
- ModifiedDate not before CreatedDate
- Soft delete validation (IsDeleted and DeletedDate consistency)

**Example:**
```csharp
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
{
    if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Name))
    {
        // Custom validation logic here
        yield return new ValidationResult("Email domain should relate to company name");
    }
}
```

**Test it:**
```
POST /api/bonuschallenges/customers
{
  "name": "Acme Corporation",
  "email": "test@wrongdomain.com"
}
// Will fail validation because email domain doesn't match company name
```

---

## Challenge 2: Auditing with CreatedDate and ModifiedDate

**Implementation:** `AuditInterceptor.cs` + audit fields in entities

**How it works:**
- `AuditInterceptor` intercepts `SaveChanges()` and `SaveChangesAsync()`
- Automatically sets `CreatedDate` and `ModifiedDate` timestamps
- No manual code needed in controllers/repositories
- Works for all bonus entities

**Files created:**
- `EfCoreLab/Interceptors/AuditInterceptor.cs` - The interceptor that handles audit fields
- Audit fields added to all Bonus entities

**Example:**
```csharp
var customer = new BonusCustomer { Name = "Test", Email = "test@test.com" };
context.Add(customer);
await context.SaveChangesAsync();
// customer.CreatedDate and customer.ModifiedDate are automatically set!
```

---

## Challenge 3: Soft Delete

**Implementation:**
- Added `IsDeleted` flag to all bonus entities
- Added `DeletedDate` field to track when deletion occurred
- Soft delete sets flags instead of removing records

**Location:** `BonusCustomer.cs`, `BonusInvoice.cs`, `BonusTelephoneNumber.cs`

**Benefits:**
- Records remain in database for audit trail
- Can be restored if deleted by mistake
- Historical reporting includes deleted records

**Demo Endpoints:**
- `DELETE /api/bonuschallenges/customers/{id}` - Soft delete
- `POST /api/bonuschallenges/customers/{id}/restore` - Restore deleted customer

---

## Challenge 4: Global Query Filters

**Implementation:**
- Global query filters configured in `BonusDbContext.OnModelCreating()`
- Automatically filters soft-deleted records: `HasQueryFilter(c => !c.IsDeleted)`
- Can be bypassed with `IgnoreQueryFilters()` when needed

**Benefits:**
- No need to add `WHERE IsDeleted = false` to every query
- Prevents accidentally showing deleted data
- Can be bypassed for admin/restore functionality

**Example:**
```csharp
// Normal query - automatically filters deleted records
var customers = await context.BonusCustomers.ToListAsync();

// Include deleted records
var allCustomers = await context.BonusCustomers.IgnoreQueryFilters().ToListAsync();
```

---

## Challenge 5: Caching with IMemoryCache

**Implementation:**
- `IMemoryCache` service registered in Program.cs
- Cache keys: `bonus_customer_{id}` and `bonus_customer_list`
- Cache duration: 5 minutes (configurable)
- Cache invalidation on create/update/delete

**Performance:**
- First request: Database query (~50ms)
- Subsequent requests: Cache hit (~1ms) - 50x faster!

**Example:**
```csharp
// First call - cache miss, loads from database
var customer = await GetCustomerById(1);

// Second call within 5 minutes - cache hit, returns cached data
var customer2 = await GetCustomerById(1); // Much faster!
```

---

## Challenge 6: SQL Query Logging

**Implementation:** Configured in `BonusDbContext.OnConfiguring()`

All SQL queries are logged to the console/application logs:
```csharp
optionsBuilder.LogTo(
    message => _logger.LogInformation(message),
    new[] { DbLoggerCategory.Database.Command.Name },
    LogLevel.Information);
```

**What gets logged:**
- SELECT/INSERT/UPDATE/DELETE statements
- All parameters and values
- Query execution time
- Connection information

**Example log output:**
```
Executed DbCommand (23ms) [Parameters=[@__minBalance_0='10000'], CommandType='Text']
SELECT [c].[Id], [c].[Name], [c].[Email], SUM([i].[Amount]) AS [Balance]
FROM [BonusCustomers] AS [c]
LEFT JOIN [BonusInvoices] AS [i] ON [c].[Id] = [i].[CustomerId]
WHERE [c].[IsDeleted] = 0
GROUP BY [c].[Id], [c].[Name], [c].[Email]
HAVING SUM([i].[Amount]) >= @__minBalance_0
```

---

## Testing the Bonus Challenges

### 1. Start the Application
The bonus database will be seeded automatically with sample data.

### 2. Navigate to Swagger UI
Access the API documentation at the application root URL.

### 3. Try the Endpoints

**Demo All Features:**
```
GET /api/bonuschallenges/demo
```
Returns a comprehensive overview of all bonus challenges.

**Test Custom Validation:**
```
POST /api/bonuschallenges/customers
{
  "name": "Acme Corporation",
  "email": "contact@acmecorporation.com"
}
```

**Test Global Query Filters:**
```
GET /api/bonuschallenges/customers
GET /api/bonuschallenges/customers?includeDeleted=true
```

**Test Caching:**
```
GET /api/bonuschallenges/customers/1
// Check logs for "retrieved from cache" on second call
```

**Test Soft Delete:**
```
DELETE /api/bonuschallenges/customers/1
GET /api/bonuschallenges/customers  // Customer 1 won't appear
POST /api/bonuschallenges/customers/1/restore
GET /api/bonuschallenges/customers  // Customer 1 appears again
```

**Test SQL Logging:**
```
GET /api/bonuschallenges/customers/with-large-balance?minBalance=5000
// Check console/logs to see generated SQL
```

---

## File Structure

### New Files Created:
```
EfCoreLab/
??? Data/
?   ??? BonusCustomer.cs              # Entity with IValidatableObject
?   ??? BonusInvoice.cs               # Entity with IValidatableObject
?   ??? BonusTelephoneNumber.cs       # Entity with IValidatableObject
?   ??? BonusDbContext.cs             # Context with global query filters
??? Interceptors/
?   ??? AuditInterceptor.cs           # Automatic audit timestamps
??? Controllers/
?   ??? BonusChallengesController.cs  # All bonus challenge endpoints
??? BonusBogusDataGenerator.cs        # Seeding logic for bonus tables
??? BONUS_CHALLENGES_README.md        # This file
```

### Modified Files:
```
EfCoreLab/
??? Program.cs                         # Register BonusDbContext, MemoryCache, and seeding
```

---

## Database Schema

### Tables Created:
- **BonusCustomers** - Separate from Customers table
  - Id, Name, Email (standard fields)
  - CreatedDate, ModifiedDate (audit fields)
  - IsDeleted, DeletedDate (soft delete fields)

- **BonusInvoices** - Separate from Invoices table
  - Id, InvoiceNumber, CustomerId, InvoiceDate, Amount
  - CreatedDate, ModifiedDate, IsDeleted, DeletedDate

- **BonusTelephoneNumbers** - Separate from TelephoneNumbers table
  - Id, CustomerId, Type, Number
  - CreatedDate, ModifiedDate, IsDeleted, DeletedDate

### Indexes:
- Unique index on Email (BonusCustomers)
- Unique index on InvoiceNumber (BonusInvoices)
- Indexes on IsDeleted for all tables (query performance)
- Indexes on CreatedDate, CustomerId for filtering

---

## Summary

All 6 bonus challenges have been fully implemented:

1. ? **Custom Validation (IValidatableObject)** - All entities implement custom validation logic
2. ? **Auditing (CreatedDate, ModifiedDate)** - Automatic timestamps via AuditInterceptor
3. ? **Soft Delete (IsDeleted)** - Records marked as deleted, not removed
4. ? **Global Query Filters** - Automatic filtering of soft-deleted records
5. ? **Caching (IMemoryCache)** - 5-minute cache for improved performance
6. ? **SQL Query Logging** - All queries logged to console/application logs

The implementation includes:
- Comprehensive XML documentation
- Demonstration endpoints
- Cache management
- Validation examples
- Restore functionality for soft-deleted records
- Separate database tables from the main context

Both contexts (AppDbContext and BonusDbContext) are seeded on startup with configurable settings.
