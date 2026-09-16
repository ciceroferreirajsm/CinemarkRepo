using Microsoft.Extensions.DependencyInjection;
using NotificationConsumer.Application.Notifications;

namespace NotificationConsumer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationConsumerApplication(this IServiceCollection services)
    {
        services.AddSingleton<INotificationProcessor, NotificationProcessor>();
        return services;
    }
}
