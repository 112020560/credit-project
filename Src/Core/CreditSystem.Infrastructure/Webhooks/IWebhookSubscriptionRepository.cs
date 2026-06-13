// This file is kept for backward compatibility.
// The interface is now defined in CreditSystem.Domain.Abstractions.Persistence.IWebhookSubscriptionRepository
// Infrastructure implementations should use the Domain interface.

global using IWebhookSubscriptionRepository = CreditSystem.Domain.Abstractions.Persistence.IWebhookSubscriptionRepository;
