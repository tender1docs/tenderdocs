using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequirementCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "project_requirements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "project_requirement_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_requirement_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_requirement_categories_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_requirements_CategoryId",
                table: "project_requirements",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_project_requirement_categories_ProjectId_SortOrder",
                table: "project_requirement_categories",
                columns: new[] { "ProjectId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_project_requirements_project_requirement_categories_Categor~",
                table: "project_requirements",
                column: "CategoryId",
                principalTable: "project_requirement_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_requirements_project_requirement_categories_Categor~",
                table: "project_requirements");

            migrationBuilder.DropTable(
                name: "project_requirement_categories");

            migrationBuilder.DropIndex(
                name: "IX_project_requirements_CategoryId",
                table: "project_requirements");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "project_requirements");
        }
    }
}
