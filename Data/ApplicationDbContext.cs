using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tbl_Menu> Tbl_Menus { get; set; }
        public DbSet<Tbl_UserRole> Tbl_UserRoles { get; set; }
        public DbSet<Tbl_RoleMenu> Tbl_RoleMenus { get; set; }
        public DbSet<Tbl_LoginAttempt> Tbl_LoginAttempts { get; set; }
        public DbSet<Tbl_AuditLog> Tbl_AuditLogs { get; set; }
        public DbSet<Tbl_Configuration> Tbl_Configurations { get; set; }
        public DbSet<Tbl_UserSession> Tbl_UserSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Rename Identity tables to have Tbl_ prefix
            builder.Entity<ApplicationUser>().ToTable("Tbl_Users");
            builder.Entity<ApplicationRole>().ToTable("Tbl_Roles");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("Tbl_UserRoles_Identity");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("Tbl_UserClaims");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("Tbl_UserLogins");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("Tbl_UserTokens");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("Tbl_RoleClaims");

            // Configure relationships
            builder.Entity<Tbl_Menu>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Tbl_UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();

            builder.Entity<Tbl_RoleMenu>()
                .HasIndex(rm => new { rm.RoleId, rm.MenuId })
                .IsUnique();

            builder.Entity<Tbl_Configuration>()
                .HasIndex(c => c.Key)
                .IsUnique();

            // Configure decimal precision
            builder.Entity<ApplicationUser>()
                .Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<ApplicationUser>()
                .Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            // Seed data will be handled in DataSeeder
        }
    }
}
