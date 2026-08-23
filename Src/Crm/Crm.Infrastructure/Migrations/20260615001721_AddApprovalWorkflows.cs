using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "workflow_definition_id",
                table: "credit_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "approval_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    credit_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workflow_step_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision = table.Column<string>(type: "text", nullable: false),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    decided_by = table.Column<string>(type: "text", nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("approval_decisions_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'Draft'::text"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("workflow_definitions_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_name = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    required_role = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("workflow_steps_pkey", x => x.id);
                    table.ForeignKey(
                        name: "workflow_steps_definition_id_fkey",
                        column: x => x.workflow_definition_id,
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_approval_decisions_application",
                table: "approval_decisions",
                column: "credit_application_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_definitions_status",
                table: "workflow_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_steps_definition",
                table: "workflow_steps",
                column: "workflow_definition_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_decisions");

            migrationBuilder.DropTable(
                name: "workflow_steps");

            migrationBuilder.DropTable(
                name: "workflow_definitions");

            migrationBuilder.DropColumn(
                name: "workflow_definition_id",
                table: "credit_applications");
        }
    }
}
