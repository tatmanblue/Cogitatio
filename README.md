# Cogitatio CMS
A Simple blog CMS written in C#/Blazor.  You can see it working on [my blog site](http://blog.tatmanblue.com/).   

Why Cogitatio CMS and not using a well established CMS?  Good question.  You can [read about it](http://blog.tatmanblue.com/post/thank-you-chatgpt-1823) a bit in my blog post.  The short answer is time.  In the time it took for me to learn and configure an established CMS system, I wrote Cogitatio and had it deployed.   Granted, Cogitatio is very lightweight but I have what I needed to run my own blog.

## Basics

Cogitatio is a very lightweight content management system.   It is a very simple to install on your own domain and get running.  Requirements:  
1. Host can run dotnet core Application
2. MS SQL instance. 
3. [Tiny MCE](https://www.tiny.cloud/) cloud license
4. Set up a couple environment variables and off you go!

## license
Released with [Apache 2.0 license](https://github.com/tatmanblue/Cogitatio/blob/main/LICENSE)  

# Installation/Configuration

At this time, the installation/Configuration documentation is very short an brief.  Please reach out to me if you have any concerns.  Below information should help but it is still expected you have some background or ability to manually build and deploy a dotnet application on a website.

## MS SQL

Cogitatio only needs a couple of database tables.  Because I have limited number of MS SQL instances, I have combined Cogitatio tables with other tables and have no issues.   The can be used in its own or shared instance.

## Tiny MCE

Tiny MCE provides the WYSIWYG editing for creating posts.  The free cloud license model is sufficient.

## Environment variables

Configuration is pretty simple.  Two environment variables are needed for db connection and authentication for editing.

## Additional work

You will probably want to setup a robots.txt file to help with search engine discovery.

## Legal
If you have any questions about the content of the repository, please email [matt.raffel@gmail.com](mailto:matt.raffel@gmail.com). I can assure you all content is either open source or has been purchased and licensed to me. Proof will be made available on request. Repeated DCMA counterfit and harassment claims will result in counter suits per Section 512(f) of the DMCA penalties for _misrepresentation can include actual damages and attorney’s fees_.

## Status
Functional, continuing to add features.

2024.12.11