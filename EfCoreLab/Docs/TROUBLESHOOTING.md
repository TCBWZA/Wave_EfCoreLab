# Troubleshooting Guide

## Common Issues and Solutions

### 1. FileNotFoundException When Running EF Migrations

**Error:**
```
Unhandled exception. System.IO.FileNotFoundException: Could not load file or assembly 'System.Runtime, Version=10.0.0.0'
```

**Cause:** 
Version mismatch between EF Core tools (dotnet-ef) and EF Core packages in your project.

**Solution:**
Ensure the `dotnet-ef` tools version matches your project's EF Core package version.

This project uses **EF Core 8.0.22**, so you need **dotnet-ef 8.0.x** (8.0.0 or higher in the 8.x line):

```powershell
# Check current version
dotnet ef --version

# If it shows version 9.x, 10.x or any major version other than 8.x:
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 8.0.22

# Verify installation
dotnet ef --version
# Should show: Entity Framework Core .NET Command-line Tools 8.0.x
```

After fixing the version, retry your migration:
```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

### 2. Connection String Issues

**Error:**
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server
```

**Solutions:**

**Option A - Use SQL Server with Authentication:**
```json
"DefaultConnection": "Server=localhost;Database=EfCoreLabDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

**Option B - Use LocalDB (Windows):**
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EfCoreLabDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```

**Verify SQL Server is running:**
```powershell
# Check SQL Server services
Get-Service | Where-Object {$_.DisplayName -like '*SQL Server*'}

# Or use SQL Server Configuration Manager
```

---

### 3. Database Already Exists Error

**Error:**
```
Database 'EfCoreLabDb' already exists
```

**Solution:**
Drop the existing database and recreate:

```powershell
# Option 1: Using EF Core
dotnet ef database drop
dotnet ef database update

# Option 2: Using SQL Server Management Studio (SSMS)
# Connect to localhost, right-click database, select Delete

# Option 3: Using PowerShell with SqlServer module (update with your credentials)
Invoke-Sqlcmd -Query "DROP DATABASE IF EXISTS EfCoreLabDb" -ServerInstance "localhost" -Username "YOUR_USER" -Password "YOUR_PASSWORD"
```

---

### 4. Seeding Not Working

**Issue:** Database is empty after running the application.

**Check:**
1. Verify seeding is enabled in `appsettings.json`:
   ```json
   "SeedSettings": {
     "EnableSeeding": true
   }
   ```

2. Check console output for seeding messages:
   - "Database already contains data. Skipping seed." (database not empty)
   - "Database seeded successfully!" (seeding completed)
   - Error messages if seeding failed

3. Ensure database connection is working:
   ```powershell
   dotnet run
   ```
   Look for Entity Framework log messages in the console.

---

### 5. Migration Files Not Generated

**Issue:** `dotnet ef migrations add` succeeds but no files are created.

**Solution:**

1. Ensure you're in the correct directory (where .csproj file is located)

2. Clean and rebuild:
   ```powershell
   dotnet clean
   dotnet build
   dotnet ef migrations add InitialCreate
   ```

3. Check the `Migrations` folder should be created with files like:
   - `20251227122721_InitialCreate.cs`
   - `20251227122721_InitialCreate.Designer.cs`
   - `AppDbContextModelSnapshot.cs`

---

### 6. Unique Constraint Violations During Seeding

**Error:**
```
Cannot insert duplicate key row in object 'dbo.Customers' with unique index 'IX_Customers_Email'
```

**Solution:**
This usually happens if you try to seed twice. The seeding logic checks for existing data:

```powershell
# Drop and recreate database
dotnet ef database drop -f
dotnet ef database update
dotnet run
```

Or change `EnableSeeding` to `false` in `appsettings.json` if you don't want to seed again.

---

### 7. Package Version Conflicts

**Issue:** Build warnings or errors about package version conflicts.

**Solution:**
Update all EF Core packages to the same version:

```powershell
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.22
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.22
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.22
```

---

### 8. Swagger/API Not Loading

**Issue:** Swagger UI shows 404 or doesn't load.

**Solution:**

1. Ensure you're running in Development mode:
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT="Development"
   dotnet run
   ```

2. Navigate to the correct URL (check console output):
   ```
   https://localhost:7xxx/swagger
   ```

3. If using HTTPS, accept the development certificate:
   ```powershell
   dotnet dev-certs https --trust
   ```

---

## Getting Help

If you encounter other issues:

1. **Check build output:**
   ```powershell
   dotnet build
   ```

2. **Check for package restore issues:**
   ```powershell
   dotnet restore
   ```

3. **Enable detailed EF Core logging** in `appsettings.json`:
   ```json
   "Logging": {
     "LogLevel": {
       "Microsoft.EntityFrameworkCore": "Debug"
     }
   }
   ```

4. **View database directly** using SQL Server Management Studio (SSMS) or Azure Data Studio

5. **Clean solution and rebuild:**
   ```powershell
   dotnet clean
   Remove-Item -Path "bin","obj" -Recurse -Force -ErrorAction SilentlyContinue
   dotnet build
   ```
