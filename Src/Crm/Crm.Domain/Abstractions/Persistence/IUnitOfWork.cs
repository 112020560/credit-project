using System;

namespace Crm.Domain.Abstractions.Persistence;

public interface IUnitOfWork: IDisposable
{
    ICustomersRepository CustomersRepository { get; }
    IProspectsRepository ProspectsRepository { get; }
    ICreditApplicationsRepository CreditApplicationsRepository { get; }
    IRiskRulesRepository RiskRulesRepository { get; }
    IRiskMatrixRepository RiskMatrixRepository { get; }
    IRiskEvaluationsRepository RiskEvaluationsRepository { get; }
    IDocumentsRepository DocumentsRepository { get; }
    IDocumentTypesRepository DocumentTypesRepository { get; }
    IWorkflowDefinitionsRepository WorkflowDefinitionsRepository { get; }
    IApprovalDecisionsRepository ApprovalDecisionsRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
