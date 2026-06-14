using Crm.Domain.Customers;
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
