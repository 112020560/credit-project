using SharedKernel;

namespace Crm.Domain.Prospects;

public static class ProspectError
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Prospect.NotFound", $"Prospect with Id '{id}' was not found");

    public static readonly Error AlreadyConverted =
        Error.Problem("Prospect.AlreadyConverted", "Prospect has already been converted to a customer and cannot be modified");

    public static Error DuplicateIdentification(string identificationNumber) =>
        Error.Conflict("Prospect.DuplicateIdentification", $"A prospect or customer with identification number '{identificationNumber}' already exists");
}
