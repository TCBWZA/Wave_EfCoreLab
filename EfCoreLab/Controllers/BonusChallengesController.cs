using EfCoreLab.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;

namespace EfCoreLab.Controllers
{
    /// <summary>
    /// Bonus Challenges Controller demonstrating advanced EF Core features:
    /// 1. Custom validation with IValidatableObject
    /// 2. Auditing with CreatedDate and ModifiedDate
    /// 3. Soft delete with IsDeleted flag
    /// 4. Global query filters
    /// 5. Caching with IMemoryCache
    /// 6. SQL query logging
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BonusChallengesController : ControllerBase
    {
        private readonly BonusDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BonusChallengesController> _logger;

        // Cache keys
        private const string CUSTOMER_LIST_CACHE_KEY = "bonus_customer_list";
        private const string CUSTOMER_CACHE_KEY_PREFIX = "bonus_customer_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public BonusChallengesController(
            BonusDbContext context,
            IMemoryCache cache,
            ILogger<BonusChallengesController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// CHALLENGE 1 & 2: Custom Validation + Auditing
        /// 
        /// POST /api/bonuschallenges/customers
        /// 
        /// Creates a new customer with:
        /// - Custom validation via IValidatableObject
        /// - Automatic audit timestamps (CreatedDate, ModifiedDate)
        /// - Data annotations validation
        /// 
        /// Request body example:
        /// {
        ///   "name": "Acme Corporation",
        ///   "email": "contact@acmecorporation.com"
        /// }
        /// 
        /// Custom validation rules:
        /// - Email domain should relate to company name
        /// - Audit fields are validated (CreatedDate not in future, etc.)
        /// 
        /// Audit fields are automatically set by AuditInterceptor:
        /// - CreatedDate: Set to current UTC time on insert
        /// - ModifiedDate: Set to current UTC time on insert/update
        /// </summary>
        [HttpPost("customers")]
        public async Task<ActionResult<BonusCustomer>> CreateCustomer([FromBody] CreateBonusCustomerRequest request)
        {
            _logger.LogInformation("Creating new bonus customer: {Name}", request.Name);

            var customer = new BonusCustomer
            {
                Name = request.Name,
                Email = request.Email,
                IsDeleted = false
            };

            // Validate using IValidatableObject
            var validationContext = new ValidationContext(customer);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(customer, validationContext, validationResults, validateAllProperties: true);

            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    ModelState.AddModelError(
                        validationResult.MemberNames.FirstOrDefault() ?? string.Empty,
                        validationResult.ErrorMessage ?? "Validation error");
                }
                return BadRequest(ModelState);
            }

            _context.BonusCustomers.Add(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Customer created with ID {Id}. CreatedDate: {CreatedDate}, ModifiedDate: {ModifiedDate}",
                customer.Id, customer.CreatedDate, customer.ModifiedDate);

            // Invalidate cache
            _cache.Remove(CUSTOMER_LIST_CACHE_KEY);

