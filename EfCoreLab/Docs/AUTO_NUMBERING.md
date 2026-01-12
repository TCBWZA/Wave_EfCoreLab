# Auto-Numbering Implementation Summary

## Overview
All entity IDs (Customer, Invoice, TelephoneNumber) are now configured as database-generated identity columns. The database automatically assigns sequential IDs when new records are inserted.

## Changes Made

### 1. AppDbContext.cs
- Added `ValueGeneratedOnAdd()` configuration for all ID properties
- This tells EF Core that the database will generate the ID values

```csharp
b.Property(c => c.Id).ValueGeneratedOnAdd();
```

### 2. Entity Classes (Customer.cs, Invoice.cs, TelephoneNumber.cs)
- Removed `[Range(0, long.MaxValue)]` validation attributes from ID properties
- Added `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` attribute

### 3. Bogus.cs Data Generator
- Updated `SeedDatabase` method to:
  - Save customers first to get their database-generated IDs
  - Use those IDs when creating related invoices and phone numbers

### 4. Flow of ID Generation

#### When Creating via API:
1. Client sends POST request with CreateDto (no ID field)
2. Controller maps DTO to entity (ID is 0 or default)
3. Repository adds entity to context
4. `SaveChangesAsync()` is called
5. **Database generates and assigns the ID**
6. EF Core updates the entity object with the new ID
7. Repository returns the entity with populated ID
8. Controller maps to DTO and returns to client with ID

#### Example Flow:
```
Client Request (POST /api/customers):
{
  "name": "Acme Corp",
  "email": "acme@example.com"
}

--> (CreateCustomerDto -> Customer entity, Id = 0)

--> (Add to DbContext)

--> (SaveChangesAsync - Database generates Id = 42)

--> (EF Core updates entity, Id = 42)

Client Response (201 Created):
{
  "id": 42,
  "name": "Acme Corp",
  "email": "acme@example.com",
  "balance": 0
}
Location: /api/customers/42
```

### 5. Benefits

[YES] **Database Integrity**: ID generation is handled by SQL Server, ensuring uniqueness and consistency
[YES] **Concurrency Safe**: No race conditions from application-level counters
[YES] **Standard Practice**: Follows EF Core conventions and best practices
[YES] **Automatic**: No manual ID management required
[YES] **Scalable**: Works across multiple application instances
[YES] **Transactional**: ID assignment is part of the database transaction

### 6. Database Schema

The database will create IDENTITY columns:
```sql
CREATE TABLE Customers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200),
    Email NVARCHAR(200)
);

CREATE TABLE Invoices (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    CustomerId BIGINT NOT NULL,
    InvoiceDate DATETIME2 NOT NULL,
    Amount DECIMAL(18,2) NOT NULL
);

CREATE TABLE TelephoneNumbers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CustomerId BIGINT NOT NULL,
    Type NVARCHAR(20),
    Number NVARCHAR(50)
);
```

## Testing

To test the auto-numbering:

1. Run the application: `dotnet run`
2. Use Swagger UI or Postman to POST a new customer
3. Observe the response includes the generated ID
4. Create multiple records and verify IDs increment properly

## Notes

- IDs start at 1 by default
- IDs are sequential but may have gaps if transactions are rolled back
- For seeding, customers must be saved first to get IDs before creating related records
- The `CreateDto` classes do not include ID fields
- The response DTOs include the ID populated by the database
