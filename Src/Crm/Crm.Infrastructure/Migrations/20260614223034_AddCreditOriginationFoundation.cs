using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditOriginationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    prospect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'Draft'::text"),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("credit_applications_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prospects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    identification_type = table.Column<string>(type: "text", nullable: false),
                    identification_number = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'Draft'::text"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("prospects_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "application_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    credit_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    storage_url = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'Uploaded'::text"),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("application_documents_pkey", x => x.id);
                    table.ForeignKey(
                        name: "application_documents_credit_application_id_fkey",
                        column: x => x.credit_application_id,
                        principalTable: "credit_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prospect_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    prospect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
                    street = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    postal_code = table.Column<string>(type: "text", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("prospect_addresses_pkey", x => x.id);
                    table.ForeignKey(
                        name: "prospect_addresses_prospect_id_fkey",
                        column: x => x.prospect_id,
                        principalTable: "prospects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prospect_emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    prospect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("prospect_emails_pkey", x => x.id);
                    table.ForeignKey(
                        name: "prospect_emails_prospect_id_fkey",
                        column: x => x.prospect_id,
                        principalTable: "prospects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prospect_fiscal_info",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    prospect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_id = table.Column<string>(type: "text", nullable: true),
                    tax_regime = table.Column<string>(type: "text", nullable: true),
                    economic_activity = table.Column<string>(type: "text", nullable: true),
                    industry = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("prospect_fiscal_info_pkey", x => x.id);
                    table.ForeignKey(
                        name: "prospect_fiscal_info_prospect_id_fkey",
                        column: x => x.prospect_id,
                        principalTable: "prospects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prospect_phones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    prospect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
                    number = table.Column<string>(type: "text", nullable: true),
                    country_code = table.Column<string>(type: "text", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("prospect_phones_pkey", x => x.id);
                    table.ForeignKey(
                        name: "prospect_phones_prospect_id_fkey",
                        column: x => x.prospect_id,
                        principalTable: "prospects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prospect_work_info",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    prospect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occupation = table.Column<string>(type: "text", nullable: true),
                    employer_name = table.Column<string>(type: "text", nullable: true),
                    salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("prospect_work_info_pkey", x => x.id);
                    table.ForeignKey(
                        name: "prospect_work_info_prospect_id_fkey",
                        column: x => x.prospect_id,
                        principalTable: "prospects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_application_documents_application",
                table: "application_documents",
                column: "credit_application_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_applications_prospect",
                table: "credit_applications",
                column: "prospect_id");

            migrationBuilder.CreateIndex(
                name: "ix_prospect_addresses_prospect",
                table: "prospect_addresses",
                column: "prospect_id");

            migrationBuilder.CreateIndex(
                name: "ux_prospect_emails_prospect_email",
                table: "prospect_emails",
                columns: new[] { "prospect_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_prospect_fiscal_info_prospect",
                table: "prospect_fiscal_info",
                column: "prospect_id");

            migrationBuilder.CreateIndex(
                name: "ux_prospect_phones_prospect_number",
                table: "prospect_phones",
                columns: new[] { "prospect_id", "number" });

            migrationBuilder.CreateIndex(
                name: "ix_prospect_work_info_prospect",
                table: "prospect_work_info",
                column: "prospect_id");

            migrationBuilder.CreateIndex(
                name: "ux_prospects_identification",
                table: "prospects",
                columns: new[] { "identification_type", "identification_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_documents");

            migrationBuilder.DropTable(
                name: "prospect_addresses");

            migrationBuilder.DropTable(
                name: "prospect_emails");

            migrationBuilder.DropTable(
                name: "prospect_fiscal_info");

            migrationBuilder.DropTable(
                name: "prospect_phones");

            migrationBuilder.DropTable(
                name: "prospect_work_info");

            migrationBuilder.DropTable(
                name: "credit_applications");

            migrationBuilder.DropTable(
                name: "prospects");
        }
    }
}
