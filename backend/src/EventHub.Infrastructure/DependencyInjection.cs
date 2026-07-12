using EventHub.Application.Common;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventHubDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("EventHubDb")));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();

        return services;
    }
}
