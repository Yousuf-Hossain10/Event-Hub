using EventHub.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
