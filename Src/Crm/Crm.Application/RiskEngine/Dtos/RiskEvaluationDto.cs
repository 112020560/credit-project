namespace Crm.Application.RiskEngine.Dtos;

public record RiskEvaluationDto(
    Guid Id,
    Guid CreditApplicationId,
    Guid RiskMatrixId,
    int RiskMatrixVersion,
    decimal TotalScore,
    string Outcome,
    decimal? SuggestedInterestRate,
    decimal? SuggestedMaxAmount,
    DateTime EvaluatedAt,
    List<ScoreCardEntryDto> Entries);
