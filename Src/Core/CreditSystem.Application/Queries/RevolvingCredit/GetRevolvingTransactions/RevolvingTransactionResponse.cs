using CreditSystem.Domain.Models.ReadModels;

namespace CreditSystem.Application.Queries.RevolvingCredit.GetRevolvingTransactions;

public record RevolvingTransactionResponse
{
    public Guid Id { get; init; }
    public string TransactionType { get; init; } = null!;
    public decimal Amount { get; init; }
    public decimal BalanceAfter { get; init; }
    public string? Description { get; init; }
    public DateTime TransactionDate { get; init; }

    public static RevolvingTransactionResponse FromReadModel(RevolvingTransactionReadModel model) => new()
    {
        Id = model.Id,
        TransactionType = model.TransactionType,
        Amount = model.Amount,
        BalanceAfter = model.BalanceAfter,
        Description = model.Description,
        TransactionDate = model.TransactionDate
    };
}