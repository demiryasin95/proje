namespace StudentStudyProgram.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserProfilePicture : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "ProfilePictureUrl", c => c.String(maxLength: 256));
            AddColumn("dbo.AspNetUsers", "DisplayName", c => c.String(maxLength: 128));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "DisplayName");
            DropColumn("dbo.AspNetUsers", "ProfilePictureUrl");
        }
    }
}
