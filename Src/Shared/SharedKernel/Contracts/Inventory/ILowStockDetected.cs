namespace SharedKernel.Contracts.Inventory;

public interface ILowStockDetected
{
    Guid ProductId { get; }
    string Sku { get; }
    string ProductName { get; }
    Guid WarehouseId { get; }
    decimal QuantityOnHand { get; }
    decimal MinimumStock { get; }
    DateTimeOffset DetectedAt { get; }
}
