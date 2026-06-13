using System;
using Credit.Application.Commands;
using Credit.Application.Tests;
using Credit.Domain.Aggregates;
using MediatR;

namespace Credit.Application.CommandHandlers;

public sealed class CreateCreditContractHandler
    : IRequestHandler<CreateCreditContract>
{
    private readonly FakeEventStore _store;

    public CreateCreditContractHandler(FakeEventStore store)
    {
        _store = store;
    }
    
    public Task Handle(CreateCreditContract command, CancellationToken ct)
    {
        // En esta fase NO persistimos
        CreditContract.Create(
            command.CreditId,
            command.Currency,
            command.When
        );

        return Task.CompletedTask;
    }
}