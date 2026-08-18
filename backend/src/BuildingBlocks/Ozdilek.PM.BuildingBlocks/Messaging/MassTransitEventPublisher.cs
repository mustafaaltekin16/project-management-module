using MassTransit;
using Ozdilek.PM.SharedKernel.Events;

namespace Ozdilek.PM.BuildingBlocks.Messaging;

/// <summary>Infrastructure-side implementation of <see cref="IEventPublisher"/> backed by MassTransit/RabbitMQ.</summary>
public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class =>
        publishEndpoint.Publish(@event, ct);
}
