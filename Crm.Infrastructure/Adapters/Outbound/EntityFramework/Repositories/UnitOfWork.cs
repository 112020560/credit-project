using System;
using Crm.Domain.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

#nullable disable
public class UnitOfWork : IUnitOfWork
{
    private readonly CrmDbContext _dbContext;
    public ICustomersRepository _customersRepository;
    private IProspectsRepository _prospectsRepository;
    private ICreditApplicationsRepository _creditApplicationsRepository;
    private IRiskRulesRepository _riskRulesRepository;
    private IRiskMatrixRepository _riskMatrixRepository;
    private IRiskEvaluationsRepository _riskEvaluationsRepository;
    private IDocumentsRepository _documentsRepository;
    private IDocumentTypesRepository _documentTypesRepository;
    private IWorkflowDefinitionsRepository _workflowDefinitionsRepository;
    private IApprovalDecisionsRepository _approvalDecisionsRepository;
    private IDbContextTransaction _transaction;
    public UnitOfWork(CrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ICustomersRepository CustomersRepository => _customersRepository ??= new CustomersRepository(_dbContext);
    public IProspectsRepository ProspectsRepository => _prospectsRepository ??= new ProspectsRepository(_dbContext);
    public ICreditApplicationsRepository CreditApplicationsRepository => _creditApplicationsRepository ??= new CreditApplicationsRepository(_dbContext);
    public IRiskRulesRepository RiskRulesRepository => _riskRulesRepository ??= new RiskRulesRepository(_dbContext);
    public IRiskMatrixRepository RiskMatrixRepository => _riskMatrixRepository ??= new RiskMatrixRepository(_dbContext);
    public IRiskEvaluationsRepository RiskEvaluationsRepository => _riskEvaluationsRepository ??= new RiskEvaluationsRepository(_dbContext);
    public IDocumentsRepository DocumentsRepository => _documentsRepository ??= new DocumentsRepository(_dbContext);
    public IDocumentTypesRepository DocumentTypesRepository => _documentTypesRepository ??= new DocumentTypesRepository(_dbContext);
    public IWorkflowDefinitionsRepository WorkflowDefinitionsRepository => _workflowDefinitionsRepository ??= new WorkflowDefinitionsRepository(_dbContext);
    public IApprovalDecisionsRepository ApprovalDecisionsRepository => _approvalDecisionsRepository ??= new ApprovalDecisionsRepository(_dbContext);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _dbContext?.Dispose();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.RollbackAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
