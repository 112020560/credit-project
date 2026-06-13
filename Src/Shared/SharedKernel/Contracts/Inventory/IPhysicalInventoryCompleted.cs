namespace SharedKernel.Contracts.Inventory;

public interface IPhysicalInventoryCompleted
{
    Guid CountId { get; }
    string CountNumber { get; }
    Guid WarehouseId { get; }
    int TotalLines { get; }
    int AdjustedLines { get; }
    DateTimeOffset CompletedAt { get; }
}
