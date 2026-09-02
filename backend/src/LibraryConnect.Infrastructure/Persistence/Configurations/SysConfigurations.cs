using LibraryConnect.Domain.Entities.Sys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryConnect.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Position).HasMaxLength(200);
        builder.Property(x => x.Department).HasMaxLength(200);
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);

        // Filtered so a deleted account frees its username for reuse.
        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_users_username");
    }
}

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_user_groups_code");
    }
}

public class UserGroupMemberConfiguration : IEntityTypeConfiguration<UserGroupMember>
{
    public void Configure(EntityTypeBuilder<UserGroupMember> builder)
    {
        builder.HasOne(x => x.User).WithMany(u => u.Groups)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Group).WithMany(g => g.Members)
            .HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.GroupId }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_user_group_members");
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Group).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_permissions_code");
    }
}

public class GroupPermissionConfiguration : IEntityTypeConfiguration<GroupPermission>
{
    public void Configure(EntityTypeBuilder<GroupPermission> builder)
    {
        builder.HasOne(x => x.Group).WithMany(g => g.Permissions)
            .HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Permission).WithMany(p => p.Groups)
            .HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.GroupId, x.PermissionId }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_group_permissions");
    }
}

public class UserDataScopeConfiguration : IEntityTypeConfiguration<UserDataScope>
{
    public void Configure(EntityTypeBuilder<UserDataScope> builder)
    {
        builder.HasOne(x => x.User).WithMany(u => u.DataScopes)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.ScopeType, x.ScopeId }).IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_user_data_scopes");
    }
}

public class SystemParameterConfiguration : IEntityTypeConfiguration<SystemParameter>
{
    public void Configure(EntityTypeBuilder<SystemParameter> builder)
    {
        builder.Property(x => x.Key).HasMaxLength(150).IsRequired();
        builder.Property(x => x.GroupCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.GroupName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Options).HasColumnType("jsonb");

        builder.HasIndex(x => x.Key).IsUnique().HasDatabaseName("ux_system_parameters_key");
        builder.HasIndex(x => x.GroupCode).HasDatabaseName("ix_system_parameters_group");
    }
}

public class SystemParameterHistoryConfiguration : IEntityTypeConfiguration<SystemParameterHistory>
{
    public void Configure(EntityTypeBuilder<SystemParameterHistory> builder)
    {
        builder.Property(x => x.Key).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new { x.Key, x.ChangedAt }).HasDatabaseName("ix_parameter_history_key");
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(x => x.Username).HasMaxLength(100);
        builder.Property(x => x.Ip).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.Entity).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.EntityDisplay).HasMaxLength(500);
        builder.Property(x => x.RequestPath).HasMaxLength(500);
        builder.Property(x => x.OldValue).HasColumnType("jsonb");
        builder.Property(x => x.NewValue).HasColumnType("jsonb");

        builder.HasIndex(x => x.OccurredAt).IsDescending().HasDatabaseName("ix_audit_occurred");
        builder.HasIndex(x => new { x.Entity, x.EntityId }).HasDatabaseName("ix_audit_entity");
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_audit_user");
    }
}

public class AuditSettingConfiguration : IEntityTypeConfiguration<AuditSetting>
{
    public void Configure(EntityTypeBuilder<AuditSetting> builder)
    {
        builder.Property(x => x.Entity).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Entity).IsUnique().HasDatabaseName("ux_audit_settings_entity");
    }
}

public class BackupJobConfiguration : IEntityTypeConfiguration<BackupJob>
{
    public void Configure(EntityTypeBuilder<BackupJob> builder)
    {
        builder.Property(x => x.FileName).HasMaxLength(300);
        builder.Property(x => x.FilePath).HasMaxLength(1000);
        builder.Property(x => x.Checksum).HasMaxLength(128);
        builder.HasIndex(x => x.StartedAt).IsDescending().HasDatabaseName("ix_backup_started");
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(x => x.Type).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Link).HasMaxLength(500);

        builder.HasIndex(x => new { x.ReaderId, x.IsRead }).HasDatabaseName("ix_notifications_reader");
        builder.HasIndex(x => new { x.UserId, x.IsRead }).HasDatabaseName("ix_notifications_user");
    }
}

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.Property(x => x.Token).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Platform).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DeviceName).HasMaxLength(200);
        builder.Property(x => x.AppVersion).HasMaxLength(50);

        builder.HasIndex(x => x.Token).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_device_tokens_token");
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedIp).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.RevokedReason).HasMaxLength(200);

        builder.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TokenHash).HasDatabaseName("ix_refresh_tokens_hash");
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_refresh_tokens_expires");
    }
}

public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(300);
        builder.Property(x => x.Ip).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);

        builder.HasIndex(x => new { x.UserId, x.OccurredAt }).HasDatabaseName("ix_login_history_user");
    }
}

public class CodeSequenceConfiguration : IEntityTypeConfiguration<CodeSequence>
{
    public void Configure(EntityTypeBuilder<CodeSequence> builder)
    {
        builder.ToTable("code_sequences", "sys");
        builder.HasKey(x => new { x.Key, x.Scope });
        builder.Property(x => x.Key).HasMaxLength(50).HasColumnName("key");
        builder.Property(x => x.Scope).HasMaxLength(20).HasColumnName("scope");
        builder.Property(x => x.CurrentValue).HasColumnName("current_value");
    }
}

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.Property(x => x.Kind).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.ReaderId, x.Kind }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_notification_preferences_reader_kind");
    }
}

public class CheckoutStationConfiguration : IEntityTypeConfiguration<LibraryConnect.Domain.Entities.Cir.CheckoutStation>
{
    public void Configure(EntityTypeBuilder<LibraryConnect.Domain.Entities.Cir.CheckoutStation> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(500);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_checkout_stations_code");
    }
}

public class DigitalOfflinePackageConfiguration : IEntityTypeConfiguration<LibraryConnect.Domain.Entities.Dig.DigitalOfflinePackage>
{
    public void Configure(EntityTypeBuilder<LibraryConnect.Domain.Entities.Dig.DigitalOfflinePackage> builder)
    {
        builder.Property(x => x.KeyBase64).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IvBase64).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Checksum).HasMaxLength(128);
        builder.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ReaderId, x.ExpiresAt }).HasDatabaseName("ix_digital_offline_packages_reader");
    }
}
