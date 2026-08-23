using SharedKernel;

namespace Crm.Domain.Documents;

public static class DocumentError
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Document.NotFound", $"Document with Id '{id}' was not found");

    public static Error InvalidDocumentType(string code) =>
        Error.Problem("Document.InvalidDocumentType", $"Document type '{code}' is not recognized");

    public static Error InvalidTransition(string current) =>
        Error.Problem("Document.InvalidTransition", $"Document in '{current}' status cannot be validated");

    public static readonly Error RejectionReasonRequired =
        Error.Problem("Document.RejectionReasonRequired", "A rejection reason is required when rejecting a document");
}
