# EF Core Examples for Beginners

This project contains comprehensive examples of Entity Framework Core patterns and features, designed to help developers new to EF Core learn best practices and common scenarios.

## Table of Contents

1. [Essential Patterns](#essential-patterns)
2. [Advanced Queries](#advanced-queries)
3. [Performance Optimization](#performance-optimization)
4. [Data Operations](#data-operations)
5. [Quick Reference](#quick-reference)

---

## Essential Patterns

### 1. Pagination (Skip/Take)

**Endpoint:** `GET /api/customers/paginated?page=1&pageSize=10`

**Why it matters:** Essential for handling large datasets. Loading 10,000 customers at once will crash browsers and waste bandwidth.

**Key concepts:**
- Always use `OrderBy()` before `Skip/Take` for consistent results
- Return metadata (total count, page number, etc.) for UI
- Limit maximum page size to prevent abuse

**Example:**
```csharp
// Get page 2 with 10 items
var query = context.Customers
    .OrderBy(c => c.Name)        // Required for consistent paging
    .Skip((2 - 1) * 10)          // Skip first 10 items
    .Take(10);                   // Take next 10 items
```

**SQL Generated:**
```sql
SELECT * FROM Customers 
ORDER BY Name 
OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY
```

**Try it:**
```powershell
# Get first page
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/paginated?page=1&pageSize=10"

# Get specific page
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/paginated?page=5&pageSize=20"
```

---

### 2. Filtering and Search

**Endpoint:** `GET /api/customers/search?name=acme&email=@gmail.com&minBalance=1000`

**Why it matters:** Users need to find specific data quickly. Dynamic filtering allows flexible search combinations.

**Key concepts:**
- Build queries conditionally (only apply filters that are provided)
- Use `Contains()` for partial matches (SQL LIKE)
- All filters combined into single SQL WHERE clause

**Example:**
```csharp
var query = context.Customers.AsQueryable();

if (!string.IsNullOrEmpty(name))
    query = query.Where(c => c.Name.Contains(name));

if (!string.IsNullOrEmpty(email))
    query = query.Where(c => c.Email.Contains(email));

// Execute only once with all filters
var results = await query.ToListAsync();
```

**SQL Generated:**
```sql
SELECT * FROM Customers 
WHERE Name LIKE '%acme%' 
  AND Email LIKE '%@gmail.com%'
  AND (SELECT SUM(Amount) FROM Invoices WHERE CustomerId = Customers.Id) >= 1000
```

**Try it:**
```powershell
# Search by name only
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/search?name=corp"

# Combine multiple filters
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/search?name=acme&minBalance=5000"
```

---

### 3. Sorting (OrderBy/ThenBy)

**Endpoint:** `GET /api/customers/sorted?sortBy=email&descending=true`

**Why it matters:** Users want to view data in different orders (A-Z, newest first, highest value, etc.).

**Key concepts:**
- Dynamic sorting based on query parameters
- Support ascending and descending
- Use switch expressions for clean code

**Example:**
```csharp
var query = context.Customers.AsQueryable();

query = sortBy.ToLower() switch
{
    "email" => descending ? query.OrderByDescending(c => c.Email) 
                          : query.OrderBy(c => c.Email),
    "balance" => descending ? query.OrderByDescending(c => c.Invoices.Sum(i => i.Amount))
                            : query.OrderBy(c => c.Invoices.Sum(i => i.Amount)),
    _ => descending ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name)
};
```

**Try it:**
```powershell
# Sort by name A-Z (default)
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/sorted"

# Sort by email Z-A
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/sorted?sortBy=email&descending=true"

# Sort by balance (low to high)
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/sorted?sortBy=balance"
```

---

## Advanced Queries

### 4. Split Queries (Cartesian Explosion)

**Endpoint:** `GET /api/customers/with-split-queries`

**Why it matters:** Loading multiple collections with `Include()` causes data duplication. Split queries solve this.

**The Problem:**
```csharp
// Default: Single query with JOINs
var customers = context.Customers
    .Include(c => c.Invoices)           // 5 invoices per customer
    .Include(c => c.PhoneNumbers)       // 3 phones per customer
    .ToListAsync();
// Returns 15 rows per customer (5  3)! Customer data duplicated 15 times!
```

**The Solution:**
```csharp
// Split queries: Separate query for each collection
var customers = context.Customers
    .AsSplitQuery()                     // Key difference
    .Include(c => c.Invoices)
    .Include(c => c.PhoneNumbers)
    .ToListAsync();
// Returns: 1000 + 5000 + 3000 = 9000 rows total, no duplication
```

**Performance comparison (1000 customers):**
- Single query: 15,000 rows, ~6 MB data transfer
- Split queries: 9,000 rows, ~1.5 MB data transfer
- **Result: 75% less data!**

**Try it:**
```powershell
# Compare file sizes
Invoke-RestMethod -Uri "http://localhost:5000/api/customers?includeRelated=true" | ConvertTo-Json | Measure-Object -Character
Invoke-RestMethod -Uri "http://localhost:5000/api/customers/with-split-queries" | ConvertTo-Json | Measure-Object -Character
```

---

### 5. Projection (Select for Efficiency)

**Endpoint:** `GET /api/advancedexamples/customer-summary`

**Why it matters:** Don't load full entities when you only need a few fields. Can be 10x faster.

**Key concepts:**
- Use `Select()` to shape data before loading
- Aggregations (Count, Sum, Max) calculated in SQL
- No change tracking overhead
- Much less data transferred

**Example:**
```csharp
// BAD: Loading everything, calculating in memory
var customers = context.Customers
    .Include(c => c.Invoices)
    .ToList();
var summary = customers.Select(c => new {
    c.Name,
    InvoiceCount = c.Invoices.Count,      // Calculated in C#
    TotalAmount = c.Invoices.Sum(i => i.Amount)  // Calculated in C#
});

// GOOD: Projection, calculated in SQL
var summary = context.Customers
    .Select(c => new CustomerSummaryDto {
        Name = c.Name,
        InvoiceCount = c.Invoices.Count,      // Calculated in SQL
        TotalAmount = c.Invoices.Sum(i => i.Amount)  // Calculated in SQL
    })
    .ToList();
```

**Performance comparison (1000 customers):**
- Without projection: 10 MB data, 500ms, high memory
- With projection: 50 KB data, 50ms, low memory
- **Result: 10x faster, 200x less data!**

**Try it:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/advancedexamples/customer-summary"
```

---

## Performance Optimization

### 6. AsNoTracking (Read-Only Queries)

**Endpoint:** `GET /api/advancedexamples/no-tracking-demo`

**Why it matters:** Change tracking has overhead. Disable it for read-only scenarios.

**What is change tracking?**
- EF creates a "snapshot" of each entity when loaded
- On `SaveChanges()`, compares current vs snapshot
- Generates UPDATE statements for changed properties
- **Overhead:** Memory + CPU

**When to use AsNoTracking:**
- [OK] GET endpoints (read-only)
- [OK] Reports and exports
- [OK] List views
- [OK] Any scenario where you won't update entities

**When NOT to use:**
- [X] POST/PUT/DELETE operations
- [X] When you need to call SaveChanges()

**Example:**
```csharp
// Default: Change tracking enabled
var customers = await context.Customers.ToListAsync();
// EF tracks all entities, uses memory for snapshots

// Optimized: No change tracking
var customers = await context.Customers
    .AsNoTracking()
    .ToListAsync();
// Faster, less memory, read-only
```

**Performance gain:**
- 10-30% faster query execution
- 50% less memory usage
- No tracking overhead

**Try it:**
```powershell
# See performance comparison
Invoke-RestMethod -Uri "http://localhost:5000/api/advancedexamples/no-tracking-demo"
```

---

### 7. GroupBy and Aggregations

**Endpoint:** `GET /api/advancedexamples/invoice-statistics`

**Why it matters:** Reports and dashboards need summarized data, not individual records.

**Key concepts:**
- All calculations done in SQL, not in memory
- Returns few rows (groups) instead of many (individual records)
- Very fast even with millions of records

**Available aggregations:**
- `Count()` - Number of items
- `Sum()` - Total of values
- `Average()` - Mean
- `Min()` - Smallest value
- `Max()` - Largest value

**Example:**
```csharp
var stats = context.Invoices
    .GroupBy(i => i.InvoiceDate.Year)
    .Select(g => new {
        Year = g.Key,
        TotalRevenue = g.Sum(i => i.Amount),
        InvoiceCount = g.Count(),
        AverageAmount = g.Average(i => i.Amount)
    })
    .ToList();
```

**SQL Generated:**
```sql
SELECT 
  YEAR(InvoiceDate) AS Year,
  SUM(Amount) AS TotalRevenue,
  COUNT(*) AS InvoiceCount,
  AVG(Amount) AS AverageAmount
FROM Invoices
GROUP BY YEAR(InvoiceDate)
```

**Try it:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/advancedexamples/invoice-statistics"
```

---

## Data Operations

### 8. Transactions (Atomic Operations)

**Endpoint:** `POST /api/advancedexamples/transfer-invoices?fromCustomerId=1&toCustomerId=2`

**Why it matters:** Ensure data consistency when multiple operations must succeed together.

**What is a transaction?**
A transaction ensures that either:
- [OK] ALL operations succeed (commit)
- [X] ALL operations fail (rollback)

There's no "partial success" - data stays consistent.

**ACID properties:**
- **A**tomicity: All or nothing
- **C**onsistency: Database rules enforced
- **I**solation: Transactions don't interfere
- **D**urability: Committed changes are permanent

**Example:**
```csharp
using var transaction = await context.Database.BeginTransactionAsync();
try
{
    // Step 1: Update invoice 1
    // Step 2: Update invoice 2
    // Step 3: Update invoice 3
    await context.SaveChangesAsync();
    
    // All succeeded - commit
    await transaction.CommitAsync();
}
catch
{
    // Something failed - rollback ALL changes
    await transaction.RollbackAsync();
    throw;
}
```

**Use cases:**
- Financial operations (transfers, payments)
- Multi-step business logic
- Operations that must succeed together
- Complex data updates

**Try it:**
```powershell
# Transfer invoices between customers
Invoke-RestMethod -Uri "http://localhost:5000/api/advancedexamples/transfer-invoices?fromCustomerId=1&toCustomerId=2" -Method POST
```

---

### 9. Explicit Loading

**Endpoint:** `GET /api/advancedexamples/explicit-loading/{id}`

**Why it matters:** Load related data on-demand, only what you need, when you need it.

**Loading strategies comparison:**

| Strategy | When Loaded | Queries | Use Case |
|----------|-------------|---------|----------|
| **Eager (Include)** | Upfront | 1 or split | Always need related data |
| **Lazy** | On access | N+1 (bad!) | Rarely, dangerous |
| **Explicit** | On demand | Controlled | Conditional loading |

**Example:**
```csharp
// Load customer without relations
var customer = await context.Customers.FindAsync(id);

// Later, explicitly load invoices
await context.Entry(customer)
    .Collection(c => c.Invoices)
    .LoadAsync();

// Load FILTERED phone numbers
await context.Entry(customer)
    .Collection(c => c.PhoneNumbers)
    .Query()
    .Where(p => p.Type == "Mobile")  // Only mobile phones
    .LoadAsync();
```

**Benefits:**
- Load only what you need
- Apply filters to related data
- Conditional loading based on business logic
- Avoid loading unused data

**Try it:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/advancedexamples/explicit-loading/1"
```

---

### 10. Raw SQL Queries

**Endpoint:** `GET /api/advancedexamples/raw-sql-demo?emailDomain=acme.com`

**Why it matters:** Sometimes LINQ is too complex or generates inefficient SQL. Use raw SQL when needed.

**When to use raw SQL:**
- [OK] Complex queries hard to express in LINQ
- [OK] Performance optimization (LINQ SQL is suboptimal)
- [OK] Database-specific features (window functions, CTEs)
- [OK] Stored procedures
- [OK] Bulk operations

**Security WARNING:**
```csharp
// DANGEROUS - SQL Injection vulnerability!
var sql = $"SELECT * FROM Users WHERE Name = '{userInput}'";

// SAFE - Parameterized query
var safe = context.Customers
    .FromSqlInterpolated($"SELECT * FROM Customers WHERE Name = {userInput}")
    .ToList();
```

**Example:**
```csharp
// Method 1: FromSqlRaw with parameters
var customers = context.Customers
    .FromSqlRaw("SELECT * FROM Customers WHERE Email LIKE {0}", "%@acme.com")
    .ToList();

// Method 2: FromSqlInterpolated (recommended)
var customers = context.Customers
    .FromSqlInterpolated($"SELECT * FROM Customers WHERE Email LIKE {searchTerm}")
    .ToList();

// Can chain LINQ operations
var filtered = context.Customers
    .FromSqlRaw("SELECT * FROM Customers WHERE Email LIKE '%@acme.com'")
    .Where(c => c.Name.Contains("Corp"))  // Additional LINQ filter
    .OrderBy(c => c.Name)
    .Take(10)
    .ToList();
```

**Try it:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/advancedexamples/raw-sql-demo?emailDomain=gmail.com"
```

---

## Quick Reference

### Performance Tips

| Scenario | Use | Don't Use | Why |
|----------|-----|-----------|-----|
| List views | `AsNoTracking()` | Default tracking | 10-30% faster |
| Reports | `Select()` projection | Full entities | 10x less data |
| Multiple collections | `AsSplitQuery()` | Default joins | Avoid cartesian explosion |
| Searching | `Where()` in SQL | `Where()` in memory | Database is faster |
| Counting | `Count()` | `ToList().Count` | Don't load data to count |

### Common Patterns

```csharp
// Pagination
query.OrderBy(c => c.Name).Skip(skip).Take(take)

// Search
query.Where(c => c.Name.Contains(search))

// Sorting
query.OrderBy(c => c.Name)
query.OrderByDescending(c => c.Amount)

// Aggregation
query.GroupBy(c => c.Year).Select(g => new { Year = g.Key, Total = g.Sum() })

// Projection
query.Select(c => new { c.Name, Count = c.Invoices.Count })

// No tracking
query.AsNoTracking()

// Split queries
query.AsSplitQuery().Include(c => c.Invoices).Include(c => c.PhoneNumbers)

// Explicit loading
context.Entry(entity).Collection(e => e.Items).Load()
```

### SQL Translation

| LINQ | SQL |
|------|-----|
| `Where(c => c.Name == "Acme")` | `WHERE Name = 'Acme'` |
| `Where(c => c.Name.Contains("Acme"))` | `WHERE Name LIKE '%Acme%'` |
| `OrderBy(c => c.Name)` | `ORDER BY Name ASC` |
| `OrderByDescending(c => c.Name)` | `ORDER BY Name DESC` |
| `Skip(10).Take(10)` | `OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY` |
| `Count()` | `COUNT(*)` |
| `Sum(i => i.Amount)` | `SUM(Amount)` |
| `Average(i => i.Amount)` | `AVG(Amount)` |
| `Max(i => i.Amount)` | `MAX(Amount)` |
| `GroupBy(c => c.Year)` | `GROUP BY Year` |

---

## Learning Path

### Beginner (Start Here)
1. **Pagination** - Most practical, immediately useful
2. **Filtering** - Common requirement
3. **Sorting** - Natural extension
4. **AsNoTracking** - Easy performance win

### Intermediate
5. **Projection** - Teaches efficient querying
6. **Split Queries** - Solves common problem
7. **Aggregations** - For reports
8. **Transactions** - Data integrity

### Advanced
9. **Explicit Loading** - Fine-grained control
10. **Raw SQL** - When LINQ isn't enough

---

## Additional Resources

### Documentation
- [EF Core Documentation](https://docs.microsoft.com/ef/core/)
- [Query Performance](https://docs.microsoft.com/ef/core/performance/)
- [Change Tracking](https://docs.microsoft.com/ef/core/change-tracking/)

### Tools
- [LINQPad](https://www.linqpad.net/) - Test LINQ queries
- [EF Core Power Tools](https://marketplace.visualstudio.com/items?itemName=ErikEJ.EFCorePowerTools) - Reverse engineering
- [SQL Server Profiler](https://docs.microsoft.com/sql/tools/sql-server-profiler/) - See generated SQL

### Tips for Learning
1. Run examples and examine results
2. Check console output to see generated SQL
3. Use profiler to understand query performance
4. Try modifying examples to see what happens
5. Start simple, add complexity gradually

---

## Testing All Examples

```powershell
$baseUrl = "http://localhost:5000"

# Essential patterns
Invoke-RestMethod "$baseUrl/api/customers/paginated?page=1&pageSize=10"
Invoke-RestMethod "$baseUrl/api/customers/search?name=corp"
Invoke-RestMethod "$baseUrl/api/customers/sorted?sortBy=name"

# Advanced queries
Invoke-RestMethod "$baseUrl/api/customers/with-split-queries"
Invoke-RestMethod "$baseUrl/api/advancedexamples/customer-summary"
Invoke-RestMethod "$baseUrl/api/advancedexamples/invoice-statistics"

# Performance
Invoke-RestMethod "$baseUrl/api/advancedexamples/no-tracking-demo"

# Data operations
Invoke-RestMethod "$baseUrl/api/advancedexamples/explicit-loading/1"
Invoke-RestMethod "$baseUrl/api/advancedexamples/raw-sql-demo?emailDomain=acme.com"
Invoke-RestMethod "$baseUrl/api/advancedexamples/transfer-invoices?fromCustomerId=1&toCustomerId=2" -Method POST
```

--- 