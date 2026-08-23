using System;
using System.Collections.Generic;
using Crm.Domain.RiskEngine;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "risk_evaluations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    credit_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    risk_matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    risk_matrix_version = table.Column<int>(type: "integer", nullable: false),
                    total_score = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    suggested_interest_rate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    suggested_max_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    evaluated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("risk_evaluations_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "risk_matrices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    status = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'Draft'::text"),
                    auto_approve_threshold = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    auto_reject_threshold = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    pricing_bands = table.Column<List<PricingBand>>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("risk_matrices_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "risk_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    rule_type = table.Column<string>(type: "text", nullable: false),
                    target_field = table.Column<string>(type: "text", nullable: false),
                    parameters = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("risk_rules_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "score_card_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    risk_evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_name = table.Column<string>(type: "text", nullable: false),
                    target_field = table.Column<string>(type: "text", nullable: false),
                    observed_value = table.Column<string>(type: "text", nullable: true),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    weighted_contribution = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("score_card_entries_pkey", x => x.id);
                    table.ForeignKey(
                        name: "score_card_entries_evaluation_id_fkey",
                        column: x => x.risk_evaluation_id,
                        principalTable: "risk_evaluations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "risk_matrix_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    risk_matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    risk_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("risk_matrix_rules_pkey", x => x.id);
                    table.ForeignKey(
                        name: "risk_matrix_rules_matrix_id_fkey",
                        column: x => x.risk_matrix_id,
                        principalTable: "risk_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "risk_matrix_rules_rule_id_fkey",
                        column: x => x.risk_rule_id,
                        principalTable: "risk_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_risk_evaluations_application",
                table: "risk_evaluations",
                column: "credit_application_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_evaluations_matrix",
                table: "risk_evaluations",
                column: "risk_matrix_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_matrices_status",
                table: "risk_matrices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_risk_matrix_rules_matrix",
                table: "risk_matrix_rules",
                column: "risk_matrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_risk_matrix_rules_risk_rule_id",
                table: "risk_matrix_rules",
                column: "risk_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_card_entries_evaluation",
                table: "score_card_entries",
                column: "risk_evaluation_id");

            // Seed: default active matrix — all scores route to ManualReview (thresholds are unreachable)
            migrationBuilder.Sql("""
                INSERT INTO risk_matrices (id, name, version, status, auto_approve_threshold, auto_reject_threshold, pricing_bands, created_at)
                VALUES (uuid_generate_v4(), 'Default Matrix', 1, 'Active', 9999, -9999, NULL, now());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "risk_matrix_rules");

            migrationBuilder.DropTable(
                name: "score_card_entries");

            migrationBuilder.DropTable(
                name: "risk_matrices");

            migrationBuilder.DropTable(
                name: "risk_rules");

            migrationBuilder.DropTable(
                name: "risk_evaluations");
        }
    }
}