            return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, customer);
        }

        /// <summary>
        /// CHALLENGE 4: Global Query Filters
        /// 
        /// GET /api/bonuschallenges/customers
        /// 
        /// Gets all customers with automatic filtering of soft-deleted records.
        /// 
        /// Query parameters:
        /// - includeDeleted: Set to true to bypass the global query filter and see deleted records
        /// 
        /// Global query filter is configured in BonusDbContext:
        /// modelBuilder.Entity<BonusCustomer>().HasQueryFilter(c => !c.IsDeleted);
        /// 
        /// This means all queries automatically exclude soft-deleted records UNLESS
        /// you explicitly use IgnoreQueryFilters().
        /// 
        /// Benefits:
        /// - No need to add WHERE IsDeleted = false to every query
        /// - Prevents accidentally showing deleted data
        /// - Can be bypassed when needed (admin views, restore functionality)
        /// </summary>
        [HttpGet("customers")]
        public async Task<ActionResult<IEnumerable<BonusCustomer>>> GetCustomers([FromQuery] bool includeDeleted = false)
        {
            _logger.LogInformation("Fetching customers. IncludeDeleted: {IncludeDeleted}", includeDeleted);

            var query = _context.BonusCustomers.AsQueryable();

            if (includeDeleted)
            {
                // Bypass global query filter to see deleted records
                _logger.LogInformation("Using IgnoreQueryFilters() to include soft-deleted customers");
                query = query.IgnoreQueryFilters();
            }

            var customers = await query
                .Include(c => c.Invoices)
                .Include(c => c.PhoneNumbers)
                .OrderBy(c => c.Name)
                .ToListAsync();

            _logger.LogInformation("Found {Count} customers", customers.Count);

            return Ok(customers);
        }

        /// <summary>
        /// CHALLENGE 5: Caching with IMemoryCache
        /// 
        /// GET /api/bonuschallenges/customers/{id}
        /// 
        /// Gets a customer by ID with caching for performance.
        /// 
        /// Caching strategy:
        /// - First request: Loads from database and stores in cache for 5 minutes
        /// - Subsequent requests: Returns cached data (much faster)
        /// - Cache is invalidated when customer is updated or deleted
        /// 
        /// Performance comparison:
        /// - First request (cache miss): ~50ms (database query)
        /// - Subsequent requests (cache hit): ~1ms (memory access)
        /// - 50x performance improvement!
        /// 
        /// Cache key format: "bonus_customer_{id}"
        /// Cache duration: 5 minutes (configurable)
        /// 
        /// When to use caching:
        /// - Frequently accessed data
        /// - Data that doesn't change often
        /// - Expensive queries (complex joins, aggregations)
        /// - High-traffic endpoints
        /// </summary>
        [HttpGet("customers/{id}")]
        public async Task<ActionResult<BonusCustomer>> GetCustomerById(long id)
        {
            var cacheKey = $"{CUSTOMER_CACHE_KEY_PREFIX}{id}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out BonusCustomer? cachedCustomer) && cachedCustomer != null)
            {
                _logger.LogInformation("Customer {Id} retrieved from cache", id);
                return Ok(cachedCustomer);
            }

            _logger.LogInformation("Customer {Id} not in cache, fetching from database", id);

            // Not in cache, load from database
            var customer = await _context.BonusCustomers
                .Include(c => c.Invoices)
                .Include(c => c.PhoneNumbers)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                _logger.LogWarning("Customer {Id} not found", id);
                return NotFound($"Customer with ID {id} not found");
            }

            // Store in cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetPriority(CacheItemPriority.Normal);

            _cache.Set(cacheKey, customer, cacheOptions);
            _logger.LogInformation("Customer {Id} stored in cache for {Duration} minutes", id, CacheDuration.TotalMinutes);

            return Ok(customer);
        }

        /// <summary>
        /// CHALLENGE 2: Auditing - Update with ModifiedDate
        /// 
        /// PUT /api/bonuschallenges/customers/{id}
        /// 
        /// Updates a customer with automatic ModifiedDate tracking.
        /// 
        /// The AuditInterceptor automatically updates:
        /// - ModifiedDate: Set to current UTC time when entity is modified
        /// 
        /// You can see the audit trail by comparing CreatedDate vs ModifiedDate.
        /// </summary>
        [HttpPut("customers/{id}")]
        public async Task<ActionResult<BonusCustomer>> UpdateCustomer(long id, [FromBody] UpdateBonusCustomerRequest request)
        {
            _logger.LogInformation("Updating customer {Id}", id);

            var customer = await _context.BonusCustomers.FindAsync(id);
            if (customer == null)
            {
                return NotFound($"Customer with ID {id} not found");
            }

            // Store old values for logging
            var oldName = customer.Name;
            var oldEmail = customer.Email;
            var oldModifiedDate = customer.ModifiedDate;

            // Update properties
            customer.Name = request.Name;
            customer.Email = request.Email;

            // Validate
            var validationContext = new ValidationContext(customer);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(customer, validationContext, validationResults, validateAllProperties: true);

            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    ModelState.AddModelError(
                        validationResult.MemberNames.FirstOrDefault() ?? string.Empty,
                        validationResult.ErrorMessage ?? "Validation error");
                }
                return BadRequest(ModelState);
            }

            // ModifiedDate will be automatically updated by AuditInterceptor
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Customer {Id} updated. Name: {OldName} -> {NewName}, Email: {OldEmail} -> {NewEmail}, ModifiedDate: {OldDate} -> {NewDate}",
                id, oldName, customer.Name, oldEmail, customer.Email, oldModifiedDate, customer.ModifiedDate);

            // Invalidate cache
            _cache.Remove($"{CUSTOMER_CACHE_KEY_PREFIX}{id}");
            _cache.Remove(CUSTOMER_LIST_CACHE_KEY);

            return Ok(customer);
        }

        /// <summary>
        /// CHALLENGE 3: Soft Delete
        /// 
        /// DELETE /api/bonuschallenges/customers/{id}
        /// 
        /// Performs a soft delete instead of hard delete.
        /// 
        /// Soft delete:
        /// - Sets IsDeleted = true
        /// - Sets DeletedDate = current UTC time
        /// - Record remains in database but is hidden by global query filter
        /// - Can be restored later if needed
        /// 
        /// vs Hard delete:
        /// - Physically removes record from database
        /// - Cannot be restored
        /// - May cause referential integrity issues
        /// 
        /// Benefits of soft delete:
        /// - Audit trail preserved
        /// - Can restore accidentally deleted data
        /// - Historical reporting includes deleted records
        /// - Safer for production systems
        /// </summary>
        [HttpDelete("customers/{id}")]
        public async Task<ActionResult> SoftDeleteCustomer(long id)
        {
            _logger.LogInformation("Soft deleting customer {Id}", id);

            var customer = await _context.BonusCustomers.FindAsync(id);
            if (customer == null)
            {
                return NotFound($"Customer with ID {id} not found");
            }

            if (customer.IsDeleted)
            {
                _logger.LogWarning("Customer {Id} is already soft deleted", id);
                return BadRequest("Customer is already deleted");
            }

            // Perform soft delete
            customer.IsDeleted = true;
            // DeletedDate will be automatically set by AuditInterceptor

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Customer {Id} soft deleted. DeletedDate: {DeletedDate}",
                id, customer.DeletedDate);

            // Invalidate cache
            _cache.Remove($"{CUSTOMER_CACHE_KEY_PREFIX}{id}");
            _cache.Remove(CUSTOMER_LIST_CACHE_KEY);

            return NoContent();
        }

        /// <summary>
        /// CHALLENGE 3: Restore Soft-Deleted Customer
        /// 
        /// POST /api/bonuschallenges/customers/{id}/restore
        /// 
        /// Restores a soft-deleted customer.
        /// 
        /// This demonstrates the advantage of soft delete:
        /// - Can undo deletions
        /// - Useful for "Recycle Bin" functionality
        /// - Audit trail shows deletion and restoration
        /// 
        /// The operation:
        /// - Sets IsDeleted = false
        /// - Clears DeletedDate
        /// - Updates ModifiedDate (via AuditInterceptor)
        /// </summary>
        [HttpPost("customers/{id}/restore")]
        public async Task<ActionResult<BonusCustomer>> RestoreCustomer(long id)
        {
            _logger.LogInformation("Restoring customer {Id}", id);

            // Must use IgnoreQueryFilters() to find soft-deleted customer
            var customer = await _context.BonusCustomers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound($"Customer with ID {id} not found");
            }

            if (!customer.IsDeleted)
            {
                _logger.LogWarning("Customer {Id} is not deleted, cannot restore", id);
                return BadRequest("Customer is not deleted");
            }

            // Restore customer
            customer.IsDeleted = false;
            // DeletedDate will be cleared by AuditInterceptor
            // ModifiedDate will be updated by AuditInterceptor

            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer {Id} restored. ModifiedDate: {ModifiedDate}", id, customer.ModifiedDate);

            // Invalidate cache
            _cache.Remove($"{CUSTOMER_CACHE_KEY_PREFIX}{id}");
            _cache.Remove(CUSTOMER_LIST_CACHE_KEY);

            return Ok(customer);
        }

        /// <summary>
        /// CHALLENGE 6: SQL Query Logging
        /// 
        /// GET /api/bonuschallenges/customers/with-large-balance
        /// 
        /// Demonstrates SQL query logging configured in BonusDbContext.
        /// 
        /// All SQL queries generated by EF Core are logged to the console/logger:
        /// - SELECT statements with all parameters
        /// - Generated SQL including JOINs, WHERE clauses, etc.
        /// - Query execution time
        /// - Connection info
        /// 
        /// Logging is configured in BonusDbContext.OnConfiguring():
        /// optionsBuilder.LogTo(
        ///     message => _logger.LogInformation(message),
        ///     new[] { DbLoggerCategory.Database.Command.Name },
        ///     LogLevel.Information);
        /// 
        /// Check your console/logs to see the SQL being generated!
        /// 
        /// Example logged SQL:
        /// SELECT [c].[Id], [c].[Name], [c].[Email], ...
        /// FROM [BonusCustomers] AS [c]
        /// LEFT JOIN [BonusInvoices] AS [i] ON [c].[Id] = [i].[CustomerId]
        /// WHERE [c].[IsDeleted] = 0
        /// GROUP BY [c].[Id], [c].[Name], [c].[Email]
        /// HAVING SUM([i].[Amount]) > @__minBalance_0
        /// </summary>
        [HttpGet("customers/with-large-balance")]
        public async Task<ActionResult> GetCustomersWithLargeBalance([FromQuery] decimal minBalance = 10000)
        {
            _logger.LogInformation("Fetching customers with balance >= {MinBalance}", minBalance);
            _logger.LogInformation("Check the logs below to see the SQL query generated by EF Core!");

            // Complex query that will generate interesting SQL
            var customers = await _context.BonusCustomers
                .Include(c => c.Invoices)
                .Where(c => c.Invoices.Sum(i => i.Amount) >= minBalance)
                .OrderByDescending(c => c.Invoices.Sum(i => i.Amount))
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Email,
                    Balance = c.Invoices.Sum(i => i.Amount),
                    InvoiceCount = c.Invoices.Count,
                    c.CreatedDate,
                    c.ModifiedDate
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} customers with balance >= {MinBalance}", customers.Count, minBalance);

            return Ok(new
            {
                MinBalance = minBalance,
                CustomerCount = customers.Count,
                Customers = customers,
                Note = "Check your application logs to see the SQL query that was generated!"
            });
        }

        /// <summary>
        /// DEMO: All Challenges Combined
        /// 
        /// GET /api/bonuschallenges/demo
        /// 
        /// Demonstrates all bonus challenges in action:
        /// 1. Custom validation (IValidatableObject)
        /// 2. Auditing (CreatedDate, ModifiedDate)
        /// 3. Soft delete (IsDeleted)
        /// 4. Global query filters (automatic filtering)
        /// 5. Caching (IMemoryCache)
        /// 6. SQL logging (check logs)
        /// </summary>
        [HttpGet("demo")]
        public async Task<ActionResult> DemoAllChallenges()
        {
            _logger.LogInformation("Running Bonus Challenges demo");

            // Get statistics (SQL will be logged)
            var totalCustomers = await _context.BonusCustomers.CountAsync();
            var deletedCustomers = await _context.BonusCustomers.IgnoreQueryFilters().CountAsync(c => c.IsDeleted);
            var totalInvoices = await _context.BonusInvoices.CountAsync();
            var totalAmount = await _context.BonusInvoices.SumAsync(i => (decimal?)i.Amount) ?? 0;

            // Get sample customer with audit info
            var sampleCustomer = await _context.BonusCustomers
                .Include(c => c.Invoices)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                Message = "Bonus Challenges Demo - All Features Working!",
                
                Challenge1_CustomValidation = new
                {
                    Feature = "IValidatableObject implementation",
                    Description = "Entities validate email domain matches company name, audit fields, etc.",
                    Example = "Try creating a customer with POST /api/bonuschallenges/customers"
                },
                
                Challenge2_Auditing = new
                {
                    Feature = "Automatic audit timestamps",
                    Description = "CreatedDate and ModifiedDate are automatically set by AuditInterceptor",
                    SampleCustomer = sampleCustomer != null ? new
                    {
                        sampleCustomer.Id,
                        sampleCustomer.Name,
                        sampleCustomer.CreatedDate,
                        sampleCustomer.ModifiedDate,
                        DaysSinceCreation = (DateTime.UtcNow - sampleCustomer.CreatedDate).Days
                    } : null
                },
                
                Challenge3_SoftDelete = new
                {
                    Feature = "Soft delete with IsDeleted flag",
                    Description = "Records are marked as deleted but not removed from database",
                    Statistics = new
                    {
                        ActiveCustomers = totalCustomers,
                        DeletedCustomers = deletedCustomers,
                        TotalCustomers = totalCustomers + deletedCustomers
                    },
                    Example = "Try DELETE /api/bonuschallenges/customers/{id} then restore with POST /api/bonuschallenges/customers/{id}/restore"
                },
                
                Challenge4_GlobalQueryFilters = new
                {
                    Feature = "Automatic filtering of soft-deleted records",
                    Description = "HasQueryFilter(c => !c.IsDeleted) in model configuration",
                    Effect = "All queries automatically exclude deleted records unless IgnoreQueryFilters() is used",
                    Example = "GET /api/bonuschallenges/customers?includeDeleted=true to see deleted records"
                },
                
                Challenge5_Caching = new
                {
                    Feature = "IMemoryCache for frequently accessed data",
                    Description = "Customers are cached for 5 minutes to improve performance",
                    Performance = "Cache hits are ~50x faster than database queries",
                    Example = "Call GET /api/bonuschallenges/customers/{id} twice and check logs for cache hit"
                },
                
                Challenge6_SqlLogging = new
                {
                    Feature = "SQL query logging to console",
                    Description = "All EF Core queries are logged with parameters and execution time",
                    Configuration = "LogTo() in BonusDbContext.OnConfiguring()",
                    Example = "Check your console/logs to see SQL queries being generated"
                },
                
                Summary = new
                {
                    TotalCustomers = totalCustomers + deletedCustomers,
                    ActiveCustomers = totalCustomers,
                    DeletedCustomers = deletedCustomers,
                    TotalInvoices = totalInvoices,
                    TotalInvoiceAmount = totalAmount,
                    Database = "BonusCustomers, BonusInvoices, BonusTelephoneNumbers (separate from main tables)"
                }
            });
        }
    }

    // DTOs for request bodies
    public class CreateBonusCustomerRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateBonusCustomerRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
    }
}
