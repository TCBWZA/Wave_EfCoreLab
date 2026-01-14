using Bogus;
using EfCoreLab.Data;
using Microsoft.EntityFrameworkCore;

namespace EfCoreLab
{
    /// <summary>
    /// Provides fake data generation for Bonus Challenge entities.
    /// Generates realistic customer, invoice, and phone number data with auditing fields.
    /// </summary>
    public static class BonusBogusDataGenerator
    {
        /// <summary>
        /// Seeds the bonus database with fake data. Creates customers with invoices and phone numbers.
        /// Includes audit fields and some soft-deleted records for testing.
        /// </summary>
        public static async Task SeedBonusDatabase(
            BonusDbContext context,
            int customerCount = 50,
            int minInvoicesPerCustomer = 1,
            int maxInvoicesPerCustomer = 5,
            int minPhoneNumbersPerCustomer = 1,
            int maxPhoneNumbersPerCustomer = 3)
        {
            // Check if database already has data
            var existingCustomerCount = await context.BonusCustomers.IgnoreQueryFilters().CountAsync();
            if (existingCustomerCount > 0)
            {
                Console.WriteLine($"Bonus database already contains {existingCustomerCount} customers. Skipping seed.");
                return;
            }

            Console.WriteLine($"Bonus database is empty. Starting seed process...");
            Console.WriteLine($"Generating {customerCount} bonus customers with invoices and phone numbers...");

            var random = new Random();
            var now = DateTime.UtcNow;

            // Generate and save customers first to get their database-generated IDs
            var customers = SeedBonusCustomers(customerCount, now);
            await context.BonusCustomers.AddRangeAsync(customers);
            await context.SaveChangesAsync();

            Console.WriteLine($"? Created {customers.Count} bonus customers");

            var allInvoices = new List<BonusInvoice>();
            var allPhoneNumbers = new List<BonusTelephoneNumber>();

            foreach (var customer in customers)
            {
                // Generate random number of invoices for this customer
                int invoiceCount = random.Next(minInvoicesPerCustomer, maxInvoicesPerCustomer + 1);
                var invoices = GenerateBonusInvoices(customer.Id, invoiceCount, now);
                allInvoices.AddRange(invoices);

                // Generate random number of phone numbers for this customer
                int phoneCount = random.Next(minPhoneNumbersPerCustomer, maxPhoneNumbersPerCustomer + 1);
                var phoneNumbers = GenerateBonusPhoneNumbers(customer.Id, phoneCount, now);
                allPhoneNumbers.AddRange(phoneNumbers);
            }

            Console.WriteLine($"? Generated {allInvoices.Count} bonus invoices");
            Console.WriteLine($"? Generated {allPhoneNumbers.Count} bonus phone numbers");

            // Add all invoices and phone numbers
            await context.BonusInvoices.AddRangeAsync(allInvoices);
            await context.BonusTelephoneNumbers.AddRangeAsync(allPhoneNumbers);
            await context.SaveChangesAsync();

            // Soft delete some records for testing (about 10%)
            var customersToDelete = customers.Take(customerCount / 10).ToList();
            foreach (var customer in customersToDelete)
            {
                customer.IsDeleted = true;
                customer.DeletedDate = now.AddDays(-random.Next(1, 30));
            }

            var invoicesToDelete = allInvoices.Take(allInvoices.Count / 10).ToList();
            foreach (var invoice in invoicesToDelete)
            {
                invoice.IsDeleted = true;
                invoice.DeletedDate = now.AddDays(-random.Next(1, 30));
            }

            await context.SaveChangesAsync();

            Console.WriteLine($"? Soft deleted {customersToDelete.Count} customers and {invoicesToDelete.Count} invoices for testing");
            Console.WriteLine($"");
            Console.WriteLine($"? Bonus database seeded successfully!");
            Console.WriteLine($"  Total: {customers.Count} customers, {allInvoices.Count} invoices, {allPhoneNumbers.Count} phone numbers");
            Console.WriteLine($"  (Including {customersToDelete.Count} soft-deleted customers)");
        }

        /// <summary>
        /// Generates a list of fake bonus customers with realistic company names and email addresses.
        /// </summary>
        public static List<BonusCustomer> SeedBonusCustomers(int count, DateTime? createdDate = null)
        {
            var now = createdDate ?? DateTime.UtcNow;

            Faker<BonusCustomer> faker = new Faker<BonusCustomer>("en_GB")
                .CustomInstantiator(f =>
                {
                    var companyName = f.Company.CompanyName();
                    return new BonusCustomer
                    {
                        Name = companyName,
                        Email = f.Internet.Email(companyName.Replace(" ", "").ToLower()),
                        CreatedDate = now.AddDays(-f.Random.Int(1, 365)),
                        ModifiedDate = now,
                        IsDeleted = false
                    };
                });

            List<BonusCustomer> list = new List<BonusCustomer>();
            for (int i = 0; i < count; i++)
            {
                list.Add(faker.Generate());
            }

            return list;
        }

        /// <summary>
        /// Generates a list of fake bonus invoices for a specific customer.
        /// </summary>
        public static List<BonusInvoice> GenerateBonusInvoices(long custId, int count, DateTime? createdDate = null)
        {
            var now = createdDate ?? DateTime.UtcNow;

            Faker<BonusInvoice> faker = new Faker<BonusInvoice>("en_GB")
                .CustomInstantiator(f =>
                {
                    var invoiceDate = f.Date.Past(2);
                    var created = invoiceDate.AddHours(f.Random.Int(1, 24));

                    return new BonusInvoice
                    {
                        InvoiceNumber = "INV-" + f.Random.AlphaNumeric(8).ToUpper(),
                        InvoiceDate = invoiceDate,
                        Amount = f.Finance.Amount(10, 5000),
                        CustomerId = custId,
                        CreatedDate = created,
                        ModifiedDate = created.AddDays(f.Random.Int(0, 30)),
                        IsDeleted = false
                    };
                });

            List<BonusInvoice> list = new List<BonusInvoice>();
            for (int i = 0; i < count; i++)
            {
                list.Add(faker.Generate());
            }

            return list;
        }

        private static readonly string[] NumberTypes = new[] { "DirectDial", "Work", "Mobile" };

        /// <summary>
        /// Generates a list of fake bonus phone numbers for a specific customer.
        /// </summary>
        public static List<BonusTelephoneNumber> GenerateBonusPhoneNumbers(long custId, int count, DateTime? createdDate = null)
        {
            var now = createdDate ?? DateTime.UtcNow;

            Faker<BonusTelephoneNumber> faker = new Faker<BonusTelephoneNumber>("en_GB")
                .CustomInstantiator(f =>
                {
                    var created = now.AddDays(-f.Random.Int(1, 365));

                    return new BonusTelephoneNumber
                    {
                        CustomerId = custId,
                        Number = f.Phone.PhoneNumber(),
                        Type = f.PickRandom(NumberTypes),
                        CreatedDate = created,
                        ModifiedDate = created.AddDays(f.Random.Int(0, 30)),
                        IsDeleted = false
                    };
                });

            List<BonusTelephoneNumber> list = new List<BonusTelephoneNumber>();
            for (int i = 0; i < count; i++)
            {
                list.Add(faker.Generate());
            }

            return list;
        }
    }
}
