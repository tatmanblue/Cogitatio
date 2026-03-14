# Project

Cogitatio CMS is  very lightweight content management system (CMS) written in C#/Blazor.

Public repo:  https://github.com/tatmanblue/Cogitatio


## Instructions for Claude

All changes must be approved before creating these changes.  Please prepare a plan of proposed changes and get confirmation before proceeding.

All configuration settings must come from either .env file or from the configuration database.  No hardcoded values for keys and other sensitive information.

## Tech stack
- .NET 10.0, Blazor Server, .NET Aspire (AppHost V2)
- Databases: Microsoft SQL Server and PostgreSQL (both supported; schema files in src/schema/)
- Data access: Custom ADO.NET abstraction (AbstractDB<T>) — no Entity Framework, no Dapper
- UI: Blazor.Bootstrap, TinyMCE.Blazor
- Email: SendGrid and Azure Communication Email
- Logging: Serilog (file sink)
- Config: DotNetEnv (.env files)


## Structure
docs - public documentation  
src - all source code  
src/AppHost V2 - .NET aspire project   
src/Cogitatio - main CMS code base  
src/ServiceDefaults - extension for AppHost V2  
src/schema - database schema files   

## Project structure for Cogitatio - main CMS code base

All of these directories are subdirectories of src/Cogitatio:  
Controllers - Api Controllers.  Mostly for robots.txt, sitemap etc  
General - Constants, exceptions and extension methods  
Interfaces - Key interfaces for injected types  
Logic - Interface implementations and other necessary logic not in pages and models  
Models - data structures   
Pages - all of the pages for the site including Admin and Users  
Properties - dotnet specific directory for settings files  
Shared - components used in multiple Pages  
wwwroot - dotnet directory for standard web assets  

## Coding Conventions
- Modern C# features enabled: primary constructors, implicit usings, nullable reference types (#nullable enable)
- Blazor components use code-behind pattern (.razor + .razor.cs)
- Dependency injection via [Inject] in Blazor, constructor injection elsewhere
- Database operations go through IDatabase / IUserDatabase interfaces in Interfaces/
- Factory methods preferred for model creation (e.g., BlogPost.Create())
- Route constants live in General/Constants.cs — add new routes there
- Multi-tenant architecture — the codebase uses TenantId throughout database operations. 
- Two database providers — when writing SQL or schema changes, both implementations should be provided 9MSSQL and PostgreSQL versions)

## Build & Run
- Set up a .env file in src/Cogitatio/ for local configuration (uses DotNetEnv)
- Run via the AppHost V2 Aspire project for full orchestration
- Requires either SQL Server or PostgreSQL — connection strings configured via .env

## Testing
There are currently no automated tests in this project.  A test project and tests can be created but it is not imperative at this time.