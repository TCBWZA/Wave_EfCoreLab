namespace EfCoreLab
{
    /// <summary>
    /// Configuration settings for database seeding.
    /// </summary>
    public class SeedSettings
    {
        /// <summary>
        /// Gets or sets whether database seeding is enabled.
        /// </summary>
        public bool EnableSeeding { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of customers to seed.
        /// </summary>
        public int CustomerCount { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the minimum number of invoices per customer.
        /// </summary>
        public int MinInvoicesPerCustomer { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum number of invoices per customer.
        /// </summary>
        public int MaxInvoicesPerCustomer { get; set; } = 5;

        /// <summary>
        /// Gets or sets the minimum number of phone numbers per customer.
        /// </summary>
        public int MinPhoneNumbersPerCustomer { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum number of phone numbers per customer.
        /// </summary>
        public int MaxPhoneNumbersPerCustomer { get; set; } = 3;
    }
}
