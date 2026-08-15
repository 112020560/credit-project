// This file is kept for backward compatibility.
// The interface is now defined in CreditSystem.Domain.Abstractions.Persistence.IWebhookDeliveryRepository
// Infrastructure implementations should use the Domain interface.

global using IWebhookDeliveryRepository = CreditSystem.Domain.Abstractions.Persistence.IWebhookDeliveryRepository;
