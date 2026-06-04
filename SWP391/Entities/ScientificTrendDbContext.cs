using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SWP391.Entities;

public partial class ScientificTrendDbContext : DbContext
{
    public ScientificTrendDbContext()
    {
    }

    public ScientificTrendDbContext(DbContextOptions<ScientificTrendDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApiDataSource> ApiDataSources { get; set; }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Bookmark> Bookmarks { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Follow> Follows { get; set; }

    public virtual DbSet<Journal> Journals { get; set; }

    public virtual DbSet<Keyword> Keywords { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Paper> Papers { get; set; }

    public virtual DbSet<PublicationTrend> PublicationTrends { get; set; }

    public virtual DbSet<ResearchTopic> ResearchTopics { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SyncJob> SyncJobs { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    private string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
             .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", true, true)
                    .Build();
        var strConn = config["ConnectionStrings:DefaultConnection"];

        return strConn;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(GetConnectionString());
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiDataSource>(entity =>
        {
            entity.HasKey(e => e.SourceId).HasName("PK__ApiDataS__16E0191954850B1E");

            entity.Property(e => e.BaseUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SourceName).HasMaxLength(100);
        });

        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.AuthorId).HasName("PK__Authors__70DAFC347F30D68E");

            entity.Property(e => e.AuthorName).HasMaxLength(200);
        });

        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasKey(e => e.BookmarkId).HasName("PK__Bookmark__541A3B71063868DC");

            entity.HasIndex(e => e.UserId, "IX_Bookmarks_User");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TargetType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Bookmarks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Bookmarks__UserI__4F7CD00D");
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.EmailVerificationTokenId);

            entity.HasIndex(e => e.TokenHash, "IX_EmailVerificationTokens_TokenHash");

            entity.HasIndex(e => new { e.UserId, e.UsedAt }, "IX_EmailVerificationTokens_User_UsedAt");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TokenHash).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(e => e.FollowId).HasName("PK__Follows__2CE810AE40649A39");

            entity.HasIndex(e => e.UserId, "IX_Follows_User");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TargetType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Follows)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Follows__UserId__534D60F1");
        });

        modelBuilder.Entity<Journal>(entity =>
        {
            entity.HasKey(e => e.JournalId).HasName("PK__Journals__250103E6EDD19799");

            entity.Property(e => e.Issn)
                .HasMaxLength(50)
                .HasColumnName("ISSN");
            entity.Property(e => e.JournalName).HasMaxLength(300);
            entity.Property(e => e.Publisher).HasMaxLength(200);
        });

        modelBuilder.Entity<Keyword>(entity =>
        {
            entity.HasKey(e => e.KeywordId).HasName("PK__Keywords__37C13521B41C6024");

            entity.HasIndex(e => e.KeywordText, "IX_Keywords_Text");

            entity.HasIndex(e => e.KeywordText, "UQ__Keywords__219EE3D704701796").IsUnique();

            entity.Property(e => e.KeywordText).HasMaxLength(150);

            entity.HasOne(d => d.Topic).WithMany(p => p.Keywords)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK__Keywords__TopicI__4316F928");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12AD2102BE");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "IX_Notifications_User");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.RelatedType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__5812160E");
        });

        modelBuilder.Entity<Paper>(entity =>
        {
            entity.HasKey(e => e.PaperId).HasName("PK__Papers__AB86120B6AA80FBB");

            entity.HasIndex(e => e.PublicationYear, "IX_Papers_Year");

            entity.Property(e => e.CitationCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ExternalId).HasMaxLength(200);

            entity.HasOne(d => d.Journal).WithMany(p => p.Papers)
                .HasForeignKey(d => d.JournalId)
                .HasConstraintName("FK__Papers__JournalI__35BCFE0A");

            entity.HasOne(d => d.Source).WithMany(p => p.Papers)
                .HasForeignKey(d => d.SourceId)
                .HasConstraintName("FK__Papers__SourceId__36B12243");

            entity.HasMany(d => d.Authors).WithMany(p => p.Papers)
                .UsingEntity<Dictionary<string, object>>(
                    "PaperAuthor",
                    r => r.HasOne<Author>().WithMany()
                        .HasForeignKey("AuthorId")
                        .HasConstraintName("FK__PaperAuth__Autho__3C69FB99"),
                    l => l.HasOne<Paper>().WithMany()
                        .HasForeignKey("PaperId")
                        .HasConstraintName("FK__PaperAuth__Paper__3B75D760"),
                    j =>
                    {
                        j.HasKey("PaperId", "AuthorId");
                        j.ToTable("PaperAuthors");
                    });

            entity.HasMany(d => d.Keywords).WithMany(p => p.Papers)
                .UsingEntity<Dictionary<string, object>>(
                    "PaperKeyword",
                    r => r.HasOne<Keyword>().WithMany()
                        .HasForeignKey("KeywordId")
                        .HasConstraintName("FK__PaperKeyw__Keywo__46E78A0C"),
                    l => l.HasOne<Paper>().WithMany()
                        .HasForeignKey("PaperId")
                        .HasConstraintName("FK__PaperKeyw__Paper__45F365D3"),
                    j =>
                    {
                        j.HasKey("PaperId", "KeywordId");
                        j.ToTable("PaperKeywords");
                    });
        });

        modelBuilder.Entity<PublicationTrend>(entity =>
        {
            entity.HasKey(e => e.TrendId).HasName("PK__Publicat__DACD10F79107C7D0");

            entity.HasIndex(e => e.TrendYear, "IX_PublicationTrends_Year");

            entity.HasIndex(e => new { e.TopicId, e.TrendYear }, "IX_Trends_TopicYear");

            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Keyword).WithMany(p => p.PublicationTrends)
                .HasForeignKey(d => d.KeywordId)
                .HasConstraintName("FK__Publicati__Keywo__4BAC3F29");

            entity.HasOne(d => d.Topic).WithMany(p => p.PublicationTrends)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK__Publicati__Topic__4AB81AF0");
        });

        modelBuilder.Entity<ResearchTopic>(entity =>
        {
            entity.HasKey(e => e.TopicId).HasName("PK__Research__022E0F5D6DD9197C");

            entity.HasIndex(e => e.TopicName, "UQ__Research__6C795E8C662E12F0").IsUnique();

            entity.Property(e => e.TopicName).HasMaxLength(150);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AAEFBCBC7");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61607F418F30").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);

            entity.HasData(
                new Role { RoleId = 1, RoleName = "Administrator" },
                new Role { RoleId = 2, RoleName = "Researcher" },
                new Role { RoleId = 3, RoleName = "Member" });
        });

        modelBuilder.Entity<SyncJob>(entity =>
        {
            entity.HasKey(e => e.SyncJobId).HasName("PK__SyncJobs__1078C047AFB584C5");

            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Source).WithMany(p => p.SyncJobs)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SyncJobs__Source__5AEE82B9");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PK__SystemSe__01E719AC4ECCE7D3");

            entity.Property(e => e.SettingKey).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CABBA0262");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105340115A41D").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DateOfBirth).HasColumnType("date");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK__UserRoles__RoleI__2D27B809"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK__UserRoles__UserI__2C3393D0"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
