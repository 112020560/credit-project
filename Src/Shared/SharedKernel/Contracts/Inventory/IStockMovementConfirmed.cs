namespace SharedKernel.Contracts.Inventory;

public interface IStockMovementConfirmed
{
    Guid MovementId { get; }
    string MovementNumber { get; }
    int MovementType { get; }
    Guid WarehouseId { get; }
    DateTimeOffset ConfirmedAt { get; }
}
