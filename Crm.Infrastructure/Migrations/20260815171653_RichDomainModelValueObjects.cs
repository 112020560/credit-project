using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RichDomainModelValueObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "customer_addresses_customer_id_fkey",
                table: "customer_addresses");

            migrationBuilder.DropForeignKey(
                name: "customer_emails_customer_id_fkey",
                table: "customer_emails");

            migrationBuilder.DropForeignKey(
                name: "customer_fiscal_info_customer_id_fkey",
                table: "customer_fiscal_info");

            migrationBuilder.DropForeignKey(
                name: "customer_phones_customer_id_fkey",
                table: "customer_phones");

            migrationBuilder.DropForeignKey(
                name: "customer_work_info_customer_id_fkey",
                table: "customer_work_info");

            migrationBuilder.DropForeignKey(
                name: "prospect_addresses_prospect_id_fkey",
                table: "prospect_addresses");

            migrationBuilder.DropForeignKey(
                name: "prospect_emails_prospect_id_fkey",
                table: "prospect_emails");

            migrationBuilder.DropForeignKey(
                name: "prospect_fiscal_info_prospect_id_fkey",
                table: "prospect_fiscal_info");

            migrationBuilder.DropForeignKey(
                name: "prospect_phones_prospect_id_fkey",
                table: "prospect_phones");

            migrationBuilder.DropForeignKey(
                name: "prospect_work_info_prospect_id_fkey",
                table: "prospect_work_info");

            migrationBuilder.DropPrimaryKey(
                name: "prospect_work_info_pkey",
                table: "prospect_work_info");

            migrationBuilder.DropPrimaryKey(
                name: "prospect_phones_pkey",
                table: "prospect_phones");

            migrationBuilder.DropIndex(
                name: "ux_prospect_phones_prospect_number",
                table: "prospect_phones");

            migrationBuilder.DropPrimaryKey(
                name: "prospect_fiscal_info_pkey",
                table: "prospect_fiscal_info");

            migrationBuilder.DropPrimaryKey(
                name: "prospect_emails_pkey",
                table: "prospect_emails");

            migrationBuilder.DropIndex(
                name: "ux_prospect_emails_prospect_email",
                table: "prospect_emails");

            migrationBuilder.DropPrimaryKey(
                name: "prospect_addresses_pkey",
                table: "prospect_addresses");

            migrationBuilder.DropPrimaryKey(
                name: "customer_work_info_pkey",
                table: "customer_work_info");

            migrationBuilder.DropPrimaryKey(
                name: "customer_phones_pkey",
                table: "customer_phones");

            migrationBuilder.DropIndex(
                name: "ux_customer_phones_customer_number",
                table: "customer_phones");

            migrationBuilder.DropPrimaryKey(
                name: "customer_fiscal_info_pkey",
                table: "customer_fiscal_info");

            migrationBuilder.DropPrimaryKey(
                name: "customer_emails_pkey",
                table: "customer_emails");

            migrationBuilder.DropIndex(
                name: "ux_customer_emails_customer_email",
                table: "customer_emails");

            migrationBuilder.DropPrimaryKey(
                name: "customer_addresses_pkey",
                table: "customer_addresses");

            migrationBuilder.RenameIndex(
                name: "ix_prospect_work_info_prospect",
                table: "prospect_work_info",
                newName: "IX_prospect_work_info_prospect_id");

            migrationBuilder.RenameIndex(
                name: "ix_prospect_fiscal_info_prospect",
                table: "prospect_fiscal_info",
                newName: "IX_prospect_fiscal_info_prospect_id");

            migrationBuilder.RenameIndex(
                name: "ix_prospect_addresses_prospect",
                table: "prospect_addresses",
                newName: "IX_prospect_addresses_prospect_id");

            migrationBuilder.RenameIndex(
                name: "ix_customer_fiscal_customer",
                table: "customer_fiscal_info",
                newName: "IX_customer_fiscal_info_customer_id");

            migrationBuilder.RenameIndex(
                name: "ix_customer_addresses_customer",
                table: "customer_addresses",
                newName: "IX_customer_addresses_customer_id");

            migrationBuilder.AddColumn<string>(
                name: "metadata",
                table: "prospect_work_info",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_address",
                table: "prospect_work_info",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "verified",
                table: "prospect_phones",
                type: "boolean",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "number",
                table: "prospect_phones",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_primary",
                table: "prospect_phones",
                type: "boolean",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "metadata",
                table: "prospect_fiscal_info",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "verified",
                table: "prospect_emails",
                type: "boolean",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "is_primary",
                table: "prospect_emails",
                type: "boolean",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "prospect_emails",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_primary",
                table: "prospect_addresses",
                type: "boolean",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "district",
                table: "prospect_addresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata",
                table: "prospect_addresses",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "customer_addresses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_prospect_work_info",
                table: "prospect_work_info",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_prospect_phones",
                table: "prospect_phones",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_prospect_fiscal_info",
                table: "prospect_fiscal_info",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_prospect_emails",
                table: "prospect_emails",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_prospect_addresses",
                table: "prospect_addresses",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customer_work_info",
                table: "customer_work_info",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customer_phones",
                table: "customer_phones",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customer_fiscal_info",
                table: "customer_fiscal_info",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customer_emails",
                table: "customer_emails",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customer_addresses",
                table: "customer_addresses",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_prospect_phones_prospect_id",
                table: "prospect_phones",
                column: "prospect_id");

            migrationBuilder.CreateIndex(
                name: "IX_prospect_emails_prospect_id",
                table: "prospect_emails",
                column: "prospect_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_phones_customer_id",
                table: "customer_phones",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_emails_customer_id",
                table: "customer_emails",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_addresses_customers_customer_id",
                table: "customer_addresses",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_emails_customers_customer_id",
                table: "customer_emails",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_fiscal_info_customers_customer_id",
                table: "customer_fiscal_info",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_phones_customers_customer_id",
                table: "customer_phones",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_work_info_customers_customer_id",
                table: "customer_work_info",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prospect_addresses_prospects_prospect_id",
                table: "prospect_addresses",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prospect_emails_prospects_prospect_id",
                table: "prospect_emails",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prospect_fiscal_info_prospects_prospect_id",
                table: "prospect_fiscal_info",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prospect_phones_prospects_prospect_id",
                table: "prospect_phones",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prospect_work_info_prospects_prospect_id",
                table: "prospect_work_info",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customer_addresses_customers_customer_id",
                table: "customer_addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_customer_emails_customers_customer_id",
                table: "customer_emails");

            migrationBuilder.DropForeignKey(
                name: "FK_customer_fiscal_info_customers_customer_id",
                table: "customer_fiscal_info");

            migrationBuilder.DropForeignKey(
                name: "FK_customer_phones_customers_customer_id",
                table: "customer_phones");

            migrationBuilder.DropForeignKey(
                name: "FK_customer_work_info_customers_customer_id",
                table: "customer_work_info");

            migrationBuilder.DropForeignKey(
                name: "FK_prospect_addresses_prospects_prospect_id",
                table: "prospect_addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_prospect_emails_prospects_prospect_id",
                table: "prospect_emails");

            migrationBuilder.DropForeignKey(
                name: "FK_prospect_fiscal_info_prospects_prospect_id",
                table: "prospect_fiscal_info");

            migrationBuilder.DropForeignKey(
                name: "FK_prospect_phones_prospects_prospect_id",
                table: "prospect_phones");

            migrationBuilder.DropForeignKey(
                name: "FK_prospect_work_info_prospects_prospect_id",
                table: "prospect_work_info");

            migrationBuilder.DropPrimaryKey(
                name: "PK_prospect_work_info",
                table: "prospect_work_info");

            migrationBuilder.DropPrimaryKey(
                name: "PK_prospect_phones",
                table: "prospect_phones");

            migrationBuilder.DropIndex(
                name: "IX_prospect_phones_prospect_id",
                table: "prospect_phones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_prospect_fiscal_info",
                table: "prospect_fiscal_info");

            migrationBuilder.DropPrimaryKey(
                name: "PK_prospect_emails",
                table: "prospect_emails");

            migrationBuilder.DropIndex(
                name: "IX_prospect_emails_prospect_id",
                table: "prospect_emails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_prospect_addresses",
                table: "prospect_addresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customer_work_info",
                table: "customer_work_info");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customer_phones",
                table: "customer_phones");

            migrationBuilder.DropIndex(
                name: "IX_customer_phones_customer_id",
                table: "customer_phones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customer_fiscal_info",
                table: "customer_fiscal_info");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customer_emails",
                table: "customer_emails");

            migrationBuilder.DropIndex(
                name: "IX_customer_emails_customer_id",
                table: "customer_emails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customer_addresses",
                table: "customer_addresses");

            migrationBuilder.DropColumn(
                name: "metadata",
                table: "prospect_work_info");

            migrationBuilder.DropColumn(
                name: "work_address",
                table: "prospect_work_info");

            migrationBuilder.DropColumn(
                name: "metadata",
                table: "prospect_fiscal_info");

            migrationBuilder.DropColumn(
                name: "district",
                table: "prospect_addresses");

            migrationBuilder.DropColumn(
                name: "metadata",
                table: "prospect_addresses");

            migrationBuilder.RenameIndex(
                name: "IX_prospect_work_info_prospect_id",
                table: "prospect_work_info",
                newName: "ix_prospect_work_info_prospect");

            migrationBuilder.RenameIndex(
                name: "IX_prospect_fiscal_info_prospect_id",
                table: "prospect_fiscal_info",
                newName: "ix_prospect_fiscal_info_prospect");

            migrationBuilder.RenameIndex(
                name: "IX_prospect_addresses_prospect_id",
                table: "prospect_addresses",
                newName: "ix_prospect_addresses_prospect");

            migrationBuilder.RenameIndex(
                name: "IX_customer_fiscal_info_customer_id",
                table: "customer_fiscal_info",
                newName: "ix_customer_fiscal_customer");

            migrationBuilder.RenameIndex(
                name: "IX_customer_addresses_customer_id",
                table: "customer_addresses",
                newName: "ix_customer_addresses_customer");

            migrationBuilder.AlterColumn<bool>(
                name: "verified",
                table: "prospect_phones",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "number",
                table: "prospect_phones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_primary",
                table: "prospect_phones",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "verified",
                table: "prospect_emails",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "is_primary",
                table: "prospect_emails",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "prospect_emails",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_primary",
                table: "prospect_addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "customer_addresses",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "prospect_work_info_pkey",
                table: "prospect_work_info",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "prospect_phones_pkey",
                table: "prospect_phones",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "prospect_fiscal_info_pkey",
                table: "prospect_fiscal_info",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "prospect_emails_pkey",
                table: "prospect_emails",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "prospect_addresses_pkey",
                table: "prospect_addresses",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "customer_work_info_pkey",
                table: "customer_work_info",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "customer_phones_pkey",
                table: "customer_phones",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "customer_fiscal_info_pkey",
                table: "customer_fiscal_info",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "customer_emails_pkey",
                table: "customer_emails",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "customer_addresses_pkey",
                table: "customer_addresses",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ux_prospect_phones_prospect_number",
                table: "prospect_phones",
                columns: new[] { "prospect_id", "number" });

            migrationBuilder.CreateIndex(
                name: "ux_prospect_emails_prospect_email",
                table: "prospect_emails",
                columns: new[] { "prospect_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ux_customer_phones_customer_number",
                table: "customer_phones",
                columns: new[] { "customer_id", "number" });

            migrationBuilder.CreateIndex(
                name: "ux_customer_emails_customer_email",
                table: "customer_emails",
                columns: new[] { "customer_id", "email" });

            migrationBuilder.AddForeignKey(
                name: "customer_addresses_customer_id_fkey",
                table: "customer_addresses",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "customer_emails_customer_id_fkey",
                table: "customer_emails",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "customer_fiscal_info_customer_id_fkey",
                table: "customer_fiscal_info",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "customer_phones_customer_id_fkey",
                table: "customer_phones",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "customer_work_info_customer_id_fkey",
                table: "customer_work_info",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "prospect_addresses_prospect_id_fkey",
                table: "prospect_addresses",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "prospect_emails_prospect_id_fkey",
                table: "prospect_emails",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "prospect_fiscal_info_prospect_id_fkey",
                table: "prospect_fiscal_info",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "prospect_phones_prospect_id_fkey",
                table: "prospect_phones",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "prospect_work_info_prospect_id_fkey",
                table: "prospect_work_info",
                column: "prospect_id",
                principalTable: "prospects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
