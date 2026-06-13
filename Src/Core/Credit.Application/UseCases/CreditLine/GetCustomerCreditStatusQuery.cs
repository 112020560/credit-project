using Credit.Application.Abstractions.Messaging;
using Credit.Application.Abstractions.Persistence;
using SharedKernel;

namespace Credit.Application.UseCases.CreditLine;

// Response model
public sealed record CustomerCreditStatusResponse(
    bool CustomerExists,
    Guid? CreditCustomerId,
    bool HasActiveCreditLine,
    IReadOnlyList<ActiveCreditLineDto> ActiveLines);

public sealed record ActiveCreditLineDto(
    Guid LineId,
    string? ProductName,
    string? ProductCode,
    decimal Principal,
    decimal Outstanding,
    string Currency,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Status,
    int TermMonths);

// Query
public record GetCustomerCreditStatusQuery(Guid ExternalCustomerId)
    : IQuery<CustomerCreditStatusResponse>;

// Handler
internal sealed class GetCustomerCreditStatusQueryHandler(
    ICreditCustomerRepository customerRepository,
    ICreditLineRepository creditLineRepository)
    : IQueryHandler<GetCustomerCreditStatusQuery, CustomerCreditStatusResponse>
{
    public async Task<Result<CustomerCreditStatusResponse>> Handle(
        GetCustomerCreditStatusQuery request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByExternalIdAsync(
            request.ExternalCustomerId, cancellationToken);

        if (customer is null)
            return Result.Success(new CustomerCreditStatusResponse(
                CustomerExists: false,
                CreditCustomerId: null,
                HasActiveCreditLine: false,
                ActiveLines: []));

        var activeLines = await creditLineRepository.GetActiveByCustomerIdAsync(
            customer.Id, cancellationToken);

        var lineDtos = activeLines.Select(l => new ActiveCreditLineDto(
            LineId: l.Id,
            ProductName: l.Product?.Name,
            ProductCode: l.Product?.Code,
            Principal: l.Principal,
            Outstanding: l.Outstanding,
            Currency: l.Currency,
            StartDate: l.StartDate,
            EndDate: l.EndDate,
            Status: l.Status,
            TermMonths: l.Product?.TermMonths ?? 0))
            .ToList();

        return Result.Success(new CustomerCreditStatusResponse(
            CustomerExists: true,
            CreditCustomerId: customer.Id,
            HasActiveCreditLine: lineDtos.Count > 0,
            ActiveLines: lineDtos));
    }
}
