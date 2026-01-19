using Microsoft.Extensions.DependencyInjection;
using LogiHub.Services.Shared;
using LogiHub.Web.SignalR;

namespace LogiHub.Web
{
    public class Container
    {
        public static void RegisterTypes(IServiceCollection container)
        {
            // Registration of all the database services you have
            container.AddScoped<SharedService>();

            // Registration of SignalR events
            container.AddScoped<IPublishDomainEvents, SignalrPublishDomainEvents>();
        }
    }
}
