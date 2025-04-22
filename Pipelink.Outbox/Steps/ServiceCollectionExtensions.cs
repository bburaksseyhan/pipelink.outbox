using Microsoft.Extensions.DependencyInjection;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;
using Pipelink.Outbox.Pipeline;
using System;

namespace Pipelink.Outbox.Steps;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the outbox publisher services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for OutboxPublisherOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOutboxPublisher(this IServiceCollection services, Action<OutboxPublisherOptions>? configure = null)
    {
        var options = new OutboxPublisherOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        return services;
    }
} 