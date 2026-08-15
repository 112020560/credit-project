using CreditSystem.Domain.Rules;

namespace CreditSystem.Application.Commands.CreateContract;

public record CreateContractResponse
{
    public bool Success { get; init; }
    public Guid? ContractId { get; init; }
    public string? Message { get; init; }
    public decimal? ApprovedRate { get; init; }
    public IReadOnlyList<RuleEvaluationResult>? EvaluationResults { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static CreateContractResponse Approved(
        Guid contractId, 
        decimal rate, 
        IReadOnlyList<RuleEvaluationResult> results) => new()
    {
        Success = true,
        ContractId = contractId,
        Message = "Contract created successfully",
        ApprovedRate = rate,
        EvaluationResults = results
    };

    public static CreateContractResponse Rejected(
        IReadOnlyList<RuleEvaluationResult> results) => new()
    {
        Success = false,
        Message = "Contract rejected",
        EvaluationResults = results,
        Errors = results.Where(r => !r.Passed).Select(r => r.Message).ToList()
    };

    public static CreateContractResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}