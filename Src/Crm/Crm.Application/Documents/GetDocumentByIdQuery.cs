using Crm.Application.Abstractions.Messaging;
using Crm.Application.Documents.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Documents;
using SharedKernel;

namespace Crm.Application.Documents;

public record GetDocumentByIdQuery(Guid DocumentId) : IQuery<DocumentDetailDto>;

internal sealed class GetDocumentByIdQueryHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<GetDocumentByIdQuery, DocumentDetailDto>
{
    public async Task<Result<DocumentDetailDto>> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await unitOfWork.DocumentsRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentDetailDto>(DocumentError.NotFound(request.DocumentId));

        return RegisterDocumentCommandHandler.ToDto(document);
    }
}
