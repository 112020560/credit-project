using Crm.Domain.ApprovalWorkflows;
using Crm.Domain.CreditApplications;
using Crm.Domain.Customers;
using Crm.Domain.Documents;
using Crm.Domain.Prospects;
using Crm.Domain.RiskEngine;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework;

public partial class CrmDbContext : DbContext
{
    public CrmDbContext()
    {
    }

    public CrmDbContext(DbContextOptions<CrmDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerAddress> CustomerAddresses { get; set; }

    public virtual DbSet<CustomerDocument> CustomerDocuments { get; set; }

    public virtual DbSet<CustomerEmail> CustomerEmails { get; set; }

    public virtual DbSet<CustomerFiscalInfo> CustomerFiscalInfos { get; set; }

    public virtual DbSet<CustomerPhone> CustomerPhones { get; set; }

    public virtual DbSet<CustomerWorkInfo> CustomerWorkInfos { get; set; }

    public virtual DbSet<CustomersRef> CustomersRefs { get; set; }

    // Prospects
    public virtual DbSet<Prospect> Prospects { get; set; }
    public virtual DbSet<ProspectAddress> ProspectAddresses { get; set; }
    public virtual DbSet<ProspectPhone> ProspectPhones { get; set; }
    public virtual DbSet<ProspectEmail> ProspectEmails { get; set; }
    public virtual DbSet<ProspectWorkInfo> ProspectWorkInfos { get; set; }
    public virtual DbSet<ProspectFiscalInfo> ProspectFiscalInfos { get; set; }

    // Credit Applications
    public virtual DbSet<CreditApplication> CreditApplications { get; set; }
    public virtual DbSet<ApplicationDocument> ApplicationDocuments { get; set; }

    // Approval Workflows
    public virtual DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
    public virtual DbSet<WorkflowStep> WorkflowSteps { get; set; }
    public virtual DbSet<ApprovalDecision> ApprovalDecisions { get; set; }

    // Documents
    public virtual DbSet<Document> Documents { get; set; }
    public virtual DbSet<DocumentValidation> DocumentValidations { get; set; }
    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    // Risk Engine
    public virtual DbSet<RiskRule> RiskRules { get; set; }
    public virtual DbSet<RiskMatrix> RiskMatrices { get; set; }
    public virtual DbSet<RiskMatrixRule> RiskMatrixRules { get; set; }
    public virtual DbSet<RiskEvaluation> RiskEvaluations { get; set; }
    public virtual DbSet<ScoreCardEntry> ScoreCardEntries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Connection string is provided via DI (AddDbContext in DependencyInjection.cs).
        // This block is only needed for EF design-time tools (dotnet ef migrations).
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql("Name=DefaultConnection");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customers_pkey");

            entity.ToTable("customers");

            entity.HasIndex(e => e.FullName, "ix_customers_full_name");

            entity.HasIndex(e => e.IdentificationNumber, "ix_customers_identification");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.ExternalCode).HasColumnName("external_code");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.IdentificationNumber).HasColumnName("identification_number");
            entity.Property(e => e.IdentificationType).HasColumnName("identification_type");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'ACTIVE'::text")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_addresses_pkey");

            entity.ToTable("customer_addresses");

            entity.HasIndex(e => e.CustomerId, "ix_customer_addresses_customer");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.Country).HasColumnName("country");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.District).HasColumnName("district");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.PostalCode).HasColumnName("postal_code");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Street).HasColumnName("street");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerAddresses)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("customer_addresses_customer_id_fkey");
        });

        modelBuilder.Entity<CustomerDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_documents_pkey");

            entity.ToTable("customer_documents");

            entity.HasIndex(e => e.CustomerId, "ix_customer_documents_customer");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.StorageUrl).HasColumnName("storage_url");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("uploaded_at");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerDocuments)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("customer_documents_customer_id_fkey");
        });

        modelBuilder.Entity<CustomerEmail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_emails_pkey");

            entity.ToTable("customer_emails");

            entity.HasIndex(e => new { e.CustomerId, e.Email }, "ux_customer_emails_customer_email");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");
            entity.Property(e => e.Verified)
                .HasDefaultValue(false)
                .HasColumnName("verified");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerEmails)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("customer_emails_customer_id_fkey");
        });

        modelBuilder.Entity<CustomerFiscalInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_fiscal_info_pkey");

            entity.ToTable("customer_fiscal_info");

            entity.HasIndex(e => e.CustomerId, "ix_customer_fiscal_customer");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.EconomicActivity).HasColumnName("economic_activity");
            entity.Property(e => e.Industry).HasColumnName("industry");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.TaxId).HasColumnName("tax_id");
            entity.Property(e => e.TaxRegime).HasColumnName("tax_regime");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerFiscalInfos)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("customer_fiscal_info_customer_id_fkey");
        });

        modelBuilder.Entity<CustomerPhone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_phones_pkey");

            entity.ToTable("customer_phones");

            entity.HasIndex(e => new { e.CustomerId, e.Number }, "ux_customer_phones_customer_number");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CountryCode).HasColumnName("country_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");
            entity.Property(e => e.Number).HasColumnName("number");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Verified)
                .HasDefaultValue(false)
                .HasColumnName("verified");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerPhones)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("customer_phones_customer_id_fkey");
        });

        modelBuilder.Entity<CustomerWorkInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_work_info_pkey");

            entity.ToTable("customer_work_info");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.EmployerName).HasColumnName("employer_name");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.Occupation).HasColumnName("occupation");
            entity.Property(e => e.Salary)
                .HasPrecision(18, 2)
                .HasColumnName("salary");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkAddress)
                .HasColumnType("jsonb")
                .HasColumnName("work_address");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerWorkInfos)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("customer_work_info_customer_id_fkey");
        });

        modelBuilder.Entity<CustomersRef>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customers_ref_pkey");

            entity.ToTable("customers_ref");

            entity.HasIndex(e => e.ExternalId, "customers_ref_external_id_key").IsUnique();

            entity.HasIndex(e => e.ExternalId, "ix_customers_ref_external");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.LegalName).HasColumnName("legal_name");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.RiskScore)
                .HasPrecision(6, 2)
                .HasColumnName("risk_score");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Version)
                .HasDefaultValue(1)
                .HasColumnName("version");
        });

        modelBuilder.Entity<Prospect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("prospects_pkey");
            entity.ToTable("prospects");
            entity.HasIndex(e => new { e.IdentificationType, e.IdentificationNumber }, "ux_prospects_identification").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.IdentificationType).HasColumnName("identification_type");
            entity.Property(e => e.IdentificationNumber).HasColumnName("identification_number");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.Status).HasDefaultValueSql("'Draft'::text").HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        });

        modelBuilder.Entity<ProspectAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("prospect_addresses_pkey");
            entity.ToTable("prospect_addresses");
            entity.HasIndex(e => e.ProspectId, "ix_prospect_addresses_prospect");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.ProspectId).HasColumnName("prospect_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Street).HasColumnName("street");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Country).HasColumnName("country");
            entity.Property(e => e.PostalCode).HasColumnName("postal_code");
            entity.Property(e => e.IsPrimary).HasDefaultValue(false).HasColumnName("is_primary");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.HasOne(d => d.Prospect).WithMany(p => p.Addresses).HasForeignKey(d => d.ProspectId).HasConstraintName("prospect_addresses_prospect_id_fkey");
        });

        modelBuilder.Entity<ProspectPhone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("prospect_phones_pkey");
            entity.ToTable("prospect_phones");
            entity.HasIndex(e => new { e.ProspectId, e.Number }, "ux_prospect_phones_prospect_number");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.ProspectId).HasColumnName("prospect_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Number).HasColumnName("number");
            entity.Property(e => e.CountryCode).HasColumnName("country_code");
            entity.Property(e => e.IsPrimary).HasDefaultValue(false).HasColumnName("is_primary");
            entity.Property(e => e.Verified).HasDefaultValue(false).HasColumnName("verified");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(d => d.Prospect).WithMany(p => p.Phones).HasForeignKey(d => d.ProspectId).HasConstraintName("prospect_phones_prospect_id_fkey");
        });

        modelBuilder.Entity<ProspectEmail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("prospect_emails_pkey");
            entity.ToTable("prospect_emails");
            entity.HasIndex(e => new { e.ProspectId, e.Email }, "ux_prospect_emails_prospect_email");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.ProspectId).HasColumnName("prospect_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.IsPrimary).HasDefaultValue(false).HasColumnName("is_primary");
            entity.Property(e => e.Verified).HasDefaultValue(false).HasColumnName("verified");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(d => d.Prospect).WithMany(p => p.Emails).HasForeignKey(d => d.ProspectId).HasConstraintName("prospect_emails_prospect_id_fkey");
        });

        modelBuilder.Entity<ProspectWorkInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("prospect_work_info_pkey");
            entity.ToTable("prospect_work_info");
            entity.HasIndex(e => e.ProspectId, "ix_prospect_work_info_prospect");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.ProspectId).HasColumnName("prospect_id");
            entity.Property(e => e.Occupation).HasColumnName("occupation");
            entity.Property(e => e.EmployerName).HasColumnName("employer_name");
            entity.Property(e => e.Salary).HasPrecision(18, 2).HasColumnName("salary");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.HasOne(d => d.Prospect).WithMany(p => p.WorkInfos).HasForeignKey(d => d.ProspectId).HasConstraintName("prospect_work_info_prospect_id_fkey");
        });

        modelBuilder.Entity<ProspectFiscalInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("prospect_fiscal_info_pkey");
            entity.ToTable("prospect_fiscal_info");
            entity.HasIndex(e => e.ProspectId, "ix_prospect_fiscal_info_prospect");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.ProspectId).HasColumnName("prospect_id");
            entity.Property(e => e.TaxId).HasColumnName("tax_id");
            entity.Property(e => e.TaxRegime).HasColumnName("tax_regime");
            entity.Property(e => e.EconomicActivity).HasColumnName("economic_activity");
            entity.Property(e => e.Industry).HasColumnName("industry");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.HasOne(d => d.Prospect).WithMany(p => p.FiscalInfos).HasForeignKey(d => d.ProspectId).HasConstraintName("prospect_fiscal_info_prospect_id_fkey");
        });

        modelBuilder.Entity<CreditApplication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("credit_applications_pkey");
            entity.ToTable("credit_applications");
            entity.HasIndex(e => e.ProspectId, "ix_credit_applications_prospect");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.ProspectId).HasColumnName("prospect_id");
            entity.Property(e => e.Status).HasDefaultValueSql("'Draft'::text").HasColumnName("status");
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            entity.Property(e => e.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        });

        modelBuilder.Entity<ApplicationDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("application_documents_pkey");
            entity.ToTable("application_documents");
            entity.HasIndex(e => e.CreditApplicationId, "ix_application_documents_application");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.CreditApplicationId).HasColumnName("credit_application_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.StorageUrl).HasColumnName("storage_url");
            entity.Property(e => e.Status).HasDefaultValueSql("'Uploaded'::text").HasColumnName("status");
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("now()").HasColumnName("uploaded_at");
            entity.HasOne(d => d.CreditApplication).WithMany(p => p.Documents).HasForeignKey(d => d.CreditApplicationId).HasConstraintName("application_documents_credit_application_id_fkey");
        });

        modelBuilder.Entity<RiskRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("risk_rules_pkey");
            entity.ToTable("risk_rules");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.RuleType).HasColumnName("rule_type");
            entity.Property(e => e.TargetField).HasColumnName("target_field");
            entity.Property(e => e.Parameters).HasColumnType("jsonb").HasColumnName("parameters");
            entity.Property(e => e.Weight).HasPrecision(10, 4).HasColumnName("weight");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        });

        modelBuilder.Entity<RiskMatrix>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("risk_matrices_pkey");
            entity.ToTable("risk_matrices");
            entity.HasIndex(e => e.Status, "ix_risk_matrices_status");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Version).HasDefaultValue(1).HasColumnName("version");
            entity.Property(e => e.Status).HasDefaultValueSql("'Draft'::text").HasColumnName("status");
            entity.Property(e => e.AutoApproveThreshold).HasPrecision(10, 4).HasColumnName("auto_approve_threshold");
            entity.Property(e => e.AutoRejectThreshold).HasPrecision(10, 4).HasColumnName("auto_reject_threshold");
            entity.Property(e => e.PricingBands).HasColumnType("jsonb").HasColumnName("pricing_bands");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        });

        modelBuilder.Entity<RiskMatrixRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("risk_matrix_rules_pkey");
            entity.ToTable("risk_matrix_rules");
            entity.HasIndex(e => e.RiskMatrixId, "ix_risk_matrix_rules_matrix");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.RiskMatrixId).HasColumnName("risk_matrix_id");
            entity.Property(e => e.RiskRuleId).HasColumnName("risk_rule_id");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.HasOne(d => d.RiskMatrix).WithMany(p => p.MatrixRules)
                .HasForeignKey(d => d.RiskMatrixId).HasConstraintName("risk_matrix_rules_matrix_id_fkey");
            entity.HasOne(d => d.RiskRule).WithMany()
                .HasForeignKey(d => d.RiskRuleId).HasConstraintName("risk_matrix_rules_rule_id_fkey");
        });

        modelBuilder.Entity<RiskEvaluation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("risk_evaluations_pkey");
            entity.ToTable("risk_evaluations");
            entity.HasIndex(e => e.CreditApplicationId, "ix_risk_evaluations_application");
            entity.HasIndex(e => e.RiskMatrixId, "ix_risk_evaluations_matrix");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.CreditApplicationId).HasColumnName("credit_application_id");
            entity.Property(e => e.RiskMatrixId).HasColumnName("risk_matrix_id");
            entity.Property(e => e.RiskMatrixVersion).HasColumnName("risk_matrix_version");
            entity.Property(e => e.TotalScore).HasPrecision(10, 4).HasColumnName("total_score");
            entity.Property(e => e.Outcome).HasColumnName("outcome");
            entity.Property(e => e.SuggestedInterestRate).HasPrecision(8, 4).HasColumnName("suggested_interest_rate");
            entity.Property(e => e.SuggestedMaxAmount).HasPrecision(18, 2).HasColumnName("suggested_max_amount");
            entity.Property(e => e.EvaluatedAt).HasDefaultValueSql("now()").HasColumnName("evaluated_at");
        });

        modelBuilder.Entity<ScoreCardEntry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("score_card_entries_pkey");
            entity.ToTable("score_card_entries");
            entity.HasIndex(e => e.RiskEvaluationId, "ix_score_card_entries_evaluation");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.RiskEvaluationId).HasColumnName("risk_evaluation_id");
            entity.Property(e => e.RuleId).HasColumnName("rule_id");
            entity.Property(e => e.RuleName).HasColumnName("rule_name");
            entity.Property(e => e.TargetField).HasColumnName("target_field");
            entity.Property(e => e.ObservedValue).HasColumnName("observed_value");
            entity.Property(e => e.Passed).HasColumnName("passed");
            entity.Property(e => e.WeightedContribution).HasPrecision(10, 4).HasColumnName("weighted_contribution");
            entity.HasOne(d => d.RiskEvaluation).WithMany(p => p.Entries)
                .HasForeignKey(d => d.RiskEvaluationId).HasConstraintName("score_card_entries_evaluation_id_fkey");
        });

        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("workflow_definitions_pkey");
            entity.ToTable("workflow_definitions");
            entity.HasIndex(e => e.Status, "ix_workflow_definitions_status");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Status).HasDefaultValueSql("'Draft'::text").HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        });

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("workflow_steps_pkey");
            entity.ToTable("workflow_steps");
            entity.HasIndex(e => e.WorkflowDefinitionId, "ix_workflow_steps_definition");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            entity.Property(e => e.StepName).HasColumnName("step_name");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.RequiredRole).HasColumnName("required_role");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(d => d.WorkflowDefinition).WithMany(p => p.Steps)
                .HasForeignKey(d => d.WorkflowDefinitionId).HasConstraintName("workflow_steps_definition_id_fkey");
        });

        modelBuilder.Entity<ApprovalDecision>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("approval_decisions_pkey");
            entity.ToTable("approval_decisions");
            entity.HasIndex(e => e.CreditApplicationId, "ix_approval_decisions_application");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.CreditApplicationId).HasColumnName("credit_application_id");
            entity.Property(e => e.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            entity.Property(e => e.WorkflowStepId).HasColumnName("workflow_step_id");
            entity.Property(e => e.Decision).HasColumnName("decision");
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            entity.Property(e => e.DecidedBy).HasColumnName("decided_by");
            entity.Property(e => e.DecidedAt).HasColumnName("decided_at");
        });

        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("document_types_pkey");
            entity.ToTable("document_types");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsRequired).HasDefaultValue(false).HasColumnName("is_required");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("documents_pkey");
            entity.ToTable("documents");
            entity.HasIndex(e => new { e.OwnerId, e.OwnerType }, "ix_documents_owner");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.OwnerType).HasColumnName("owner_type");
            entity.Property(e => e.DocumentTypeCode).HasColumnName("document_type_code");
            entity.Property(e => e.StorageUrl).HasColumnName("storage_url");
            entity.Property(e => e.Status).HasDefaultValueSql("'Uploaded'::text").HasColumnName("status");
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("now()").HasColumnName("uploaded_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        });

        modelBuilder.Entity<DocumentValidation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("document_validations_pkey");
            entity.ToTable("document_validations");
            entity.HasIndex(e => e.DocumentId, "ix_document_validations_document");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.Decision).HasColumnName("decision");
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.HasOne(d => d.Document).WithMany(p => p.Validations)
                .HasForeignKey(d => d.DocumentId).HasConstraintName("document_validations_document_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
