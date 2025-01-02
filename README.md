# Cogitatio CMS
A very lightweight content management system (CMS) written in C#/Blazor. You can see it working on [my blog site](http://blog.tatmanblue.com/).   

Why Cogitatio CMS verses using a well established CMS?  Good question.    

The short answer is time.  In the time it took for me to learn and configure an established CMS system, I wrote Cogitatio and had it deployed.   Granted, Cogitatio is very lightweight but I have what I needed to run my own blog.  You can [read about it](http://blog.tatmanblue.com/post/thank-you-chatgpt-1823) a bit more about this, in my blog post. 

## Features
1. Add/Edit posts  
2. Tag Posts with keywords  
3. Search for posts  
4. Contact form
5. RSS Feed  
6. Sitemap builder 
7. Admin portal
8. [Google Analytics tracking](https://analytics.google.com/analytics/web)


## System Requirements at a glance

It is a very simple to install on your own domain and get running.  Requirements:  
1. Host can run dotnet core Application
2. MS SQL instance. 
3. [Tiny MCE](https://www.tiny.cloud/) cloud license
4. Create the DB schema, create a couple environment variables, build and deploy and off you go!  

## Addendum
Some of the pages in this project are specific to me.  I know there are good options for making this more configurable but it is not a priority for me at this time.  You are free to fork this project and make it work for you.   Pull requests are welcome.  Please keep in mind, however, this project is for me and if it complicates my ability to support my blog, I may insist that your changes remain the branch.  

## License
Released with [Apache 2.0 license](https://github.com/tatmanblue/Cogitatio/blob/main/LICENSE)  

# Installation/Configuration

At this time, the installation/Configuration documentation is very short an brief.  Please reach out to me if you have any concerns.  Below information should help but it is still expected you have some background or ability to manually build and deploy a dotnet application on a website.

## MS SQL

Cogitatio only needs a couple of database tables.  Because I have limited number of MS SQL instances, I have combined Cogitatio tables with other tables for other projects and have no issues.   The database can be configured on its own or shared instance.  Cogitatio is not multi-tennant so if you intended to host multiple Cogitatio sites, you will need to create separate databases for each site.  The [DB schema](https://github.com/tatmanblue/Cogitatio/blob/main/src/schema/create_blog_tables.sql) is pretty simple.

## Tiny MCE

Tiny MCE provides the WYSIWYG editing for creating posts.  The free cloud license model is sufficient.

## Environment variables

Configuration is pretty simple.  A few environment variables are needed as listed below.  You will need the following environment variables:

- CogitatioAdminPassword :  This is the password to the admin portal
- CogitatioSiteDB : connection string to the MS SQL database.
- CogitatioAnalyticsId: id for google analytics.  If this is empty, google analytics will not be installed.

## Additional work

You will probably want to setup a robots.txt file to help with search engine discovery.

# Additional

## Legal
If you have any questions about the content of the repository, please email [matt.raffel@gmail.com](mailto:matt.raffel@gmail.com). I can assure you all content is either open source or has been purchased and licensed to me. Proof will be made available on request. Repeated DCMA counterfit and harassment claims will result in counter suits per Section 512(f) of the DMCA penalties for _misrepresentation can include actual damages and attorney’s fees_.

## Status
Functional, continuing to add features.

2024.12.28
