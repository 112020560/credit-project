using System;
using SharedKernel;

namespace Crm.Domain.Customers;

public static class CustomerError
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Customer.NotFound",
        $"The Customer with the Id = '{userId}' was not found");
}
