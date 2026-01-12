# Configuration Guide

## Setting Up Your Local Environment

This guide will help you configure the application with your own database credentials.

## Quick Start

### 1. Copy the Example Configuration

```powershell
# Copy the example file to create your local development config
Copy-Item appsettings.Example.json appsettings.Development.json
```

### 2. Update Your Connection String

Open `appsettings.Development.json` and update the connection string with your credentials:

**For SQL Server with Authentication:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=efCoreLabs;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**For SQL Server LocalDB (Windows):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=efCoreLabs;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Replace Placeholders

Replace the following placeholders with your actual values:
- `YOUR_USER` - Your SQL Server username (e.g., `sa`)
- `YOUR_PASSWORD` - Your SQL Server password

## Security Best Practices

### Important Security Notes

1. **Never commit credentials to Git**
   - The `.gitignore` file is configured to exclude `appsettings.Development.json`
   - Always keep sensitive data in ignored files

2. **Use User Secrets for Development** (Recommended)
   ```powershell
   # Initialize user secrets
   dotnet user-secrets init
   
   # Set connection string securely
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=efCoreLabs;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=true"
   ```

3. **Use Environment Variables for Production**
   ```powershell
   # Set environment variable
   $env:ConnectionStrings__DefaultConnection = "YOUR_PRODUCTION_CONNECTION_STRING"
   ```

4. **Use Azure Key Vault or similar services** for production secrets

## Files You Should Modify

[YES] **Safe to commit:**
- `appsettings.json` (contains placeholders only)
- `appsettings.Example.json` (template file)
- `README.md`, `TROUBLESHOOTING.md` (documentation)

[NO] **Never commit:**
- `appsettings.Development.json` (contains your credentials)
- `appsettings.Local.json` (local overrides)
- `appsettings.Production.json` (production settings)
- Any file with actual passwords or secrets

## Connection String Options

### SQL Server Authentication
```
Server=localhost;Database=efCoreLabs;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### Windows Authentication
```
Server=localhost;Database=efCoreLabs;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### LocalDB (Windows Development)
```
Server=(localdb)\\mssqllocaldb;Database=efCoreLabs;Trusted_Connection=True;MultipleActiveResultSets=true
```

### Azure SQL Database
```
Server=tcp:yourserver.database.windows.net,1433;Database=efCoreLabs;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Verifying Your Configuration

After setting up your configuration:

1. **Test the connection:**
   ```powershell
   dotnet run
   ```

2. **Check for database connectivity** in the console output

3. **Run migrations:**
   ```powershell
   dotnet ef database update
   ```

## Troubleshooting

If you encounter issues, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for common problems and solutions.

### Quick Checks

- [YES] Is SQL Server running?
- [YES] Are your credentials correct?
- [YES] Does the database exist or can it be created?
- [YES] Is the server accessible from your machine?
- [YES] Are firewall rules configured correctly?

## Additional Resources

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [User Secrets in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Connection Strings Documentation](https://docs.microsoft.com/en-us/sql/connect/ado-net/connection-string-syntax)
