# Split Queries Example - EF Core

## Overview

Split queries are an EF Core feature that helps avoid the **cartesian explosion problem** when loading multiple related collections. This example demonstrates the difference between single queries and split queries.

## The Problem: Cartesian Explosion

When using `Include()` to load multiple collections (like Invoices and PhoneNumbers for Customers), EF Core by default uses a single SQL query with JOINs. This can result in a cartesian product:

**Single Query (Default):**
```sql
SELECT c.*, i.*, p.*
FROM Customers c
LEFT JOIN Invoices i ON c.Id = i.CustomerId
LEFT JOIN TelephoneNumbers p ON c.Id = p.CustomerId
```

**Result:** If a customer has 5 invoices and 3 phone numbers, this returns **15 rows** (5  3) instead of just 1 customer record. The customer data is duplicated 15 times.

## The Solution: Split Queries

Using `AsSplitQuery()`, EF Core executes **separate queries** for each collection:

**Split Queries:**
```sql
-- Query 1: Get customers
SELECT * FROM Customers;

-- Query 2: Get invoices for those customers
SELECT * FROM Invoices WHERE CustomerId IN (1, 2, 3, ...);

-- Query 3: Get phone numbers for those customers
SELECT * FROM TelephoneNumbers WHERE CustomerId IN (1, 2, 3, ...);
```

**Result:** More efficient - no duplication, each table queried once.

## Implementation

### Repository Method
```csharp
public async Task<IEnumerable<Customer>> GetAllWithSplitQueriesAsync()
{
    var query = _context.Customers
        .AsSplitQuery()  // Key difference
        .Include(c => c.Invoices)
        .Include(c => c.PhoneNumbers);

    return await query.ToListAsync();
}
```

### Controller Endpoint
```csharp
[HttpGet("with-split-queries")]
public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAllWithSplitQueries()
{
    var customers = await _customerRepository.GetAllWithSplitQueriesAsync();
    return Ok(customers.Select(c => c.ToDto()));
}
```

## Usage

### Endpoint: GET /api/customers/with-split-queries

**Example Request:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/with-split-queries" -Method GET
```

**Example Response:**
```json
[
  {
    "id": 1,
    "name": "Acme Corporation",
    "email": "contact@acme.com",
    "balance": 5250.50,
    "invoices": [
      {
        "id": 101,
        "invoiceNumber": "INV-ABC123",
        "customerId": 1,
        "invoiceDate": "2024-01-15T00:00:00",
        "amount": 1250.50
      },
      {
        "id": 102,
        "invoiceNumber": "INV-DEF456",
        "customerId": 1,
        "invoiceDate": "2024-02-20T00:00:00",
        "amount": 4000.00
      }
    ],
    "phoneNumbers": [
      {
        "id": 201,
        "customerId": 1,
        "type": "Mobile",
        "number": "+44 7700 900123"
      },
      {
        "id": 202,
        "customerId": 1,
        "type": "Work",
        "number": "+44 20 1234 5678"
      }
    ]
  }
]
```

## Comparison

### Regular Query (Default)
```csharp
// Uses single query with JOINs
var customers = await _context.Customers
    .Include(c => c.Invoices)
    .Include(c => c.PhoneNumbers)
    .ToListAsync();
```

**SQL Generated:**
```sql
SELECT [c].[Id], [c].[Name], [c].[Email], 
       [i].[Id], [i].[InvoiceNumber], [i].[Amount], [i].[CustomerId],
       [p].[Id], [p].[Number], [p].[Type], [p].[CustomerId]
FROM [Customers] AS [c]
LEFT JOIN [Invoices] AS [i] ON [c].[Id] = [i].[CustomerId]
LEFT JOIN [TelephoneNumbers] AS [p] ON [c].[Id] = [p].[CustomerId]
ORDER BY [c].[Id], [i].[Id]
```

**Result Size:** Customer data duplicated for every invoice  phone number combination

### Split Query
```csharp
// Uses multiple separate queries
var customers = await _context.Customers
    .AsSplitQuery()  // Add this
    .Include(c => c.Invoices)
    .Include(c => c.PhoneNumbers)
    .ToListAsync();
```

**SQL Generated (3 queries):**
```sql
-- Query 1
SELECT [c].[Id], [c].[Name], [c].[Email]
FROM [Customers] AS [c];

-- Query 2
SELECT [i].[Id], [i].[InvoiceNumber], [i].[Amount], [i].[CustomerId]
FROM [Invoices] AS [i]
WHERE [i].[CustomerId] IN (SELECT [c].[Id] FROM [Customers] AS [c]);

-- Query 3
SELECT [p].[Id], [p].[Number], [p].[Type], [p].[CustomerId]
FROM [TelephoneNumbers] AS [p]
WHERE [p].[CustomerId] IN (SELECT [c].[Id] FROM [Customers] AS [c]);
```

**Result Size:** Minimal - no duplication

## Performance Comparison

### Example Scenario:
- 1000 customers
- Average 3 invoices per customer
- Average 2 phone numbers per customer

**Single Query:**
- Rows returned: 1000  3  2 = **6,000 rows**
- Customer data duplicated 6 times per customer
- Data transfer: ~6 MB (assuming 1KB per row)

**Split Query:**
- Query 1: 1000 rows (customers)
- Query 2: 3000 rows (invoices)
- Query 3: 2000 rows (phone numbers)
- Total: **6,000 rows** but no duplication
- Data transfer: ~1.5 MB (no duplicate customer data)

**Benefit:** ~75% reduction in data transfer, faster JSON serialization

## When to Use Split Queries

### Use Split Queries When:
1. Loading **multiple collections** (2+ `Include()` statements)
2. Collections are **large** (many related records per parent)
3. **Performance** is critical
4. Working with **large datasets** (1000+ parent records)

### Don't Use Split Queries When:
1. Loading only **one collection** (no cartesian explosion)
2. Small datasets (< 100 records)
3. Collections are typically **empty or small** (< 3 items)
4. You need **consistency** across all queries in a transaction

## Global Configuration

You can make split queries the default for all queries:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
           .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
```

Then use `.AsSingleQuery()` when you explicitly want a single query.

## Viewing Generated SQL

To see the actual SQL generated:

```csharp
// Enable sensitive data logging
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine, LogLevel.Information));
```

Or check the console output when running the application - EF Core logs SQL queries at Information level.

## Testing

### Test Regular Query:
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/customers?includeRelated=true" -Method GET
```

### Test Split Query:
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/with-split-queries" -Method GET
```

### Compare Results:
Both return the same data, but split queries are more efficient for large datasets.

## Summary

- **Split Queries**: Use `.AsSplitQuery()` to execute separate SQL queries for each collection
- **Benefits**: Avoid cartesian explosion, reduce data duplication, improve performance
- **Trade-off**: Multiple round-trips to database (usually negligible with modern networks)
- **Best Practice**: Use for loading multiple large collections
- **New Endpoint**: `GET /api/customers/with-split-queries`

The example is now implemented and ready to use!
