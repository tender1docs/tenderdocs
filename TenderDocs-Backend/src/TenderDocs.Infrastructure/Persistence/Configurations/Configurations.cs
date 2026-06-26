using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Infrastructure.Persistence.Configurations;

public class OrganizationConfig : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> b)
    {
        b.ToTable("organizations");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.GoogleId);
        b.Property(x => x.FullName).HasMaxLength(120).IsRequired();
        b.Property(x => x.Initials).HasMaxLength(4).IsRequired();
        b.HasOne(x => x.Organization).WithMany(o => o.Users)
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.Property(x => x.Token).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.Token).IsUnique();
        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Ignore(x => x.IsActive);
    }
}

public class FolderConfig : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> b)
    {
        b.ToTable("folders");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.MaterializedPath).HasMaxLength(2048).IsRequired();
        b.HasIndex(x => x.MaterializedPath);                 // fast subtree retrieval
        b.HasIndex(x => new { x.OrganizationId, x.ParentFolderId });
        b.HasOne(x => x.ParentFolder).WithMany(f => f.Children)
            .HasForeignKey(x => x.ParentFolderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ProjectConfig : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.ToTable("projects");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.HasIndex(x => new { x.OrganizationId, x.Name });
        b.HasOne(x => x.RootFolder).WithMany().HasForeignKey(x => x.RootFolderId).OnDelete(DeleteBehavior.SetNull);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class DocumentConfig : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("documents");
        b.Property(x => x.Name).HasMaxLength(260).IsRequired();
        b.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(150);
        b.Property(x => x.IssuingAuthority).HasMaxLength(200);
        b.Property(x => x.FinancialYear).HasMaxLength(20);
        b.Property(x => x.RejectionReason).HasMaxLength(1000);
        b.HasIndex(x => new { x.OrganizationId, x.DocumentType });
        b.HasIndex(x => new { x.OrganizationId, x.ExpiryDate });
        b.HasIndex(x => new { x.OrganizationId, x.ApprovalStatus });
        b.HasOne(x => x.Folder).WithMany(f => f.Documents).HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ApprovedBy).WithMany().HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.SetNull);
        b.Ignore(x => x.Organization);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class DocumentVersionConfig : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> b)
    {
        b.ToTable("document_versions");
        b.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        b.HasOne(x => x.Document).WithMany(d => d.Versions)
            .HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique();
    }
}

public class ProjectRequirementCategoryConfig : IEntityTypeConfiguration<ProjectRequirementCategory>
{
    public void Configure(EntityTypeBuilder<ProjectRequirementCategory> b)
    {
        b.ToTable("project_requirement_categories");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.HasOne(x => x.Project).WithMany(p => p.RequirementCategories)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ProjectId, x.SortOrder });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ProjectRequirementConfig : IEntityTypeConfiguration<ProjectRequirement>
{
    public void Configure(EntityTypeBuilder<ProjectRequirement> b)
    {
        b.ToTable("project_requirements");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.HasOne(x => x.Project).WithMany(p => p.Requirements)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Category).WithMany(c => c.Requirements)
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ProjectDocumentAssignmentConfig : IEntityTypeConfiguration<ProjectDocumentAssignment>
{
    public void Configure(EntityTypeBuilder<ProjectDocumentAssignment> b)
    {
        b.ToTable("project_document_assignments");
        b.HasIndex(x => new { x.ProjectId, x.DocumentId }).IsUnique();
        b.HasOne(x => x.Project).WithMany(p => p.Assignments)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Document).WithMany(d => d.Assignments)
            .HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Requirement).WithMany(r => r.Assignments)
            .HasForeignKey(x => x.RequirementId).OnDelete(DeleteBehavior.SetNull);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class TagConfig : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.ToTable("tags");
        b.Property(x => x.Name).HasMaxLength(60).IsRequired();
        b.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class DocumentTagConfig : IEntityTypeConfiguration<DocumentTag>
{
    public void Configure(EntityTypeBuilder<DocumentTag> b)
    {
        b.ToTable("document_tags");
        b.HasKey(x => new { x.DocumentId, x.TagId });
        b.HasOne(x => x.Document).WithMany(d => d.DocumentTags)
            .HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Tag).WithMany(t => t.DocumentTags)
            .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.HasIndex(x => new { x.UserId, x.IsRead });
    }
}

public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        b.Property(x => x.DetailsJson).HasColumnType("jsonb");
        b.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
    }
}

public class StorageConnectionConfig : IEntityTypeConfiguration<StorageConnection>
{
    public void Configure(EntityTypeBuilder<StorageConnection> b)
    {
        b.ToTable("storage_connections");
        b.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        b.Property(x => x.CredentialsEncrypted).HasColumnType("text");
        b.HasIndex(x => new { x.OrganizationId, x.IsActive });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        b.Property(x => x.Key).HasMaxLength(100).IsRequired();
        b.Property(x => x.Category).HasMaxLength(60).IsRequired();
        b.Property(x => x.Description).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Key).IsUnique();
    }
}

public class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        b.Property(x => x.PermissionKey).HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.Role, x.PermissionKey }).IsUnique();
    }
}

public class UserProjectConfig : IEntityTypeConfiguration<UserProject>
{
    public void Configure(EntityTypeBuilder<UserProject> b)
    {
        b.ToTable("user_projects");
        b.HasIndex(x => new { x.UserId, x.ProjectId }).IsUnique();
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
