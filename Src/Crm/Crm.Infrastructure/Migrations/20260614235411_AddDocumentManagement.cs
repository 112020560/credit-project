using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_types",
                columns: table => new
                {
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_types_pkey", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_type = table.Column<string>(type: "text", nullable: false),
                    document_type_code = table.Column<string>(type: "text", nullable: false),
                    storage_url = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'Uploaded'::text"),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("documents_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_validations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "text", nullable: false),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    reviewed_by = table.Column<string>(type: "text", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_validations_pkey", x => x.id);
                    table.ForeignKey(
                        name: "document_validations_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_document_validations_document",
                table: "document_validations",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_documents_owner",
                table: "documents",
                columns: new[] { "owner_id", "owner_type" });

            migrationBuilder.Sql(@"
                INSERT INTO document_types (code, name, description, is_required) VALUES
                ('NationalId',      'National ID',          'Government-issued national identity card',    true),
                ('Passport',        'Passport',             'International travel document',               false),
                ('IncomeProof',     'Income Proof',         'Proof of income (payslip, bank statement)',   true),
                ('BankStatement',   'Bank Statement',       'Recent bank account statement',               true),
                ('TaxRegistration', 'Tax Registration',     'Tax registration certificate',                false),
                ('ProofOfAddress',  'Proof of Address',     'Utility bill or official address document',   true)
                ON CONFLICT (code) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_types");

            migrationBuilder.DropTable(
                name: "document_validations");

            migrationBuilder.DropTable(
                name: "documents");
        }
    }
}
