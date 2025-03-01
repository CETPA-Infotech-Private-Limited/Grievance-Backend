using Grievance_DAL.DbModels;
using Microsoft.EntityFrameworkCore;

namespace Grievance_DAL.DatabaseContext
{
    public class GrievanceDbContext : DbContext
    {
        public GrievanceDbContext(DbContextOptions<GrievanceDbContext> options) : base(options)
        {

        }

        public void EnsureTableCreated()
        {
            var createTableQuery = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ErrorLogs')
                BEGIN
                 CREATE TABLE ErrorLogs (
                     Id INT PRIMARY KEY IDENTITY,
                     StatuCode INT NOT NULL,
                     Message NVARCHAR(Max)  NULL,
                     Details NVARCHAR(Max)  NULL,
                     RequestPath NVARCHAR(250)  NULL,
                     StackTrace NVARCHAR(Max)  NULL,
                     CreatedDateTime DateTime NOT NULL,
                 )
             END";
            Database.ExecuteSqlRaw(createTableQuery);

        }

        // Master Tables
        public DbSet<AppRole> AppRoles { get; set; }
        public DbSet<ServiceMaster> Services { get; set; }
        public DbSet<GroupMaster> Groups { get; set; }
        public DbSet<GrievanceStatus> GrievanceStatuses { get; set; }

        // Mapping Tables
        public DbSet<UserRoleMapping> UserRoleMappings { get; set; }
        public DbSet<UserGroupMapping> UserGroupMappings { get; set; }
        public DbSet<UserDepartmentMapping> UserDepartmentMappings { get; set; }

        // Grievance Related Tables
        public DbSet<GrievanceMaster> GrievanceMasters { get; set; }
        public DbSet<GrievanceProcess> GrievanceProcesses { get; set; }

        // Supporting Tables
        public DbSet<AttachmentDetail> Attachments { get; set; }
        public DbSet<CommentDetail> Comments { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GrievanceMaster>()
                .HasOne(g => g.ServiceMaster)
                .WithMany()
                .HasForeignKey(g => g.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GrievanceMaster>()
                .HasOne(g => g.Status)
                .WithMany()
                .HasForeignKey(g => g.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GrievanceProcess>()
                .HasOne(g => g.ServiceMaster)
                .WithMany()
                .HasForeignKey(g => g.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GrievanceProcess>()
                .HasOne(gp => gp.Status)
                .WithMany()
                .HasForeignKey(gp => gp.StatusId)
                .OnDelete(DeleteBehavior.Restrict); 
        }

    }
}
