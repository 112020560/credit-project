using Credit.Application.Abstractions.Messaging;
using Credit.Application.UseCases.Applications.Dtos;

namespace Credit.Application.Commands;

public record RejectCreditApplicationCommand
(
        Guid CreditApplicationId,
        RejectCreditApplication? Body = null
    ) : ICommand;