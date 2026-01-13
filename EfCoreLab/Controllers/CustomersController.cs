using EfCoreLab.DTOs;
using EfCoreLab.Mappings;
using EfCoreLab.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EfCoreLab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(
            ICustomerRepository customerRepository,
            ILogger<CustomersController> logger)
        {
            _customerRepository = customerRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll([FromQuery] bool includeRelated = false)
        {
            var customers = await _customerRepository.GetAllAsync(includeRelated);
            return Ok(customers.Select(c => c.ToDto()));
        }

        /// <summary>
        /// EXAMPLE: Split Queries to avoid cartesian explosion
        /// 
        /// GET /api/customers/with-split-queries
        /// 
        /// This endpoint demonstrates how to use AsSplitQuery() to optimize queries
        /// that load multiple related collections. Instead of using JOINs (which create
        /// a cartesian product), EF Core executes separate queries:
        /// 
        /// Query 1: SELECT * FROM Customers
        /// Query 2: SELECT * FROM Invoices WHERE CustomerId IN (1,2,3,...)
        /// Query 3: SELECT * FROM TelephoneNumbers WHERE CustomerId IN (1,2,3,...)
        /// 
        /// Benefits:
        /// - No data duplication (customer data not repeated for each invoice/phone)
        /// - Reduced data transfer (~75% less for typical datasets)
        /// - Better performance with large collections
        /// 
        /// Trade-off:
        /// - Multiple database round-trips (usually negligible with modern networks)
        /// 
        /// When to use:
        /// - Loading 2+ collections with Include()
        /// - Collections have many items per parent
        /// - Large datasets (1000+ parent records)
        /// </summary>
        [HttpGet("with-split-queries")]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAllWithSplitQueries()
        {
            var customers = await _customerRepository.GetAllWithSplitQueriesAsync();
            return Ok(customers.Select(c => c.ToDto()));
        }

        /// <summary>
        /// EXAMPLE: Pagination with Skip/Take
        /// 
        /// GET /api/customers/paginated?page=1&pageSize=10
        /// 
        /// Essential for handling large datasets in real-world applications.
        /// Returns a page of results along with metadata for UI pagination controls.
        /// 
        /// Query Parameters:
        /// - page: Page number (1-based, default: 1)
        /// - pageSize: Items per page (default: 10, max recommended: 100)
        /// 
        /// Response includes:
        /// - items: Current page of customers
        /// - totalCount: Total customers in database
        /// - page: Current page number
        /// - pageSize: Items per page
        /// - totalPages: Calculated total pages
        /// - hasPrevious: Can navigate to previous page
        /// - hasNext: Can navigate to next page
        /// 
        /// SQL Generated:
        /// SELECT * FROM Customers 
        /// ORDER BY Name 
        /// OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY
        /// 
        /// Performance tips:
        /// - Always use OrderBy() before Skip/Take
        /// - Keep pageSize reasonable (10-100)
        /// - Consider caching totalCount for very large tables
        /// - Use indexes on ORDER BY columns
        /// </summary>
        [HttpGet("paginated")]
        public async Task<ActionResult<PagedResult<CustomerDto>>> GetPaginated(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeRelated = false)
        {
            // Validate input
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit max page size

            var (items, totalCount) = await _customerRepository.GetPagedAsync(page, pageSize, includeRelated);

            var result = new PagedResult<CustomerDto>
            {
                Items = items.Select(c => c.ToDto()),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        /// <summary>
        /// EXAMPLE: Dynamic filtering and search
        /// 
        /// GET /api/customers/search?name=acme&email=@acme.com&minBalance=1000
        /// 
        /// Demonstrates flexible querying with multiple optional filters.
        /// Only specified filters are applied - allows any combination.
        /// 
        /// Query Parameters (all optional):
        /// - name: Partial match on customer name (case-insensitive)
        /// - email: Partial match on email address
        /// - minBalance: Customers with total invoice amount >= this value
        /// 
        /// Examples:
        /// - /api/customers/search?name=corp
        ///   Returns customers with "corp" in name
        /// 
        /// - /api/customers/search?email=gmail
        ///   Returns customers with Gmail addresses
        /// 
        /// - /api/customers/search?minBalance=5000
        ///   Returns customers with invoices totaling >= $5000
        /// 
        /// - /api/customers/search?name=acme&minBalance=1000
        ///   Returns Acme customers with >= $1000 in invoices
        /// 
        /// SQL Pattern:
        /// All filters combined into single WHERE clause:
        /// WHERE Name LIKE '%acme%' AND Email LIKE '%gmail%' AND 
        /// (SELECT SUM(Amount) FROM Invoices WHERE CustomerId = Customers.Id) >= 1000
        /// 
        /// Performance tips:
        /// - Create indexes on Name and Email columns
        /// - minBalance creates a correlated subquery (can be slow on huge datasets)
        /// - Consider adding pagination for large result sets
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> Search(
            [FromQuery] string? name,
            [FromQuery] string? email,
            [FromQuery] decimal? minBalance)
        {
            var customers = await _customerRepository.SearchAsync(name, email, minBalance);
            return Ok(customers.Select(c => c.ToDto()));
        }

        /// <summary>
        /// EXAMPLE: Sorting with dynamic OrderBy
        /// 
        /// GET /api/customers/sorted?sortBy=email&descending=true
        /// 
        /// Allows client to control sort order for list views.
        /// Supports sorting by different fields and directions.
        /// 
        /// Query Parameters:
        /// - sortBy: Field to sort by (name, email, balance). Default: name
        /// - descending: Sort direction (true/false). Default: false
        /// 
        /// Examples:
        /// - /api/customers/sorted
        ///   Sort by name ascending (A-Z)
        /// 
        /// - /api/customers/sorted?sortBy=email&descending=true
        ///   Sort by email descending (Z-A)
        /// 
        /// - /api/customers/sorted?sortBy=balance
        ///   Sort by total invoice amount ascending (low to high)
        /// 
        /// SQL Generated:
        /// SELECT * FROM Customers ORDER BY [Name|Email|(SELECT SUM...)] [ASC|DESC]
        /// 
        /// Implementation note:
        /// Uses switch expression (C# 8.0) for clean, readable sorting logic.
        /// The balance sort uses a calculated column (SUM of invoices).
        /// 
        /// Performance tips:
        /// - Create indexes on frequently sorted columns
        /// - Sorting by balance (calculated) is slower than simple columns
        /// - Combine with pagination for better UX on large datasets
        /// </summary>
        [HttpGet("sorted")]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetSorted(
            [FromQuery] string sortBy = "name",
            [FromQuery] bool descending = false)
        {
            // Get all customers as IQueryable (no SQL executed yet)
            var query = _customerRepository.GetAllAsync(includeRelated: false)
                .Result.AsQueryable();

            // Apply sorting based on parameters
            // Switch expression provides clean, type-safe sorting logic
            query = sortBy.ToLower() switch
            {
                "email" => descending 
                    ? query.OrderByDescending(c => c.Email) 
                    : query.OrderBy(c => c.Email),
                
                "balance" => descending 
                    ? query.OrderByDescending(c => c.Invoices.Sum(i => i.Amount))
                    : query.OrderBy(c => c.Invoices.Sum(i => i.Amount)),
                
                // Default to sorting by name
                _ => descending 
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name)
            };

            return Ok(query.Select(c => c.ToDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetById(long id)
        {
            var customer = await _customerRepository.GetByIdAsync(id, includeRelated: true);
            if (customer == null)
                return NotFound($"Customer with ID {id} not found.");

            return Ok(customer.ToDto());
        }

        [HttpGet("email/{email}")]
        public async Task<ActionResult<CustomerDto>> GetByEmail(string email)
        {
            var customer = await _customerRepository.GetByEmailAsync(email);
            if (customer == null)
                return NotFound($"Customer with email {email} not found.");

            return Ok(customer.ToDto());
        }

        /// <summary>
        /// EXAMPLE: Creating a customer with related entities
        /// 
        /// POST /api/customers
        /// 
        /// Creates a new customer and optionally includes related invoices and phone numbers
        /// in a single atomic transaction. All entities are saved together.
        /// 
        /// Request body example:
        /// {
        ///   "name": "Acme Corp",
        ///   "email": "contact@acme.com",
        ///   "invoices": [
        ///     {
        ///       "invoiceNumber": "INV-001",
        ///       "invoiceDate": "2024-01-15T00:00:00Z",
        ///       "amount": 1500.00
        ///     }
        ///   ],
        ///   "phoneNumbers": [
        ///     {
        ///       "type": "Mobile",
        ///       "number": "555-1234"
        ///     },
        ///     {
        ///       "type": "Work",
        ///       "number": "555-5678"
        ///     }
        ///   ]
        /// }
        /// 
        /// EF Core behavior:
        /// - Creates Customer record
        /// - Creates related Invoice records with CustomerId automatically set
        /// - Creates related TelephoneNumber records with CustomerId automatically set
        /// - All operations happen in a single database transaction
        /// - If any operation fails, entire transaction is rolled back
        /// 
        /// Note: Invoices and PhoneNumbers are optional - you can create a customer without them.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if email already exists
            if (await _customerRepository.EmailExistsAsync(dto.Email))
                return Conflict($"A customer with email {dto.Email} already exists.");

            var customer = dto.ToEntity();
            var created = await _customerRepository.CreateAsync(customer);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToDto());
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CustomerDto>> Update(long id, [FromBody] UpdateCustomerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
                return NotFound($"Customer with ID {id} not found.");

            // Check if email already exists for another customer
            if (await _customerRepository.EmailExistsAsync(dto.Email, id))
                return Conflict($"A customer with email {dto.Email} already exists.");

            dto.UpdateEntity(customer);
            var updated = await _customerRepository.UpdateAsync(customer);

            return Ok(updated.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            var deleted = await _customerRepository.DeleteAsync(id);
            if (!deleted)
                return NotFound($"Customer with ID {id} not found.");

            return NoContent();
        }
    }
}
