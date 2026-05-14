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

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<ApiDataSource> ApiDataSources { get; set; }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Bookmark> Bookmarks { get; set; }

    public virtual DbSet<DashboardReport> DashboardReports { get; set; }

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

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SyncJob> SyncJobs { get; set; }

    public virtual DbSet<TrendSnapshot> TrendSnapshots { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=MINHDUCK\\DUCDEPTRAI;uid=sa;pwd=12345;database=ScientificTrendDB;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Activity__5E548648982FB5A3");

            entity.Property(e => e.ActionType).HasMaxLength(100);
            entity.Property(e => e.EntityName).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__ActivityL__UserI__5BE2A6F2");
        });

        modelBuilder.Entity<ApiDataSource>(entity =>
        {
            entity.HasKey(e => e.SourceId).HasName("PK__ApiDataS__16E01919A65725D0");

            entity.Property(e => e.BaseUrl).HasMaxLength(500);
            entity.Property(e => e.SourceName).HasMaxLength(100);
        });

        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.AuthorId).HasName("PK__Authors__70DAFC34739A1F7A");

            entity.Property(e => e.AuthorName).HasMaxLength(200);
            entity.Property(e => e.ResearchArea).HasMaxLength(300);
        });

        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasKey(e => e.BookmarkId).HasName("PK__Bookmark__541A3B719434DCD2");

            entity.Property(e => e.TargetType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Bookmarks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Bookmarks__UserI__534D60F1");
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

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(e => e.FollowId).HasName("PK__Follows__2CE810AE04754E13");

            entity.Property(e => e.TargetType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Follows)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Follows__UserId__5629CD9C");
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
            entity.HasKey(e => e.JournalId).HasName("PK__Journals__250103E6E5562641");

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
            entity.HasKey(e => e.KeywordId).HasName("PK__Keywords__37C13521DBEF16B8");

            entity.HasIndex(e => e.KeywordText, "UQ__Keywords__219EE3D77ED6770E").IsUnique();

            entity.Property(e => e.KeywordText).HasMaxLength(150);

            entity.HasOne(d => d.Topic).WithMany(p => p.Keywords)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK__Keywords__TopicI__4222D4EF");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E126B6F5A46");

            entity.Property(e => e.RelatedType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__59063A47");
        });

        modelBuilder.Entity<Paper>(entity =>
        {
            entity.HasKey(e => e.PaperId).HasName("PK__Papers__AB86120BEF591324");

            entity.Property(e => e.ExternalId).HasMaxLength(200);

            entity.HasOne(d => d.Journal).WithMany(p => p.Papers)
                .HasForeignKey(d => d.JournalId)
                .HasConstraintName("FK__Papers__JournalI__34C8D9D1");

            entity.HasOne(d => d.Source).WithMany(p => p.Papers)
                .HasForeignKey(d => d.SourceId)
                .HasConstraintName("FK__Papers__SourceId__35BCFE0A");

            entity.HasMany(d => d.Keywords).WithMany(p => p.Papers)
                .UsingEntity<Dictionary<string, object>>(
                    "PaperKeyword",
                    r => r.HasOne<Keyword>().WithMany()
                        .HasForeignKey("KeywordId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PaperKeyw__Keywo__45F365D3"),
                    l => l.HasOne<Paper>().WithMany()
                        .HasForeignKey("PaperId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PaperKeyw__Paper__44FF419A"),
                    j =>
                    {
                        j.HasKey("PaperId", "KeywordId").HasName("PK__PaperKey__A8FA0159877AA10A");
                        j.ToTable("PaperKeywords");
                    });
        });

        modelBuilder.Entity<PaperAuthor>(entity =>
        {
            entity.HasKey(e => new { e.PaperId, e.AuthorId }).HasName("PK__PaperAut__FC8BBDC843D72F9A");

            entity.Property(e => e.Affiliation).HasMaxLength(300);

            entity.HasOne(d => d.Author).WithMany(p => p.PaperAuthors)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PaperAuth__Autho__3B75D760");

            entity.HasOne(d => d.Paper).WithMany(p => p.PaperAuthors)
                .HasForeignKey(d => d.PaperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PaperAuth__Paper__3A81B327");
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
            entity.HasKey(e => e.TrendId).HasName("PK__Publicat__DACD10F7E2878934");

            entity.HasOne(d => d.Keyword).WithMany(p => p.PublicationTrends)
                .HasForeignKey(d => d.KeywordId)
                .HasConstraintName("FK__Publicati__Keywo__49C3F6B7");

            entity.HasOne(d => d.Topic).WithMany(p => p.PublicationTrends)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK__Publicati__Topic__48CFD27E");
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
            entity.HasKey(e => e.TopicId).HasName("PK__Research__022E0F5DDEEF26DC");

            entity.HasIndex(e => e.TopicName, "UQ__Research__6C795E8C4D969ED0").IsUnique();

            entity.Property(e => e.TopicName).HasMaxLength(150);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AE0C5E1C0");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160B7C191F3").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SyncJob>(entity =>
        {
            entity.HasKey(e => e.SyncJobId).HasName("PK__SyncJobs__1078C047BEFB0210");

            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Source).WithMany(p => p.SyncJobs)
                .HasForeignKey(d => d.SourceId)
                .HasConstraintName("FK__SyncJobs__Source__5EBF139D");
        });

        modelBuilder.Entity<TrendSnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId).HasName("PK__TrendSna__664F572B4234EE71");

            entity.HasOne(d => d.Keyword).WithMany(p => p.TrendSnapshots)
                .HasForeignKey(d => d.KeywordId)
                .HasConstraintName("FK__TrendSnap__Keywo__4D94879B");

            entity.HasOne(d => d.Topic).WithMany(p => p.TrendSnapshots)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK__TrendSnap__Topic__4CA06362");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C8858B5F2");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534499ED822").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__RoleI__2B3F6F97"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__UserI__2A4B4B5E"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF2760ADCF1A8E01");
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
