using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Dropsonic.AspNetCore.Routing.Tests
{
    internal class MvcBuilder : IMvcBuilder
    {
        public MvcBuilder(IServiceCollection services) 
            : this(services, new ApplicationPartManager())
        {
        }

        public MvcBuilder(IServiceCollection services, ApplicationPartManager partManager)
        {
            Services = services;
            PartManager = partManager;
        }

        public IServiceCollection Services { get; }
        public ApplicationPartManager PartManager { get; }
    }
}
