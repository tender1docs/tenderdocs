using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderDocs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // organizations
        migrationBuilder.CreateTable(
            name: "organizations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Slug = table.Column<string>(type: "text", nullable: false),
                DemoMode = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_organizations", x => x.Id));

        // users
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: true),
                FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Initials = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false),
                GoogleId = table.Column<string>(type: "text", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.Id);
                table.ForeignKey("FK_users_organizations_OrganizationId", x => x.OrganizationId,
                    "organizations", "Id", onDelete: ReferentialAction.Cascade);
            });

        // refresh_tokens
        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ReplacedByToken = table.Column<string>(type: "text", nullable: true),
                CreatedByIp = table.Column<string>(type: "text", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                table.ForeignKey("FK_refresh_tokens_users_UserId", x => x.UserId,
                    "users", "Id", onDelete: ReferentialAction.Cascade);
            });

        // projects (created before folders due to FK from folders.ProjectId; folders.RootFolder handled via projects.RootFolderId -> folders, so we add that FK after folders)
        migrationBuilder.CreateTable(
            name: "projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                RootFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects", x => x.Id);
                table.ForeignKey("FK_projects_organizations_OrganizationId", x => x.OrganizationId,
                    "organizations", "Id", onDelete: ReferentialAction.Cascade);
            });

        // folders
        migrationBuilder.CreateTable(
            name: "folders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                ParentFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                MaterializedPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                Depth = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_folders", x => x.Id);
                table.ForeignKey("FK_folders_organizations_OrganizationId", x => x.OrganizationId,
                    "organizations", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_folders_folders_ParentFolderId", x => x.ParentFolderId,
                    "folders", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_folders_projects_ProjectId", x => x.ProjectId,
                    "projects", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.AddForeignKey(
            name: "FK_projects_folders_RootFolderId",
            table: "projects", column: "RootFolderId",
            principalTable: "folders", principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        // documents
        migrationBuilder.CreateTable(
            name: "documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                FolderId = table.Column<Guid>(type: "uuid", nullable: true),
                Name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                DocumentType = table.Column<int>(type: "integer", nullable: false),
                IssuingAuthority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                FinancialYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                Notes = table.Column<string>(type: "text", nullable: true),
                IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                StorageProvider = table.Column<int>(type: "integer", nullable: false),
                StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Checksum = table.Column<string>(type: "text", nullable: true),
                UploadedById = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_documents", x => x.Id);
                table.ForeignKey("FK_documents_organizations_OrganizationId", x => x.OrganizationId,
                    "organizations", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_documents_folders_FolderId", x => x.FolderId,
                    "folders", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_documents_users_UploadedById", x => x.UploadedById,
                    "users", "Id", onDelete: ReferentialAction.SetNull);
            });

        // document_versions
        migrationBuilder.CreateTable(
            name: "document_versions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                VersionNumber = table.Column<int>(type: "integer", nullable: false),
                StorageProvider = table.Column<int>(type: "integer", nullable: false),
                StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                ContentType = table.Column<string>(type: "text", nullable: false),
                Checksum = table.Column<string>(type: "text", nullable: true),
                UploadedById = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_versions", x => x.Id);
                table.ForeignKey("FK_document_versions_documents_DocumentId", x => x.DocumentId,
                    "documents", "Id", onDelete: ReferentialAction.Cascade);
            });

        // project_requirements
        migrationBuilder.CreateTable(
            name: "project_requirements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_project_requirements", x => x.Id);
                table.ForeignKey("FK_project_requirements_projects_ProjectId", x => x.ProjectId,
                    "projects", "Id", onDelete: ReferentialAction.Cascade);
            });

        // project_document_assignments
        migrationBuilder.CreateTable(
            name: "project_document_assignments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                RequirementId = table.Column<Guid>(type: "uuid", nullable: true),
                AssignedById = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_project_document_assignments", x => x.Id);
                table.ForeignKey("FK_pda_projects_ProjectId", x => x.ProjectId,
                    "projects", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_pda_documents_DocumentId", x => x.DocumentId,
                    "documents", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_pda_project_requirements_RequirementId", x => x.RequirementId,
                    "project_requirements", "Id", onDelete: ReferentialAction.SetNull);
            });

        // tags
        migrationBuilder.CreateTable(
            name: "tags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                Color = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_tags", x => x.Id));

        // document_tags (join)
        migrationBuilder.CreateTable(
            name: "document_tags",
            columns: table => new
            {
                DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                TagId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_tags", x => new { x.DocumentId, x.TagId });
                table.ForeignKey("FK_document_tags_documents_DocumentId", x => x.DocumentId,
                    "documents", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_document_tags_tags_TagId", x => x.TagId,
                    "tags", "Id", onDelete: ReferentialAction.Cascade);
            });

        // notifications
        migrationBuilder.CreateTable(
            name: "notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Message = table.Column<string>(type: "text", nullable: false),
                RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                RelatedEntityType = table.Column<string>(type: "text", nullable: true),
                IsRead = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_notifications", x => x.Id));

        // audit_logs
        migrationBuilder.CreateTable(
            name: "audit_logs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<int>(type: "integer", nullable: false),
                EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                DetailsJson = table.Column<string>(type: "jsonb", nullable: true),
                IpAddress = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_audit_logs", x => x.Id));

        // storage_connections
        migrationBuilder.CreateTable(
            name: "storage_connections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                ProviderType = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CredentialsEncrypted = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_storage_connections", x => x.Id);
                table.ForeignKey("FK_storage_connections_organizations_OrganizationId", x => x.OrganizationId,
                    "organizations", "Id", onDelete: ReferentialAction.Cascade);
            });

        // ---- indexes ----
        migrationBuilder.CreateIndex("IX_organizations_Slug", "organizations", "Slug", unique: true);

        migrationBuilder.CreateIndex("IX_users_Email", "users", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_users_GoogleId", "users", "GoogleId");
        migrationBuilder.CreateIndex("IX_users_OrganizationId", "users", "OrganizationId");

        migrationBuilder.CreateIndex("IX_refresh_tokens_Token", "refresh_tokens", "Token", unique: true);
        migrationBuilder.CreateIndex("IX_refresh_tokens_UserId", "refresh_tokens", "UserId");

        migrationBuilder.CreateIndex("IX_projects_OrganizationId_Name", "projects",
            new[] { "OrganizationId", "Name" });
        migrationBuilder.CreateIndex("IX_projects_RootFolderId", "projects", "RootFolderId");

        migrationBuilder.CreateIndex("IX_folders_MaterializedPath", "folders", "MaterializedPath");
        migrationBuilder.CreateIndex("IX_folders_OrganizationId_ParentFolderId", "folders",
            new[] { "OrganizationId", "ParentFolderId" });
        migrationBuilder.CreateIndex("IX_folders_ParentFolderId", "folders", "ParentFolderId");
        migrationBuilder.CreateIndex("IX_folders_ProjectId", "folders", "ProjectId");

        migrationBuilder.CreateIndex("IX_documents_OrganizationId_DocumentType", "documents",
            new[] { "OrganizationId", "DocumentType" });
        migrationBuilder.CreateIndex("IX_documents_OrganizationId_ExpiryDate", "documents",
            new[] { "OrganizationId", "ExpiryDate" });
        migrationBuilder.CreateIndex("IX_documents_FolderId", "documents", "FolderId");
        migrationBuilder.CreateIndex("IX_documents_UploadedById", "documents", "UploadedById");

        migrationBuilder.CreateIndex("IX_document_versions_DocumentId_VersionNumber",
            "document_versions", new[] { "DocumentId", "VersionNumber" }, unique: true);

        migrationBuilder.CreateIndex("IX_project_requirements_ProjectId", "project_requirements", "ProjectId");

        migrationBuilder.CreateIndex("IX_pda_ProjectId_DocumentId", "project_document_assignments",
            new[] { "ProjectId", "DocumentId" }, unique: true);
        migrationBuilder.CreateIndex("IX_pda_DocumentId", "project_document_assignments", "DocumentId");
        migrationBuilder.CreateIndex("IX_pda_RequirementId", "project_document_assignments", "RequirementId");

        migrationBuilder.CreateIndex("IX_tags_OrganizationId_Name", "tags",
            new[] { "OrganizationId", "Name" }, unique: true);

        migrationBuilder.CreateIndex("IX_document_tags_TagId", "document_tags", "TagId");

        migrationBuilder.CreateIndex("IX_notifications_UserId_IsRead", "notifications",
            new[] { "UserId", "IsRead" });

        migrationBuilder.CreateIndex("IX_audit_logs_OrganizationId_CreatedAt", "audit_logs",
            new[] { "OrganizationId", "CreatedAt" });

        migrationBuilder.CreateIndex("IX_storage_connections_OrganizationId_IsActive", "storage_connections",
            new[] { "OrganizationId", "IsActive" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_folders_projects_ProjectId", "folders");
        migrationBuilder.DropTable("audit_logs");
        migrationBuilder.DropTable("document_tags");
        migrationBuilder.DropTable("document_versions");
        migrationBuilder.DropTable("notifications");
        migrationBuilder.DropTable("project_document_assignments");
        migrationBuilder.DropTable("refresh_tokens");
        migrationBuilder.DropTable("storage_connections");
        migrationBuilder.DropTable("tags");
        migrationBuilder.DropTable("project_requirements");
        migrationBuilder.DropTable("documents");
        migrationBuilder.DropTable("projects");
        migrationBuilder.DropTable("folders");
        migrationBuilder.DropTable("users");
        migrationBuilder.DropTable("organizations");
    }
}
