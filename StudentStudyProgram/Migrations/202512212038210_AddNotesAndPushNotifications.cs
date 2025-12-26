namespace StudentStudyProgram.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNotesAndPushNotifications : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PushSubscriptions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(maxLength: 128),
                        Endpoint = c.String(nullable: false, maxLength: 500),
                        P256dh = c.String(nullable: false, maxLength: 100),
                        Auth = c.String(nullable: false, maxLength: 100),
                        CreatedAt = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.StudyNotes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StudentId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 200),
                        Content = c.String(nullable: false),
                        Category = c.String(maxLength: 50),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .Index(t => t.StudentId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StudyNotes", "StudentId", "dbo.Students");
            DropForeignKey("dbo.PushSubscriptions", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.StudyNotes", new[] { "StudentId" });
            DropIndex("dbo.PushSubscriptions", new[] { "UserId" });
            DropTable("dbo.StudyNotes");
            DropTable("dbo.PushSubscriptions");
        }
    }
}
