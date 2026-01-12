using EfCoreLab;
using EfCoreLab.Data;
using EfCoreLab.Repositories;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Register repositories
        builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
        builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        builder.Services.AddScoped<ITelephoneNumberRepository, TelephoneNumberRepository>();

        // Configure seed settings
        builder.Services.Configure<SeedSettings>(builder.Configuration.GetSection("SeedSettings"));

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Seed the database
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                var seedSettings = builder.Configuration.GetSection("SeedSettings").Get<SeedSettings>() ?? new SeedSettings();

                logger.LogInformation("Checking database...");

                // Ensure database is created
                await context.Database.EnsureCreatedAsync();

                // Seed data if enabled and database is empty
                if (seedSettings.EnableSeeding)
                {
                    logger.LogInformation("Seeding is enabled. Checking for existing data...");
                    await BogusDataGenerator.SeedDatabase(
                        context,
                        customerCount: seedSettings.CustomerCount,
                        minInvoicesPerCustomer: seedSettings.MinInvoicesPerCustomer,
                        maxInvoicesPerCustomer: seedSettings.MaxInvoicesPerCustomer,
                        minPhoneNumbersPerCustomer: seedSettings.MinPhoneNumbersPerCustomer,
                        maxPhoneNumbersPerCustomer: seedSettings.MaxPhoneNumbersPerCustomer
                    );
                }
                else
                {
                    logger.LogInformation("Database seeding is disabled in configuration.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        // Configure the HTTP request pipeline
        // Enable Swagger for all environments
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "EF Core Lab API V1");
            c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
        });

        // Only use HTTPS redirection in production
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}