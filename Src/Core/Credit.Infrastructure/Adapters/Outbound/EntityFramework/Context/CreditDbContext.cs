using Credit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Credit.Infrastructure.Adapters.Outbound.EntityFramework.Context;

public partial class CreditDbContext : DbContext
{
    public CreditDbContext()
    {
    }

    public CreditDbContext(DbContextOptions<CreditDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CreditApplication> CreditApplications { get; set; }

    public virtual DbSet<CreditLine> CreditLines { get; set; }

    public virtual DbSet<CreditPayment> CreditPayments { get; set; }

    public virtual DbSet<CreditProduct> CreditProducts { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<DomainEvent> DomainEvents { get; set; }

    public virtual DbSet<Installment> Installments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql("Name=DefaultConnection");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<CreditApplication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("credit_applications_pkey");

            entity.ToTable("credit_applications");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.DecisionNotes).HasColumnName("decision_notes");
            entity.Property(e => e.Documents)
                .HasColumnType("jsonb")
                .HasColumnName("documents");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Score)
                .HasColumnType("jsonb")
                .HasColumnName("score");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TermMonths).HasColumnName("term_months");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Customer).WithMany(p => p.CreditApplications)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("credit_applications_customer_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.CreditApplications)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("credit_applications_product_id_fkey");
        });

        modelBuilder.Entity<CreditLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("credit_lines_pkey");

            entity.ToTable("credit_lines");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AmortizationSchedule)
                .HasColumnType("jsonb")
                .HasColumnName("amortization_schedule");
            entity.Property(e => e.ApplicationId).HasColumnName("application_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.Outstanding)
                .HasPrecision(18, 2)
                .HasColumnName("outstanding");
            entity.Property(e => e.Principal)
                .HasPrecision(18, 2)
                .HasColumnName("principal");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Application).WithMany(p => p.CreditLines)
                .HasForeignKey(d => d.ApplicationId)
                .HasConstraintName("credit_lines_application_id_fkey");

            entity.HasOne(d => d.Customer).WithMany(p => p.CreditLines)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("credit_lines_customer_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.CreditLines)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("credit_lines_product_id_fkey");
        });

        modelBuilder.Entity<CreditPayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("credit_payments_pkey");

            entity.ToTable("credit_payments");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreditLineId).HasColumnName("credit_line_id");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.Method).HasColumnName("method");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("paid_at");

            entity.HasOne(d => d.CreditLine).WithMany(p => p.CreditPayments)
                .HasForeignKey(d => d.CreditLineId)
                .HasConstraintName("credit_payments_credit_line_id_fkey");
        });

        modelBuilder.Entity<CreditProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("credit_products_pkey");

            entity.ToTable("credit_products");

            entity.HasIndex(e => e.Code, "credit_products_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AmortizationMethod).HasColumnName("amortization_method");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.InterestRate)
                .HasPrecision(12, 6)
                .HasColumnName("interest_rate");
            entity.Property(e => e.InterestType).HasColumnName("interest_type");
            entity.Property(e => e.MaxAmount)
                .HasPrecision(18, 2)
                .HasColumnName("max_amount");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.MinAmount)
                .HasPrecision(18, 2)
                .HasColumnName("min_amount");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.TermMonths).HasColumnName("term_months");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customers_pkey");

            entity.ToTable("customers");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasColumnType("jsonb")
                .HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.Emails).HasColumnName("emails");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.LegalName).HasColumnName("legal_name");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.Roles).HasColumnName("roles");
            entity.Property(e => e.TaxId).HasColumnName("tax_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<DomainEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("domain_events_pkey");

            entity.ToTable("domain_events");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AggregateId).HasColumnName("aggregate_id");
            entity.Property(e => e.AggregateType).HasColumnName("aggregate_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EventType).HasColumnName("event_type");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.Processed)
                .HasDefaultValue(false)
                .HasColumnName("processed");
        });

        modelBuilder.Entity<Installment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("installments_pkey");

            entity.ToTable("installments");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreditLineId).HasColumnName("credit_line_id");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.FeesDue)
                .HasPrecision(18, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("fees_due");
            entity.Property(e => e.InterestDue)
                .HasPrecision(18, 2)
                .HasColumnName("interest_due");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PrincipalDue)
                .HasPrecision(18, 2)
                .HasColumnName("principal_due");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.CreditLine).WithMany(p => p.Installments)
                .HasForeignKey(d => d.CreditLineId)
                .HasConstraintName("installments_credit_line_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
