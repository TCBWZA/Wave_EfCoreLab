using EfCoreLab.Data;
using EfCoreLab.DTOs;
using EfCoreLab.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfCoreLab.Controllers
{
    /// <summary>
    /// Advanced EF Core examples for learning query patterns and optimization techniques.
    /// These endpoints demonstrate various EF Core features that are important for
    /// building efficient, scalable applications.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AdvancedExamplesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<AdvancedExamplesController> _logger;

        public AdvancedExamplesController(
            AppDbContext context,
            ICustomerRepository customerRepository,
            ILogger<AdvancedExamplesController> logger)
        {
            _context = context;
            _customerRepository = customerRepository;
            _logger = logger;
        }

        /// <summary>
        /// EXAMPLE: Projection with Select() for efficient queries
        /// 
        /// GET /api/advancedexamples/customer-summary
        /// 
        /// Demonstrates using Select() to project data instead of loading full entities.
        /// This is MUCH more efficient than loading entities and mapping in memory.
        /// 
        /// Key Benefits:
        /// 1. Only selected columns retrieved from database (not entire rows)
        /// 2. Aggregations (Count, Sum, Max) calculated in SQL, not in memory
        /// 3. No change tracking overhead (projection returns anonymous types/DTOs)
        /// 4. Significantly less data transferred from database
        /// 
        /// Performance comparison for 1000 customers:
        /// 
        /// WITHOUT Projection (loading full entities):
        /// - Loads: Customer + all Invoices + all PhoneNumbers
        /// - Data transfer: ~5-10 MB
        /// - Memory: High (all entities tracked)
        /// - Time: ~500ms
        /// 
        /// WITH Projection (this example):
        /// - Loads: Only Id, Name, Email + 3 calculated values
        /// - Data transfer: ~50 KB
        /// - Memory: Low (no tracking)
        /// - Time: ~50ms
        /// 
        /// SQL Generated:
        /// SELECT 
        ///   c.Id,
        ///   c.Name, 
        ///   c.Email,
        ///   COUNT(i.Id) AS InvoiceCount,
        ///   SUM(i.Amount) AS TotalAmount,
        ///   MAX(i.InvoiceDate) AS LastInvoiceDate
        /// FROM Customers c
        /// LEFT JOIN Invoices i ON c.Id = i.CustomerId
        /// GROUP BY c.Id, c.Name, c.Email
        /// 
        /// When to use:
        /// - List views (don't need all entity data)
        /// - Reports and dashboards
        /// - API responses (shape data for client needs)
        /// - Any read-only scenario where you don't need full entities
        /// 
        /// Best practice: Always use projection for list views and reports.
        /// </summary>
        [HttpGet("customer-summary")]
        public async Task<ActionResult<IEnumerable<CustomerSummaryDto>>> GetCustomerSummary()
        {
            var summaries = await _context.Customers
                .Select(c => new CustomerSummaryDto
                {
                    Id = c.Id,
                    Name = c.Name ?? string.Empty,
                    Email = c.Email ?? string.Empty,
                    
                    // COUNT calculated in database, not in memory
                    // SQL: COUNT(i.Id)
                    InvoiceCount = c.Invoices.Count,
                    
                    // SUM calculated in database
                    // SQL: SUM(i.Amount)
                    TotalAmount = c.Invoices.Sum(i => i.Amount),
                    
                    // MAX calculated in database, returns NULL if no invoices
                    // SQL: MAX(i.InvoiceDate)
                    LastInvoiceDate = c.Invoices.Max(i => (DateTime?)i.InvoiceDate)
                })
                .ToListAsync();

            return Ok(summaries);
        }

        /// <summary>
        /// EXAMPLE: AsNoTracking() for read-only performance
        /// 
        /// GET /api/advancedexamples/no-tracking-demo
        /// 
        /// Compares query performance with and without change tracking.
        /// Change tracking is EF Core's mechanism for detecting changes to entities
        /// so they can be saved back to the database.
        /// 
        /// What is Change Tracking?
        /// - EF Core creates a "snapshot" of each entity when loaded
        /// - On SaveChanges(), compares current values to snapshot
        /// - Generates UPDATE statements for changed properties
        /// - Required for: Update, Delete operations
        /// - Overhead: Memory + CPU for tracking
        /// 
        /// AsNoTracking() Benefits:
        /// - No snapshots created (saves memory)
        /// - Faster query execution (10-30% improvement)
        /// - Better for: Reports, list views, exports, APIs
        /// 
        /// Performance Comparison (1000 customers):
        /// 
        /// WITH Tracking (default):
        /// - Memory: ~10 MB (entities + snapshots)
        /// - Time: ~150ms
        /// - Use when: You need to update entities
        /// 
        /// WITHOUT Tracking (AsNoTracking):
        /// - Memory: ~5 MB (entities only, no snapshots)
        /// - Time: ~100ms  
        /// - Use when: Read-only scenarios
        /// 
        /// WARNING: Do NOT use AsNoTracking() if you plan to:
        /// - Update the entities (will fail or require re-attaching)
        /// - Delete the entities (will fail or require re-attaching)
        /// - Call SaveChanges() with these entities
        /// 
        /// Rule of thumb:
        /// - GET endpoints (read-only) ? Use AsNoTracking()
        /// - POST/PUT/DELETE endpoints ? Need tracking (don't use AsNoTracking)
        /// </summary>
        [HttpGet("no-tracking-demo")]
        public async Task<ActionResult> NoTrackingDemo()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Test 1: WITH change tracking (default)
            // EF creates snapshots for all entities
            stopwatch.Restart();
            var customersTracked = await _context.Customers
                .Include(c => c.Invoices)
                .Include(c => c.PhoneNumbers)
                .ToListAsync();
            var trackedTime = stopwatch.ElapsedMilliseconds;
            
            // These entities are "tracked" - EF monitors changes
            var trackedState = _context.ChangeTracker.Entries().Count();

            // Clear context to reset for next test
            _context.ChangeTracker.Clear();

            // Test 2: WITHOUT change tracking
            // No snapshots created - better performance
            stopwatch.Restart();
            var customersNoTracking = await _context.Customers
                .AsNoTracking()  // Key difference - disables change tracking
                .Include(c => c.Invoices)
                .Include(c => c.PhoneNumbers)
                .ToListAsync();
            var noTrackingTime = stopwatch.ElapsedMilliseconds;
            
            // These entities are NOT tracked
            var noTrackingState = _context.ChangeTracker.Entries().Count();

            return Ok(new
            {
                ResultsCount = customersTracked.Count,
                
                // Tracking metrics
                TrackedQuery = new
                {
                    TimeMs = trackedTime,
                    EntitiesTracked = trackedState,
                    Description = "Default behavior - entities are tracked for changes"
                },
                
                // No-tracking metrics
                NoTrackingQuery = new
                {
                    TimeMs = noTrackingTime,
                    EntitiesTracked = noTrackingState,
                    Description = "AsNoTracking - better performance, read-only"
                },
                
                // Performance improvement
                PerformanceGain = new
                {
                    TimeReduction = $"{trackedTime - noTrackingTime}ms",
                    PercentFaster = trackedTime > 0 
                        ? $"{((trackedTime - noTrackingTime) / (double)trackedTime * 100):F1}%"
                        : "0%"
                },
                
                Recommendation = "Use AsNoTracking() for all read-only queries (GET endpoints)"
            });
        }

        /// <summary>
        /// EXAMPLE: GroupBy and Aggregations
        /// 
        /// GET /api/advancedexamples/invoice-statistics
        /// 
        /// Demonstrates SQL aggregation functions using LINQ GroupBy.
        /// All calculations performed in database, not in application memory.
        /// 
        /// This example groups invoices by year and calculates statistics.
        /// Essential for reporting, dashboards, and analytics.
        /// 
        /// SQL Generated:
        /// SELECT 
        ///   YEAR(i.InvoiceDate) AS Year,
        ///   SUM(i.Amount) AS TotalRevenue,
        ///   COUNT(*) AS InvoiceCount,
        ///   AVG(i.Amount) AS AverageAmount,
        ///   MIN(i.Amount) AS MinAmount,
        ///   MAX(i.Amount) AS MaxAmount
        /// FROM Invoices i
        /// GROUP BY YEAR(i.InvoiceDate)
        /// ORDER BY YEAR(i.InvoiceDate)
        /// 
        /// Available aggregation functions:
        /// - Count(): Number of items
        /// - Sum(): Total of numeric values
        /// - Average(): Mean of numeric values
        /// - Min(): Smallest value
        /// - Max(): Largest value
        /// 
        /// Performance note:
        /// For 10,000 invoices across 3 years:
        /// - Returns only 3 rows (one per year)
        /// - All calculations done in SQL Server
        /// - Very fast (~10ms) even with millions of records
        /// 
        /// Use cases:
        /// - Financial reports
        /// - Sales dashboards
        /// - Trend analysis
        /// - Executive summaries
        /// </summary>
        [HttpGet("invoice-statistics")]
        public async Task<ActionResult> GetInvoiceStatistics()
        {
            var statistics = await _context.Invoices
                .GroupBy(i => i.InvoiceDate.Year)  // Group by year
                .Select(g => new
                {
                    Year = g.Key,
                    
                    // SUM aggregation
                    TotalRevenue = g.Sum(i => i.Amount),
                    
                    // COUNT aggregation
                    InvoiceCount = g.Count(),
                    
                    // AVERAGE aggregation
                    AverageAmount = g.Average(i => i.Amount),
                    
                    // MIN aggregation
                    MinAmount = g.Min(i => i.Amount),
                    
                    // MAX aggregation
                    MaxAmount = g.Max(i => i.Amount)
                })
                .OrderBy(s => s.Year)  // Sort by year
                .ToListAsync();

            return Ok(statistics);
        }

        /// <summary>
        /// EXAMPLE: Transactions for atomic operations
        /// 
        /// POST /api/advancedexamples/transfer-invoices
        /// 
        /// Demonstrates using database transactions to ensure data consistency.
        /// A transaction ensures that either ALL operations succeed or ALL fail.
        /// 
        /// What is a Transaction?
        /// - A unit of work that must be atomic (all-or-nothing)
        /// - If any operation fails, ALL changes are rolled back
        /// - Database remains consistent even if errors occur
        /// 
        /// Transaction guarantees (ACID):
        /// - Atomicity: All operations succeed or all fail
        /// - Consistency: Database rules are enforced
        /// - Isolation: Concurrent transactions don't interfere
        /// - Durability: Committed changes are permanent
        /// 
        /// Example scenario:
        /// Transfer all invoices from one customer to another.
        /// 
        /// WITHOUT Transaction (BAD):
        /// 1. Update invoice 1 ✓
        /// 2. Update invoice 2 ✓
        /// 3. Error occurs ✗
        /// 4. Invoice 3 not updated
        /// Result: Partial update, data inconsistency!
        /// 
        /// WITH Transaction (GOOD):
        /// 1. Begin transaction
        /// 2. Update invoice 1 ✓
        /// 3. Update invoice 2 ✓
        /// 4. Error occurs ✗
        /// 5. Rollback - ALL changes undone
        /// Result: Data remains consistent!
        /// 
        /// When to use transactions:
        /// - Multiple related database operations
        /// - Operations that must succeed together
        /// - Financial operations (transfers, payments)
        /// - Complex business logic with multiple steps
        /// 
        /// Performance note:
        /// - Transactions have overhead (locking, logging)
        /// - Keep transactions short (minimize lock duration)
        /// - Don't call external APIs inside transactions
        /// </summary>
        [HttpPost("transfer-invoices")]
        public async Task<IActionResult> TransferInvoices(
            [FromQuery] long fromCustomerId, 
            [FromQuery] long toCustomerId)
        {
            // Validate input
            if (fromCustomerId == toCustomerId)
                return BadRequest("Cannot transfer to the same customer");

            // Begin transaction - creates a savepoint
            // If anything fails, we can rollback to this point
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Step 1: Verify target customer exists
                var toCustomer = await _context.Customers.FindAsync(toCustomerId);
                if (toCustomer == null)
                {
                    return NotFound($"Target customer {toCustomerId} not found");
                }

                // Step 2: Get all invoices for source customer
                var invoices = await _context.Invoices
                    .Where(i => i.CustomerId == fromCustomerId)
                    .ToListAsync();

                if (!invoices.Any())
                {
                    return NotFound($"No invoices found for customer {fromCustomerId}");
                }

                // Step 3: Update all invoice customer IDs
                // All these changes are part of the transaction
                foreach (var invoice in invoices)
                {
                    invoice.CustomerId = toCustomerId;
                }

                // Step 4: Save changes (still in transaction, not committed)
                await _context.SaveChangesAsync();

                // If we got here, everything succeeded
                // Commit the transaction - makes all changes permanent
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Transferred {Count} invoices from customer {From} to {To}",
                    invoices.Count, fromCustomerId, toCustomerId);

                return Ok(new
                {
                    Success = true,
                    InvoicesTransferred = invoices.Count,
                    FromCustomerId = fromCustomerId,
                    ToCustomerId = toCustomerId,
                    Message = "All invoices transferred successfully"
                });
            }
            catch (Exception ex)
            {
                // Something went wrong - rollback ALL changes
                // Database returns to state before BeginTransaction()
                await transaction.RollbackAsync();

                _logger.LogError(ex, 
                    "Failed to transfer invoices from customer {From} to {To}",
                    fromCustomerId, toCustomerId);

                return StatusCode(500, new
                {
                    Success = false,
                    Error = "Transaction failed and was rolled back",
                    Details = ex.Message
                });
            }
            // Transaction is automatically disposed here (finally block)
        }

        /// <summary>
        /// EXAMPLE: Explicit Loading for on-demand data
        /// 
        /// GET /api/advancedexamples/explicit-loading/{id}
        /// 
        /// Demonstrates loading related data on-demand after entity is loaded.
        /// Useful when you don't know upfront which relations you'll need.
        /// 
        /// Loading Strategies Comparison:
        /// 
        /// 1. Eager Loading (Include):
        ///    var customer = context.Customers.Include(c => c.Invoices).FirstOrDefault();
        ///    - Loads everything upfront
        ///    - Single database query (or split queries)
        ///    - Use when: You always need the related data
        /// 
        /// 2. Lazy Loading:
        ///    var customer = context.Customers.FirstOrDefault();
        ///    var invoices = customer.Invoices; // Loads now
        ///    - Loads on first access
        ///    - Multiple queries (N+1 problem risk)
        ///    - Requires proxies or lazy loading enabled
        ///    - Use when: Rarely, can cause performance issues
        /// 
        /// 3. Explicit Loading (this example):
        ///    var customer = context.Customers.FirstOrDefault();
        ///    context.Entry(customer).Collection(c => c.Invoices).Load();
        ///    - Load exactly what you need, when you need it
        ///    - Full control over queries
        ///    - Can apply filters to related data
        ///    - Use when: Conditional loading based on business logic
        /// 
        /// Benefits of Explicit Loading:
        /// - Load relations conditionally (if balance > 1000, load invoices)
        /// - Filter related data (load only recent invoices)
        /// - Optimize for specific scenarios
        /// - Avoid loading unused data
        /// 
        /// SQL Generated:
        /// Query 1: SELECT * FROM Customers WHERE Id = @p0
        /// Query 2: SELECT * FROM Invoices WHERE CustomerId = @p0  (if loaded)
        /// Query 3: SELECT * FROM TelephoneNumbers WHERE CustomerId = @p0 AND Type = 'Mobile'  (filtered)
        /// </summary>
        [HttpGet("explicit-loading/{id}")]
        public async Task<ActionResult> ExplicitLoadingDemo(long id)
        {
            // Step 1: Load customer WITHOUT related data
            // SQL: SELECT * FROM Customers WHERE Id = @id
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
                return NotFound();

            // At this point, Invoices and PhoneNumbers are null
            var initialState = new
            {
                CustomerLoaded = true,
                InvoicesLoaded = customer.Invoices != null,
                PhoneNumbersLoaded = customer.PhoneNumbers != null
            };

            // Step 2: Explicitly load invoices collection
            // SQL: SELECT * FROM Invoices WHERE CustomerId = @id
            await _context.Entry(customer)
                .Collection(c => c.Invoices)  // Target the Invoices collection
                .LoadAsync();

            // Step 3: Load FILTERED phone numbers (only Mobile)
            // SQL: SELECT * FROM TelephoneNumbers WHERE CustomerId = @id AND Type = 'Mobile'
            // This is powerful - you can filter related data!
            await _context.Entry(customer)
                .Collection(c => c.PhoneNumbers)
                .Query()  // Get IQueryable to apply filters
                .Where(p => p.Type == "Mobile")  // Only load mobile numbers
                .LoadAsync();

            var finalState = new
            {
                CustomerLoaded = true,
                InvoicesLoaded = customer.Invoices != null,
                InvoicesCount = customer.Invoices?.Count ?? 0,
                PhoneNumbersLoaded = customer.PhoneNumbers != null,
                PhoneNumbersCount = customer.PhoneNumbers?.Count ?? 0,
                Note = "Only mobile phone numbers were loaded"
            };

            return Ok(new
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                InitialState = initialState,
                FinalState = finalState,
                LoadingPattern = "Explicit Loading with Filtering",
                Explanation = new
                {
                    Step1 = "Loaded customer entity only",
                    Step2 = "Explicitly loaded all invoices",
                    Step3 = "Explicitly loaded filtered phone numbers (Mobile only)",
                    Benefit = "Full control over what data is loaded and when"
                }
            });
        }

        /// <summary>
        /// EXAMPLE: Raw SQL queries for complex scenarios
        /// 
        /// GET /api/advancedexamples/raw-sql-demo
        /// 
        /// Demonstrates executing raw SQL when LINQ is too complex or inefficient.
        /// 
        /// When to use Raw SQL:
        /// - Complex queries that are hard/impossible in LINQ
        /// - Performance optimization (LINQ generates suboptimal SQL)
        /// - Database-specific features (window functions, CTEs, etc.)
        /// - Stored procedures
        /// - Bulk operations
        /// 
        /// FromSqlRaw vs FromSqlInterpolated:
        /// 
        /// FromSqlRaw (this example):
        /// - Uses numbered parameters: {0}, {1}, {2}
        /// - Must manually specify parameters
        /// - Example: FromSqlRaw("SELECT * FROM Customers WHERE Id = {0}", id)
        /// 
        /// FromSqlInterpolated (recommended):
        /// - Uses string interpolation: $"... {variable} ..."
        /// - Automatically parameterized (SQL injection safe)
        /// - Example: FromSqlInterpolated($"SELECT * FROM Customers WHERE Id = {id}")
        /// 
        /// IMPORTANT Security Notes:
        /// 1. NEVER concatenate user input into SQL strings
        ///    BAD:  $"SELECT * FROM Users WHERE Name = '{userInput}'"  // SQL injection!
        ///    GOOD: FromSqlInterpolated($"SELECT * FROM Users WHERE Name = {userInput}")
        /// 
        /// 2. Always use parameterized queries
        ///    EF automatically parameterizes FromSqlInterpolated
        /// 
        /// 3. Be cautious with FromSqlRaw - easy to make mistakes
        /// 
        /// Limitations:
        /// - Query must return all columns of entity
        /// - Can't use for INSERT/UPDATE/DELETE (use ExecuteSqlRaw for that)
        /// - Still returns tracked entities (unless combined with AsNoTracking)
        /// 
        /// Advanced: You can chain LINQ after FromSqlRaw:
        /// context.Customers
        ///   .FromSqlRaw("SELECT * FROM Customers WHERE Email LIKE '%@acme.com'")
        ///   .Where(c => c.Name.Contains("Corp"))  // Additional filter in LINQ
        ///   .OrderBy(c => c.Name)
        ///   .ToListAsync();
        /// </summary>
        [HttpGet("raw-sql-demo")]
        public async Task<ActionResult> RawSqlDemo([FromQuery] string emailDomain = "acme.com")
        {
            // Example 1: Simple raw SQL query
            // Returns full Customer entities (all columns must be selected)
            var customersRaw = await _context.Customers
                .FromSqlRaw(
                    "SELECT * FROM Customers WHERE Email LIKE '%' + {0} + '%'",
                    emailDomain)
                .ToListAsync();

            // Example 2: Raw SQL with LINQ chaining
            // You can add additional LINQ operations after FromSqlRaw
            var customersChained = await _context.Customers
                .FromSqlRaw("SELECT * FROM Customers WHERE Email LIKE '%@acme.com'")
                .Where(c => c.Name != null)  // Additional LINQ filter
                .OrderBy(c => c.Name)        // LINQ ordering
                .Take(10)                    // LINQ pagination
                .ToListAsync();

            // Example 3: Using FromSqlInterpolated (safer, recommended)
            var searchTerm = $"%{emailDomain}%";
            var customersInterpolated = await _context.Customers
                .FromSqlInterpolated($"SELECT * FROM Customers WHERE Email LIKE {searchTerm}")
                .ToListAsync();

            return Ok(new
            {
                SearchTerm = emailDomain,
                Method1Results = customersRaw.Count,
                Method2Results = customersChained.Count,
                Method3Results = customersInterpolated.Count,
                
                Examples = new
                {
                    Method1 = "FromSqlRaw with numbered parameters {0}",
                    Method2 = "FromSqlRaw with LINQ chaining",
                    Method3 = "FromSqlInterpolated (recommended)"
                },
                
                SecurityWarning = "Always use parameterized queries. Never concatenate user input!",
                
                WhenToUse = new[]
                {
                    "Complex queries not expressible in LINQ",
                    "Performance optimization",
                    "Database-specific features",
                    "Calling stored procedures"
                }
            });
        }

        /// <summary>
        /// EXAMPLE: Customer Balance without Loading Invoices Collection
        /// 
        /// GET /api/advancedexamples/customer-balance-optimized/{id}
        /// 
        /// Demonstrates how to get a customer with a correct balance WITHOUT
        /// loading the entire Invoices collection (includeRelated = false).
        /// 
        /// THE PROBLEM:
        /// The Customer.Balance property is a calculated property:
        ///   public decimal Balance => Invoices?.Sum(i => i.Amount) ?? 0;
        /// 
        /// This requires the Invoices collection to be loaded in memory.
        /// If you query with includeRelated = false, the collection is empty,
        /// so Balance will be 0 even if invoices exist in the database!
        /// 
        /// BAD APPROACH (incorrect balance):
        /// var customer = await _customerRepository.GetByIdAsync(id, includeRelated: false);
        /// // customer.Balance is 0 because Invoices collection is not loaded!
        /// 
        /// SOLUTION 1: Use Projection (recommended)
        /// Don't load the Customer entity at all. Use Select() to project only
        /// the data you need, calculating balance in the database:
        /// 
        /// SQL Generated:
        /// SELECT 
        ///   c.Id, 
        ///   c.Name, 
        ///   c.Email,
        ///   COALESCE((SELECT SUM(i.Amount) FROM Invoices i WHERE i.CustomerId = c.Id), 0) AS Balance
        /// FROM Customers c
        /// WHERE c.Id = @id
        /// 
        /// Benefits:
        /// - Balance calculated in SQL (correct value)
        /// - No Invoices collection loaded (saves memory)
        /// - Single efficient query
        /// - No change tracking overhead
        /// - Much faster for large invoice collections
        /// 
        /// SOLUTION 2: Explicit Loading (alternative)
        /// Load customer first, then calculate balance using a separate query:
        /// 
        /// SQL Generated:
        /// Query 1: SELECT * FROM Customers WHERE Id = @id
        /// Query 2: SELECT SUM(Amount) FROM Invoices WHERE CustomerId = @id
        /// 
        /// This demonstrates both approaches side-by-side.
        /// 
        /// Performance comparison for customer with 1000 invoices:
        /// - With Include (includeRelated = true): Loads 1000+ invoice records
        /// - With Projection: Single aggregate query, returns 1 number
        /// - Speedup: 50-100x faster
        /// 
        /// Use cases:
        /// - List views showing customer balances
        /// - Reports and dashboards
        /// - API responses where you don't need full invoice details
        /// - Mobile apps (minimize data transfer)
        /// </summary>
        [HttpGet("customer-balance-optimized/{id}")]
        public async Task<ActionResult> GetCustomerBalanceOptimized(long id)
        {
            // SOLUTION 1: Projection (recommended)
            // Calculate balance in database without loading Invoices collection
            var customerProjection = await _context.Customers
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Email,
                    // Balance calculated in SQL - correct even without loading Invoices
                    Balance = c.Invoices.Sum(i => i.Amount),
                    InvoiceCount = c.Invoices.Count,
                    PhoneCount = c.PhoneNumbers.Count
                })
                .FirstOrDefaultAsync();

            if (customerProjection == null)
                return NotFound($"Customer with ID {id} not found");

            // SOLUTION 2: Explicit Loading for Balance Calculation (alternative)
            // Load customer without relations, then calculate balance separately
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
                return NotFound($"Customer with ID {id} not found");

            // Calculate balance with a separate aggregate query
            // SQL: SELECT SUM(Amount) FROM Invoices WHERE CustomerId = @id
            var balanceExplicit = await _context.Invoices
                .Where(i => i.CustomerId == id)
                .SumAsync(i => (decimal?)i.Amount) ?? 0;

            // For comparison: WRONG APPROACH (shows the problem)
            var customerWrong = await _customerRepository.GetByIdAsync(id, includeRelated: false);
            var wrongBalance = customerWrong?.Balance ?? 0;  // This will be 0!

            return Ok(new
            {
                CustomerId = id,
                
                // Solution 1: Projection (best approach)
                ProjectionApproach = new
                {
                    Method = "Select() projection with aggregate",
                    Name = customerProjection.Name,
                    Email = customerProjection.Email,
                    Balance = customerProjection.Balance,
                    InvoiceCount = customerProjection.InvoiceCount,
                    PhoneCount = customerProjection.PhoneCount,
                    Description = "Balance calculated in database using SUM() in SELECT",
                    Queries = 1,
                    Recommended = true
                },
                
                // Solution 2: Explicit calculation (alternative)
                ExplicitCalculationApproach = new
                {
                    Method = "Separate aggregate query",
                    Name = customer.Name,
                    Email = customer.Email,
                    Balance = balanceExplicit,
                    Description = "Customer loaded separately, balance calculated with explicit query",
                    Queries = 2,
                    Recommended = false
                },
                
                // WRONG: For comparison (shows the problem)
                WrongApproach = new
                {
                    Method = "includeRelated = false with Balance property",
                    Name = customerWrong?.Name,
                    Email = customerWrong?.Email,
                    Balance = wrongBalance,  // WRONG - will be 0!
                    Description = "⚠️ INCORRECT: Balance property requires Invoices to be loaded",
                    Problem = "The Balance calculated property depends on the Invoices collection being in memory. " +
                              "When includeRelated = false, the collection is empty, so Balance returns 0.",
                    Queries = 1,
                    Recommended = false
                },
                
                Explanation = new
                {
                    Problem = "Customer.Balance is a calculated property that requires Invoices collection to be loaded",
                    Solution1 = "Use projection with Select() to calculate balance in SQL",
                    Solution2 = "Load customer separately and calculate balance with explicit aggregate query",
                    Recommendation = "Always use Solution 1 (projection) for best performance",
                    KeyTakeaway = "Calculated properties don't work with includeRelated = false. Use database aggregation instead."
                },
                
                PerformanceNote = new
                {
                    WithInclude = "Loads ALL invoice records into memory (slow for many invoices)",
                    WithProjection = "Calculates SUM in database, returns only the total (fast)",
                    SpeedupFactor = "50-100x faster for customers with many invoices"
                }
            });
        }
    }
}
