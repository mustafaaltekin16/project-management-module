namespace Ozdilek.PM.SharedKernel.Events;

/// <summary>
/// Application-layer abstraction over the message bus. Application services depend on this
/// interface only; Infrastructure provides the real implementation (MassTransit/RabbitMQ), so
/// Application stays free of messaging-library and framework dependencies.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
}
