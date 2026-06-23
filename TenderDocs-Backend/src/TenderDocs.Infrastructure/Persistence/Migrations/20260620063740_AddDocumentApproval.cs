using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovalAt",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_ApprovedById",
                table: "documents",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_documents_OrganizationId_ApprovalStatus",
                table: "documents",
                columns: new[] { "OrganizationId", "ApprovalStatus" });

            migrationBuilder.AddForeignKey(
                name: "FK_documents_users_ApprovedById",
                table: "documents",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_users_ApprovedById",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_ApprovedById",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_OrganizationId_ApprovalStatus",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ApprovalAt",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "documents");
        }
    }
}
