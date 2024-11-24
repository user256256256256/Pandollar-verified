using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class PandollarDbContext : DbContext
{
    public PandollarDbContext()
    {
    }

    public PandollarDbContext(DbContextOptions<PandollarDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<ClientAddress> ClientAddresses { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<CompanyAddress> CompanyAddresses { get; set; }

    public virtual DbSet<CompanyClient> CompanyClients { get; set; }

    public virtual DbSet<DataMigration> DataMigrations { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-AUHHGDP\\SQLEXPRESS;Initial Catalog=PandollarDB;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedName] IS NOT NULL)");

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.Property(e => e.RoleId).IsRequired();

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedUserName] IS NOT NULL)");

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.Property(e => e.UserId).IsRequired();

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.Property(e => e.UserId).IsRequired();

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK__AuditLog__EB5F6CBD75CDDDCB");

            entity.Property(e => e.AuditLogId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(e => e.CompanyId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ComplianceStatus).HasMaxLength(100);
            entity.Property(e => e.DeviceInfo).HasMaxLength(512);
            entity.Property(e => e.EntityAffected)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasOne(d => d.Company).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_AuditLogs_Company");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AuditLogs__UserI__0D7A0286");
        });

        modelBuilder.Entity<ClientAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId);

            entity.ToTable("ClientAddress");

            entity.Property(e => e.AddressId).ValueGeneratedNever();
            entity.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Country)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Street)
                .IsRequired()
                .HasMaxLength(256);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(e => e.CompanyId).ValueGeneratedNever();
            entity.Property(e => e.ApiCode).HasMaxLength(256);
            entity.Property(e => e.CompanyEmail).HasMaxLength(256);
            entity.Property(e => e.CompanyInitials).HasMaxLength(10);
            entity.Property(e => e.CompanyLogoFilePath).HasMaxLength(512);
            entity.Property(e => e.CompanyName).HasMaxLength(256);
            entity.Property(e => e.CompanyPhone).HasMaxLength(15);
            entity.Property(e => e.CompanyStatus).HasMaxLength(50);
            entity.Property(e => e.CompanyType).HasMaxLength(100);
            entity.Property(e => e.ContactPerson).HasMaxLength(256);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ExpDate).HasColumnType("datetime");
            entity.Property(e => e.Motto).HasMaxLength(512);
            entity.Property(e => e.PayAccount).HasMaxLength(256);
            entity.Property(e => e.PayAccountName).HasMaxLength(256);
            entity.Property(e => e.PayBank).HasMaxLength(256);
            entity.Property(e => e.SmsAccount).HasMaxLength(256);
            entity.Property(e => e.SubscriptionAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Address).WithMany(p => p.Companies)
                .HasForeignKey(d => d.AddressId)
                .HasConstraintName("FK_Companies_CompanyAddress");
        });

        modelBuilder.Entity<CompanyAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId);

            entity.ToTable("CompanyAddress");

            entity.Property(e => e.AddressId).ValueGeneratedNever();
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Street).HasMaxLength(256);
        });

        modelBuilder.Entity<CompanyClient>(entity =>
        {
            entity.HasKey(e => e.ClientId);

            entity.Property(e => e.ClientId).ValueGeneratedNever();
            entity.Property(e => e.ClientName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(256);
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(15);
            entity.Property(e => e.Gender)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.MaritalStatus)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Nationality)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PhoneNumber)
                .IsRequired()
                .HasMaxLength(15);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Address).WithMany(p => p.CompanyClients)
                .HasForeignKey(d => d.AddressId)
                .HasConstraintName("FK_Client_Address");

            entity.HasOne(d => d.Company).WithMany(p => p.CompanyClients)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Company_Client");
        });

        modelBuilder.Entity<DataMigration>(entity =>
        {
            entity.HasKey(e => e.MigrationId).HasName("PK__DataMigr__E5D3573B40556A31");

            entity.ToTable("DataMigration");

            entity.Property(e => e.MigrationId).ValueGeneratedNever();
            entity.Property(e => e.DestinationSystem)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.SourceSystem)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(d => d.Company).WithMany(p => p.DataMigrations)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_DataMigration_Company");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDD6D745BCAA");

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CompanyId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.FeedbackText).IsRequired();
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasOne(d => d.Company).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_Feedback_Company");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Feedback__UserId__08B54D69");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserProfileId).HasName("PK__UserProf__9E267F622315FB7B");

            entity.ToTable("UserProfile");

            entity.Property(e => e.UserProfileId).ValueGeneratedNever();
            entity.Property(e => e.ProfileImagePath).HasMaxLength(255);
            entity.Property(e => e.UserBio).HasMaxLength(1000);
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.UserProfiles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserProfi__UserI__114A936A");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
