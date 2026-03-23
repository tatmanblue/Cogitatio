
There are 2 types in the dbMigrate project I want to update: MigrationWriter and MigrationReader.  They should use the appropriate implemenation of IDatabase and IUserDatabase found in the Cogititio project.  we can reference the
Cogititio project in the migration project.  There is no need to duplicate the sql between the two projects.  There are some TODOs in the dbMigrate project code that further explain this change.

this is some pseudo code describing the expected workflow  

IDatabase add new method  
List<Comment> GetAllPostComments(id)

and implement for both MS SQL and postgres


create dbReader: IUserDatabase for source database  
create dbWriter: IUserDatabase for destination database  

dbReader.LoadAll  
for each user  
  dbWriter.Save  	  
  get new user id and add to id mapping for saving comments  

create dbReader: IDatabase for source database  
create dbWriter: IDatabase for destination database  

for settings  
    dbReader.GetAllSettings  
	for each setting  
	   dbWriter.SaveSetting  
	

custom sql:
SELECT id FROM Blog_Post order by id DESC  

For each id  
   dbReader.GetPost(id)  
   dbWriter.CreatePost(....)  
   dbReader.GetPostTags(id)  
   dbWriter.UpdatePost(....)  
   dbReader.GetAllPostComments(id)  
   for each comment  
      find correct user id from mapping made above  
      dbWriter.SaveSingleComment  
	  
	  
	  
Risks:  blog publish and comment dates are lost on migration.  we could write custom code to update after insert  	  

I am ok if settings that do not use the enum are lost.  There should not be any settings without an appropriate
enum.  We can make the system report these if that helps but I do not think that is necessary  


  