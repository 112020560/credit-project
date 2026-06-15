using SharedKernel;

namespace Crm.Domain.CreditApplications;

public static class CreditApplicationError
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("CreditApplication.NotFound", $"Credit application with Id '{id}' was not found");

    public static Error InvalidTransition(string current, string target) =>
        Error.Problem("CreditApplication.InvalidTransition", $"Cannot transition from '{current}' to '{target}'");

    public static Error MissingDocuments(IEnumerable<string> types) =>
        Error.Problem("CreditApplication.MissingDocuments", $"Required documents missing: {string.Join(", ", types)}");

    public static readonly Error AlreadyProcessed =
        Error.Problem("CreditApplication.AlreadyProcessed", "Credit application has already been approved or rejected");
}
