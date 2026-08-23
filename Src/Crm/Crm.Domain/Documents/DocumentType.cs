namespace Crm.Domain.Documents;

public class DocumentType
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
}
