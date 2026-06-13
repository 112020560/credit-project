namespace CreditSystem.Infrastructure.Projections;

public interface IProjectionStore
{
    Task<long> GetCheckpointAsync(string projectionName, CancellationToken ct = default);
    Task SaveCheckpointAsync(string projectionName, long position, Guid? lastEventId, CancellationToken ct = default);
    
    // Operaciones genéricas
    Task UpsertAsync<T>(string tableName, T model, string keyColumn, CancellationToken ct = default);
    Task DeleteAsync(string tableName, string keyColumn, object keyValue, CancellationToken ct = default);
    Task<T?> GetByIdAsync<T>(string tableName, string keyColumn, object keyValue, CancellationToken ct = default);
    Task ExecuteAsync(string sql, object? parameters = null, CancellationToken ct = default);
}