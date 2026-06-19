using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<ApiDataSource> ApiDataSources { get; set; }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Bookmark> Bookmarks { get; set; }

    public virtual DbSet<DashboardReport> DashboardReports { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Follow> Follows { get; set; }

    public virtual DbSet<GroupMember> GroupMembers { get; set; }

    public virtual DbSet<Journal> Journals { get; set; }

    public virtual DbSet<Keyword> Keywords { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Paper> Papers { get; set; }

    public virtual DbSet<PaperAuthor> PaperAuthors { get; set; }

    public virtual DbSet<PaperCitation> PaperCitations { get; set; }

    public virtual DbSet<PublicationTrend> PublicationTrends { get; set; }

    public virtual DbSet<ResearchGroup> ResearchGroups { get; set; }

    public virtual DbSet<ResearchTopic> ResearchTopics { get; set; }

    public virtual DbSet<RevokedToken> RevokedTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SyncJob> SyncJobs { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<TrendSnapshot> TrendSnapshots { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.ActivityLogId);

            entity.HasIndex(e => e.CreatedAt, "IX_ActivityLogs_CreatedAt");
            entity.HasIndex(e => e.UserId, "IX_ActivityLogs_UserId");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.TargetType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

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
            entity.Property(e => e.ResearchArea).HasMaxLength(300);
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

        modelBuilder.Entity<DashboardReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Dashboar__D5BD48054FB0E0D0");

            entity.Property(e => e.ReportName).HasMaxLength(200);
            entity.Property(e => e.ReportType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.DashboardReports)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Dashboard__UserI__5070F446");
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

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.UserId }).HasName("PK__GroupMem__C5E27FAE44EE6647");

            entity.Property(e => e.RoleInGroup).HasMaxLength(50);

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GroupMemb__Group__68487DD7");

            entity.HasOne(d => d.User).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GroupMemb__UserI__693CA210");
        });

        modelBuilder.Entity<Journal>(entity =>
        {
            entity.HasKey(e => e.JournalId).HasName("PK__Journals__250103E6EDD19799");

            entity.Property(e => e.ContactEmail).HasMaxLength(255);
            entity.Property(e => e.Issn)
                .HasMaxLength(50)
                .HasColumnName("ISSN");
            entity.Property(e => e.JournalName).HasMaxLength(300);
            entity.Property(e => e.Publisher).HasMaxLength(200);
            entity.Property(e => e.Website).HasMaxLength(500);
        });

        modelBuilder.Entity<Keyword>(entity =>
        {
            entity.HasKey(e => e.KeywordId).HasName("PK__Keywords__37C13521B41C6024");

            entity.HasIndex(e => e.KeywordText, "IX_Keywords_Text");

            entity.HasIndex(e => e.KeywordText, "UQ__Keywords__219EE3D704701796").IsUnique();

            entity.HasIndex(e => e.TopicId, "IX_Keywords_TopicId");

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

            entity.HasIndex(e => e.JournalId, "IX_Papers_JournalId");
            entity.HasIndex(e => e.PublicationYear, "IX_Papers_Year");
            entity.HasIndex(e => e.SourceId, "IX_Papers_SourceId");

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
                .UsingEntity<PaperAuthor>(
                    r => r.HasOne(d => d.Author).WithMany(p => p.PaperAuthors)
                        .HasForeignKey(d => d.AuthorId)
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PaperAuth__Autho__3C69FB99"),
                    l => l.HasOne(d => d.Paper).WithMany(p => p.PaperAuthors)
                        .HasForeignKey(d => d.PaperId)
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PaperAuth__Paper__3B75D760"),
                    j =>
                    {
                        j.HasKey(e => new { e.PaperId, e.AuthorId }).HasName("PK__PaperAut__FC8BBDC843D72F9A");
                        j.HasIndex(e => e.AuthorId, "IX_PaperAuthors_AuthorId");
                        j.Property(e => e.Affiliation).HasMaxLength(300);
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
                        j.HasIndex(new[] { "KeywordId" }, "IX_PaperKeywords_KeywordId");
                        j.ToTable("PaperKeywords");
                    });
        });

        modelBuilder.Entity<PaperCitation>(entity =>
        {
            entity.HasKey(e => e.CitationId).HasName("PK__PaperCit__EAD2ADFB7C803DC4");

            entity.HasOne(d => d.CitedPaper).WithMany(p => p.PaperCitationCitedPapers)
                .HasForeignKey(d => d.CitedPaperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PaperCita__Cited__628FA481");

            entity.HasOne(d => d.CitingPaper).WithMany(p => p.PaperCitationCitingPapers)
                .HasForeignKey(d => d.CitingPaperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PaperCita__Citin__619B8048");
        });

        modelBuilder.Entity<PublicationTrend>(entity =>
        {
            entity.HasKey(e => e.TrendId).HasName("PK__Publicat__DACD10F79107C7D0");

            entity.HasIndex(e => e.KeywordId, "IX_PublicationTrends_KeywordId");

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

        modelBuilder.Entity<ResearchGroup>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("PK__Research__149AF36A1F2F87A7");

            entity.Property(e => e.GroupName).HasMaxLength(200);

            entity.HasOne(d => d.Owner).WithMany(p => p.ResearchGroups)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ResearchG__Owner__656C112C");
        });

        modelBuilder.Entity<ResearchTopic>(entity =>
        {
            entity.HasKey(e => e.TopicId).HasName("PK__Research__022E0F5D6DD9197C");

            entity.HasIndex(e => e.TopicName, "UQ__Research__6C795E8C662E12F0").IsUnique();

            entity.Property(e => e.TopicName).HasMaxLength(150);
        });

        modelBuilder.Entity<RevokedToken>(entity =>
        {
            entity.HasKey(e => e.RevokedTokenId);

            entity.HasIndex(e => e.TokenHash, "IX_RevokedTokens_TokenHash").IsUnique();
            entity.HasIndex(e => e.ExpiresAt, "IX_RevokedTokens_ExpiresAt");
            entity.HasIndex(e => e.UserId, "IX_RevokedTokens_UserId");

            entity.Property(e => e.TokenHash).HasMaxLength(255);
            entity.Property(e => e.RevokedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.RevokedTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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

            entity.HasIndex(e => e.SourceId, "IX_SyncJobs_SourceId");

            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Source).WithMany(p => p.SyncJobs)
                .HasForeignKey(d => d.SourceId)
                .HasConstraintName("FK__SyncJobs__Source__5AEE82B9");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PK__SystemSe__01E719AC4ECCE7D3");

            entity.Property(e => e.SettingKey).HasMaxLength(100);
        });

        modelBuilder.Entity<TrendSnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId);

            entity.HasIndex(e => e.SnapshotDate, "IX_TrendSnapshots_Date");

            entity.HasIndex(e => new { e.KeywordId, e.SnapshotDate }, "IX_TrendSnapshots_Keyword_Date");

            entity.HasIndex(e => new { e.TopicId, e.SnapshotDate }, "IX_TrendSnapshots_Topic_Date");

            entity.Property(e => e.SnapshotDate).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Keyword).WithMany(p => p.TrendSnapshots)
                .HasForeignKey(d => d.KeywordId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Topic).WithMany(p => p.TrendSnapshots)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CABBA0262");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105340115A41D").IsUnique();

            entity.Property(e => e.ActorType)
                .HasMaxLength(50)
                .HasDefaultValue("Student");
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
                        j.HasIndex(new[] { "RoleId" }, "IX_UserRoles_RoleId");
                        j.ToTable("UserRoles");
                    });
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.PreferenceId).HasName("PK__UserPref__E228496F80308D5A");

            entity.Property(e => e.NotificationFrequency).HasMaxLength(50);
            entity.Property(e => e.PreferredField).HasMaxLength(150);
            entity.Property(e => e.PreferredYearRange).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.UserPreferences)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserPrefe__UserI__2E1BDC42");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
