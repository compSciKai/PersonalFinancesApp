# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Personal Finance Application built with C# .NET 8.0 that categorizes and analyzes banking transactions from CSV files. The application processes transactions from multiple financial institutions (RBC, Amex, PC Financial), categorizes them using vendor mapping, and compares spending against budget profiles.

## ⚠️ CRITICAL: BuildBot Usage Requirements ⚠️

**MANDATORY**: You MUST use the BuildBot subagent for ALL build, test, and database migration tasks. This is NOT optional.

### When BuildBot is REQUIRED (Auto-invoke without asking):

**ALWAYS use BuildBot when:**
1. ✅ After making code changes that affect compilation (new files, edits to .cs files)
2. ✅ When the user asks to "build", "test", "run tests", or "verify the changes"
3. ✅ After completing a feature implementation or bug fix
4. ✅ When creating or applying database migrations
5. ✅ When the user says "make sure it works" or similar verification requests
6. ✅ Proactively after significant code changes to verify no regressions

**Examples of when you MUST auto-invoke BuildBot:**
- User: "Fix the OutputBudgetVsActual method" → After fixing, automatically invoke BuildBot to build and test
- User: "Add a new feature" → After implementation, automatically invoke BuildBot to verify
- User: "Does the build pass?" → Immediately invoke BuildBot, don't explain what you would do

**DO NOT:**
- ❌ Run `dotnet build` or `dotnet test` directly via Bash tool
- ❌ Ask the user if they want you to build/test - just do it with BuildBot
- ❌ Skip verification after code changes

### How to Invoke BuildBot

```
Use Task tool with subagent_type="BuildBot"
Example prompt:
"Please build and test the solution to verify recent changes to [description].
Return build status, test results, and any errors."
```

### Why BuildBot is Mandatory

1. **Context Savings**: Writes full output to `build-logs/` directory, returns concise summaries (90-95% context reduction)
2. **Cost Efficiency**: Uses Haiku model for faster, cheaper execution
3. **Clean Logs**: Detailed logs in `build-logs/TIMESTAMP-[build|test]-[success|failure].log` without polluting conversation
4. **Proper Permissions**: BuildBot has pre-approved permissions for build operations

### BuildBot Output Location

All build and test logs are written to: `build-logs/YYYYMMDD-HHMMSS-[build|test]-[success|failure].log`

---

## Common Commands

### Build and Run
```bash
# Build the solution
dotnet build PersonalFinancesApp.sln

# Run the main application
dotnet run --project PersonalFinancesApp/PersonalFinancesApp.csproj

# Run tests
dotnet test PersonalFinancesAppTests/PersonalFinancesAppTests.csproj

# Restore packages
dotnet restore
```

### Database Operations
The application uses Entity Framework Core with SQL Server. Connection string is configured in `Data/DataContext.cs`.

```bash
# Create/update database
dotnet ef database update --project PersonalFinancesApp

# Add new migration
dotnet ef migrations add MigrationName --project PersonalFinancesApp
```

## Architecture

### Core Components

**Models (`PersonalFinances.Models`)**
- `Transaction` - Abstract base class with common transaction properties
- `RBCTransaction`, `AmexTransaction`, `PCFinancialTransaction` - Bank-specific implementations with CSV mapping attributes
- `BudgetProfile` - User budget configuration with categories and limits
- `BaseEntity` - Base class with Id, CreatedDate, UpdatedDate

**Services (`PersonalFinances.App`)**
- `PersonalFinancesApp` - Main application orchestrator
- `BudgetService` - Budget profile management
- `CategoriesService` - Transaction categorization logic
- `VendorsService` - Vendor identification and mapping
- `TransactionFilterService` - Date range and user filtering

**Repositories (`PersonalFinances.Repositories`)**
- `CsvTransactionRepository` - CSV file reading/writing using CsvHelper
- `SqlServerTransactionRepository` - Database operations with duplicate detection via hash
- Repository pattern with interfaces for testability

**Data Layer (`PersonalFinances.Data`)**
- `TransactionContext` - Entity Framework DbContext
- Separate tables for each transaction type
- Hash-based duplicate prevention

### Data Flow

1. **CSV Processing**: Read transaction CSV files using bank-specific mappings
2. **Vendor Mapping**: Match transaction descriptions to known vendors
3. **Categorization**: Assign budget categories to vendors
4. **Filtering**: Apply date range and user filters
5. **Reporting**: Generate spending reports vs budget
6. **Persistence**: Store processed transactions in SQL Server

### Configuration Files

The application uses JSON files for configuration data:
- `budgetProfiles.json` - User budget profiles
- `categories.json` - Category definitions
- `vendors.json` - Vendor-to-category mappings

### Transaction Processing

Transaction types are determined by CSV header detection and processed using CsvHelper with custom class maps. Each transaction generates a SHA256 hash for duplicate detection during database import.

### Key Design Patterns

- **Repository Pattern**: Data access abstraction with CSV and SQL implementations
- **Dependency Injection**: Manual DI in Program.cs for service composition
- **Strategy Pattern**: Different transaction types with shared interface
- **Template Method**: Base Transaction class with bank-specific implementations

## Testing

Tests are written using NUnit with Moq for mocking. Test files are in `PersonalFinancesAppTests/` and cover service layer logic.