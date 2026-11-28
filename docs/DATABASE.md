# Database Design

The purpose of this file is to document how the database is structured, and how it is used in the website

While this documentation is primarily intended for Claude code, it is written to be human readable readable.

# The interfaces

The site is designed to use to different databases: Microsoft SQL server and postgress. The code 
uses interfaces to read and write to the database so that the exact database type is abstracted out 
from the implementation.  The database is further abstracted out between blog data and user data.

The separation between blog and user data was intended to allow user data to be stored in a database types 
separate from everything else.  This also helps protect user data since a compromised blog database will 
not compromise user data.

The database is initialized through injection, and is determined by environment variables.

# Multi tenancy

The site is capable of having quasi-multi tenancy.  Most database tables include a tenancy ID. This 
value is retrieved from the environment.

# Database schema

[schema](../src/schema)

There are separate sql files for creating Microsoft sequel server databases, and postgres databases.

# Implementations

[interfaces](../src/Cogitatio/Interfaces)  
[ms sql implementatiom](../src/Cogitatio/Logic/SqlServer.cs) - for a blog data using ms sql  
[postgress implementation](../src/Cogitatio/Logic/Postgressql.cs) - for blog data using postgres  
[abstract base class](../src/Cogitatio/Logic/AbstractDB.cs)  - the abstract class is intended to 
implement common behaviors between both implementations, so has to reduce duplicate code and reduce difficulty in making changes.

[ms sql user db implementation](../src/Cogitatio/Logic/SqlServerUsers.cs) - for user data using ms sql  
[postgres user db implementation](../src/Cogitatio/Logic/PostgresssqlUsers.cs) - for user data using postgres  

[ms sql image db implementation](../src/Cogitatio/Logic/SqlServerImages.cs) - post image metadata using ms sql  
[postgres image db implementation](../src/Cogitatio/Logic/PostgresqlImages.cs) - post image metadata using postgres  

## Post images

Image metadata is kept in `Blog_Images` / `blog_images`, in the **main blog database**
(`CogitatioSiteDB`), through `IImageDatabase`. Create it with `create_blog_images.sql`
(MSSQL and Postgresql folders). The image files themselves are kept in the configured image
store (Azure Blob or a local folder, see [Admin Portal](ADMIN.md)). Posts reference an image
as `/img/{Id}`, never by storage path.

# Work needed
the postgress implementation does not inherit from the base class and it needs to be updated.

## Doc version

2026.11.27