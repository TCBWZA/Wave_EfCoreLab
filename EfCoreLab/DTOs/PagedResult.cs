namespace EfCoreLab.DTOs
{
    /// <summary>
    /// Generic paged result wrapper for paginated API responses.
    /// Contains the current page of items along with metadata about the total dataset.
    /// </summary>
    /// <typeparam name="T">The type of items in the page</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The items for the current page.
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Total number of items in the entire dataset (across all pages).
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page number (1-based).
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of pages available.
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>
        /// Indicates if there is a previous page available.
        /// </summary>
        public bool HasPrevious => Page > 1;

        /// <summary>
        /// Indicates if there is a next page available.
        /// </summary>
        public bool HasNext => Page < TotalPages;
    }

    /// <summary>
    /// Lightweight customer summary DTO for projection queries.
    /// Contains only essential customer information without loading entire entities.
    /// Used for efficient list views and reports.
    /// </summary>
    public class CustomerSummaryDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Total number of invoices for this customer.
        /// Calculated in database query, not loaded from navigation property.
        /// </summary>
        public int InvoiceCount { get; set; }
        
        /// <summary>
        /// Sum of all invoice amounts for this customer.
        /// Calculated in database query using SQL SUM aggregation.
        /// </summary>
        public decimal TotalAmount { get; set; }
        
        /// <summary>
        /// Date of the most recent invoice, or null if no invoices exist.
        /// Calculated using SQL MAX function, avoiding loading all invoices.
        /// </summary>
        public DateTime? LastInvoiceDate { get; set; }
    }
}
