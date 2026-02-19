using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace ASIB.Models;

public partial class AsibContext : DbContext
{
    public AsibContext()
    {
    }

    public AsibContext(DbContextOptions<AsibContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Action> Actions { get; set; }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<AdminAction> AdminActions { get; set; }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Connection> Connections { get; set; }

    public virtual DbSet<Embedding> Embeddings { get; set; }

    public virtual DbSet<Engagement> Engagements { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventRequest> EventRequests { get; set; }

    public virtual DbSet<Follow> Follows { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PasswordReset> PasswordResets { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Share> Shares { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<ASIB.Models.ViewModels.MyNetworkSuggestionRow> MyNetworkSuggestionRows { get; set; }
    public virtual DbSet<ASIB.Models.ViewModels.ViewPostRow> ViewPostRows { get; set; }
    public virtual DbSet<ASIB.Models.ViewModels.AdminPromotedAlumniRow> AdminPromotedAlumniRows { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;port=3306;database=asib;uid=root", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.4.32-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Action>(entity =>
        {
            entity.HasKey(e => e.ActionId).HasName("PRIMARY");

            entity.ToTable("action");

            entity.Property(e => e.ActionId)
                .HasColumnType("bigint(20)")
                .HasColumnName("action_id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(255)
                .HasColumnName("action_type");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EntityId)
                .HasColumnType("int(11)")
                .HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasColumnType("text")
                .HasColumnName("entity_type");
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PRIMARY");

            entity.ToTable("admins");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.Property(e => e.AdminId)
                .HasColumnType("bigint(20)")
                .HasColumnName("admin_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
        });

        modelBuilder.Entity<AdminAction>(entity =>
        {
            entity.HasKey(e => e.AdminActionId).HasName("PRIMARY");

            entity.ToTable("admin_actions");

            entity.HasIndex(e => e.AdminId, "fk_admin_actions_admin");

            entity.Property(e => e.AdminActionId)
                .HasColumnType("bigint(20)")
                .HasColumnName("admin_action_id");
            entity.Property(e => e.ActionTime)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("action_time");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .HasColumnName("action_type");
            entity.Property(e => e.AdminId)
                .HasColumnType("bigint(20)")
                .HasColumnName("admin_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Reason)
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.TargetEventId)
                .HasColumnType("bigint(20)")
                .HasColumnName("target_event_id");
            entity.Property(e => e.TargetPostId)
                .HasColumnType("bigint(20)")
                .HasColumnName("target_post_id");
            entity.Property(e => e.TargetUserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("target_user_id");

            entity.HasOne(d => d.Admin).WithMany(p => p.AdminActions)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("admin_actions_ibfk_1");
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("PRIMARY");

            entity.ToTable("announcements");

            entity.HasIndex(e => e.AdminId, "fk_announcements_admin");

            entity.Property(e => e.AnnouncementId)
                .HasColumnType("bigint(20)")
                .HasColumnName("announcement_id");
            entity.Property(e => e.AdminId)
                .HasColumnType("bigint(20)")
                .HasColumnName("admin_id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Admin).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("fk_announcements_admin");
        });

        modelBuilder.Entity<Connection>(entity =>
        {
            entity.HasKey(e => e.ConnectionId).HasName("PRIMARY");

            entity.ToTable("connections");

            entity.HasIndex(e => e.AddresseeId, "fk_connections_addressee");

            entity.HasIndex(e => new { e.RequesterId, e.AddresseeId }, "unique_connection").IsUnique();

            entity.Property(e => e.ConnectionId)
                .HasColumnType("bigint(20)")
                .HasColumnName("connection_id");
            entity.Property(e => e.AddresseeId)
                .HasColumnType("bigint(20)")
                .HasColumnName("addressee_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.RequesterId)
                .HasColumnType("bigint(20)")
                .HasColumnName("requester_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'pending'")
                .HasColumnType("enum('pending','accepted','declined','blocked')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Embedding>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("embeddings");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20)")
                .HasColumnName("id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Embedding1)
                .HasColumnType("text")
                .HasColumnName("embedding");
        });

        modelBuilder.Entity<Engagement>(entity =>
        {
            entity.HasKey(e => e.EngagementId).HasName("PRIMARY");

            entity.ToTable("engagements");

            entity.HasIndex(e => e.UserId, "fk_engagements_user");

            entity.HasIndex(e => new { e.PostId, e.UserId, e.EngagementType }, "post_user_type_idx");

            entity.Property(e => e.EngagementId)
                .HasColumnType("bigint(20)")
                .HasColumnName("engagement_id");
            entity.Property(e => e.Content)
                .HasComment("Used for comments")
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.EngagementType)
                .HasMaxLength(50)
                .HasComment("e.g., 'like', 'comment'")
                .HasColumnName("engagement_type");
            entity.Property(e => e.PostId)
                .HasColumnType("bigint(20)")
                .HasColumnName("post_id");
            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PRIMARY");

            entity.ToTable("events");

            entity.HasIndex(e => e.CreatedBy, "fk_events_created_by");

            entity.HasIndex(e => e.RoleId, "fk_events_role");

            entity.HasIndex(e => e.UserId, "fk_events_user");

            entity.Property(e => e.EventId)
                .HasColumnType("bigint(20)")
                .HasColumnName("event_id");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("bigint(20)")
                .HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp")
                .HasColumnName("end_time");
            entity.Property(e => e.MeetingUrl)
                .HasMaxLength(255)
                .HasColumnName("meeting_url");
            entity.Property(e => e.RoleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("role_id");
            entity.Property(e => e.StartTime)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("start_time");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<EventRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PRIMARY");

            entity.ToTable("event_requests");

            entity.HasIndex(e => new { e.EventId, e.UserId }, "event_user_unique").IsUnique();

            entity.HasIndex(e => e.UserId, "fk_eventrequests_user");

            entity.Property(e => e.RequestId)
                .HasColumnType("int(11)")
                .HasColumnName("request_id");
            entity.Property(e => e.EventId)
                .HasColumnType("bigint(20)")
                .HasColumnName("event_id");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("requested_at");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'pending'")
                .HasColumnType("enum('pending','approved','declined')")
                .HasColumnName("status");
            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(e => e.FollowId).HasName("PRIMARY");

            entity.ToTable("follows");

            entity.HasIndex(e => e.FollowingId, "fk_follows_following");

            entity.HasIndex(e => new { e.FollowerId, e.FollowingId }, "unique_follow").IsUnique();

            entity.Property(e => e.FollowId)
                .HasColumnType("bigint(20)")
                .HasColumnName("follow_id");
            entity.Property(e => e.FollowedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("followed_at");
            entity.Property(e => e.FollowerId)
                .HasColumnType("bigint(20)")
                .HasColumnName("follower_id");
            entity.Property(e => e.FollowingId)
                .HasColumnType("bigint(20)")
                .HasColumnName("following_id");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PRIMARY");

            entity.ToTable("messages");

            entity.HasIndex(e => e.ReceiverId, "fk_messages_receiver");

            entity.HasIndex(e => e.SenderId, "fk_messages_sender");

            entity.Property(e => e.MessageId)
                .HasColumnType("bigint(20)")
                .HasColumnName("message_id");
            entity.Property(e => e.AttachmentPath)
                .HasMaxLength(255)
                .HasColumnName("attachment_path");
            entity.Property(e => e.AttachmentType)
                .HasMaxLength(100)
                .HasColumnName("attachment_type");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.Deleted)
                .HasDefaultValueSql("'10'")
                .HasColumnName("deleted");
            entity.Property(e => e.ReceiverId)
                .HasColumnType("bigint(20)")
                .HasColumnName("receiver_id");
            entity.Property(e => e.SenderId)
                .HasColumnType("bigint(20)")
                .HasColumnName("sender_id");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("sent_at");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'0'")
                .HasColumnName("status");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PRIMARY");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.ReceiverId, "fk_notifications_receiver");

            entity.HasIndex(e => e.SenderId, "fk_notifications_sender");

            entity.HasIndex(e => new { e.ReceiverId, e.IsRead }, "idx_receiver_read");

            entity.Property(e => e.NotificationId)
                .HasColumnType("bigint(20)")
                .HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.EntityId)
                .HasComment("ID of the related item (post_id, sender_id, etc.)")
                .HasColumnType("bigint(20)")
                .HasColumnName("entity_id");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.ReceiverId)
                .HasColumnType("bigint(20)")
                .HasColumnName("receiver_id");
            entity.Property(e => e.SenderId)
                .HasColumnType("bigint(20)")
                .HasColumnName("sender_id");
            entity.Property(e => e.Type)
                .HasColumnType("enum('like','comment','follow','connect_request','connect_accept','new_post','message')")
                .HasColumnName("type");
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("password_resets");

            entity.HasIndex(e => e.UserId, "idx_user");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.OtpHash)
                .HasMaxLength(255)
                .HasColumnName("otp_hash");
            entity.Property(e => e.Used).HasColumnName("used");
            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PRIMARY");

            entity.ToTable("posts");

            entity.HasIndex(e => e.UserId, "fk_posts_user");

            entity.Property(e => e.PostId)
                .HasColumnType("bigint(20)")
                .HasColumnName("post_id");
            entity.Property(e => e.CommentsCount)
                .HasColumnType("int(11)")
                .HasColumnName("comments_count");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.LikesCount)
                .HasColumnType("int(11)")
                .HasColumnName("likes_count");
            entity.Property(e => e.PhotoUrl)
                .HasMaxLength(255)
                .HasColumnName("photo_url");
            entity.Property(e => e.PostType)
                .HasMaxLength(50)
                .HasDefaultValueSql("'general'")
                .HasColumnName("post_type");
            entity.Property(e => e.SharesCount)
                .HasColumnType("int(11)")
                .HasColumnName("shares_count");
            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PRIMARY");

            entity.ToTable("profiles");

            entity.HasIndex(e => e.UserId, "fk_profiles_user");

            entity.Property(e => e.ProfileId)
                .HasColumnType("bigint(20)")
                .HasColumnName("profile_id");
            entity.Property(e => e.Bio)
                .HasColumnType("text")
                .HasColumnName("bio");
            entity.Property(e => e.Experience)
                .HasColumnType("text")
                .HasColumnName("experience");
            entity.Property(e => e.Headline)
                .HasMaxLength(255)
                .HasColumnName("headline");
            entity.Property(e => e.PrivacySetting)
                .HasDefaultValueSql("'1'")
                .HasColumnName("privacy_setting");
            entity.Property(e => e.ProfilePhotoUrl)
                .HasMaxLength(255)
                .HasColumnName("profile_photo_url");
            entity.Property(e => e.Skills)
                .HasColumnType("text")
                .HasColumnName("skills");
            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PRIMARY");

            entity.ToTable("reports");

            entity.HasIndex(e => e.ReporterId, "reporter_id");

            entity.HasIndex(e => new { e.PostId, e.ReporterId }, "unique_report").IsUnique();

            entity.Property(e => e.ReportId)
                .HasColumnType("bigint(20)")
                .HasColumnName("report_id");
            entity.Property(e => e.PostId)
                .HasColumnType("bigint(20)")
                .HasColumnName("post_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasDefaultValueSql("'No reason provided.'")
                .HasColumnName("reason");
            entity.Property(e => e.ReportedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("reported_at");
            entity.Property(e => e.ReporterId)
                .HasColumnType("bigint(20)")
                .HasColumnName("reporter_id");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PRIMARY");

            entity.ToTable("role");

            entity.Property(e => e.RoleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("role_id");
            entity.Property(e => e.Role1)
                .HasMaxLength(100)
                .HasColumnName("role");
        });

        modelBuilder.Entity<Share>(entity =>
        {
            entity.HasKey(e => e.ShareId).HasName("PRIMARY");

            entity.ToTable("shares");

            entity.HasIndex(e => new { e.PostId, e.UserId }, "post_id").IsUnique();

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.ShareId)
                .HasColumnType("bigint(20)")
                .HasColumnName("share_id");
            entity.Property(e => e.PostId)
                .HasColumnType("bigint(20)")
                .HasColumnName("post_id");
            entity.Property(e => e.SharedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("shared_at");
            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.RoleId, "fk_users_role");

            entity.HasIndex(e => e.RoleRequested, "fk_users_role_requested");

            entity.HasIndex(e => e.EnrollmentNumber, "idx_enrollment_unique").IsUnique();

            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("user_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.BatchYear)
                .HasColumnType("int(11)")
                .HasColumnName("batch_year");
            entity.Property(e => e.ContactNumber)
                .HasColumnType("bigint(20)")
                .HasColumnName("contact_number");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.EnrollmentNumber)
                .HasMaxLength(100)
                .HasColumnName("enrollment_number");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.IsOnline)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_online");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.LastSeen)
                .HasColumnType("timestamp")
                .HasColumnName("last_seen");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(100)
                .HasColumnName("middle_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.RoleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("role_id");
            entity.Property(e => e.RoleRequested)
                .HasColumnType("bigint(20)")
                .HasColumnName("role_requested");
            entity.Property(e => e.VerificationStatus)
                .HasDefaultValueSql("'0'")
                .HasColumnName("verification_status");
        });

        modelBuilder.Entity<ASIB.Models.ViewModels.MyNetworkSuggestionRow>(entity =>
        {
            entity.HasNoKey();
        });

        modelBuilder.Entity<ASIB.Models.ViewModels.ViewPostRow>(entity =>
        {
            entity.HasNoKey();
        });

        modelBuilder.Entity<ASIB.Models.ViewModels.AdminPromotedAlumniRow>(entity =>
        {
            entity.HasNoKey();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
