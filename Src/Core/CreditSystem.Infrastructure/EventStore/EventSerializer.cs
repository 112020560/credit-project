using System.Text.Json;
using System.Text.Json.Serialization;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;
using CreditSystem.Infrastructure.EventStore.Models;

namespace CreditSystem.Infrastructure.EventStore;

public class JsonEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _options;
    private readonly Dictionary<string, Type> _eventTypes;

    public JsonEventSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true, 
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(),
                // new MoneyJsonConverter(),
                // new InterestRateJsonConverter()
            }
        };

        // Registrar tipos de eventos
        _eventTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsAbstract)
            .ToDictionary(t => t.Name, t => t);
    }

    public string Serialize(IDomainEvent @event)
    {
        return JsonSerializer.Serialize(@event, @event.GetType(), _options);
    }

    public IDomainEvent Deserialize(string eventType, string data)
    {
        if (!_eventTypes.TryGetValue(eventType, out var type))
            throw new InvalidOperationException($"Unknown event type: {eventType}");

        var @event = JsonSerializer.Deserialize(data, type, _options);
        
        return @event as IDomainEvent 
            ?? throw new InvalidOperationException($"Failed to deserialize event: {eventType}");
    }

    public string SerializeMetadata(EventMetadata? metadata)
    {
        return metadata == null ? "{}" : JsonSerializer.Serialize(metadata, _options);
    }

    public EventMetadata? DeserializeMetadata(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return null;
        
        return JsonSerializer.Deserialize<EventMetadata>(data, _options);
    }

    public string SerializeState<TState>(TState state)
    {
        return JsonSerializer.Serialize(state, _options);
    }

    public TState? DeserializeState<TState>(string data)
    {
        return JsonSerializer.Deserialize<TState>(data, _options);
    }
}

public class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var amount = doc.RootElement.GetProperty("amount").GetDecimal();
        var currency = doc.RootElement.GetProperty("currency").GetString() ?? "USD";
        return new Money(amount, currency);
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}

public class InterestRateJsonConverter : JsonConverter<InterestRate>
{
    public override InterestRate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var rate = reader.GetDecimal();
        return new InterestRate(rate);
    }

    public override void Write(Utf8JsonWriter writer, InterestRate value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.AnnualRate);
    }
}