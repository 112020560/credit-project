namespace SharedKernel.Contracts.Inventory;

public interface IStockReservationReleased
{
    Guid ReservationId { get; }
    Guid SalesOrderId { get; }
    DateTimeOffset ReleasedAt { get; }
}
