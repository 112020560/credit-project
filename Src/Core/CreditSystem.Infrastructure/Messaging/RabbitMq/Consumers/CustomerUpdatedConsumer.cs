using Dapper;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel.Contracts.Crm.Customers;

namespace CreditSystem.Infrastructure.Messaging.RabbitMq.Consumers;

public class CustomerUpdatedConsumer: IConsumer<CustomerUpdated>
{
    private readonly string _connectionString;
    private readonly ILogger<CustomerUpdatedConsumer> _logger;

    public CustomerUpdatedConsumer(IConfiguration configuration, ILogger<CustomerUpdatedConsumer> logger)
    {
        _connectionString = configuration.GetConnectionString("CreditDb")!;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<CustomerUpdated> context)
    {
        var message = context.Message;

        await UpdateCustomerAsync(message);
    }

    public async Task UpdateCustomerAsync(CustomerUpdated message)
    {
        const string sql = @"
            UPDATE customer_references SET
                full_name = COALESCE(@FullName, full_name),
                email = COALESCE(@Email, email),
                phone = COALESCE(@Phone, phone),
                updated_at = @UpdatedAt
            WHERE external_id = @ExternalId";

        await using var connection = new NpgsqlConnection(_connectionString);

        var fullName = message.Changes.FirstOrDefault(x => x.Key == "FullName").Value;
        var email = message.Changes.FirstOrDefault(x => x.Key == "Email").Value;
        var phone = message.Changes.FirstOrDefault(x => x.Key == "Phone").Value;
        await connection.ExecuteAsync(sql, new
        {
            ExternalId = message.CustomerId,
            FullName = fullName,
            Email = email,
            Phone = phone,
            UpdatedAt = DateTime.UtcNow
        });

        _logger.LogInformation(
            "Customer reference updated for external ID {ExternalId}", 
            message.CustomerId);
    }
}