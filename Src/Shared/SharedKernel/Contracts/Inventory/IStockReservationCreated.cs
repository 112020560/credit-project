namespace SharedKernel.Contracts.Inventory;

public interface IStockReservationCreated
{
    Guid ReservationId { get; }
    string ReservationNumber { get; }
    Guid ProductId { get; }
    Guid WarehouseId { get; }
    decimal ReservedQuantity { get; }
    Guid SalesOrderId { get; }
    DateTimeOffset CreatedAt { get; }
}
